using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using Yolov5Net;
using Yolov5Net.Scorer;
using Yolov5Net.Scorer.Models;
using System.Drawing.Drawing2D;
using System.Threading;
using System.IO;
using Newtonsoft.Json;


namespace VinXiangQi
{
    public partial class Mainform : Form
    {
        public Mainform()
        {
            InitializeComponent();
            MouseHelper.mainform = this;
            // 首帧显示后再次归一化（此时所有容器已完成最终布局）；并约束趋势条宽度
            this.Shown += delegate { NormalizeLayout(); ClampTrendPanelWidth(); };
            // 识别画面容器尺寸变化时同步约束趋势条宽度（窗口缩放时不让趋势条挤占画面）
            this.groupBox_picturebox.SizeChanged += delegate { ClampTrendPanelWidth(); };
        }

        // UI 初始化期间置 true：避免程序化赋值触发控件联动的副作用（如启动时误关揭棋模式）
        bool settingsUILoading = false;

        // 各选项悬停说明（ToolTip）
        readonly ToolTip toolTip_main = new ToolTip();

        // 趋势曲线：评分/胜率数据（横轴=出招次数；每局开始时清空重绘）
        readonly List<float> trendScores = new List<float>();
        readonly object trendLock = new object();   // 引擎线程写 / UI 线程读，需加锁
        float pendingTrendScore = 0f;
        // 仅在“落子被检测确认”后绘制：出招时先暂存评分
        float trendStashValue = 0f;
        bool trendStashValid = false;

        // 布局调整模式（菜单“布局调整”）：拖动分组框移动 / 边缘缩放，调整后自动保存
        readonly LayoutEditor layoutEditor = new LayoutEditor();
        readonly List<GroupBox> layoutBoxes = new List<GroupBox>();
        readonly Dictionary<string, Rectangle> defaultLayoutBounds = new Dictionary<string, Rectangle>();
        bool layoutInited = false;

        // 程序版本
        public static string Version = "1.4.0";

        // 用于截图的窗口句柄
        public static IntPtr GameHandle = IntPtr.Zero;
        // 用于点击的窗口句柄
        public static IntPtr ClickHandle = IntPtr.Zero;
        // R42：后台点击被棋软忽略（ok=True 但棋子未选中）时的自动降级——重试路径置 ForceForegroundClickOnce，
        // 本局后续点击改走前台（一次性标志 + 本局保持标志；新局/切换鼠标模式时复位）。
        public static bool ForceForegroundClickOnce = false;
        static bool _autoDegradedToForeground = false;
        // 第二十五轮：上一次成功取到的游戏窗口标题（句柄失效时按其自动重取）
        static string _lastGameWindowTitle = "";
        // 棋盘识别模型（象棋模型：小模型/中模型/万能带旋转等）
        public static Dictionary<string, YoloScorer<YoloXiangQiModel>> ModelList = new Dictionary<string, YoloScorer<YoloXiangQiModel>>();
        public static YoloScorer<YoloXiangQiModel> Model;
        // 揭棋识别器（揭棋模型：jieqi.onnx）
        public static JieqiDetector JieqiModel = null;
        // 揭棋模型清单（第十六轮）：与象棋模型的 ModelList 对称，供「交叉验证用揭棋模型」下拉列举。
        // 两者严格分开：揭棋模型只进这里，象棋模型只进 ModelList，两个下拉不混用。
        public static List<string> JieqiModelNames = new List<string>();
        // 当前 JieqiModel 实际加载的揭棋模型名（不含扩展名）
        public static string ActiveJieqiModelName = "";
        // 揭棋检测器更换锁：Detect（检测线程）与换模型（UI 线程）互斥，
        // 防止旧实例在推理进行中被 Dispose（ONNX session 释放后 Run 会抛 ObjectDisposedException）。
        public static readonly object JieqiModelLock = new object();
        // UCI引擎封装
        public static EngineHelper Engine;
        // 开局库
        public static Dictionary<string, OpenBookHelper.OpenBook> OpenBookList = new Dictionary<string, OpenBookHelper.OpenBook>();
        // 用于储存方案供选择
        public static Dictionary<string, Solution> SolutionList = new Dictionary<string, Solution>();
        // 当前选择的方案
        public static Solution CurrentSolution;
        // 用于存放每个棋盘上的点对应图片上点击坐标
        public static Point[,] ClickPositionMap = new Point[9, 10];
        // 当GameHandle和ClickHandle不是同一个时，获取他们尺寸的插值作为图片上坐标和点击坐标转换的偏移量
        public static Size ClickOffset = new Size(0, 0);
        // 最近一次抓屏的坐标空间（点击换算依赖；由 ScreenshotOnce 在成功抓屏时更新）
        public static WinCoord.CaptureSpace LastCaptureSpace = WinCoord.CaptureSpace.FrontWindowPhysical;
        // 用于截图和识别的主线程
        Thread DetectionThread;
        // 用于自动点击的线程
        Thread AutoClickThread;
        // 用于在原始截图上画出检测框和分类
        Bitmap YoloDisplayBitmap;
        Graphics YoloGDI;
        // 用于绘制示意棋盘
        Bitmap BoardDisplayBitmap = new Bitmap(400, 440);
        Graphics BoardGDI;
        // 用于在示意棋盘上显示箭头
        ChessMove BestMove = new ChessMove(new Point(-1, -1), new Point(-1, -1));
        ChessMove PonderMove = new ChessMove(new Point(-1, -1), new Point(-1, -1));
        ChessMove BackgroundAnalysisMove = new ChessMove(new Point(-1, -1), new Point(-1, -1));
        // Flag 用于在程序退出时示意其他子线程退出
        public static bool Running = true;
        // 用于判断引擎当前是否存在别的分析任务，是否需要给引擎发送stop，是否需要跳过发送stop产生的bestmove
        public static int EngineAnalyzeCount = 0;
        // 全局设置
        public static ProgramSettings Settings = new ProgramSettings();
        public static string SettingsPath = @".\settings.json";
        public static string MODEL_FOLDER = @".\Models\";
        public static string OPENBOOK_FOLDER = @".\OpenBooks\";
        public static string SOLUTION_FOLDER = @".\Solutions\";
        // 是否开始连线
        public static bool DetectEnabled = false;
        // 自动点击的图片
        public static List<Bitmap> AutoClickImageList = new List<Bitmap>();
        public static Random Rand = new Random();
        // 时间设置用：本局已走的己方着法数（「前 N 回合使用步时」）与本局开始时刻（「局时 N 分钟后使用步时」）
        public static int SelfMoveCount = 0;
        public static DateTime GameStartTime = DateTime.MinValue;

        public class Solution
        {
            public string ScreenshotCaption;
            public string ScreenshotClass;
            public string ClickCaption;
            public string ClickClass;
            public Solution(string ScreenshotCaption, string ScreenshotClass, string ClickCaption, string ClickClass)
            {
                this.ScreenshotCaption = ScreenshotCaption;
                this.ScreenshotClass = ScreenshotClass;
                this.ClickCaption = ClickCaption;
                this.ClickClass = ClickClass;
            }
        }

        public struct ChessMove
        {
            public ChessMove(Point from, Point to)
            {
                this.From = from;
                this.To = to;
            }
            public Point From;
            public Point To;
        }

        private void Mainform_Load(object sender, EventArgs e)
        {
            try
            {
                StatisticsHelper.Statistics();
                this.Text = $"Vin象棋 {Version}";
                InitFolders();
                LoadSettings();
                SanitizeSettings();
                InitOpenBooks();
                InitYoloModels();
                InitSolutions();
                InitSettingsUI();
                InitToolTips();
                InitThreads();
                InitResultDisplay();
                InitLayoutEditor();
                ApplySavedLayout();
                InitEngine();
                NormalizeLayout();
                ClampTrendPanelWidth();
                RenderTrend();
                LayoutTrendPanel();
            }
            catch (Exception ex)
            {
                settingsUILoading = false;
                MessageBox.Show(ex.ToString());
            }
        }

        public static void LoadSettings()
        {
            if (File.Exists(SettingsPath))
            {
                try
                {
                    Settings = JsonConvert.DeserializeObject<ProgramSettings>(File.ReadAllText(SettingsPath));
                    if (Settings == null) Settings = new ProgramSettings();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("settings.json 解析失败，使用默认设置: " + ex.Message);
                    Settings = new ProgramSettings();
                }
            }
        }

        public static void SaveSettings()
        {
            try
            {
                string tmp = SettingsPath + ".tmp";
                File.WriteAllText(tmp, JsonConvert.SerializeObject(Settings));
                File.Copy(tmp, SettingsPath, true);
                File.Delete(tmp);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("settings.json 保存失败: " + ex.Message);
                try { File.WriteAllText(SettingsPath, JsonConvert.SerializeObject(Settings)); } catch { }
            }
        }

        // 设置值清洗：越界值钳制、空字典补默认（旧版/损坏的 settings.json 也能安全打开）
        public static void SanitizeSettings()
        {
            if (Settings == null) Settings = new ProgramSettings();
            if (Settings.LayoutPositions == null) Settings.LayoutPositions = new Dictionary<string, string>();
            if (Settings.LayoutSplits == null) Settings.LayoutSplits = new Dictionary<string, string>();
            if (Settings.EngineList == null) Settings.EngineList = new Dictionary<string, EngineSettings>();
            if (Settings.DetectionInterval < 100) Settings.DetectionInterval = 100;
            if (Settings.DetectionInterval > 10000) Settings.DetectionInterval = 10000;
            if (Settings.DetectionConfirmCount < 1) Settings.DetectionConfirmCount = 1;
            if (Settings.DetectionConfirmCount > 20) Settings.DetectionConfirmCount = 20;
            if (Settings.ScreenshotRetryCount < 0) Settings.ScreenshotRetryCount = 0;
            if (Settings.ScreenshotRetryCount > 5) Settings.ScreenshotRetryCount = 5;
            if (Settings.ClickInterval < 50) Settings.ClickInterval = 50;
            if (Settings.ClickInterval > 2000) Settings.ClickInterval = 2000;
            if (Settings.ScaleFactor < 0.5f) Settings.ScaleFactor = 0.5f;
            if (Settings.ScaleFactor > 4f) Settings.ScaleFactor = 4f;
            if (Settings.ThreadCount < 1) Settings.ThreadCount = 1;
            if (Settings.ThreadCount > 128) Settings.ThreadCount = 128;
            if (Settings.JieqiConfThreshold < 0.05f || Settings.JieqiConfThreshold > 0.95f) Settings.JieqiConfThreshold = 0.5f;
            if (Settings.JieqiNmsThreshold < 0.10f || Settings.JieqiNmsThreshold > 0.90f) Settings.JieqiNmsThreshold = 0.45f;
            if (Settings.ScoreTrendMode != 0 && Settings.ScoreTrendMode != 1) Settings.ScoreTrendMode = 0;
            if (Settings.ClickHoldMs < 10) Settings.ClickHoldMs = 10;
            if (Settings.ClickHoldMs > 500) Settings.ClickHoldMs = 500;
            if (Settings.ClickPreWaitMs < 0) Settings.ClickPreWaitMs = 0;
            if (Settings.ClickPreWaitMs > 1000) Settings.ClickPreWaitMs = 1000;
            if (Settings.ClickRetryCount < 0) Settings.ClickRetryCount = 0;
            if (Settings.ClickRetryCount > 5) Settings.ClickRetryCount = 5;
            if (Settings.BackgroundClickStrategy < 0) Settings.BackgroundClickStrategy = 0;
            if (Settings.BackgroundClickStrategy > 2) Settings.BackgroundClickStrategy = 2;
            if (Settings.InvalidResetCount < 2) Settings.InvalidResetCount = 2;
            if (Settings.InvalidResetCount > 20) Settings.InvalidResetCount = 20;
            if (Settings.EngineStepTime < 0.1) Settings.EngineStepTime = 0.1;
            if (Settings.EngineStepTime > 9999) Settings.EngineStepTime = 9999;
            if (Settings.EngineDepth < 1) Settings.EngineDepth = 1;
            if (Settings.EngineDepth > 200) Settings.EngineDepth = 200;
            // 第十三轮：时间设置字段钳制（旧 settings.json 缺字段取默认，越界回钳）
            if (Settings.FixedStepSeconds < 0.1) Settings.FixedStepSeconds = 0.1;
            if (Settings.FixedStepSeconds > 9999) Settings.FixedStepSeconds = 9999;
            if (Settings.TimeModeDepthValue < 1) Settings.TimeModeDepthValue = 1;
            if (Settings.TimeModeDepthValue > 200) Settings.TimeModeDepthValue = 200;
            if (Settings.OpeningStepMoves < 1) Settings.OpeningStepMoves = 1;
            if (Settings.OpeningStepMoves > 999) Settings.OpeningStepMoves = 999;
            if (Settings.OpeningStepSeconds < 0.1) Settings.OpeningStepSeconds = 0.1;
            if (Settings.OpeningStepSeconds > 9999) Settings.OpeningStepSeconds = 9999;
            if (Settings.GameTimeMinutes < 1) Settings.GameTimeMinutes = 1;
            if (Settings.GameTimeMinutes > 999) Settings.GameTimeMinutes = 999;
            if (Settings.GameTimeStepSeconds < 0.1) Settings.GameTimeStepSeconds = 0.1;
            if (Settings.GameTimeStepSeconds > 9999) Settings.GameTimeStepSeconds = 9999;
            if (Settings.EngineMoveGapMin < 0) Settings.EngineMoveGapMin = 0;
            if (Settings.EngineMoveGapMin > 600) Settings.EngineMoveGapMin = 600;
            if (Settings.EngineMoveGapMax < 0) Settings.EngineMoveGapMax = 0;
            if (Settings.EngineMoveGapMax > 600) Settings.EngineMoveGapMax = 600;
            if (Settings.EngineMoveGapMin > Settings.EngineMoveGapMax) Settings.EngineMoveGapMin = Settings.EngineMoveGapMax;
            if (Settings.BookMoveGapMin < 0) Settings.BookMoveGapMin = 0;
            if (Settings.BookMoveGapMin > 600) Settings.BookMoveGapMin = 600;
            if (Settings.BookMoveGapMax < 0) Settings.BookMoveGapMax = 0;
            if (Settings.BookMoveGapMax > 600) Settings.BookMoveGapMax = 600;
            if (Settings.BookMoveGapMin > Settings.BookMoveGapMax) Settings.BookMoveGapMin = Settings.BookMoveGapMax;
            ApplyDetectionParamsToDetector();
        }

