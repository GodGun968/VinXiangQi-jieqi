using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace VinXiangQi
{
    // 通用检测结果：普通象棋模型（Yolov5Net）与揭棋模型（jieqi.onnx）共用
    public class PieceDetection
    {
        public string Name;
        public float Score;
        public RectangleF Rectangle;
    }

    // 揭棋局面识别器。
    // jieqi.onnx 是 YOLO11 输出格式 ([1, 38, 8400]，34 类 + 4 bbox)，不能走 YoloScorer/YoloXiangQiModel 的 YOLOv5 解码路径，
    // 因此独立用 InferenceSession 实现，解码逻辑与已通过端到端验证的 scratch/ortx86/JieqiX86Test.cs 一致。
    // 模型要求：opset15 / IR8（旧 x86 ONNX Runtime 最高支持 IR8）。Models\jieqi.onnx 已是降级版；
    // 原始 IR9 版备份在 Models\jieqi.onnx.ir9.bak。
    public class JieqiDetector : IDisposable
    {
        const float CONF_THRESHOLD = 0.5f;
        const float NMS_THRESHOLD = 0.45f;

        // jieqi.onnx 34 类 → 本程序内部棋子名；null 表示忽略该类（数字类 2/3/4/5）
        static readonly string[] ClassMap = new string[]
        {
            null, null, null, null,                                                 // 0-3:  2 3 4 5
            "board",                                                                // 4:    Board
            "b_shi", "b_pao", "b_che", "b_xiang", "b_jiang", "b_ma", "b_bing",      // 5-11: b_*
            "dark",                                                                 // 12:   裸 dark（按所在半区着色）
            "b_anzi", "b_anzi", "b_anzi", "b_anzi", "b_anzi", "b_anzi", "b_anzi",   // 13-19: dark_b_*
            "r_anzi", "r_anzi", "r_anzi", "r_anzi", "r_anzi", "r_anzi", "r_anzi",   // 20-26: dark_r_*
            "r_shi", "r_pao", "r_che", "r_xiang", "r_jiang", "r_ma", "r_bing"       // 27-33: r_*
        };

        public readonly string ModelPath;
        InferenceSession session = null;
        string inputName = "images";

        public JieqiDetector(string modelPath)
        {
            ModelPath = modelPath;
            session = new InferenceSession(modelPath);
            bool found = false;
            foreach (KeyValuePair<string, NodeMetadata> kv in session.InputMetadata)
            {
                if (kv.Key == "images") { found = true; }
            }
            if (!found)
            {
                foreach (KeyValuePair<string, NodeMetadata> kv in session.InputMetadata)
                {
                    inputName = kv.Key;
                    break;
                }
            }
        }

        public List<PieceDetection> Detect(Bitmap image)
        {
            List<PieceDetection> result = new List<PieceDetection>();
            if (session == null) return result;
            int W = image.Width, H = image.Height;
            if (W <= 0 || H <= 0) return result;
            float scale = Math.Min(640f / W, 640f / H);
            int nw = (int)Math.Round((double)(W * scale));
            int nh = (int)Math.Round((double)(H * scale));
            int padX = (640 - nw) / 2;
            int padY = (640 - nh) / 2;
            if (nw < 1) nw = 1;
            if (nh < 1) nh = 1;

            Bitmap canvas = new Bitmap(640, 640, PixelFormat.Format24bppRgb);
            try
            {
                using (Graphics g = Graphics.FromImage(canvas))
                {
                    g.Clear(Color.FromArgb(114, 114, 114));
                    g.InterpolationMode = InterpolationMode.Bilinear;
                    g.PixelOffsetMode = PixelOffsetMode.Half;
                    g.DrawImage(image, new Rectangle(padX, padY, nw, nh));
                }

                DenseTensor<float> tensor = new DenseTensor<float>(new int[] { 1, 3, 640, 640 });
                BitmapData data = canvas.LockBits(new Rectangle(0, 0, 640, 640), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                try
                {
                    int stride = data.Stride;
                    byte[] row = new byte[stride];
                    long baseAddr = data.Scan0.ToInt64();
                    for (int y = 0; y < 640; y++)
                    {
                        Marshal.Copy(new IntPtr(baseAddr + (long)y * stride), row, 0, stride);
                        for (int x = 0; x < 640; x++)
                        {
                            int o = x * 3;
                            tensor[0, 0, y, x] = row[o + 2] / 255f;
                            tensor[0, 1, y, x] = row[o + 1] / 255f;
                            tensor[0, 2, y, x] = row[o] / 255f;
                        }
                    }
                }
                finally
                {
                    canvas.UnlockBits(data);
                }

                List<NamedOnnxValue> inputs = new List<NamedOnnxValue>();
                inputs.Add(NamedOnnxValue.CreateFromTensor(inputName, tensor));

                using (IDisposableReadOnlyCollection<DisposableNamedOnnxValue> outputs = session.Run(inputs))
                {
                    DenseTensor<float> outt = null;
                    foreach (DisposableNamedOnnxValue r in outputs)
                    {
                        DenseTensor<float> dt = r.Value as DenseTensor<float>;
                        if (dt != null)
                        {
                            if (outt == null || r.Name == "output0") outt = dt;
                        }
                    }
                    if (outt == null) return result;
                    int dims1 = outt.Dimensions[1];
                    int dims2 = outt.Dimensions[2];

                    List<float> scores = new List<float>();
                    List<string> names = new List<string>();
                    List<float[]> boxes = new List<float[]>();
                    for (int c = 0; c < dims2; c++)
                    {
                        int bestCls = -1;
                        float bestSc = -1f;
                        for (int j = 4; j < dims1; j++)
                        {
                            float v = outt[0, j, c];
                            if (v > bestSc)
                            {
                                bestSc = v;
                                bestCls = j - 4;
                            }
                        }
                        if (bestSc < CONF_THRESHOLD) continue;
                        if (bestCls < 0 || bestCls >= ClassMap.Length) continue;
                        string name = ClassMap[bestCls];
                        if (name == null) continue;
                        float cx = outt[0, 0, c], cy = outt[0, 1, c], bw = outt[0, 2, c], bh = outt[0, 3, c];
                        float x1 = (cx - bw / 2f - padX) / scale;
                        float y1 = (cy - bh / 2f - padY) / scale;
                        float x2 = (cx + bw / 2f - padX) / scale;
                        float y2 = (cy + bh / 2f - padY) / scale;
                        scores.Add(bestSc);
                        names.Add(name);
                        boxes.Add(new float[] { x1, y1, x2, y2 });
                    }

                    // NMS（与 JieqiX86Test 相同的实现，按置信度降序）
                    int[] order = new int[scores.Count];
                    for (int i = 0; i < order.Length; i++) order[i] = i;
                    Array.Sort(order, delegate (int a, int b) { return scores[b].CompareTo(scores[a]); });
                    bool[] removed = new bool[scores.Count];
                    for (int i = 0; i < order.Length; i++)
                    {
                        int a = order[i];
                        if (removed[a]) continue;
                        float[] boxA = boxes[a];
                        result.Add(new PieceDetection()
                        {
                            Name = names[a],
                            Score = scores[a],
                            Rectangle = new RectangleF(boxA[0], boxA[1], boxA[2] - boxA[0], boxA[3] - boxA[1])
                        });
                        for (int j = i + 1; j < order.Length; j++)
                        {
                            int b = order[j];
                            if (removed[b]) continue;
                            if (Iou(boxA, boxes[b]) > NMS_THRESHOLD) removed[b] = true;
                        }
                    }
                }
            }
            finally
            {
                canvas.Dispose();
            }
            return result;
        }

        static float Iou(float[] a, float[] b)
        {
            float x1 = Math.Max(a[0], b[0]), y1 = Math.Max(a[1], b[1]);
            float x2 = Math.Min(a[2], b[2]), y2 = Math.Min(a[3], b[3]);
            float iw = Math.Max(0f, x2 - x1), ih = Math.Max(0f, y2 - y1);
            float inter = iw * ih;
            float ua = (a[2] - a[0]) * (a[3] - a[1]) + (b[2] - b[0]) * (b[3] - b[1]) - inter;
            if (ua <= 0f) return 0f;
            return inter / ua;
        }

        public void Dispose()
        {
            if (session != null)
            {
                session.Dispose();
                session = null;
            }
        }
    }
}