        // 把设置里的检测参数推送给揭棋检测器（模型未加载时为空操作）
        public static void ApplyDetectionParamsToDetector()
        {
            JieqiDetector d = JieqiModel;
            if (d == null) return;
            float conf = Settings.JieqiConfThreshold;
            if (conf < 0.05f || conf > 0.95f) conf = 0.5f;
            float nms = Settings.JieqiNmsThreshold;
            if (nms < 0.10f || nms > 0.90f) nms = 0.45f;
            d.ConfThreshold = conf;
            d.NmsThreshold = nms;
        }

        // ---- 识别绘制：裁定模型配色（第十六轮）----
        // 颜色只表达「该格最终由哪个模型裁定」，不表达棋子红黑方（否则一个框两种语义）。
        public static Color GetDrawChessColor()
        {
            try { return Color.FromArgb(Settings.DrawChessColorArgb); }
            catch { return Color.Red; }
        }

        public static Color GetDrawJieqiColor()
        {
            try { return Color.FromArgb(Settings.DrawJieqiColorArgb); }
            catch { return Color.Blue; }
        }

        // 两模型结果一致（高度可信不验证）时的绘制颜色：绿色
        public static Color GetDrawAgreeColor()
        {
            try { return Color.FromArgb(Settings.DrawAgreeColorArgb); }
            catch { return Color.FromArgb(0, 170, 0); }
        }

        // 按裁定来源取绘制颜色：一致→绿，象棋模型→红，揭棋模型→蓝，无裁定→中性灰
        public static Color GetAuthorityColor(Utils.DetectionAuthority a)
        {
            switch (a)
            {
                case Utils.DetectionAuthority.BothAgree: return GetDrawAgreeColor();
                case Utils.DetectionAuthority.ChessModel: return GetDrawChessColor();
                case Utils.DetectionAuthority.JieqiModel: return GetDrawJieqiColor();
                default: return Color.Gray;
            }
        }

        // 框线粗细（1~8，越界夹取）
        public static int GetDrawThickness()
        {
            int t = Settings.DrawBoxThickness;
            if (t < 1) t = 1;
            if (t > 8) t = 8;
            return t;
        }


        void InitThreads()
        {
            DetectionThread = new Thread(new ThreadStart(DetectionLoop));
            DetectionThread.IsBackground = true;   // R22：关窗后不再驻留
            DetectionThread.Start();
            AutoClickThread = new Thread(new ThreadStart(AutoClickLoop));
            AutoClickThread.IsBackground = true;   // R22：关窗后不再驻留
            AutoClickThread.Start();
        }

        void InitFolders()
        {
            if (!Directory.Exists(MODEL_FOLDER))
            {
                Directory.CreateDirectory(MODEL_FOLDER);
                Thread.Sleep(200);
            }
            if (!Directory.Exists(SOLUTION_FOLDER))
            {
                Directory.CreateDirectory(SOLUTION_FOLDER);
                Thread.Sleep(200);
            }
            if (!Directory.Exists(OPENBOOK_FOLDER))
            {
                Directory.CreateDirectory(OPENBOOK_FOLDER);
            }
        }

        public static void InitOpenBooks()
        {
            foreach (var book in OpenBookList)
            {
                try
                {
                    book.Value.Dispose();
                }
                catch (Exception ex) { Debug.WriteLine(ex.ToString()); }
            }
            OpenBookList.Clear();
            foreach (var file in Directory.GetFiles(OPENBOOK_FOLDER))
            {
                if (file.Split('.').Last() == "obk")
                {
                    try
                    {
                        string[] parts = file.Split('\\').Last().Split('.');
                        string name = string.Join(".", parts.Take(parts.Length - 1));
                        var book = new OpenBookHelper.OpenBook(file);
                        OpenBookList.Add(name, book);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("加载开局库 " + file + " 失败：" + file + "\n" + ex.ToString());
                    }
                }
            }
        }

        void InitYoloModels()
        {
            foreach (string file in Directory.GetFiles("./Models"))
            {
                if (file.EndsWith(".onnx"))
                {
                    string modelName = file.Split('\\').Last().Replace(".onnx", "");
                    if (modelName.StartsWith("jieqi", StringComparison.OrdinalIgnoreCase))
                    {
                        // 揭棋模型为 YOLO11 输出格式，不能走 Yolov5Net 解码路径，单独加载。
                        // 只登记进揭棋模型清单，不进象棋模型的 ModelList（两类不混）。
                        if (!JieqiModelNames.Contains(modelName)) JieqiModelNames.Add(modelName);
                        try
                        {
                            JieqiModel = new JieqiDetector(file);
                            ActiveJieqiModelName = modelName;
                            ApplyDetectionParamsToDetector();
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("揭棋模型加载失败: " + ex.ToString());
                        }
                        continue;
                    }
                    var model = new YoloScorer<YoloXiangQiModel>(file);
                    ModelList.Add(modelName, model);
                    if (Settings.YoloModel == modelName)
                    {
                        Model = model;
                    }
                }
            }
            // 启动时把「交叉验证用揭棋模型」的选择落到实际检测器上（选择生效，非摆设）
            ApplyDualJieqiModel();
            // 首次运行没有该键时：把清单里实际加载到的模型名写回默认值（供设置窗回填，避免空值）
            if (string.IsNullOrEmpty(Settings.DualJieqiModel) && JieqiModelNames.Count > 0)
            {
                Settings.DualJieqiModel = JieqiModelNames[0];
            }
            if (string.IsNullOrEmpty(Settings.DualChessModel) && ModelList.Count > 0)
            {
                Settings.DualChessModel = ModelList.ContainsKey(Settings.YoloModel)
                    ? Settings.YoloModel : FirstKey(ModelList);
            }
        }

        // 取字典首个键（供默认值回填；字典为空时返回空串）
        static string FirstKey(Dictionary<string, YoloScorer<YoloXiangQiModel>> dict)
        {
            foreach (var kv in dict) return kv.Key;
            return "";
        }

        // 让「交叉验证用揭棋模型」的选择真正生效。
        // 当前版本只有一个揭棋模型文件（jieqi.onnx），复用同一实例即可；
        // 若目录里将来出现多个揭棋模型，则按所选文件重新构建检测器。
        public static void ApplyDualJieqiModel()
        {
            string want = Settings.DualJieqiModel;
            if (string.IsNullOrEmpty(want)) return;          // 空 = 跟随当前揭棋模型
            if (!JieqiModelNames.Contains(want)) return;     // 该模型不在清单中：保持现状
            if (ActiveJieqiModelName == want && JieqiModel != null) return;   // 已是该模型
            try
            {
                string path = MODEL_FOLDER.TrimEnd('\\') + "\\" + want + ".onnx";
                if (!File.Exists(path)) return;
                JieqiDetector created = new JieqiDetector(path);   // 耗时构建放在锁外，不卡检测线程
                lock (JieqiModelLock)
                {
                    JieqiDetector old = JieqiModel;
                    JieqiModel = created;
                    ActiveJieqiModelName = want;
                    ApplyDetectionParamsToDetector();
                    // 此刻持锁：任何在途的 Detect 已结束（推理同样持锁），旧实例可安全释放
                    if (old != null) { try { old.Dispose(); } catch { } }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("切换交叉验证揭棋模型失败: " + ex.ToString());
            }
        }

        void InitSolutions()
        {
            if (!Directory.Exists(@".\Solutions"))
            {
                Directory.CreateDirectory(@".\Solutions");
            }
            comboBox_solution.Items.Clear();
            SolutionList.Clear();
            string[] paramsKeys = "截图标题 截图类 点击标题 点击类".Split(' ');
            foreach (var folder in Directory.GetDirectories(@".\Solutions"))
            {
                if (folder.EndsWith(@"\自定义方案")) continue;
                string file = folder + @"\window.txt";
                if (!File.Exists(file)) continue;
                string[] lines = File.ReadAllLines(file);
                if (lines.Length == 4)
                {
                    List<string> paramList = new List<string>();
                    for (int i = 0; i < 4; i++)
                    {
                        string line = lines[i];
                        string[] args = line.Split('=');
                        if (args[0] != paramsKeys[i])
                        {
                            MessageBox.Show("方案: " + file + " 格式错误: 第" + (i + 1) + "行应为" + paramsKeys[i]);
                            break;
                        }
                        if (args[1] != "")
                        {
                            paramList.Add(args[1]);
                        }
                        else
                        {
                            paramList.Add(null);
                        }
                    }
                    SolutionList.Add(folder.Split('\\').Last(), new Solution(paramList[0], paramList[1], paramList[2], paramList[3]));
                }
                else
                {
                    MessageBox.Show("方案: " + file + " 格式错误");
                }
            }
            comboBox_solution.Items.Clear();
            foreach (var solution in SolutionList)
            {
                comboBox_solution.Items.Add(solution.Key);
                if (solution.Key == Settings.SelectedSolution)
                {
                    comboBox_solution.SelectedIndex = comboBox_solution.Items.Count - 1;
                }
            }
            if (comboBox_solution.SelectedItem == null)
            {
                if (comboBox_solution.Items.Count > 0)
                {
                    Settings.SelectedSolution = comboBox_solution.Items[0].ToString();
                    comboBox_solution.SelectedIndex = 0;
                }
                else
                {
                    Settings.SelectedSolution = "";
                }
                SaveSettings();
            }
        }

        void InitSettingsUI()
        {
            settingsUILoading = true;
            // 引擎多选框
            comboBox_engine.Items.Clear();
            comboBox_engine.Text = "";
            int selectedIndex = 0;
            foreach (var engine in Settings.EngineList)
            {
                comboBox_engine.Items.Add(engine.Key);
                if (engine.Key == Settings.SelectedEngine)
                {
                    selectedIndex = comboBox_engine.Items.Count - 1;
                }
            }
            if (comboBox_engine.Items.Count > 0)
            {
                comboBox_engine.SelectedIndex = selectedIndex;
                Settings.SelectedEngine = comboBox_engine.Items[selectedIndex].ToString();
                SaveSettings();
            }
            // 后台思考快捷开关
            checkBox_ponder.Checked = Settings.BackgroundAnalysis;
            checkBox_engine_log.Checked = Settings.EngineIoLog;
            // 线程数（与时间窗同步写引擎配置）
            decimal tc = Settings.ThreadCount;
            if (tc < numericUpDown_thread_count.Minimum) tc = numericUpDown_thread_count.Minimum;
            if (tc > numericUpDown_thread_count.Maximum) tc = numericUpDown_thread_count.Maximum;
            numericUpDown_thread_count.Value = tc;
            // Hash（当前引擎配置优先，缺省 16）
            RefreshHashBox();
            // 自动走棋
            checkBox_auto_go.Checked = Settings.AutoGo;
            // 缩放比
            numericUpDown_scale_factor.Value = (decimal)Settings.ScaleFactor;
            // 截图模式（前台/后台）
            comboBox_capture_mode.SelectedIndex = Settings.UniversalMode ? 0 : 1;
            // 鼠标模式（前台/后台）
            comboBox_mouse_mode.SelectedIndex = Settings.UniversalMouse ? 0 : 1;
            // 分析模式
            checkBox_analyze_mode.Checked = Settings.AnalyzingMode;
            // 自动点击
            checkBox_auto_click.Checked = Settings.AutoClick;
            // 绝杀自动立即走棋
            checkBox_stop_when_mate.Checked = Settings.StopWhenMate;
            // 自动走棋分数
            numericUpDown_stop_score.Value = (decimal)Settings.StopScore;
            // 识别模型
            comboBox_yolo_models.Items.Clear();
            foreach (var yolo in ModelList)
            {
                comboBox_yolo_models.Items.Add(yolo.Key);
                if (yolo.Key == Settings.YoloModel)
                {
                    comboBox_yolo_models.SelectedIndex = comboBox_yolo_models.Items.Count - 1;
                }
            }
            if (comboBox_yolo_models.Items.Count > 0)
            {
                if (comboBox_yolo_models.SelectedItem == null) comboBox_yolo_models.SelectedIndex = 0;
            }
            // 识别模式（象棋 / 揭棋）：模式决定识别模型，须在模型列表填充之后再回填
            comboBox_detect_mode.SelectedIndex = Settings.JieqiMode ? 1 : 0;
            ApplyDetectModeUI();
            // 趋势曲线显示模式
            comboBox_trend_mode.SelectedIndex = Settings.ScoreTrendMode == 1 ? 1 : 0;
            LayoutTrendPanel();
            settingsUILoading = false;
        }

        // 按当前识别模式同步界面：揭棋模式固定用 jieqi.onnx（独立检测器），模型下拉锁定显示
        void ApplyDetectModeUI()
        {
            const string jieqiItem = "jieqi（揭棋）";
            if (Settings.JieqiMode && JieqiModel != null)
            {
                if (!comboBox_yolo_models.Items.Contains(jieqiItem))
                {
                    comboBox_yolo_models.Items.Add(jieqiItem);
                }
                comboBox_yolo_models.SelectedItem = jieqiItem;
                comboBox_yolo_models.Enabled = false;
            }
            else
            {
                if (comboBox_yolo_models.Items.Contains(jieqiItem))
                {
                    comboBox_yolo_models.Items.Remove(jieqiItem);
                }
                comboBox_yolo_models.Enabled = true;
                if (ModelList.ContainsKey(Settings.YoloModel))
                {
                    comboBox_yolo_models.SelectedItem = Settings.YoloModel;
                    Model = ModelList[Settings.YoloModel];
                }
                else if (comboBox_yolo_models.Items.Count > 0)
                {
                    comboBox_yolo_models.SelectedIndex = 0;
                }
            }
        }

        private void comboBox_detect_mode_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (settingsUILoading) return;   // 初始化回填时跳过保存与提示
            DiscardTrendPoint();   // 切换识别模式：上一手未确认的趋势点不再画
            Settings.JieqiMode = (comboBox_detect_mode.SelectedIndex == 1);
            // 第十六轮：交叉验证已是「识别设置」里的独立选项，此处不再静默强制开启——
            // 否则复选框形同虚设（用户取消后程序仍跑双模型）。仅在关闭时提示，交由用户决定。
            if (Settings.JieqiMode && !Settings.DualModelValidate)
            {
                DisplayStatus("揭棋模式：交叉验证当前为关闭（可在「识别设置」中开启）");
            }
            ApplyDetectModeUI();
            SaveSettings();
            if (Settings.JieqiMode && JieqiModel == null)
            {
                MessageBox.Show("未找到或无法加载揭棋模型，请确认 Models 目录下存在 jieqi.onnx（opset15 兼容版）。", "揭棋模式", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // ================= 布局调整模式 =================
        // 菜单“布局调整”开启后：小分组框可直接拖动/缩放；大分组框拖动其分隔边调整表格分栏
        void InitLayoutEditor()
        {
            if (layoutInited) return;
            layoutInited = true;
            GroupBox[] boxes = new GroupBox[] { groupBox4, groupBox5, groupBox2, groupBox3, groupBox11, groupBox_capture_mouse };
            foreach (GroupBox box in boxes)
            {
                if (box == null) continue;
                layoutBoxes.Add(box);
                defaultLayoutBounds[box.Name] = box.Bounds;
                layoutEditor.Add(box);
            }
            // 以下大分组框由表格布局托管：拖动其分隔边调整对应表格的行/列
            // 边缘编号：上=1 下=2 左=3 右=4；角：5=左上 6=右上 7=左下 8=右下
            layoutEditor.AddHosted(groupBox_picturebox, (1 << 2) | (1 << 7) | (1 << 8), DragDetectionSplit, EndHostedSplit);
            layoutEditor.AddHosted(groupBox_result_board, (1 << 1) | (1 << 5) | (1 << 6), DragDetectionSplit, EndHostedSplit);
            layoutEditor.AddHosted(groupBox_engine_basic, (1 << 2) | (1 << 7) | (1 << 8), DragEngineSplit, EndHostedSplit);
            layoutEditor.AddHosted(groupBox_side, (1 << 1) | (1 << 5) | (1 << 6) | (1 << 2) | (1 << 7) | (1 << 8), DragEngineSplit, EndHostedSplit);
            layoutEditor.AddHosted(groupBox_log, (1 << 1) | (1 << 5) | (1 << 6), DragEngineSplit, EndHostedSplit);
            layoutEditor.AddHosted(groupBox_engine_settings, (1 << 3) | (1 << 5) | (1 << 7), DragMainSplit, EndHostedSplit);
            layoutEditor.Changed = SaveLayout;
        }

        // ---- 托管容器分隔调整 ----
        bool splitSession = false;
        int[] splitDetStart = new int[0];
        int[] splitEngStart = new int[0];
        int splitMainStart = 743;

        void CaptureSplitStart()
        {
            splitDetStart = tableLayoutPanel_detection.GetRowHeights();
            splitEngStart = tableLayoutPanel_engine.GetRowHeights();
            int[] w = tableLayoutPanel_main.GetColumnWidths();
            splitMainStart = (w.Length > 1) ? w[1] : 743;
        }

        void EndHostedSplit(GroupBox box)
        {
            splitSession = false;
        }

        // 识别画面 / 识别结果 之间的分隔（拖动各自相邻边缘）
        void DragDetectionSplit(GroupBox box, int zone, int dx, int dy)
        {
            if (!splitSession) { splitSession = true; CaptureSplitStart(); }
            if (splitDetStart.Length < 2) return;
            var t = tableLayoutPanel_detection;
            if (t.RowStyles.Count < 2) return;
            int total = splitDetStart[0] + splitDetStart[1];
            int h0 = splitDetStart[0] + dy;
            if (h0 < 80) h0 = 80;
            if (total - h0 < 120) h0 = total - 120;
            t.RowStyles[0].SizeType = System.Windows.Forms.SizeType.Absolute;
            t.RowStyles[0].Height = h0;
            t.RowStyles[1].SizeType = System.Windows.Forms.SizeType.Percent;
            t.RowStyles[1].Height = 100F;
            t.PerformLayout();
        }

        // 引擎区两个分界：引擎设置/识别设置（row0/1）、识别设置/引擎输出（row1/2）
        void DragEngineSplit(GroupBox box, int zone, int dx, int dy)
        {
            if (!splitSession) { splitSession = true; CaptureSplitStart(); }
            if (splitEngStart.Length < 2) return;
            var t = tableLayoutPanel_engine;
            if (t.RowStyles.Count < 2) return;
            bool logBoundary = (box == groupBox_log) || (box == groupBox_side && (zone == 2 || zone == 7 || zone == 8));
            if (logBoundary && t.RowStyles.Count >= 3)
            {
                int h1 = splitEngStart[1] + dy;
                if (h1 < 160) h1 = 160;
                int avail = t.ClientSize.Height - splitEngStart[0];
                // 输出区最小高度：自动滚动复选框 + 边距 + 可读日志区（趋势条已移出本框）
                int minOut = 160;
                if (checkBox_auto_scroll != null)
                {
                    minOut = checkBox_auto_scroll.Height + 130;
                }
                if (minOut < 160) minOut = 160;
                if (h1 > avail - minOut) h1 = avail - minOut;
                if (h1 < 160) h1 = 160;
                t.RowStyles[1].SizeType = System.Windows.Forms.SizeType.Absolute;
                t.RowStyles[1].Height = h1;
                t.PerformLayout();
                return;
            }
            int total = splitEngStart[0] + splitEngStart[1];
            int h0 = splitEngStart[0] + dy;
            if (h0 < 80) h0 = 80;
            if (total - h0 < 150) h0 = total - 150;
            t.RowStyles[0].SizeType = System.Windows.Forms.SizeType.Absolute;
            t.RowStyles[0].Height = h0;
            t.RowStyles[1].SizeType = System.Windows.Forms.SizeType.Absolute;
            t.RowStyles[1].Height = total - h0;
            t.PerformLayout();
        }

        // 左右两栏之间的分隔（拖动“基本设置”的左边缘）
        void DragMainSplit(GroupBox box, int zone, int dx, int dy)
        {
            if (!splitSession) { splitSession = true; CaptureSplitStart(); }
            var t = tableLayoutPanel_main;
            if (t.ColumnStyles.Count < 2) return;
            int w1 = splitMainStart - dx;
            int max1 = t.ClientSize.Width - 260;
            if (max1 < 420) max1 = 420;
            if (w1 < 420) w1 = 420;
            if (w1 > max1) w1 = max1;
            t.ColumnStyles[1].SizeType = System.Windows.Forms.SizeType.Absolute;
            t.ColumnStyles[1].Width = w1;
            t.PerformLayout();
        }

        // 运行时布局归一化：DPI 缩放下子控件被放大而表格行列未同步 → 边缘裁切。
        // 逐容器实测溢出量，按精确差值扩展表格行列（只放大、不缩小）。
        void NormalizeLayout()
        {
            try
            {
                var et = tableLayoutPanel_engine;
                var mt = tableLayoutPanel_main;
                if (et == null || mt == null) return;

                // ---- 横向：右列宽度补足 ----
                int deficitW = 0;
                if (groupBox_side != null && groupBox_side.ClientSize.Width > 0)
                {
                    foreach (Control c in groupBox_side.Controls)
                    {
                        deficitW = Math.Max(deficitW, c.Right + 8 - groupBox_side.ClientSize.Width);
                    }
                }
                if (groupBox_engine_basic != null && groupBox_engine_basic.ClientSize.Width > 0)
                {
                    foreach (Control c in groupBox_engine_basic.Controls)
                    {
                        deficitW = Math.Max(deficitW, c.Right + 8 - groupBox_engine_basic.ClientSize.Width);
                    }
                }
                if (deficitW > 0 && mt.ColumnStyles.Count >= 2)
                {
                    float maxW = mt.ClientSize.Width - 420;
                    if (maxW < 420) maxW = 420;
                    float grow = Math.Min(deficitW, maxW - mt.ColumnStyles[1].Width);
                    if (grow > 0)
                    {
                        mt.ColumnStyles[1].SizeType = System.Windows.Forms.SizeType.Absolute;
                        mt.ColumnStyles[1].Width += grow;
                    }
                }

                // ---- 纵向：识别设置行 ----
                int deficitRow1 = 0;
                if (groupBox_side != null && groupBox_side.ClientSize.Height > 0)
                {
                    foreach (Control c in groupBox_side.Controls)
                    {
                        deficitRow1 = Math.Max(deficitRow1, c.Bottom + 8 - groupBox_side.ClientSize.Height);
                    }
                }
                if (deficitRow1 > 0 && et.RowStyles.Count >= 2)
                {
                    et.RowStyles[1].SizeType = System.Windows.Forms.SizeType.Absolute;
                    et.RowStyles[1].Height += deficitRow1;
                }

                // ---- 纵向：引擎设置行 ----
                int deficitRow0 = 0;
                if (groupBox_engine_basic != null && groupBox_engine_basic.ClientSize.Height > 0)
                {
                    foreach (Control c in groupBox_engine_basic.Controls)
                    {
                        deficitRow0 = Math.Max(deficitRow0, c.Bottom + 8 - groupBox_engine_basic.ClientSize.Height);
                    }
                }
                if (deficitRow0 > 0 && et.RowStyles.Count >= 1)
                {
                    et.RowStyles[0].SizeType = System.Windows.Forms.SizeType.Absolute;
                    et.RowStyles[0].Height += deficitRow0;
                }

                et.PerformLayout();
                mt.PerformLayout();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("NormalizeLayout: " + ex.ToString());
            }
        }

        void ApplySavedLayout()
        {
            if (Settings.LayoutPositions == null) Settings.LayoutPositions = new Dictionary<string, string>();
            // 布局版本校验：旧版本保存的矩形与当前控件集不匹配（控件增删/改尺寸），
            // 直接套用会让分组框互相压住并越界 —— 丢弃旧布局，使用 Designer 当前布局。
            if (Settings.LayoutVersion != ProgramSettings.CurrentLayoutVersion)
            {
                Settings.LayoutPositions.Clear();
                Settings.LayoutSplits.Clear();
                Settings.LayoutVersion = ProgramSettings.CurrentLayoutVersion;
                SaveSettings();
            }
            foreach (GroupBox box in layoutBoxes)
            {
                string val;
                if (!Settings.LayoutPositions.TryGetValue(box.Name, out val)) continue;
                // 只恢复当前仍存在的框（旧版本遗留的 groupBox6/7/10 等键直接跳过）
                if (!defaultLayoutBounds.ContainsKey(box.Name)) continue;
                string[] parts = val.Split(',');
                if (parts.Length != 4) continue;
                int x, y, w, h;
                if (!int.TryParse(parts[0], out x) || !int.TryParse(parts[1], out y)
                    || !int.TryParse(parts[2], out w) || !int.TryParse(parts[3], out h)) continue;
                Control parent = box.Parent;
                if (parent == null) continue;
                if (w < 80) w = 80;
                if (h < 50) h = 50;
                if (w > parent.ClientSize.Width) w = parent.ClientSize.Width;
                if (h > parent.ClientSize.Height) h = parent.ClientSize.Height;
                if (x < 0) x = 0;
                if (y < 0) y = 0;
                if (x + w > parent.ClientSize.Width) x = parent.ClientSize.Width - w;
                if (y + h > parent.ClientSize.Height) y = parent.ClientSize.Height - h;
                box.Bounds = new Rectangle(x, y, w, h);
            }
            // 恢复后自检：任何两个自由分组框相交、或压住同父的「截图与鼠标」，说明保存的布局已过期
            // （旧版本控件集不同）。整组回退到 Designer 默认布局，绝不留重叠给用户看。
            if (HasLayoutCollision())
            {
                Debug.WriteLine("ApplySavedLayout: 恢复的布局存在重叠，已回退默认布局");
                Settings.LayoutPositions.Clear();
                Settings.LayoutSplits.Clear();
                SaveSettings();
                foreach (GroupBox box in layoutBoxes)
                {
                    Rectangle r;
                    if (defaultLayoutBounds.TryGetValue(box.Name, out r)) box.Bounds = r;
                }
            }
            // 应用已保存的表格分栏
            if (Settings.LayoutSplits == null) Settings.LayoutSplits = new Dictionary<string, string>();
            string split;
            if (Settings.LayoutSplits.TryGetValue("detection", out split))
            {
                int h0;
                if (int.TryParse(split, out h0) && tableLayoutPanel_detection.RowStyles.Count >= 2)
                {
                    tableLayoutPanel_detection.RowStyles[0].SizeType = System.Windows.Forms.SizeType.Absolute;
                    tableLayoutPanel_detection.RowStyles[0].Height = h0;
                    tableLayoutPanel_detection.RowStyles[1].SizeType = System.Windows.Forms.SizeType.Percent;
                    tableLayoutPanel_detection.RowStyles[1].Height = 100F;
                }
            }
            if (Settings.LayoutSplits.TryGetValue("engine", out split))
            {
                string[] pr = split.Split(',');
                int h0, h1;
                if (pr.Length == 2 && int.TryParse(pr[0], out h0) && int.TryParse(pr[1], out h1) && tableLayoutPanel_engine.RowStyles.Count >= 2)
                {
                    if (h0 < 118) h0 = 118;   // 引擎设置行最小高度（容纳新增 后台思考/Hash 快捷项）
                    if (h1 < 350) h1 = 350;   // 识别设置行最小高度（容纳新增「截图与鼠标」分组框）
                    tableLayoutPanel_engine.RowStyles[0].SizeType = System.Windows.Forms.SizeType.Absolute;
                    tableLayoutPanel_engine.RowStyles[0].Height = h0;
                    tableLayoutPanel_engine.RowStyles[1].SizeType = System.Windows.Forms.SizeType.Absolute;
                    tableLayoutPanel_engine.RowStyles[1].Height = h1;
                }
            }
            if (Settings.LayoutSplits.TryGetValue("main", out split))
            {
                int w1;
                if (int.TryParse(split, out w1) && tableLayoutPanel_main.ColumnStyles.Count >= 2)
                {
                    tableLayoutPanel_main.ColumnStyles[1].SizeType = System.Windows.Forms.SizeType.Absolute;
                    tableLayoutPanel_main.ColumnStyles[1].Width = w1;
                }
            }
        }

        void RestoreDefaultLayout()
        {
            foreach (GroupBox box in layoutBoxes)
            {
                Rectangle r;
                if (defaultLayoutBounds.TryGetValue(box.Name, out r))
                {
                    box.Bounds = r;
                }
            }
            // 还原表格分栏默认值
            if (tableLayoutPanel_detection.RowStyles.Count >= 2)
            {
                tableLayoutPanel_detection.RowStyles[0].SizeType = System.Windows.Forms.SizeType.Percent;
                tableLayoutPanel_detection.RowStyles[0].Height = 32.31884F;
                tableLayoutPanel_detection.RowStyles[1].SizeType = System.Windows.Forms.SizeType.Percent;
                tableLayoutPanel_detection.RowStyles[1].Height = 67.68116F;
            }
            if (tableLayoutPanel_engine.RowStyles.Count >= 2)
            {
                tableLayoutPanel_engine.RowStyles[0].SizeType = System.Windows.Forms.SizeType.Absolute;
                tableLayoutPanel_engine.RowStyles[0].Height = 118F;
                tableLayoutPanel_engine.RowStyles[1].SizeType = System.Windows.Forms.SizeType.Absolute;
                tableLayoutPanel_engine.RowStyles[1].Height = 350F;
            }
            if (tableLayoutPanel_main.ColumnStyles.Count >= 2)
            {
                tableLayoutPanel_main.ColumnStyles[1].SizeType = System.Windows.Forms.SizeType.Absolute;
                tableLayoutPanel_main.ColumnStyles[1].Width = 743F;
            }
        }

        // 自由分组框两两相交 / 越界 / 压住同父小框 → 视为过期布局
        bool HasLayoutCollision()
        {
            try
            {
                List<GroupBox> boxes = new List<GroupBox>();
                foreach (GroupBox b in layoutBoxes)
                {
                    if (b != null && b.Parent != null && b.Dock == DockStyle.None) boxes.Add(b);
                }
                for (int i = 0; i < boxes.Count; i++)
                {
                    GroupBox a = boxes[i];
                    if (a.Right > a.Parent.ClientSize.Width || a.Bottom > a.Parent.ClientSize.Height) return true;
                    for (int j = i + 1; j < boxes.Count; j++)
                    {
                        GroupBox b = boxes[j];
                        if (a.Parent != b.Parent) continue;
                        if (a.Left < b.Right && b.Left < a.Right && a.Top < b.Bottom && b.Top < a.Bottom) return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("HasLayoutCollision: " + ex.Message);
            }
            return false;
        }

        void SaveLayout()
        {
            if (Settings.LayoutPositions == null) Settings.LayoutPositions = new Dictionary<string, string>();
            foreach (GroupBox box in layoutBoxes)
            {
                Settings.LayoutPositions[box.Name] = box.Bounds.X + "," + box.Bounds.Y + "," + box.Bounds.Width + "," + box.Bounds.Height;
            }
            if (Settings.LayoutSplits == null) Settings.LayoutSplits = new Dictionary<string, string>();
            int[] hd = tableLayoutPanel_detection.GetRowHeights();
            if (hd.Length >= 2) Settings.LayoutSplits["detection"] = hd[0].ToString();
            int[] he = tableLayoutPanel_engine.GetRowHeights();
            if (he.Length >= 2) Settings.LayoutSplits["engine"] = he[0] + "," + he[1];
            int[] wm = tableLayoutPanel_main.GetColumnWidths();
            if (wm.Length >= 2) Settings.LayoutSplits["main"] = wm[1].ToString();
            SaveSettings();
        }

        private void ToolStripMenuItem_layout_edit_CheckedChanged(object sender, EventArgs e)
        {
            bool on = ToolStripMenuItem_layout_edit.Checked;
            layoutEditor.SetEnabled(on);
            // 勾选后菜单文字改为「完成调整」：再点一下即保存并退出（用户要求）
            ToolStripMenuItem_layout_edit.Text = on ? "完成调整" : "布局调整";
            ToolStripMenuItem_layout_edit.ToolTipText = on
                ? "完成调整：点击保存当前布局并退出调整模式"
                : "布局调整：点击进入调整模式，拖动/缩放各分组框调整布局";
            if (!on) SaveLayout();
            DisplayStatus(on ? "布局调整模式已开启：小分组框可拖动/缩放；大分组框拖动其边缘可调整分栏（再次点击菜单「完成调整」保存退出）" : "布局调整模式已关闭，布局已保存");
        }

        private void ToolStripMenuItem_layout_reset_Click(object sender, EventArgs e)
        {
            if (Settings.LayoutPositions != null) Settings.LayoutPositions.Clear();
            if (Settings.LayoutSplits != null) Settings.LayoutSplits.Clear();
            SaveSettings();
            RestoreDefaultLayout();
            NormalizeLayout();
            DisplayStatus("已恢复默认布局");
            Settings.LayoutVersion = ProgramSettings.CurrentLayoutVersion;
            SaveSettings();
        }

        // 各选项的悬停说明（功能 / 数值过调后果 / 默认值）
        void InitToolTips()
        {
            toolTip_main.AutoPopDelay = 15000;
            toolTip_main.InitialDelay = 400;
            toolTip_main.ReshowDelay = 200;
            toolTip_main.ShowAlways = true;
            toolTip_main.SetToolTip(checkBox_debug, "调试状态：显示抓取画面与识别框，勾选时暂停连线。默认：不勾选");
            toolTip_main.SetToolTip(checkBox_auto_go, "自动走棋：引擎出招后自动点击走子；取消则只显示出招不自动走。默认：勾选");
            toolTip_main.SetToolTip(checkBox_analyze_mode, "分析模式：只分析局面显示评分，不自动走子。默认：不勾选");
            toolTip_main.SetToolTip(checkBox_auto_click, "自动点击：自动点击「方案管理」中保存的模板图片（自动续盘）。默认：不勾选");
            toolTip_main.SetToolTip(checkBox_stop_when_mate, "绝杀立即出招：算出绝杀或评分超过阈值时立即落子，不等思考时间。默认：不勾选");
            toolTip_main.SetToolTip(numericUpDown_stop_score, "自动走棋分数：评分高于该值时立即出招。过小=稍有优势就走可能草率；过大=几乎不触发。默认：2000");
            toolTip_main.SetToolTip(comboBox_detect_mode, "识别模式：象棋=用象棋模型识别常规棋子；揭棋=用揭棋模型识别暗棋（需选揭棋引擎）。默认：象棋");
            toolTip_main.SetToolTip(comboBox_yolo_models, "识别模型（象棋模型）：小模型=快、配置要求低；中模型=更准更慢；万能带旋转=界面旋转/异形时用。揭棋模式下此项不生效（改用揭棋模型）。默认：列表第一项（通常为小模型）");
            toolTip_main.SetToolTip(numericUpDown_scale_factor, "缩放比：截图与点击坐标的缩放系数。截图显示不全调大；出现黑边调小。默认：1.00");
            toolTip_main.SetToolTip(comboBox_capture_mode, "截图模式：前台=抓屏（通用，窗口不可被遮挡）；后台=向窗口取图（可被遮挡，个别游戏黑屏）。默认：后台");
            toolTip_main.SetToolTip(comboBox_mouse_mode, "鼠标模式：前台=真实光标点击（通用，占用鼠标）；后台=向窗口发送消息点击（不占鼠标，个别软件不响应）。默认：后台");
            toolTip_main.SetToolTip(checkBox_auto_scroll, "自动滚动：引擎输出自动滚动到最新一行。默认：勾选");
            toolTip_main.SetToolTip(comboBox_engine, "引擎选择：当前使用的引擎；揭棋模式请选择支持揭棋的引擎");
            toolTip_main.SetToolTip(checkBox_ponder, "后台思考：对手行棋时引擎预计算应对，轮到对手走出预判着法时可立即出招。占 CPU 不改变正确性。默认：关");
            toolTip_main.SetToolTip(checkBox_engine_log, "引擎记录：开启后把引擎通信(position/go/stop/着法)与 10 秒状态心跳写入程序目录 engine_io.log，供排查「一直思考/不落子」；平时请保持关闭（避免频繁写盘）。默认：关");
            toolTip_main.SetToolTip(numericUpDown_hash, "Hash 大小(MB)：引擎局面缓存容量。调大=缓存更多局面、棋力略升（占用内存）；调小=反复重算。常用 16~1024。默认：16");
            toolTip_main.SetToolTip(numericUpDown_thread_count, "线程数：引擎计算使用的 CPU 线程。调大=搜索更快但占 CPU 更多（过大可能拖慢点击/识别）；调小=棋力下降。默认：4");
            toolTip_main.SetToolTip(label_thread_unit, "单位：个（线程数）");
            toolTip_main.SetToolTip(label_thread_count, "线程数：引擎计算使用的 CPU 线程。默认：4");
            toolTip_main.SetToolTip(label_hash_unit, "单位：MB（兆字节）");
            toolTip_main.SetToolTip(label_hash, "Hash 大小(MB)：引擎局面缓存容量。默认：16");
            toolTip_main.SetToolTip(button_start_from_self, "我方开始：从轮到我方走棋开始连线");
            toolTip_main.SetToolTip(button_start_from_oppo, "对方开始：从对方走棋开始连线（先等对手走子）");
            toolTip_main.SetToolTip(button_stop_detection, "停止连线：停止识别与自动走子");
            toolTip_main.SetToolTip(button_go_immediately, "立即出招：不等思考结束，立刻按当前最优着法走子");
            toolTip_main.SetToolTip(button_advance_settings, "识别设置：确认次数 / 识别间隔 / 揭棋阈值 / 不合法重试次数");
            toolTip_main.SetToolTip(button_capture_mouse_settings, "截图与鼠标设置：截图重试 / 点击间隔 / 按住时长 / 点击前等待 / 落子失败重试 / 后台点击序列");
            toolTip_main.SetToolTip(button_time_settings, "时间设置：出招时限与最短出招间隔（线程数/Hash 在本分组框内）");            toolTip_main.SetToolTip(button_redetect, "重新检测：重新框定棋盘区域并刷新识别");
            toolTip_main.SetToolTip(comboBox_solution, "连线方案：目标游戏窗口的匹配方案；不合适时用「寻找新连线窗口」自定义");
            toolTip_main.SetToolTip(button_get_hwnd, "寻找新连线窗口：点击后 2 秒内把鼠标移到目标窗口上获取句柄（也可按 F2）");
            toolTip_main.SetToolTip(button_save_as_solution, "保存当前方案：保存当前窗口配置供下次使用");
            toolTip_main.SetToolTip(button_clipboard_solution, "识别剪切板图片：方案选「剪切板」时，识别剪切板中的图像");
            toolTip_main.SetToolTip(button_screenshot, "自动点击续盘管理：管理自动点击的模板图片");
            toolTip_main.SetToolTip(comboBox_trend_mode, "趋势曲线显示：分数=引擎评分走势（我方视角）；胜率=换算百分比。每局开始重新绘制；每次成功落子记录一个点。默认：分数");
            toolTip_main.SetToolTip(pictureBox_trend, "趋势曲线：横轴为出招次数，纵轴为分数/胜率（正值=我方优势，对方行棋的分数已取反）；每局开始重新绘制；每次成功落子记录一个点");
            // 菜单项提示
            menuStrip1.ShowItemToolTips = true;
            ToolStripMenuItem_copy_fen.ToolTipText = "复制局面：把当前识别到的局面 FEN 复制到剪贴板";
            ToolStripMenuItem_engine_management.ToolTipText = "引擎管理：添加/删除引擎、配置引擎参数";
            ToolStripMenuItem_openbook_management.ToolTipText = "开局库管理：导入并选择开局库文件（.obk）";
            ToolStripMenuItem_layout_edit.ToolTipText = "布局调整：勾选后拖动/缩放各分组框调整布局，取消勾选时保存";
            ToolStripMenuItem_layout_reset.ToolTipText = "恢复默认布局：清除已保存布局，还原初始排列与分栏";
            ToolStripMenuItem_about.ToolTipText = "关于：版本信息与制作名单";
            byVincentzyxToolStripMenuItem.ToolTipText = "作者/项目信息";
            // 可点击查看的控件也补提示
            toolTip_main.SetToolTip(pictureBox_show_result, "识别画面：检测过程的实时画面（含检测框）；单击可放大");
            toolTip_main.SetToolTip(pictureBox_board, "识别结果：当前识别棋盘示意（含走子箭头）；单击可放大");
            toolTip_main.SetToolTip(textBox_engine_log, "引擎输出：引擎思考信息（深度/得分/时间/胜率）与着法记录");
            // NumericUpDown 内部编辑框是独立子控件：把提示同步给子控件，悬停任意位置都显示
            SyncNumericToolTips(this, toolTip_main);
        }

        // 递归把 NumericUpDown 的提示同步到其内部子控件（UpDownEdit），保证悬停任意位置都显示
        public static void SyncNumericToolTips(Control parent, ToolTip tt)
        {
            foreach (Control c in parent.Controls)
            {
                if (c is NumericUpDown)
                {
                    string t = tt.GetToolTip(c);
                    if (!string.IsNullOrEmpty(t))
                    {
                        foreach (Control child in c.Controls) tt.SetToolTip(child, t);
                    }
                }
                if (c.HasChildren) SyncNumericToolTips(c, tt);
            }
        }

        // ================= 趋势曲线 =================
        // 数据点只在“落子被检测确认”后经 ConfirmTrendPoint 写入（见下），保证曲线只记录真实走子。

        // 每局开始：清空曲线重新绘制（并丢弃未确认的暂存评分）
        void ResetTrend()
        {
            lock (trendLock) { trendScores.Clear(); trendStashValid = false; }
            RenderTrend();
        }

        // 分数 → 胜率（与引擎输出一致：50% 为均势）
        static float ToWinRate(float score)
        {
            if (score >= 9999f) return 100f;
            if (score <= -9999f) return 0f;
            return (float)(100.0 / (1.0 + Math.Pow(10, -score / 400.0)));
        }

        // 绘制趋势曲线到右侧小面板
        void RenderTrend()
        {
            try
            {
                SafeInvoke(new Action(() =>
                {
                    if (pictureBox_trend == null) return;
                    int w = pictureBox_trend.Width;
                    int h = pictureBox_trend.Height;
                    if (w <= 4 || h <= 4) return;
                    bool winRateMode = (Settings.ScoreTrendMode == 1);
                    float[] snapshot;
                    lock (trendLock) { snapshot = trendScores.ToArray(); }
                    Bitmap bmp = new Bitmap(w, h);
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.Clear(Color.White);
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        float yMin, yMax;
                        if (winRateMode) { yMin = 0; yMax = 100; }
                        else
                        {
                            float maxAbs = 100;
                            foreach (float v in snapshot) maxAbs = Math.Max(maxAbs, Math.Abs(v));
                            yMax = maxAbs; yMin = -maxAbs;
                        }
                        using (Pen gridPen = new Pen(Color.FromArgb(225, 225, 225)))
                        using (Pen midPen = new Pen(Color.FromArgb(150, 150, 150)))
                        using (Pen curvePen = new Pen(Color.FromArgb(30, 90, 200), 2))
                        using (Font font = new Font("微软雅黑", 10, GraphicsUnit.Pixel))
                        using (SolidBrush textBrush = new SolidBrush(Color.FromArgb(90, 90, 90)))
                        using (SolidBrush dotBrush = new SolidBrush(Color.FromArgb(30, 90, 200)))
                        {
                            for (int i = 0; i <= 4; i++)
                            {
                                int y = h * i / 4;
                                if (y < 1) y = 1;
                                if (y > h - 1) y = h - 1;
                                g.DrawLine(i == 2 ? midPen : gridPen, 0, y, w, y);
                            }
                            // “分数/胜率”下拉已移到曲线上方（新布局），曲线不再需要避让
                            int rightReserve = 4;
                            int n = snapshot.Length;
                            if (n >= 2)
                            {
                                PointF[] pts = new PointF[n];
                                for (int i = 0; i < n; i++)
                                {
                                    float v = winRateMode ? ToWinRate(snapshot[i]) : snapshot[i];
                                    float x = (float)i / (n - 1) * (w - 6 - rightReserve) + 3;
                                    float yn = (v - yMin) / (yMax - yMin);
                                    if (yn < 0) yn = 0;
                                    if (yn > 1) yn = 1;
                                    float y = h - 3 - yn * (h - 6);
                                    pts[i] = new PointF(x, y);
                                }
                                g.DrawLines(curvePen, pts);
                            }
                            else if (n == 1)
                            {
                                // 单点也按分值换算纵坐标（与多点分支同一公式），不再固定画在中线
                                float v1 = winRateMode ? ToWinRate(snapshot[0]) : snapshot[0];
                                float yn1 = (v1 - yMin) / (yMax - yMin);
                                if (yn1 < 0) yn1 = 0;
                                if (yn1 > 1) yn1 = 1;
                                float x1 = 3 + (w - 6 - rightReserve) / 2f;
                                float y1 = h - 3 - yn1 * (h - 6);
                                g.FillEllipse(dotBrush, x1 - 2, y1 - 2, 4, 4);
                            }
                            string cur = "--";
                            if (n > 0)
                            {
                                float lastV = snapshot[n - 1];
                                cur = winRateMode ? (Math.Round(ToWinRate(lastV), 1) + "%") : lastV.ToString("0");
                            }
                            g.DrawString((winRateMode ? "胜率 " : "分数 ") + cur, font, textBrush, 4, 2);
                        }
                    }
                    Image old = pictureBox_trend.Image;
                    pictureBox_trend.Image = bmp;
                    if (old != null) old.Dispose();
                }));
            }
            catch (Exception ex)
            {
                Debug.WriteLine("RenderTrend: " + ex.ToString());
            }
        }

        private void comboBox_trend_mode_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (settingsUILoading) return;
            Settings.ScoreTrendMode = (comboBox_trend_mode.SelectedIndex == 1) ? 1 : 0;
            SaveSettings();
            RenderTrend();
        }

        private void pictureBox_trend_SizeChanged(object sender, EventArgs e)
        {
            RenderTrend();
        }

        private void panel_trend_SizeChanged(object sender, EventArgs e)
        {
            LayoutTrendPanel();
        }

        // 面板 Layout 事件：确定性布局（防 Dock 顺序导致下拉/曲线重叠）
        private void panel_trend_Layout(object sender, LayoutEventArgs e)
        {
            LayoutTrendPanel();
        }

        bool layoutingTrendPanel = false;   // SetBounds 重入保护（Layout 事件内改子控件尺寸可能重入）

        // 趋势面板内部布局：下拉条固定顶部，曲线填满其余区域（确定性布局，不依赖 Dock/锚点）
        void LayoutTrendPanel()
        {
            if (layoutingTrendPanel) return;
            layoutingTrendPanel = true;
            try
            {
                if (panel_trend == null || comboBox_trend_mode == null || pictureBox_trend == null) return;
                if (panel_trend.Width <= 6 || panel_trend.Height <= 6) return;
                int innerW = panel_trend.ClientSize.Width - panel_trend.Padding.Horizontal;
                if (innerW < 20) return;
                int comboH = comboBox_trend_mode.Height;
                comboBox_trend_mode.SetBounds(panel_trend.Padding.Left, panel_trend.Padding.Top, innerW, comboH);
                int picTop = panel_trend.Padding.Top + comboH + 4;
                int picH = panel_trend.ClientSize.Height - picTop - panel_trend.Padding.Bottom;
                if (picH < 10) picH = 10;
                pictureBox_trend.SetBounds(panel_trend.Padding.Left, picTop, innerW, picH);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("LayoutTrendPanel: " + ex.Message);
            }
            finally
            {
                layoutingTrendPanel = false;
            }
        }

        // 趋势面板宽度上限：不挤占「识别画面」（面板停靠右侧时按其实际宽度约束）
        void ClampTrendPanelWidth()
        {
            try
            {
                if (panel_trend == null || groupBox_picturebox == null) return;
                if (panel_trend.Dock != DockStyle.Right) return;
                double f = Math.Max(1.0, this.Font.Height / 15.0);
                int maxW = groupBox_picturebox.ClientSize.Width - (int)(220 * f);
                if (maxW < (int)(150 * f)) maxW = (int)(150 * f);
                int want = Math.Min((int)(216 * f), maxW);
                if (want < 140) want = 140;
                if (panel_trend.Width != want) panel_trend.Width = want;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ClampTrendPanelWidth: " + ex.Message);
            }
        }

        // 出招时暂存评分（等落子被检测确认后再画点）
        void StashTrendPoint(float v)
        {
            lock (trendLock) { trendStashValue = v; trendStashValid = true; }
        }

        // 落子被检测确认：把暂存的评分画到曲线（同一着法重试不重复提交）。
        // 清暂存 + 追加数据点在同一把锁内原子完成，避免与 ResetTrend（开新局清空）竞态产生“幽灵点”。
        void ConfirmTrendPoint()
        {
            lock (trendLock)
            {
                if (!trendStashValid) return;
                trendStashValid = false;
                trendScores.Add(trendStashValue);
                if (trendScores.Count > 400) trendScores.RemoveAt(0);
            }
            RenderTrend();
        }

        // 放弃该着法（重置识别/非引擎出招）：丢弃暂存点，避免旧评分落到别的着法上
        void DiscardTrendPoint()
        {
            lock (trendLock) { trendStashValid = false; }
        }

        void InitResultDisplay()
        {
            pictureBox_board.Image = BoardDisplayBitmap;
            BoardGDI = Graphics.FromImage(BoardDisplayBitmap);
            RenderDisplayBoard();
        }

        void InitEngine()
        {
            if (Engine != null)
            {
                Engine.Stop();
            }
            if (Settings.SelectedEngine.Length > 0 && Settings.EngineList.ContainsKey(Settings.SelectedEngine))
            {
                EngineSettings engineSettings = Settings.EngineList[Settings.SelectedEngine];
                if (File.Exists(engineSettings.ExePath))
                {
                    var configs = Settings.EngineList[Settings.SelectedEngine].Configs;
                    if (configs.ContainsKey("Threads"))
                    {
                        configs["Threads"] = Settings.ThreadCount.ToString();
                    }
                    else
                    {
                        configs.Add("Threads", Settings.ThreadCount.ToString());
                    }
                    Engine = new EngineHelper(engineSettings.ExePath, configs);
                    Engine.BestMoveEvent += Engine_BestMove_Event;
                    Engine.InfoEvent += Engine_InfoEvent;
                    Engine.Init();
                }

            }
        }

        void ResetDetection()
        {
            DiscardTrendPoint();   // 放弃待确认着法：清掉尚未确认的暂存评分
            Engine.StopAnalyze();
            Engine.AnalyzeQueue.Clear();
            Engine.SkipList.Clear();
            MultiPvClear();   // 清空 MultiPV 候选箭头（第二十轮）
            BackgroundAnalyzing = false;   // R22：重置时复位后台思考状态（防卡在「等待 ponder 命中」分支）
            PonderBoard = null;
            PonderReadyMove = "";
            PonderReadyPonder = "";
            PonderReadyFen = "";
            InvalidCount = 0;
            if (!string.IsNullOrEmpty(Settings.SelectedSolution))
            {
                string key = Settings.SelectedSolution;
                if (SolutionList.ContainsKey(key))
                {
                    CurrentSolution = SolutionList[key];
                    GameHandle = ScreenshotHelper.FindWindow(CurrentSolution.ScreenshotClass, CurrentSolution.ScreenshotCaption);
                    if (CurrentSolution.ClickClass == null && CurrentSolution.ClickCaption == null)
                    {
                        ClickHandle = GameHandle;
                    }
                    else
                    {
                        ClickHandle = ScreenshotHelper.FindWindowEx(GameHandle, IntPtr.Zero, CurrentSolution.ClickClass, CurrentSolution.ClickCaption);
                    }
                }
                else
                {
                    DisplayStatus("未找到连线方案 " + key + "，请重新选择方案");
                    CurrentSolution = null;
                    GameHandle = IntPtr.Zero;
                    ClickHandle = IntPtr.Zero;
                }
            }
            ReloadBoard();
            BoardArea = new Rectangle(-1, -1, -1, -1);
            EngineAnalyzeCount = 0;
            CurrentBoard = null;
            LastBoard = null;
            PendingBoard = null;
            LastValidPendingBoard = null;
            ExpectedSelfGoBoard = null;
            ExpectedMove = "";
            ExpectedBoardMap.Clear();
        }

        void StartDetection(bool fromOpponent)
        {
            if (Engine == null)
            {
                DetectEnabled = false;
                MessageBox.Show("引擎未加载！");
                ModeDisplay();
                return;
            }
            // R26：新开一局——清空崩溃黑名单与重启节流（上一局崩溃的盘面/节流不拖累新局）
            try { Engine.OnNewGameReset(); } catch { }
            // R38：新一局/重新开始连线 → 清除「落子未确认暂停」与自愈计数
            SelfHealStreak = 0;
            ClickingSuspended = false;
            // R42：同时复位「已自动降级前台」与黑名单补发状态
            _autoDegradedToForeground = false;
            ForceForegroundClickOnce = false;
            _blacklistRetryAt = DateTime.MinValue;
            _blacklistRetryCount = 0;
            // 本局计时起点与己方着法计数（时间设置的「局时 N 分钟后」「前 N 回合」由此判定）
            GameStartTime = DateTime.Now;
            SelfMoveCount = 0;
            if (checkBox_debug.Checked)
            {
                checkBox_debug.Checked = false;
            }
            if (BackgroundAnalyzing)
            {
                BackgroundAnalyzing = false;
            }
            PonderReadyMove = "";   // R23：新一局，清后台思考暂存
            PonderReadyPonder = "";
            PonderReadyFen = "";
            TurnToOpponent = fromOpponent;
            DetectEnabled = true;
            ResetTrend();   // 每局开始：清空趋势曲线重新绘制
            ResetDetection();
            ModeDisplay();
        }

        // 安全 UI 调度：窗口已销毁 / 句柄未创建 / 正在关闭时静默跳过，避免退出瞬间 Invoke 竞态
        public void SafeInvoke(Action action)
        {
            try
            {
                if (IsDisposed || Disposing || !IsHandleCreated) return;
                Invoke(action);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("SafeInvoke: " + ex.Message);
            }
        }

        void DisplayStatus(string status)
        {
            Debug.WriteLine(status);
            SafeInvoke(new Action(() => label_status.Text = "识别状态: " + status));
        }

        void ModeDisplay(string mode="")
        {
            if (string.IsNullOrEmpty(mode))
            {
                mode = DetectEnabled ? "识别中" : "空闲";
            }
            Debug.WriteLine(mode);
            SafeInvoke(new Action(() => label_detection_status.Text = "连线状态: " + mode));
        }

        // 第二十五轮：返回「两次点击是否都真的投递」；起点失败即中止（不再空点终点）。
        bool MouseLeftClient_2Point(int x1, int y1, int x2, int y2)
        {
            int interval = Settings.ClickInterval;
            if (interval < 50) interval = 50;
            if (interval > 2000) interval = 2000;
            bool ok1 = ClickAtBitmap(new Point(x1, y1));
            Thread.Sleep(interval);
            bool ok2 = ClickAtBitmap(new Point(x2, y2));
            return ok1 && ok2;
        }

        void MouseLeftClick(int x, int y)
        {
            ClickAtBitmap(new Point(x, y));
        }

        // 第二十五轮：回归测试探针——验证 DetectionLogic.PlayChess 已改为同步布尔返回。
        // （DetectionLogic 是 internal，测试程序集无法 typeof，故由此处转发。）
        public static bool PlayChessReturnsBool()
        {
            var m = typeof(Mainform).GetMethod("PlayChess", new System.Type[] { typeof(string) });
            return m != null && m.ReturnType == typeof(bool);
        }

        // 第二十五轮：静态上下文（ClickAtBitmap 是 static）不能直接调用实例方法——经 MouseHelper.mainform 转发。
        // 找不到主窗体或重取失败都返回 false（调用方会给出可见提示，不静默）。
        static bool ReacquireTargetWindowStatic()
        {
            Mainform mf = MouseHelper.mainform;
            if (mf == null) return false;
            return mf.TryReacquireTargetWindow();
        }

        // R36：后台点击「越界回退到截图窗口」的一次性提示（同一坏目标只提示一次，防刷屏）
        static string _lastOobTargetNotice = "";
        static void ClickFallbackNotice(IntPtr badTarget)
        {
            try
            {
                string key = badTarget.ToInt64().ToString("X");
                if (_lastOobTargetNotice == key) return;
                _lastOobTargetNotice = key;
                Status("点击目标与截图窗口不一致（坐标越界），已自动改按截图窗口投递；如仍不落子请重新获取窗口句柄");
            }
            catch { }
        }

        // 第二十五轮：供识别线程（DetectionLogic.PlayChess）调用的静态入口。
        public static bool MouseLeftClient_2PointSafe(int x1, int y1, int x2, int y2)
        {
            Mainform mf = MouseHelper.mainform;
            if (mf == null) return false;
            return mf.MouseLeftClient_2Point(x1, y1, x2, y2);
        }

        // ---- 点击统一入口 ----
        // 传入“位图坐标”：最近一次抓屏位图（Screenshot()）内的像素坐标，
        // 与 ClickPositionMap / 模板匹配坐标同一空间（LastCaptureSpace 记录该空间）。
        // 前台鼠标：位图 → 物理屏幕坐标，真实光标点击；后台鼠标：位图 → 目标客户区消息坐标，PostMessage 点击。
        // 换算规则已由靶场实测验证（前台/后台截屏 × 前台/后台点击 = 20/20 命中）。
        // 第二十五轮：返回「本次点击是否真的投递出去」（false = 被守卫拦下或投递失败，调用方不得当作已落子）。
        public static bool ClickAtBitmap(Point bmp)
        {
            if (bmp.X < -100 || bmp.Y < -100 || bmp.X > 20000 || bmp.Y > 20000)
            {
                Status("点击坐标异常，已跳过本次点击");
                EngineHelper.IoLog("CLICK", "skip: bad-coord " + bmp.X + "," + bmp.Y);
                return false;
            }
            if (LastCaptureSpace == WinCoord.CaptureSpace.None)
            {
                Status("刚切换模式，等待新一帧重建坐标，本次点击已跳过（属正常提示）");
                EngineHelper.IoLog("CLICK", "skip: space-none pending-fresh-frame");
                return false;
            }
            int hold = Settings.ClickHoldMs;
            int pre = Settings.ClickPreWaitMs;
            // R42：一次性/本局自动降级前台
            bool front = Settings.UniversalMouse;
            if (!front && (ForceForegroundClickOnce || _autoDegradedToForeground))
            {
                if (!_autoDegradedToForeground)
                {
                    _autoDegradedToForeground = true;
                    Status("后台点击未被棋软响应，本局已自动改用前台点击（切换鼠标模式或重新开始连线可复位）");
                    EngineHelper.IoLog("WATCH", "auto-degrade-to-foreground (session)");
                }
                front = true;
                ForceForegroundClickOnce = false;
            }
            if (front)
            {
                // 前台点击（第二十四轮）：只依赖 GameHandle，不再要求 ClickHandle（修复「切回前台后点不动」）。
                if (GameHandle == IntPtr.Zero || !WinCoord.IsWindowAlive(GameHandle))
                {
                    // 第二十五轮：句柄失效不再静默跳过——按方案自动重取窗口（成功即继续点击）。
                    // 失败原因写明到状态栏与日志（用户此前只看到「点不动」，无从判断）。
                    EngineHelper.IoLog("CLICK", "front dead-handle -> reacquire game=" + GameHandle.ToInt64().ToString("X"));
                    if (!ReacquireTargetWindowStatic())
                    {
                        Status("点击目标窗口已失效且无法自动重取，请按「寻找新连线窗口」重新取窗口");
                        EngineHelper.IoLog("CLICK", "front reacquire-failed -> skip");
                        return false;
                    }
                    EngineHelper.IoLog("CLICK", "front reacquired GameHandle=" + GameHandle.ToInt64().ToString("X"));
                }
                Point scr = WinCoord.BitmapToScreen(LastCaptureSpace, bmp, GameHandle);
                if (!SystemInformation.VirtualScreen.Contains(scr))
                {
                    Status("点击坐标超出屏幕，已跳过本次点击");
                    EngineHelper.IoLog("CLICK", "front skip: offscreen " + scr.X + "," + scr.Y);
                    return false;
                }
                var orig = MouseHelper.MouseOperations.GetCursorPosition();
                bool fg = WinCoord.ForegroundClick(GameHandle, scr, pre, hold);
                Thread.Sleep(40);
                MouseHelper.MouseOperations.SetCursorPosition(orig.X, orig.Y);
                EngineHelper.IoLog("CLICK", "front " + bmp.X + "," + bmp.Y + " -> " + scr.X + "," + scr.Y + " space=" + LastCaptureSpace + " fg=" + fg);
                if (!fg) return false;
            }
            else
            {
                // 后台点击（第二十四轮）：ClickHandle 缺失时退回 GameHandle，避免静默失效。
                IntPtr target = ClickHandle != IntPtr.Zero ? ClickHandle : GameHandle;
                if (GameHandle == IntPtr.Zero || !WinCoord.IsWindowAlive(GameHandle) || target == IntPtr.Zero || !WinCoord.IsWindowAlive(target))
                {
                    EngineHelper.IoLog("CLICK", "rear dead-handle -> reacquire game=" + GameHandle.ToInt64().ToString("X") + " target=" + target.ToInt64().ToString("X"));
                    if (!ReacquireTargetWindowStatic())
                    {
                        Status("点击目标窗口已失效且无法自动重取，请按「寻找新连线窗口」重新取窗口");
                        EngineHelper.IoLog("CLICK", "rear reacquire-failed -> skip");
                        return false;
                    }
                    target = ClickHandle != IntPtr.Zero ? ClickHandle : GameHandle;
                    EngineHelper.IoLog("CLICK", "rear reacquired GameHandle=" + GameHandle.ToInt64().ToString("X") + " target=" + target.ToInt64().ToString("X"));
                }
                Point lp = WinCoord.BitmapToMessageLParam(LastCaptureSpace, bmp, GameHandle, target);
                // R36：后台点击坐标越界校验——lParam 必须落在目标客户区范围内，越界消息会被目标窗口
                // 直接忽略（实测症状：投递 ok=True 但棋盘永远不变，触发「棋盘未改变/落子多次未成功」循环）。
                // 点击目标与截图窗口不同源时 lp 常为大负数；处理：若 target 与截图窗口不同，先改按截图
                // 窗口重算，范围内则回退投递；两处都越界才拒绝发送（拒绝不记 PLAY，交上层重试/恢复）。
                if (!WinCoord.IsClientPointInRange(target, lp))
                {
                    Size tsz = WinCoord.ClientSizeTarget(target);
                    Point cg = WinCoord.ClientOriginPhysical(GameHandle);
                    Point ct = WinCoord.ClientOriginPhysical(target);
                    string oobDiag = "lp=" + lp.X + "," + lp.Y
                        + " game=" + GameHandle.ToInt64().ToString("X")
                        + " target=" + target.ToInt64().ToString("X")
                        + " c0game=" + cg.X + "," + cg.Y
                        + " c0target=" + ct.X + "," + ct.Y
                        + " client=" + tsz.Width + "x" + tsz.Height;
                    Point lp2 = target != GameHandle
                        ? WinCoord.BitmapToMessageLParam(LastCaptureSpace, bmp, GameHandle, GameHandle)
                        : lp;
                    if (target != GameHandle && WinCoord.IsClientPointInRange(GameHandle, lp2))
                    {
                        EngineHelper.IoLog("CLICK", "rear OOB " + oobDiag + " -> fallback-to-game lp2=" + lp2.X + "," + lp2.Y);
                        ClickFallbackNotice(target);
                        target = GameHandle;
                        ClickHandle = GameHandle;   // 持久纠正：后续点击不再走错误目标
                        lp = lp2;
                    }
                    else
                    {
                        Status("后台点击坐标越界，已跳过本次点击（详见引擎记录）");
                        EngineHelper.IoLog("DROP", "rear coord-out-of-range " + oobDiag
                            + (target != GameHandle ? " lp2=" + lp2.X + "," + lp2.Y : ""));
                        return false;
                    }
                }
                int strategy = Settings.BackgroundClickStrategy;
                if (strategy < 0 || strategy > 2) strategy = 0;
                bool ok = WinCoord.BackgroundClick(target, lp, hold, strategy);
                if (!ok)
                {
                    Status("后台点击消息投递失败，请检查目标窗口是否仍在运行");
                }
                EngineHelper.IoLog("CLICK", "rear " + bmp.X + "," + bmp.Y + " -> lp " + lp.X + "," + lp.Y + " space=" + LastCaptureSpace + " target=" + (target == ClickHandle ? "click" : "game") + " strat=" + strategy + " ok=" + ok);
                if (!ok) return false;
            }
            return true;
        }

        // R26：跨类状态桥（EngineHelper 的跳盘/黑名单提示用；主窗体不存在时静默）
        public static void StatusPub(string msg)
        {
            try
            {
                Mainform mf = MouseHelper.mainform;
                if (mf != null) mf.DisplayStatus(msg);
            }
            catch { }
        }

        // 静态上下文里的用户提示（主窗口存在时显示到状态栏）
        static void Status(string msg)
        {
            Debug.WriteLine(msg);
            Mainform mf = MouseHelper.mainform;
            if (mf != null)
            {
                mf.DisplayStatus(msg);
            }
        }

        void GetHandle()
        {
            Task.Run(new Action(() =>
            {
                DisplayStatus("请点击目标窗体并等待 准备获取游戏窗口句柄...2");
                Thread.Sleep(1000);
                DisplayStatus("请点击目标窗体并等待 准备获取游戏窗口句柄...1");
                Thread.Sleep(1000);
                GameHandle = ScreenshotHelper.GetForegroundWindow();
                try { _lastGameWindowTitle = ScreenshotHelper.GetWindowTextSafe(GameHandle); } catch { }
                ClickHandle = (IntPtr)ScreenshotHelper.WindowFromPoint(Cursor.Position.X, Cursor.Position.Y);
                DisplayStatus("游戏窗口句柄: " + GameHandle);
                ScreenshotHelper.WindowHandleInfo handleInfo = new ScreenshotHelper.WindowHandleInfo(GameHandle);
                var handles = handleInfo.GetAllChildHandles();
                Debug.WriteLine("GameHandle " + GameHandle + " " + ScreenshotHelper.GetWindowTitle(GameHandle));
                Debug.WriteLine(ScreenshotHelper.GetWindowRectangleWithShadow(GameHandle));
                Debug.WriteLine("ClickHandle " + ClickHandle + " " + ScreenshotHelper.GetWindowTitle(ClickHandle));
                Debug.WriteLine(ScreenshotHelper.GetWindowRectangleWithShadow(ClickHandle));
                Settings.SelectedSolution = "";
                foreach (var handle in handles)
                {
                    Debug.WriteLine(handle + " " + ScreenshotHelper.GetWindowTitle(handle));
                    Debug.WriteLine(ScreenshotHelper.GetWindowRectangleWithShadow(handle));
                    Debug.WriteLine(ScreenshotHelper.GetWindowClass(handle));
                }    
                SafeInvoke(new Action(() =>
                {
                    comboBox_solution.SelectedItem = null;
                    comboBox_solution.Text = "自定义方案";
                }));
            }));
        }

        // 带重试的截图入口：无效帧（空图 / 1x1 错误占位）按设置重试，仍失败则返回最后一帧
        Bitmap Screenshot()
        {
            int retry = Settings.ScreenshotRetryCount;
            if (retry < 0) retry = 0;
            if (retry > 5) retry = 5;
            Bitmap last = null;
            for (int attempt = 0; attempt <= retry; attempt++)
            {
                Bitmap img = ScreenshotOnce();
                if (img != null && img.Width > 1 && img.Height > 1)
                {
                    if (last != null) last.Dispose();
                    return img;
                }
                if (last != null) last.Dispose();
                last = img;
                if (attempt < retry) Thread.Sleep(80);
            }
            return last ?? new Bitmap(1, 1);
        }

        Bitmap ScreenshotOnce()
        {
            try
            {
                if (Settings.SelectedSolution == "剪切板")
                {
                    if (Clipboard.ContainsImage())
                    {
                        Debug.WriteLine("Before " + Clipboard.ContainsImage().ToString());
                        Bitmap img = (Bitmap)Clipboard.GetImage().Clone();
                        Debug.WriteLine("After " + Clipboard.ContainsImage().ToString());
                        Debug.WriteLine("-=-=-=-=-=-==-=");
                        return img;
                    }
                    else
                    {
                        Debug.WriteLine("NO Clipboard Image");
                        Bitmap no_image = new Bitmap(150, 100);
                        using (Graphics gdi = Graphics.FromImage(no_image))
                        {
                            gdi.DrawString("剪切板中没有图片", this.Font, Brushes.Black, 0, 40);
                        }
                        return no_image;
                    }
                }
                if (Settings.UniversalMode)
                {
                    // 前台抓屏：窗口矩形（物理像素：DWM 扩展边框），与 WinCoord.FrontWindowPhysical 一致
                    if (GameHandle == IntPtr.Zero) return new Bitmap(1, 1);
                    Rectangle rect = WinCoord.WindowRectPhysical(GameHandle);
                    if (rect.Width > 0 && rect.Height > 0)
                    {
                        Bitmap b = new Bitmap(rect.Width, rect.Height);
                        using (Graphics g = Graphics.FromImage(b))
                        {
                            g.CopyFromScreen(rect.Location, new Point(0, 0), rect.Size);
                        }
                        LastCaptureSpace = WinCoord.CaptureSpace.FrontWindowPhysical;
                        return b;
                    }
                    else
                    {
                        return new Bitmap(1, 1);
                    }
                }
                else
                {
                    // 后台抓屏：PrintWindow 渲染目标客户区（位图坐标=目标客户区像素），与 WinCoord.RearClientTarget 一致
                    Bitmap rear = WinCoord.CaptureClient(GameHandle);
                    if (rear.Width > 1 && rear.Height > 1)
                    {
                        LastCaptureSpace = WinCoord.CaptureSpace.RearClientTarget;
                    }
                    return rear;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                GameHandle = IntPtr.Zero;
                ClickHandle = IntPtr.Zero;
                return new Bitmap(1, 1);
            }
        }

        void LoadCurrentSolution()
        {
            string[] imgExtensions = new string[] { "png", "jpg", "bmp", "jpeg" };
            if (Settings.SelectedSolution == "剪切板")
            {
                ClickHandle = (IntPtr)1;
                GameHandle = (IntPtr)1;
                return;
            }
            if (Settings.SelectedSolution != "" && Settings.SelectedSolution != "自定义方案")
            {
                string key = Settings.SelectedSolution;
                if (!SolutionList.ContainsKey(key))
                {
                    DisplayStatus("未找到连线方案 " + key + "，请重新选择方案");
                    return;
                }
                CurrentSolution = SolutionList[key];
                GameHandle = ScreenshotHelper.FindWindow(CurrentSolution.ScreenshotClass, CurrentSolution.ScreenshotCaption);
                if (CurrentSolution.ClickClass == null && CurrentSolution.ClickCaption == null)
                {
                    ClickHandle = GameHandle;
                    ClickOffset = new Size(0, 0);
                }
                else
                {
                    ScreenshotHelper.WindowHandleInfo handleInfo = new ScreenshotHelper.WindowHandleInfo(GameHandle);
                    var childHandles = handleInfo.GetAllChildHandles();
                    foreach (var child in childHandles)
                    {
                        string childTitle = ScreenshotHelper.GetWindowTitle(child);
                        string childClass = ScreenshotHelper.GetWindowClass(child);
                        if ((CurrentSolution.ClickCaption == null || childTitle == CurrentSolution.ClickCaption) &&
                            (CurrentSolution.ClickClass == null || childClass == CurrentSolution.ClickClass))
                        {
                            ClickHandle = child;
                            break;
                        }
                    }
                    ClickHandle = ScreenshotHelper.FindWindowEx(GameHandle, IntPtr.Zero, CurrentSolution.ClickClass, CurrentSolution.ClickCaption);
                    Rectangle gameRect = ScreenshotHelper.GetWindowRectangle(GameHandle);
                    Rectangle clickRect = ScreenshotHelper.GetWindowRectangleWithShadow(ClickHandle);
                    ClickOffset = new Size(0, clickRect.Height - gameRect.Height);
                }

                string autoClickPath = @".\Solutions\" + Settings.SelectedSolution + @"\AutoClick";
                if (Directory.Exists(autoClickPath))
                {
                    AutoClickImageList.Clear();
                    foreach (var image in Directory.GetFiles(autoClickPath))
                    {
                        if (imgExtensions.Contains(image.Split('.').Last()))
                        {
                            Bitmap tmpImg = new Bitmap(image);
                            Bitmap img = new Bitmap(tmpImg);
                            tmpImg.Dispose();
                            AutoClickImageList.Add(img);
                        }
                    }
                }
                SaveSettings();
            }
            else if(Settings.SelectedSolution != "自定义方案")
            {
                if (Directory.Exists(@".\Solutions\自定义方案") && Directory.Exists(@".\Solutions\自定义方案\AutoClick"))
                {
                    AutoClickImageList.Clear();
                    foreach (var image in Directory.GetFiles(@".\Solutions\自定义方案\AutoClick"))
                    {
                        if (imgExtensions.Contains(image.Split('.').Last()))
                        {
                            Bitmap tmpImg = new Bitmap(image);
                            Bitmap img = new Bitmap(tmpImg);
                            tmpImg.Dispose();
                            AutoClickImageList.Add(img);
                        }
                    }
                }
            }
        }

        private void Mainform_FormClosing(object sender, FormClosingEventArgs e)
        {
            Running = false;
            if (Engine != null)
            {
                Engine.Stop();
            }
            foreach (var book in OpenBookList)
            {
                try
                {
                    book.Value.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }
            }
        }

        private void timer_set_window_hwnd_Tick(object sender, EventArgs e)
        {
            if (MouseHelper.GetAsyncKeyState(Keys.F2))
            {
                GetHandle();
            }
        }

        private void pictureBox_board_MouseDown(object sender, MouseEventArgs e)
        {
            bool MouseOnImage = false;
            Point MousePositionOnImage = new Point(-1, -1);
            double img_ratio = (double)BoardDisplayBitmap.Width / (double)BoardDisplayBitmap.Height;
            double box_ratio = (double)pictureBox_board.Width / (double)pictureBox_board.Height;
            double display_ratio;
            int display_x, display_y, display_width, display_height;
            if (box_ratio >= img_ratio)
            {
                display_y = 0;
                display_height = pictureBox_board.Height;
                display_x = (int)(pictureBox_board.Width - display_height * img_ratio) / 2;
                display_width = (int)(display_height * img_ratio);
                display_ratio = (double)BoardDisplayBitmap.Height / display_height;
            }
            else
            {
                display_x = 0;
                display_width = pictureBox_board.Width;
                display_y = (int)(pictureBox_board.Height - display_width / img_ratio) / 2;
                display_height = (int)(display_width / img_ratio);
                display_ratio = (double)BoardDisplayBitmap.Width / display_width;
            }
            int img_x, img_y;
            bool mouse_on_x, mouse_on_y;
            if (e.X <= display_x)
            {
                img_x = 0;
                mouse_on_x = false;
            }
            else if (e.X >= display_x + display_width)
            {
                img_x = BoardDisplayBitmap.Width - 1;
                mouse_on_x = false;
            }
            else
            {
                img_x = (int)((e.X - display_x) * display_ratio);
                mouse_on_x = true;
            }
            if (e.Y <= display_y)
            {
                img_y = 0;
                mouse_on_y = false;
            }
            else if (e.Y >= display_y + display_height)
            {
                img_y = BoardDisplayBitmap.Height - 1;
                mouse_on_y = false;
            }
            else
            {
                img_y = (int)((e.Y - display_y) * display_ratio);
                mouse_on_y = true;
            }
            if (mouse_on_x && mouse_on_y)
            {
                MouseOnImage = true;
                MousePositionOnImage = new Point(img_x, img_y);
            }
            else
            {
                MouseOnImage = false;
            }
            if (MouseOnImage)
            {
                int width = 40;
                int height = 40;
                int xoffset = width / 2;
                int yoffset = height / 2;
                Point pos = new Point(-1, -1);
                for (int y = 0; y < 10; y++)
                {
                    for (int x = 0; x < 9; x++)
                    {
                        if (MousePositionOnImage.X >= x * width + xoffset && MousePositionOnImage.X <= x * width + xoffset + width &&
                            MousePositionOnImage.Y >= y * height + yoffset && MousePositionOnImage.Y <= y * height + yoffset + height)
                        {
                            pos = new Point(x, y);
                            break;
                        }
                    }
                    if (pos.X != -1) break;
                }
                if (pos.X != -1)
                {
                    Point p = ClickPositionMap[pos.X, pos.Y];
                    ClickAtBitmap(p);
                }
            }
        }

        private void button_redetect_Click(object sender, EventArgs e)
        {
            ResetDetection();
        }

        // 后台思考快捷开关（持久化；不再每次启动强制关闭）
        private void checkBox_ponder_CheckedChanged(object sender, EventArgs e)
        {
            if (settingsUILoading) return;   // 初始化回填时跳过
            Settings.BackgroundAnalysis = checkBox_ponder.Checked;
            SaveSettings();
            DisplayStatus(Settings.BackgroundAnalysis ? "后台思考：开（对手行棋时引擎预计算）" : "后台思考：关");
        }

        // 引擎记录开关（默认关；开启时才写 engine_io.log，供排查「一直思考/不落子」）
        private void checkBox_engine_log_CheckedChanged(object sender, EventArgs e)
        {
            if (settingsUILoading) return;   // 初始化回填时跳过
            Settings.EngineIoLog = checkBox_engine_log.Checked;
            SaveSettings();
            if (Settings.EngineIoLog)
            {
                EngineHelper.IoLog("HB", "记录仪已开启（engine_io.log）");
                DisplayStatus("引擎记录：开（写入 engine_io.log，排查完请关闭）");
            }
            else
            {
                DisplayStatus("引擎记录：关");
            }
        }

        // 线程数快捷调整（同步到当前引擎配置表，重启引擎后自动生效）
        private void numericUpDown_thread_count_ValueChanged(object sender, EventArgs e)
        {
            if (settingsUILoading) return;   // 初始化回填时跳过
            string threadStr = ((int)numericUpDown_thread_count.Value).ToString();
            if (Engine != null)
            {
                Engine.SetOption("Threads", threadStr);
            }
            Settings.ThreadCount = (int)numericUpDown_thread_count.Value;
            if (Settings.CurrentEngine == null || Settings.CurrentEngine.Configs == null)
            {
                SaveSettings();
                return;
            }
            if (Settings.CurrentEngine.Configs.ContainsKey("Threads"))
            {
                Settings.CurrentEngine.Configs["Threads"] = threadStr;
            }
            else
            {
                Settings.CurrentEngine.Configs.Add("Threads", threadStr);
            }
            SaveSettings();
        }

        // Hash 快捷调整（同步到当前引擎配置表，重启引擎后自动生效）
        private void numericUpDown_hash_ValueChanged(object sender, EventArgs e)
        {
            if (settingsUILoading) return;   // 初始化回填时跳过
            string hashStr = ((int)numericUpDown_hash.Value).ToString();
            if (Engine != null)
            {
                Engine.SetOption("Hash", hashStr);
            }
            if (Settings.CurrentEngine == null || Settings.CurrentEngine.Configs == null)
            {
                return;
            }
            if (Settings.CurrentEngine.Configs.ContainsKey("Hash"))
            {
                Settings.CurrentEngine.Configs["Hash"] = hashStr;
            }
            else
            {
                Settings.CurrentEngine.Configs.Add("Hash", hashStr);
            }
            SaveSettings();
        }

        // 刷新 Hash 快捷框（当前引擎配置优先；无配置时显示默认 16）
        void RefreshHashBox()
        {
            decimal hashVal = 16;
            if (Settings.CurrentEngine != null && Settings.CurrentEngine.Configs != null && Settings.CurrentEngine.Configs.ContainsKey("Hash"))
            {
                decimal.TryParse(Settings.CurrentEngine.Configs["Hash"], out hashVal);
            }
            if (hashVal < numericUpDown_hash.Minimum) hashVal = numericUpDown_hash.Minimum;
            if (hashVal > numericUpDown_hash.Maximum) hashVal = numericUpDown_hash.Maximum;
            numericUpDown_hash.Value = hashVal;
        }

        private void button_engine_add_Click(object sender, EventArgs e)
        {
            if (openFileDialog_engine.ShowDialog() == DialogResult.OK)
            {
                string path = openFileDialog_engine.FileName;
                path = path.Replace(Environment.CurrentDirectory, ".");
                if (Settings.EngineList.ContainsKey(path))
                {
                    MessageBox.Show("该引擎已存在");
                    return;
                }
                EngineSettings esettings = new EngineSettings();
                esettings.ExePath = path;
                Settings.EngineList.Add(path, esettings);
                SaveSettings();
                InitSettingsUI();
            }
        }

        private void comboBox_engine_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox_engine.SelectedItem.ToString() != Settings.SelectedEngine)
            {
                Settings.SelectedEngine = comboBox_engine.SelectedItem.ToString();
                if (!Settings.EngineList.ContainsKey(Settings.SelectedEngine) || !File.Exists(Settings.EngineList[Settings.SelectedEngine].ExePath))
                {
                    MessageBox.Show("引擎文件不存在！");
                    return;
                }
                InitEngine();
                RefreshHashBox();   // 切换引擎后刷新 Hash 显示为该引擎的配置值
            }
        }

        private void button_engine_delete_Click(object sender, EventArgs e)
        {
            if (Settings.SelectedEngine.Length > 0)
            {
                if (Engine != null) Engine.Stop();
                Settings.EngineList.Remove(Settings.SelectedEngine);
                if (Settings.EngineList.Count > 0)
                {
                    Settings.SelectedEngine = Settings.EngineList.Keys.First();
                }
                else
                {
                    Settings.SelectedEngine = "";
                }
                SaveSettings();
                InitSettingsUI();
            }
        }

        private void button_get_hwnd_Click(object sender, EventArgs e)
        {
            button_stop_detection_Click(sender, e);
            GetHandle();
        }

        private void numericUpDown_scale_factor_ValueChanged(object sender, EventArgs e)
        {
            Settings.ScaleFactor = 1.0f / (float)numericUpDown_scale_factor.Value;
            SaveSettings();
        }

        private void comboBox_capture_mode_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (settingsUILoading) return;   // 初始化回填时跳过保存
            Settings.UniversalMode = (comboBox_capture_mode.SelectedIndex == 0);
            SaveSettings();
            DisplayStatus(Settings.UniversalMode ? "截图模式：前台（抓屏，窗口不可被遮挡）" : "截图模式：后台（窗口可被遮挡，个别游戏不兼容）");
            RefreshClickPipeline("截图模式");
            LastCaptureSpace = WinCoord.CaptureSpace.None;   // 第二十四轮：抓屏空间随模式改变——作废坐标空间，强制下一帧重建（防切换后旧空间误点）
        }

        private void checkBox_auto_go_CheckedChanged(object sender, EventArgs e)
        {
            Settings.AutoGo = checkBox_auto_go.Checked;
            if (Settings.AutoGo)
            {
                checkBox_analyze_mode.Checked = false;
            }
            SaveSettings();
        }

        private void comboBox_solution_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox_solution.SelectedItem != null)
            {
                Settings.SelectedSolution = comboBox_solution.SelectedItem.ToString();
                SaveSettings();
                LoadCurrentSolution();
            }
            else
            {
                if (comboBox_solution.Text == "自定义方案")
                {
                    Settings.SelectedSolution = "自定义方案";
                    SaveSettings();
                    LoadCurrentSolution();
                }                    
            }
        }

        private void ToolStripMenuItem_copy_fen_Click(object sender, EventArgs e)
        {
            string fen = Utils.BoardToFen(CurrentBoard,RedSide ? "w" : "b",RedSide ? "w" : "b");
            Clipboard.SetText(fen);
            MessageBox.Show("局面: \n" + fen + "\n 已经复制到剪贴板");
        }

        private void checkBox_debug_CheckedChanged(object sender, EventArgs e)
        {
            ScreenDebug = checkBox_debug.Checked;
            if (checkBox_debug.Checked)
            {
                if (DetectEnabled)
                {
                    DetectEnabled = false;
                    ModeDisplay();
                }
            }
        }

        private void checkBox_analyze_mode_CheckedChanged(object sender, EventArgs e)
        {
            if (Engine != null)
            {
                Engine.StopAnalyze();
                Engine.AnalyzeQueue.Clear();
                Engine.SkipList.Clear();
            }
            Settings.AnalyzingMode = checkBox_analyze_mode.Checked;
            if (Settings.AnalyzingMode)
            {
                if (Settings.AutoGo)
                {
                    checkBox_auto_go.Checked = false;
                }
            }
            SaveSettings();
        }

        private void comboBox_yolo_models_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox_yolo_models.SelectedItem == null) return;
            string selectedModel = comboBox_yolo_models.SelectedItem.ToString();
            if (selectedModel.StartsWith("jieqi")) return;   // 揭棋条目由识别模式决定，不作为象棋模型
            Settings.YoloModel = selectedModel;
            if (ModelList.ContainsKey(Settings.YoloModel))
            {
                Model = ModelList[Settings.YoloModel];
            }
            SaveSettings();
        }

        private void button_engine_settings_Click(object sender, EventArgs e)
        {

        }

        private void comboBox_mouse_mode_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (settingsUILoading) return;   // 初始化回填时跳过保存
            Settings.UniversalMouse = (comboBox_mouse_mode.SelectedIndex == 0);
            SaveSettings();
            RefreshClickPipeline("鼠标模式");
            // 第二十五轮：鼠标模式切换同样作废坐标空间（此前只有截图模式作废）——两个空间的
            // 换算公式不同（前台=物理窗口原点，后台=客户区+缩放），沿用旧空间会把点打到别处。
            LastCaptureSpace = WinCoord.CaptureSpace.None;
        }

        // 第二十五轮：窗口句柄失效后的自动重取（前台/后台点击共用）。
        // 顺序：① 按当前方案重取（LoadCurrentSolution）；② 方案为空/失败 → 按上次窗口标题模糊查找。
        // 成功刷新 GameHandle/ClickHandle 并返回 true；失败清零句柄返回 false（调用方必须提示用户，不静默）。
        public bool TryReacquireTargetWindow()
        {
            try
            {
                string prevTitle = _lastGameWindowTitle;
                if (!string.IsNullOrEmpty(Settings.SelectedSolution) && Settings.SelectedSolution != "剪切板")
                {
                    LoadCurrentSolution();
                    if (GameHandle != IntPtr.Zero && WinCoord.IsWindowAlive(GameHandle))
                    {
                        if (!Settings.UniversalMouse && ClickHandle == IntPtr.Zero) ClickHandle = GameHandle;
                        EngineHelper.IoLog("CLICK", "reacquire-by-solution -> " + GameHandle.ToInt64().ToString("X"));
                        return true;
                    }
                }
                if (!string.IsNullOrEmpty(prevTitle))
                {
                    IntPtr found = ScreenshotHelper.FindWindowByTitleFuzzy(prevTitle);
                    if (found != IntPtr.Zero)
                    {
                        GameHandle = found;
                        ClickHandle = found;
                        try { _lastGameWindowTitle = ScreenshotHelper.GetWindowTextSafe(found); } catch { }
                        EngineHelper.IoLog("CLICK", "reacquire-by-title -> " + found.ToInt64().ToString("X") + " title=" + prevTitle);
                        return true;
                    }
                }
            }
            catch (Exception ex) { try { EngineHelper.IoLog("CLICK", "reacquire-ex " + ex.Message); } catch { } }
            GameHandle = IntPtr.Zero;
            ClickHandle = IntPtr.Zero;
            return false;
        }

        // 第二十四轮：点击链路切换收口——复核句柄有效性、后台点击句柄缺失时退回主窗口，
        // 并把状态明确写到状态栏（坐标空间由下一帧截图自动重建：LastCaptureSpace/ClickPositionMap
        // 均在每帧抓屏与棋盘检测中更新）。前台/后台来回切换后点不中的问题由此有迹可循。
        void RefreshClickPipeline(string what)
        {
            // R38：切换截图/鼠标模式视为人工干预 → 清除「落子未确认暂停」
            bool wasSuspended = ClickingSuspended;
            SelfHealStreak = 0;
            ClickingSuspended = false;
            // R42：模式切换给后台一次机会，复位自动降级标志
            _autoDegradedToForeground = false;
            ForceForegroundClickOnce = false;
            try
            {
                if (GameHandle != IntPtr.Zero && !WinCoord.IsWindowAlive(GameHandle)) GameHandle = IntPtr.Zero;
                if (ClickHandle != IntPtr.Zero && !WinCoord.IsWindowAlive(ClickHandle)) ClickHandle = IntPtr.Zero;
                if (GameHandle == IntPtr.Zero && !string.IsNullOrEmpty(Settings.SelectedSolution) && Settings.SelectedSolution != "剪切板")
                {
                    LoadCurrentSolution();   // 方案存在：按方案重取窗口
                }
                if (!Settings.UniversalMouse && ClickHandle == IntPtr.Zero && GameHandle != IntPtr.Zero)
                {
                    ClickHandle = GameHandle;   // 后台点击退回主窗口句柄，避免静默失效
                }
            }
            catch { }
            string front = Settings.UniversalMouse ? "前台（真实光标点击，会占用鼠标）" : "后台（发送消息点击，不占用鼠标）";
            string handles = "窗口句柄=" + (GameHandle == IntPtr.Zero ? "无（请按“寻找新连线窗口”重取）" : "有效")
                + (Settings.UniversalMouse ? "" : "，点击句柄=" + (ClickHandle == IntPtr.Zero ? "无" : "有效"));
            DisplayStatus(what + "：" + front + "；" + handles + "；坐标将在下一帧自动校准");
            EngineHelper.IoLog("CLICKMODE", (Settings.UniversalMouse ? "front" : "rear") +
                " GameHandle=" + GameHandle.ToInt64().ToString("X") + " ClickHandle=" + ClickHandle.ToInt64().ToString("X"));
            // R38：暂停期间切换模式 → 视为用户选用新方式重试，立即按当前分析盘补发一次
            if (wasSuspended)
            {
                EngineHelper.IoLog("WATCH", "切换模式解除暂停，尝试重新出招");
                TryReissueAfterClickFail();
            }
        }

        private void button_screenshot_Click(object sender, EventArgs e)
        {
            Bitmap screen = Screenshot();
            if (!Directory.Exists(@".\Solutions\" + Settings.SelectedSolution))
            {
                Directory.CreateDirectory(@".\Solutions\" + Settings.SelectedSolution);
            }
            ImageEditForm editForm = new ImageEditForm(screen, DateTime.Now.ToString("yyyyMMddHHmmss"), @".\Solutions\" + Settings.SelectedSolution + @"\AutoClick");
            editForm.StartPosition = FormStartPosition.CenterParent;
            editForm.Text = "方案 " + Settings.SelectedSolution + " 自动点击图片管理";
            editForm.ShowDialog();
            InitSolutions();
        }

        private void checkBox_auto_click_CheckedChanged(object sender, EventArgs e)
        {
            Settings.AutoClick = checkBox_auto_click.Checked;
            SaveSettings();
        }

        private void checkBox_stop_when_mate_CheckedChanged(object sender, EventArgs e)
        {
            Settings.StopWhenMate = checkBox_stop_when_mate.Checked;
            SaveSettings();
        }

        private void button_go_immediately_Click(object sender, EventArgs e)
        {
            Engine.StopAnalyze();
        }

        private void button_openbook_settings_Click(object sender, EventArgs e)
        {

        }

        private void button_save_as_solution_Click(object sender, EventArgs e)
        {
            SolutionSavingForm ssf = new SolutionSavingForm();
            ssf.StartPosition = FormStartPosition.CenterParent;
            ssf.ShowDialog();
            InitSolutions();
        }

        private void ToolStripMenuItem_about_Click(object sender, EventArgs e)
        {
            AboutForm abf = new AboutForm();
            abf.StartPosition = FormStartPosition.CenterParent;
            abf.Show();
        }

        private void button_start_from_self_Click(object sender, EventArgs e)
        {
            StartDetection(false);
        }

        private void button_start_from_oppo_Click(object sender, EventArgs e)
        {
            StartDetection(true);
        }

        private void button_stop_detection_Click(object sender, EventArgs e)
        {
            DiscardTrendPoint();   // 停止连线：放弃尚未确认的趋势点
            DetectEnabled = false;
            checkBox_debug.Checked = true;
            if (Engine != null)
            {
                Engine.Stop();
            }
            ModeDisplay();
        }

        private void numericUpDown_stop_score_ValueChanged(object sender, EventArgs e)
        {
            Settings.StopScore = (int)numericUpDown_stop_score.Value;
            SaveSettings();
        }

        private void button_advance_settings_Click(object sender, EventArgs e)
        {
            DetectionSettingsForm detectionSettingsForm = new DetectionSettingsForm();
            detectionSettingsForm.StartPosition = FormStartPosition.CenterParent;
            detectionSettingsForm.ShowDialog();
        }

        // 「截图与鼠标」设置弹窗：截图重试 / 点击时序 / 后台点击序列
        private void button_capture_mouse_settings_Click(object sender, EventArgs e)
        {
            MouseCaptureSettingsForm form = new MouseCaptureSettingsForm();
            form.StartPosition = FormStartPosition.CenterParent;
            form.ShowDialog();
        }

        // 「时间设置」弹窗：思考时间 / 线程数 / 思考深度 / Hash
        private void button_time_settings_Click(object sender, EventArgs e)
        {
            TimeSettingsForm form = new TimeSettingsForm();
            form.StartPosition = FormStartPosition.CenterParent;
            form.ShowDialog();
            // 关闭后同步主界面显示（弹窗里的思考时间/线程数/思考深度/Hash 已即时保存）
            RefreshHashBox();   // Hash 也在弹窗里调整过：同步回主界面快捷框
        }

        private void pictureBox_show_result_Click(object sender, EventArgs e)
        {
            if (pictureBox_show_result.Image != null)
            {
                ImageDisplayForm imageDisplayForm = new ImageDisplayForm(pictureBox_show_result.Image);
                imageDisplayForm.Show();
            }
        }

        private void button_clipboard_solution_Click(object sender, EventArgs e)
        {
            if (SolutionList.ContainsKey("剪切板"))
            {
                Settings.SelectedSolution = "剪切板";
            }
            else
            {
                SolutionList.Add("剪切板", new Solution("", "", "", ""));
                Settings.SelectedSolution = "剪切板";
            }
        }

        private void ToolStripMenuItem_openbook_management_Click(object sender, EventArgs e)
        {
            OpenBookSettingsForm opsf = new OpenBookSettingsForm();
            opsf.StartPosition = FormStartPosition.CenterParent;
            opsf.ShowDialog();
        }

        private void ToolStripMenuItem_engine_management_Click(object sender, EventArgs e)
        {
            EngineManageForm engineManageForm = new EngineManageForm();
            engineManageForm.ShowDialog();
            if (!Settings.EngineList.ContainsKey(Settings.SelectedEngine))
            {
                if (Settings.EngineList.Count > 0)
                {
                    Settings.SelectedEngine = Settings.EngineList.Keys.ToList()[0];
                    InitEngine();
                }
                else
                {
                    Settings.SelectedEngine = "";
                }
                SaveSettings();
            }
            InitSettingsUI();
        }
    }
}
