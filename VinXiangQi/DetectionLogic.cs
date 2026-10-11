using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using Yolov5Net.Scorer;
using System.Threading;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace VinXiangQi
{
    partial class Mainform
    {
        // 是否为调试截图状态
        public static bool ScreenDebug = true;
        // 是否为红方
        public static bool RedSide = true;
        // 最近一次识别的棋盘，可能不合法 / 处于动画状态
        public static string[,] PendingBoard = new string[9, 10];
        // 最近一次"校验通过"的检测帧（仅用于暗子颜色记忆，避免非法帧把错误颜色带入下一帧）
        public static string[,] LastValidPendingBoard = null;
        // jieqi_invalid.log 去重/限流：同一非法局面不重复记录；校正类消息每 3 秒最多一条
        public static string LastInvalidLoggedKey = "";
        static DateTime LastCorrectionLogTime = DateTime.MinValue;
        // 被拒帧截图 dump：每 5 秒最多 1 张、整程最多 20 张（供实机排查定位）
        static DateTime LastInvalidFrameDumpTime = DateTime.MinValue;
        static int InvalidFrameDumpCount = 0;
        // 上一个稳定棋盘
        public static string[,] LastBoard = new string[9, 10];
        // 储存当前引擎正在计算的棋盘
        public static string[,] EngineAnalyzingBoard = new string[9, 10];
        // 当前已经被确认的棋盘
        public static string[,] CurrentBoard = new string[9, 10];
        // 交叉验证裁定来源图（第十六轮）：每格由哪个模型裁定 —— 识别画面分色绘制以此为准。
        // 由 MergeDualModelGrids 整图原子替换，渲染时只读，不会读到半新半旧。
        public Utils.DetectionAuthority[,] DualAuthorityGrid = null;

        // 自己走棋后的预期状态，遇到该状态不触发引擎计算
        public static string[,] ExpectedSelfGoBoard = new string[9, 10];
        public static Dictionary<string, string[,]> ExpectedBoardMap = new Dictionary<string, string[,]>();
        // 用于后台计算的预期棋盘
        public static string[,] PonderBoard = new string[9, 10];
        public static List<string> BestMoveList = new List<string>();
        // 图片上方形的棋盘区域 x, y, width, height 包括棋盘的框框 一个扩展的范围
        Rectangle BoardArea = new Rectangle(-1, -1, -1, -1);
        // Yolo模型识别出的棋盘区域 x, y, width, height，因为可能送进模型的已经是裁切过的棋盘区域，所以该数值需要被还原为上方的BoardArea
        Rectangle BoardAreaRaw = new Rectangle(-1, -1, -1, -1);
        // 上一次截图时，棋盘显示区域的图片，用于对比图片来判断是否处在动画中
        Bitmap LastBoardAreaBitmap = null;
        // 当失败次数过多时，自动重载
        int InvalidCount = 0;
        // 是否第一次收到绝杀
        bool FirstMate = true;
        Color NormalColor = Color.Black;
        Color PositiveColor = Color.DarkBlue;
        Color NegativeColor = Color.DarkRed;
        Color MateColor = Color.Green;

        public string ExpectedMove = "";
        public bool ChangeDetectedAfterClick = false;
        public bool TurnToOpponent = false;
        public bool BackgroundAnalyzing = false;
        // R23：后台思考（ponder）期间引擎提前返回的着法暂存（等「命中」时直接采用；未命中作废）
        public string PonderReadyMove = "";
        public string PonderReadyPonder = "";
        public string PonderReadyFen = "";
        // R26：上一次暗子着色使用的视角（RedSide）——换边后旧颜色记忆作废，防生成「暗子归属矛盾」毒盘
        bool _lastColoringRedSide = true;

        // MultiPV 候选线快照（第二十轮）：Engine_InfoEvent 收集（每个新批次整体重建），
        // RenderDisplayBoard 只读消费；整表重建，渲染线程不会读到半新半旧。
        public class MultiPvLine
        {
            public int Rank;          // 1 = 主线（走主箭头），>=2 = 候选线（细箭头 + 序号徽标）
            public Point From, To;    // 着法坐标（已按视角转换）
            public string ScoreText;  // 该线评分（如 "36" 或 "绝杀 (3)"）
        }
        static List<MultiPvLine> _multiPvLines = null;
        public static List<MultiPvLine> MultiPvSnapshot { get { return _multiPvLines; } }
        public static void MultiPvClear() { _multiPvLines = null; }
        static void MultiPvPut(int rank, string moveStr, string scoreText)
        {
            try
            {
                if (string.IsNullOrEmpty(moveStr) || moveStr.Length < 4) return;
                Point fp = Utils.Move2Point(moveStr.Substring(0, 2), RedSide);
                Point tp = Utils.Move2Point(moveStr.Substring(2, 2), RedSide);
                if (fp.X < 0 || fp.X > 8 || fp.Y < 0 || fp.Y > 9 ||
                    tp.X < 0 || tp.X > 8 || tp.Y < 0 || tp.Y > 9) return;
                // 整表拷贝替换：渲染线程只读到完整快照，不会读到半新半旧；按 rank 排序（乱序到达也稳定）
                var cur = _multiPvLines;
                var nl = new List<MultiPvLine>(cur != null ? cur : new List<MultiPvLine>());
                nl.RemoveAll(x => x != null && x.Rank == rank);
                nl.Add(new MultiPvLine() { Rank = rank, From = fp, To = tp, ScoreText = scoreText });
                nl.Sort((a, b) => a.Rank.CompareTo(b.Rank));
                _multiPvLines = nl;
            }
            catch { }
        }

        public XiangQiGame Game = new XiangQiGame();

        //Dictionary<string, Bitmap> TemplateList = new Dictionary<string, Bitmap>();
        //public bool TemplateInitiated = false;

        public enum ResultSource
        {
            Openbook,
            ChessDB,
            Engine
        }

        void AutoClickLoop()
        {
            while (Running)
            {
                try
                {
                    if (!Settings.AutoClick)
                    {
                        Thread.Sleep(500);
                        continue;
                    }
                    Bitmap screen = Screenshot();
                    foreach (Bitmap image in AutoClickImageList.ToArray())
                    {
                        Point pos = ImageHelper.FindImageFromTop(screen, image);
                        if (pos.X != -1)
                        {
                            Point clickPos = new Point(pos.X + image.Width / 2, pos.Y + image.Height / 2);
                            ClickAtBitmap(clickPos);
                            Thread.Sleep(1000);
                        }
                    }
                }
                catch(Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                    File.AppendAllText("err.log", DateTime.Now.ToString() + "\r\n" + ex.ToString() + "\r\n\r\n");
                }
                Thread.Sleep(2000);
            }
        }

        void DetectionLoop()
        {
            int gcCount = 0;
            int getHandleCount = 0;
            int invalidCount = 0;
            bool modelChanged = false;
            while (Running)
            {
                DateTime startTime = DateTime.Now;
                try
                {
                    gcCount = (gcCount + 1) % 20;
                    if (EngineHelper.IsIoLogEnabled() && (DateTime.Now - _lastHeartbeat).TotalSeconds >= 10)
                    {
                        _lastHeartbeat = DateTime.Now;
                        try { EngineHelper.IoLog("HB", "det=" + DetectEnabled + " bg=" + BackgroundAnalyzing + " cnt=" + EngineAnalyzeCount + " susp=" + (ClickingSuspended ? 1 : 0) + " area=" + BoardArea.X + " handle=" + GameHandle.ToInt64().ToString("X")); } catch { }
                    }
                    if (_lastGameWindowTitle == "" && GameHandle != IntPtr.Zero)
                    {
                        try { _lastGameWindowTitle = ScreenshotHelper.GetWindowTextSafe(GameHandle); } catch { }
                    }
                    RecoverEngineMidThink();   // R22：巡检移到循环顶部——识别不到棋盘时也会接管（引擎已死则重启并按当前盘补发）
                    BlacklistRetryCheck();     // R42：崩溃黑名单到期后的延迟补发
                    ForceStopStuckSearch();    // R22：普通搜索超时未返回 bestmove → 强制 stop（防「一直思考」）
                    PonderTimeoutCheck();      // R22：后台思考超时未复位 → 主动复位（防卡在等待 ponder）
                    if (gcCount == 0) GC.Collect();
                    if (GameHandle == IntPtr.Zero && Settings.SelectedSolution != "剪切板")
                    {
                        ModeDisplay("未找到窗体");
                        if (BackgroundAnalyzing && Engine != null)
                        {
                            try { if (PonderBoard != null) Engine.PonderMiss(Utils.BoardToFen(PonderBoard, RedSide)); else Engine.StopAnalyze(); } catch { }
                            BackgroundAnalyzing = false;
                            PonderReadyMove = "";   // R23：一并作废暂存
                            PonderReadyPonder = "";
                            PonderReadyFen = "";
                            EngineHelper.IoLog("WATCH", "未找到目标窗口，后台思考已复位");
                        }
                        if (Settings.SelectedSolution != "")
                        {
                            getHandleCount++;
                            if (getHandleCount % 10 == 0)
                            {
                                getHandleCount = 0;
                                LoadCurrentSolution();
                            }
                        }
                        Thread.Sleep(100);
                        continue;
                    }
                    else
                    {
                        if (DetectEnabled)
                        {
                            ModeDisplay("识别中");
                        }
                        else
                        {
                            ModeDisplay("空闲");
                        }
                    }
                    if (checkBox_debug.Checked)
                    {
                        SafeInvoke(new Action(() =>
                        {
                            pictureBox_show_result.Image = Screenshot();
                            pictureBox_show_result.Refresh();
                        }));
                        Thread.Sleep(200);
                        continue;
                    }
                    // 引擎计算期间持续识别（T5b）：不再暂停检测；
                    // 确认出现新局面时由 ReloadBoard 以最新局面重新开始思考。
                   Bitmap screenshot = Screenshot();
                    Size maxSize = screenshot.Size;
                    if (LastBoardAreaBitmap != null && BoardArea.X != -1)
                    {
                        Bitmap currentBoard = (Bitmap)ImageHelper.CropImage(screenshot, BoardArea);
                        DisplayStatus("检测图像");
                        bool result = ModelDetectBoard(currentBoard, false);
                        screenshot.Dispose();   // 本帧棋盘区已裁出，原始截图不再需要
                        if (result)
                        {
                            if (LastBoardAreaBitmap != null) LastBoardAreaBitmap.Dispose();
                            LastBoardAreaBitmap = currentBoard;
                            string invalidReason;
                            bool checkValid = Utils.CheckBoardValid(PendingBoard, RedSide, out invalidReason);
                            if (!checkValid)
                            {
                                DisplayStatus("当前棋盘不合法（" + invalidReason + "），可能是出现了识别错误！");
                                if (Settings.JieqiMode)
                                {
                                    // 揭棋误判排查日志：记录被拒局面与原因（同一局面去重，避免重复帧刷屏）
                                    try
                                    {
                                        string logFen = Utils.BoardToFen(PendingBoard, RedSide ? "w" : "b", RedSide ? "w" : "b");
                                        string logKey = invalidReason + "|" + logFen;
                                        if (logKey != LastInvalidLoggedKey)
                                        {
                                            LastInvalidLoggedKey = logKey;
                                            int rDark = 0, bDark = 0;
                                            for (int ly = 0; ly < 10; ly++)
                                            {
                                                for (int lx = 0; lx < 9; lx++)
                                                {
                                                    string lc = PendingBoard[lx, ly];
                                                    if (lc == "r_anzi") rDark++;
                                                    else if (lc == "b_anzi") bDark++;
                                                }
                                            }
                                            File.AppendAllText("jieqi_invalid.log",
                                                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  原因: " + invalidReason
                                                + "  RedSide=" + RedSide + "  暗子r=" + rDark + " 暗子b=" + bDark + "\r\n"
                                                + "  局面: " + logFen + "\r\n\r\n");
                                        }
                                    }
                                    catch (Exception logEx)
                                    {
                                        Debug.WriteLine("jieqi_invalid.log: " + logEx.Message);
                                    }
                                }
                                // 被拒帧截图 dump（供实机排查：看画面即可定位哪格识别错；每 5 秒最多 1 张、整程最多 20 张）
                                try
                                {
                                    if ((DateTime.Now - LastInvalidFrameDumpTime).TotalSeconds >= 5 && InvalidFrameDumpCount < 20 && currentBoard != null)
                                    {
                                        LastInvalidFrameDumpTime = DateTime.Now;
                                        InvalidFrameDumpCount++;
                                        string dumpDir = "jieqi_invalid_frames";
                                        if (!Directory.Exists(dumpDir)) Directory.CreateDirectory(dumpDir);
                                        currentBoard.Save(Path.Combine(dumpDir, "invalid_" + DateTime.Now.ToString("HHmmss_fff") + ".png"), System.Drawing.Imaging.ImageFormat.Png);
                                    }
                                }
                                catch (Exception dumpEx)
                                {
                                    Debug.WriteLine("invalid frame dump: " + dumpEx.Message);
                                }
                                invalidCount++;
                                int invalidResetLimit = Settings.InvalidResetCount;
                                if (invalidResetLimit < 2) invalidResetLimit = 2;
                                if (invalidResetLimit > 20) invalidResetLimit = 20;
                                if (invalidCount >= invalidResetLimit)
                                {
                                    if (Settings.JieqiMode)
                                    {
                                        // 揭棋模式使用独立检测器，不做象棋模型轮换
                                        DisplayStatus("连续 " + invalidCount + " 次不合法，重置识别范围");
                                    }
                                    else if (ModelList.Count > 1 && !modelChanged)
                                    {
                                        foreach (var model in ModelList)
                                        {
                                            if (model.Key != Settings.YoloModel)
                                            {
                                                Model = model.Value;
                                                modelChanged = true;
                                                DisplayStatus("尝试切换模型识别");
                                                break;
                                            }
                                        }
                                    }
                                    invalidCount = 0;
                                    ResetDetection();
                                }
                                Thread.Sleep(300);
                                continue;
                            }
                            invalidCount = 0;
                            LastValidPendingBoard = (string[,])PendingBoard.Clone();   // 校验通过：可作为暗子颜色记忆源
                            // 第十八轮：打断重算只在局面变化被确认后进行（ReloadBoard：切盘→停旧思考→按最新确认局面重算）；
                            // 未确认帧（动画/识别抖动）一律不打断——引擎被停后确认门看不到变化永不重启，表现为一直思考不点击。
                            bool checkResult = CheckNewBoard();
                            if (checkResult)
                            {
                                if (ClickingSuspended)
                                {
                                    // R39：仅「暂停后局面变化」时恢复并清计数；未暂停时不动计数（交给时间窗归零）。
                                    ClickingSuspended = false;
                                    SelfHealStreak = 0;
                                    _lastSelfHealTime = DateTime.MinValue;
                                    EngineHelper.IoLog("WATCH", "局面已变化，恢复自动出招");
                                }
                                // R42：局面变化 → 清除黑名单延迟补发计数
                                _blacklistRetryAt = DateTime.MinValue;
                                _blacklistRetryCount = 0;
                                if (modelChanged)
                                {
                                    if (ModelList.ContainsKey(Settings.YoloModel))
                                    {
                                        Model = ModelList[Settings.YoloModel];
                                    }
                                    modelChanged = false;
                                }
                                ReloadBoard();
                            }
                        }
                        else
                        {
                            currentBoard.Dispose();
                            BoardArea = new Rectangle(-1, -1, -1, -1);
                            Thread.Sleep(200);
                            continue;
                        }
                    }
                    else
                    {
                        DisplayStatus("识别不到棋盘，重置图像范围");
                        bool result = ModelDetectBoard(screenshot, true);
                        if (result)
                        {
                            BoardArea = Utils.ExpendArea(Utils.RestoreArea(BoardArea, BoardAreaRaw), maxSize);
                            if (LastBoardAreaBitmap != null) LastBoardAreaBitmap.Dispose();
                            LastBoardAreaBitmap = (Bitmap)ImageHelper.CropImage(screenshot, BoardArea);
                            DisplayStatus("更新范围成功");
                        }
                        screenshot.Dispose();
                    }

                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                    File.AppendAllText("err.log", DateTime.Now.ToString() + "\r\n" + ex.ToString() + "\r\n\r\n");
                }
                DateTime endTime = DateTime.Now;
                double miliSec = (endTime - startTime).TotalMilliseconds;
                Debug.WriteLine($"总耗时: {miliSec}ms");
                if (miliSec < Settings.DetectionInterval)
                {
                    Thread.Sleep((int)(Settings.DetectionInterval - miliSec));
                }
            }
        }

        public void RenderDisplayBoard(string[,] renderBoard=null)
        {
            if (renderBoard == null) renderBoard = CurrentBoard;
            if (renderBoard == null) return;
            SafeInvoke(new Action(() =>
            {
                int width = 40;
                int height = 40;
                int xoffset = width / 2;
                int yoffset = height / 2;
                BoardGDI.Clear(Color.White);
                BoardGDI.DrawImage(Properties.Resources.board, 0, 0, width * 10, height * 11);
                // draw chess
                for (int y = 0; y < 10; y++)
                {
                    for (int x = 0; x < 9; x++)
                    {
                        if (renderBoard[x, y] != null && renderBoard[x, y] != "")
                        {
                            string name = renderBoard[x, y];
                            BoardGDI.DrawImage((Bitmap)Properties.Resources.ResourceManager.GetObject(renderBoard[x, y]), x * width + xoffset, y * height + yoffset, width, height);
                        }
                    }
                }
                // draw hint（Pen 统一 finally 释放，避免每次渲染泄漏 GDI 句柄）
                Pen rp = new Pen(Color.FromArgb(180, Color.OrangeRed), 10);
                Pen bp = new Pen(Color.FromArgb(180, Color.Blue), 10);
                Pen rp_dim = new Pen(Color.FromArgb(180, Color.DarkOrange), 8);
                Pen bp_dim = new Pen(Color.FromArgb(180, Color.DarkGreen), 8);
                try
                {
                rp.EndCap = LineCap.ArrowAnchor;
                bp.EndCap = LineCap.ArrowAnchor;
                rp_dim.EndCap = LineCap.ArrowAnchor;
                bp_dim.EndCap = LineCap.ArrowAnchor;
                // 箭头数量 = 引擎 MultiPV 设置（第二十轮）：N=设置值时画 N 支箭头（主线 1 + 候选 2..N），
                // 总数>1 时在箭头中点标序号；颜色按「走这步棋的一方」：我方=本方色（红方橙红/黑方蓝）、
                // 对方=对方色；后台思考阶段画的是对方应招（改用背景分析着法）。
                var mpvLines = MultiPvSnapshot;
                int mpvSet = 1;
                try { mpvSet = Engine.GetMultiPvSetting(); } catch { }
                bool oppThinking = BackgroundAnalyzing;
                Point mainFrom = oppThinking ? BackgroundAnalysisMove.From : BestMove.From;
                Point mainTo = oppThinking ? BackgroundAnalysisMove.To : BestMove.To;
                bool hasMain = mainFrom.X != -1;
                // 第二十五轮：引擎未返回候选时，若设置为 N>1，主线仍按带序号绘制（避免掉回旧的无数字箭头）
                var cand = new List<MultiPvLine>();
                if (mpvSet > 1 && mpvLines != null)
                {
                    foreach (var ml in mpvLines)
                    {
                        if (ml == null || ml.Rank <= 1) continue;
                        if (ml.From.X < 0 || ml.From.X > 8 || ml.From.Y < 0 || ml.From.Y > 9 ||
                            ml.To.X < 0 || ml.To.X > 8 || ml.To.Y < 0 || ml.To.Y > 9) continue;
                        cand.Add(ml);
                        if (cand.Count >= mpvSet - 1) break;
                    }
                }
                // 第二十五轮：序号条件看「MultiPV 设置」而非「本帧收到几支」——后者会先在主线帧画一支
                // 无数字箭头，候选行到达后再补数字，两套并存即用户看到的「没改之前的箭头」。
                bool withNums = mpvSet > 1;
                bool mainIsRed = oppThinking ? !RedSide : RedSide;
                if (hasMain)
                {
                    Point mf = new Point(mainFrom.X * width + xoffset + width / 2, mainFrom.Y * height + yoffset + height / 2);
                    Point mt = new Point(mainTo.X * width + xoffset + width / 2, mainTo.Y * height + yoffset + height / 2);
                    BoardGDI.DrawLine(mainIsRed ? rp : bp, mf, mt);
                    if (withNums) DrawArrowBadge("1", mf, mt);   // 第二十五轮：MultiPV>1 时主线恒带序号 1
                }
                foreach (var ml in cand)
                {
                    Point cf = new Point(ml.From.X * width + xoffset + width / 2, ml.From.Y * height + yoffset + height / 2);
                    Point ct = new Point(ml.To.X * width + xoffset + width / 2, ml.To.Y * height + yoffset + height / 2);
                    BoardGDI.DrawLine(mainIsRed ? rp_dim : bp_dim, cf, ct);
                    if (withNums) DrawArrowBadge(ml.Rank.ToString(), cf, ct);
                }
                }
                finally
                {
                    rp.Dispose();
                    bp.Dispose();
                    rp_dim.Dispose();
                    bp_dim.Dispose();
                }
                pictureBox_board.Refresh();
            }));
        }

        // 箭头序号（第二十一轮）：无背景、细字直接绘制（原白底圆会遮挡棋盘）；字体静态复用，避免每帧 new Font。
        static readonly Font _mpvBadgeFont = new Font("Arial", 9, FontStyle.Regular);
        void DrawArrowBadge(string tag, Point from, Point to)
        {
            try
            {
                float mx = (from.X + to.X) / 2f, my = (from.Y + to.Y) / 2f;
                SizeF tsz = BoardGDI.MeasureString(tag, _mpvBadgeFont);
                BoardGDI.DrawString(tag, _mpvBadgeFont, Brushes.Black, mx - tsz.Width / 2, my - tsz.Height / 2);
            }
            catch { }
        }

        // 引擎 info 触发的重绘节流（第二十一轮）：MultiPV 开启时 info 每秒几十条，逐条重绘会把
        // UI 线程打满（卡顿根因之一）；限流到 ≥150ms 一次。bestmove 的绘制不受限。
        DateTime _lastEngineRenderTime = DateTime.MinValue;
        string _lastInfoLogKey = "";
        string _lastCandLogKey = "";
        void RenderBoardFromEngineInfo()
        {
            if ((DateTime.Now - _lastEngineRenderTime).TotalMilliseconds < 150) return;
            _lastEngineRenderTime = DateTime.Now;
            RenderDisplayBoard();
        }

        // 引擎中途崩溃恢复（第二十一轮补）：思考计数>0 但进程已死（崩在出招前、无 bestmove 可丢）时，
        // 识别线程每 2 秒尝试：重启引擎并按当前分析盘补发分析——此前该路径无人接管，
        // 表现为引擎崩溃后永远空闲、不再自动走棋。
        DateTime _lastHeartbeat = DateTime.MinValue;   // R22：状态心跳（10s 一行写入 engine_io.log）
        DateTime _lastRecoverAttempt = DateTime.MinValue;
        // R42：崩溃黑名单到期后的延迟补发（防「拉黑→整局哑火」）
        DateTime _blacklistRetryAt = DateTime.MinValue;
        int _blacklistRetryCount = 0;
        DateTime _forceStopAt = DateTime.MinValue;   // R23：强停后观察窗口（6 秒仍无响应 → 判定僵死并重启）
        void RecoverEngineMidThink()
        {
            try
            {
                if (Engine == null || EngineAnalyzeCount <= 0) return;          // 没在思考：无需接管
                if (BackgroundAnalyzing) return;                                // ponder 断线由下一次确认的 PonderMiss 路径自愈
                if (!Engine.EngineDead && !Engine.SafeHasExited()) return;                                 // 进程仍可用：正常思考中（未触发 Exited 的隐性退出亦覆盖）
                if ((DateTime.Now - _lastRecoverAttempt).TotalSeconds < 2) return;  // 限频 2s
                _lastRecoverAttempt = DateTime.Now;
                DisplayStatus("引擎已崩溃，正在恢复…");
                if (!Engine.EnsureAlive())
                {
                    // R26b：重启限流未放行——归还本次计数，避免残留「正在思考」使看门狗空转
                    try { EngineAnalyzeCount--; if (EngineAnalyzeCount < 0) EngineAnalyzeCount = 0; } catch { }
                    // R43：限流期内无人再触发恢复——安排延迟巡检（到期由 BlacklistRetryCheck 重试重启与补发）
                    if (_blacklistRetryCount < 3) _blacklistRetryAt = DateTime.Now.AddSeconds(30);
                    return;
                }
                if (Engine.AnalyzeQueue.Count > 0) { try { EngineAnalyzeCount--; if (EngineAnalyzeCount < 0) EngineAnalyzeCount = 0; } catch { } return; }
                // R26b：以下早退路径同样归还计数（此前只归还黑名单路径，其余路径残留计数）
                if (EngineAnalyzingBoard == null) { try { EngineAnalyzeCount--; if (EngineAnalyzeCount < 0) EngineAnalyzeCount = 0; } catch { } return; }
                string recGuard;
                if (!Utils.IsFenSafeForJieqiEngine(EngineAnalyzingBoard, out recGuard)) { try { EngineAnalyzeCount--; if (EngineAnalyzeCount < 0) EngineAnalyzeCount = 0; } catch { } return; }
                string recFen = Utils.BoardToFen(EngineAnalyzingBoard, RedSide ? "w" : "b", RedSide ? "w" : "b");
                // R26：曾使引擎崩溃的局面不再重发（切断「崩→重启→重发同帧→再崩」死循环）
                if (EngineHelper.IsBoardBlacklisted(recFen))
                {
                    DisplayStatus("该局面曾使引擎崩溃，已跳过重发（稍后自动重试）");
                    try { EngineAnalyzeCount--; if (EngineAnalyzeCount < 0) EngineAnalyzeCount = 0; } catch { }
                    try { File.AppendAllText("err.log", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                        + "  [引擎] 恢复补发跳过：该局面在崩溃黑名单 FEN=" + recFen + "\r\n"); } catch { }
                    // R42：安排延迟补发——黑名单到期后自动重试同一局面（≤3 次）
                    if (_blacklistRetryCount < 3) _blacklistRetryAt = DateTime.Now.AddSeconds(30);
                    return;
                }
                // R26b：不预加计数——StartAnalyze 成功时才由调用方记账，避免 Engine 不可用（抛异常）时计数虚高
                try { StartAnalyze(recFen); }
                catch (Exception rsx)
                {
                    try { System.Diagnostics.Debug.WriteLine("RecoverEngineMidThink.StartAnalyze: " + rsx.Message); } catch { }
                    return;
                }
                try { File.AppendAllText("err.log", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  [引擎] 崩溃后已自动恢复并补发分析\r\n"); } catch { }
                DisplayStatus("已按当前局面恢复引擎计算（引擎曾崩溃）");
            }
            catch (Exception rex) { Debug.WriteLine("RecoverEngineMidThink: " + rex.Message); }
        }

        // R22：普通搜索超时兜底——movetime 模式下正常几秒内必回 bestmove；超过阈值仍未回，
        // 主动 stop，防止界面卡在「一直思考」。（纯深度模式不做强停；后台思考由 PonderTimeoutCheck 管）
        // R23：强停后 6 秒仍无响应（stop 都打不断 = 引擎搜索线程僵死）→ 杀进程，交由 RecoverEngineMidThink 重启补发。
        void ForceStopStuckSearch()
        {
            try
            {
                if (Engine == null || EngineAnalyzeCount <= 0) { _forceStopAt = DateTime.MinValue; return; }
                if (BackgroundAnalyzing) return;
                if (Engine.EngineDead || Engine.SafeHasExited()) { _forceStopAt = DateTime.MinValue; return; }   // 崩溃路径由 RecoverEngineMidThink 处理
                if (_forceStopAt != DateTime.MinValue)
                {
                    if ((DateTime.Now - _forceStopAt).TotalSeconds >= 6)
                    {
                        _forceStopAt = DateTime.MinValue;
                        EngineHelper.IoLog("WATCH", "强停 6 秒后仍无响应，判定引擎僵死：强制重启（cnt=" + EngineAnalyzeCount + "）");
                        DisplayStatus("引擎无响应，正在强制重启…");
                        try { if (Engine.Engine != null) Engine.Engine.Kill(); } catch { }
                        Engine.EngineDead = true;   // 后续帧由 RecoverEngineMidThink 重启引擎并按当前盘补发
                    }
                    return;
                }
                if (Engine.LastGoTime == DateTime.MinValue) return;
                double cap; int dcap; bool dOnly;
                EngineHelper.ResolveGoParams(Settings, true, out cap, out dcap, out dOnly);
                if (dOnly) return;   // 纯深度模式：单次搜索本就可能很久
                if ((DateTime.Now - Engine.LastGoTime).TotalSeconds < cap * 2.5 + 10) return;
                Engine.LastGoTime = DateTime.MinValue;   // 本次只触发一次
                EngineHelper.IoLog("WATCH", "搜索超过 " + (int)(cap * 2.5 + 10) + " 秒未返回，强制 stop（cnt=" + EngineAnalyzeCount + "）");
                Engine.StopAnalyze();
                _forceStopAt = DateTime.Now;
                DisplayStatus("引擎思考超时，已强制停止（如频繁出现请反馈日志）");
            }
            catch (Exception ex) { Debug.WriteLine("ForceStopStuckSearch: " + ex.Message); }
        }

        // R22：后台思考超时兜底——引擎以「等待对手落子」的名义无限搜索（如画面丢失导致永不确认），
        // 超过 180 秒未命中/未失配则主动复位：stop + 配对表丢弃其结果，回到等待确认状态。
        void PonderTimeoutCheck()
        {
            try
            {
                if (!BackgroundAnalyzing) return;
                if (Engine == null) { BackgroundAnalyzing = false; return; }
                if (Engine.LastGoTime == DateTime.MinValue || !Engine.LastGoWasPonder) return;
                if ((DateTime.Now - Engine.LastGoTime).TotalSeconds < 180) return;
                EngineHelper.IoLog("WATCH", "后台思考超过 180 秒未复位，主动停止（回到等待对手落子）");
                if (PonderBoard != null) { Engine.PonderMiss(Utils.BoardToFen(PonderBoard, RedSide)); }
                else { Engine.StopAnalyze(); }
                BackgroundAnalyzing = false;
                PonderReadyMove = "";   // R23：超时复位同时作废暂存
                PonderReadyPonder = "";
                PonderReadyFen = "";
                DisplayStatus("后台思考超时，已复位（如频繁出现请反馈日志）");
            }
            catch (Exception ex) { Debug.WriteLine("PonderTimeoutCheck: " + ex.Message); }
        }

        int StableDetectionCount = 0;
        int ClickRetryCount = 0;
        int AutoGoFailingCheckCount = 0;
        // R38：落子持续未被确认 → 自愈设上限并暂停自动出招（防「点击无效→重试→自愈重发→再点无效」无限循环）
        DateTime _lastSelfHealTime = DateTime.MinValue;   // R39：自愈时间窗基准（距上次自愈 >2 分钟则计数归零）
        int SelfHealStreak = 0;
        bool ClickingSuspended = false;
        const int SelfHealMax = 2;
        // 用于检测是否稳定的棋盘，当有棋盘变化时，在第一次变化时会置该棋盘
        string[,] CheckingBoard = new string[9, 10];
        // 每次检测棋盘后这个函数会被调用
        bool CheckNewBoard()
        {
            var cmpResult = Utils.CompareBoard(LastBoard, PendingBoard);
            if (cmpResult.DiffCount > 0)
            {
                if (ChangeDetectedAfterClick == false)
                {
                    BestMove = new ChessMove(new Point(-1, -1), new Point(-1, -1));
                    ChangeDetectedAfterClick = true;
                    AutoGoFailingCheckCount = 0;
                    ClickRetryCount = 0;
                }

                foreach (var expectedBoard in ExpectedBoardMap)
                {
                    var cmpExpected = Utils.CompareBoard(expectedBoard.Value, PendingBoard, Settings.JieqiMode);
                    if (cmpExpected.DiffCount == 0)
                    {
                        string revealText = Settings.JieqiMode ? Utils.DescribeReveals(expectedBoard.Value, PendingBoard) : "";
                        DisplayStatus("棋盘发生变化 为预期棋盘 跳过确认" + (revealText != "" ? ("，暗子翻开: " + revealText) : ""));
                        CurrentBoard = (string[,])PendingBoard.Clone();
                        return true;
                    }
                }
               
                var cmpChecking = Utils.CompareBoard(CheckingBoard, PendingBoard);
                if (cmpChecking.DiffCount == 0)
                {
                    StableDetectionCount++;
                    DisplayStatus("棋盘发生变化 第" + StableDetectionCount + "次确认");
                    if (StableDetectionCount >= Settings.DetectionConfirmCount) // 三次检测相同
                    {
                        CurrentBoard = (string[,])PendingBoard.Clone();
                        return true;
                    }
                    return false;
                }
                else
                {
                    StableDetectionCount = 0;
                    CheckingBoard = (string[,])PendingBoard.Clone();
                    DisplayStatus("棋盘发生变化 等待确认");
                    return false;
                }
            }
            else
            {
                if (!ClickingSuspended) DisplayStatus("棋盘未改变");
                StableDetectionCount = 0;
                // 处理可能未完成的点击，允许多点2次
                if (Settings.AutoGo && !ClickingSuspended && !ChangeDetectedAfterClick)
                {
                    AutoGoFailingCheckCount++;
                    if (AutoGoFailingCheckCount >= 5)
                    {
                        int maxRetry = Settings.ClickRetryCount;
                        if (maxRetry < 0) maxRetry = 0;
                        if (maxRetry > 5) maxRetry = 5;
                        if (ClickRetryCount < maxRetry)
                        {
                            AutoGoFailingCheckCount = 0;
                            ClickRetryCount++;
                            DisplayStatus("检测到可能的落子失败，重试 " + ClickRetryCount + "/" + maxRetry);
                            // R42：后台消息可能被棋软忽略（ok=True ≠ 落子生效）——重试改走前台补完这一手
                            Mainform.ForceForegroundClickOnce = true;
                            EngineHelper.IoLog("WATCH", "落子失败重试：本次改用前台点击 move=" + ExpectedMove);
                            // 第二十五轮：重试同样走终审（起点必须仍是我方棋子），并根据投递结果给出可见原因
                            if (string.IsNullOrEmpty(ExpectedMove) || ExpectedMove.Length != 4)
                            {
                                ClickRetryCount = 0;
                                DisplayStatus("无有效预期着法，重置检测");
                                ResetDetection();
                            }
                            else if (!PlayChess(ExpectedMove))
                            {
                                DisplayStatus("重试点击仍未投递成功（请检查窗口句柄/坐标空间）");
                                EngineHelper.IoLog("DROP", "retry-click-not-delivered move=" + ExpectedMove);
                            }
                        }
                        else
                        {
                            // R34：多次重试仍未见盘面变化 → 判定落子未生效。
                            // 不再只做 ResetDetection（它会清空队列却不重新发令，而 ReloadBoard
                            // 因「盘面未变化」直接返回 → 同 FEN 又被去重挡下 → 永久卡死）。
                            // 这里主动解锁去重并强制按当前分析盘重新发令，使引擎重新出招。
                            ClickRetryCount = 0;
                            // R39：自愈计数改为「时间窗」语义——距上次自愈超过 2 分钟归零；短时间连续自愈才累积并触发暂停。
                            // （R38 按「局面变化」归零不成立：对手走子/迟到确认都会清零，上限永远触不到——实测 90 秒 5 连循环。）
                            if ((DateTime.Now - _lastSelfHealTime).TotalSeconds > 120) SelfHealStreak = 0;
                            _lastSelfHealTime = DateTime.Now;
                            SelfHealStreak++;
                            if (SelfHealStreak > SelfHealMax)
                            {
                                // R38：自愈达上限仍未确认——点击投递成功≠落子生效；若继续自动重发就是
                                // 无限循环（实测 19:48 会话 7 次自愈、整局不走棋）。这里暂停自动出招，
                                // 停掉在飞分析并停发；局面真正变化或用户操作后由 DetectionLoop 恢复。
                                AutoGoFailingCheckCount = 0;
                                ClickingSuspended = true;
                                DisplayStatus("连续多次落子未被确认，已暂停自动出招（防死循环）。可改用前台鼠标、重新框选窗口或更换后台点击序列；手动走一手或重新开始连线后自动恢复。");
                                EngineHelper.IoLog("WATCH", "落子持续未确认，暂停自动出招（streak=" + SelfHealStreak + "）");
                                EngineAnalyzeCount = 0;
                                BackgroundAnalyzing = false;   // R38：一并复位后台思考状态（防暂停期间残留 ponder）
                                PonderBoard = null;
                                PonderReadyMove = "";
                                PonderReadyPonder = "";
                                PonderReadyFen = "";
                                try { Engine.ClearAnalyzeState(); Engine.StopAnalyze(); } catch { }
                                return false;
                            }
                            DisplayStatus("落子多次未成功，已恢复分析（引擎将重新出招）");
                            EngineHelper.IoLog("WATCH", "落子失败自愈：清去重并重新发令 move=" + ExpectedMove);
                            ResetDetection();
                            TryReissueAfterClickFail();
                        }
                    }
                }
                return false;
            }
        }

        bool ModelDetectBoard(Bitmap image, bool refreshBoard)
        {
            if (Settings.JieqiMode && JieqiModel != null)
            {
                // 交叉验证：附加一次象棋模型推理（仅用于明子校正）
                List<PieceDetection> auxDetections = null;
                if (Settings.DualModelValidate)
                {
                    YoloScorer<YoloXiangQiModel> normalModel = Model;
                    // 交叉验证指定的象棋模型优先；为空则跟随主识别模型
                    string wantChessModel = Settings.DualChessModel;
                    if (!string.IsNullOrEmpty(wantChessModel) && ModelList.ContainsKey(wantChessModel))
                    {
                        normalModel = ModelList[wantChessModel];
                    }
                    if (normalModel == null && ModelList.Count > 0)
                    {
                        foreach (var kv in ModelList)
                        {
                            normalModel = kv.Value;
                            break;
                        }
                    }
                    if (normalModel != null)
                    {
                        try
                        {
                            var auxPreds = normalModel.Predict(image);
                            auxDetections = new List<PieceDetection>();
                            foreach (var p in auxPreds)
                            {
                                auxDetections.Add(new PieceDetection()
                                {
                                    Name = p.Label.Name,
                                    Score = p.Score,
                                    Rectangle = p.Rectangle
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("交叉验证：象棋模型推理失败 " + ex.Message);
                        }
                    }
                }
                return GetBoardFromDetection(JieqiModelPredict(image), refreshBoard, auxDetections);
            }
            var predictions = ModelPredict(image);
            List<PieceDetection> detections = new List<PieceDetection>();
            foreach (var prediction in predictions)
            {
                detections.Add(new PieceDetection()
                {
                    Name = prediction.Label.Name,
                    Score = prediction.Score,
                    Rectangle = prediction.Rectangle
                });
            }
            return GetBoardFromDetection(detections, refreshBoard);
        }

        List<PieceDetection> JieqiModelPredict(Bitmap image)
        {
            // 释放上一帧绘图上下文（避免每帧泄漏 GDI 句柄；旧位图由 GC 回收）
            if (YoloGDI != null) { YoloGDI.Dispose(); YoloGDI = null; }
            YoloDisplayBitmap = new Bitmap(image.Width, image.Height);
            YoloGDI = Graphics.FromImage(YoloDisplayBitmap);
            YoloGDI.DrawImage(image, 0, 0);
            DateTime st = DateTime.Now;
            List<PieceDetection> detections;
            // 持锁推理：与换模型（ApplyDualJieqiModel）互斥，防止推理进行中实例被释放
            lock (JieqiModelLock)
            {
                detections = JieqiModel.Detect(image);
            }
            Debug.WriteLine("揭棋模型耗时: " + Math.Round((DateTime.Now - st).TotalSeconds, 2).ToString() + "s");
            // 像素判色修正：模型偶发把黑子标成 r_*、红子标成 b_*（实测日志：双红帅/缺黑将/三黑车）。
            // 对明子按棋子面像素红黑特征改写前缀（不确定者保持原判），从根上消除这类"不合法"误判。
            int colorFixes = Utils.CorrectVisiblePieceColors(image, detections);
            if (colorFixes > 0)
            {
                Debug.WriteLine("揭棋：像素判色修正明子颜色 " + colorFixes + " 个");
                LogJieqiCorrection("[校正] 像素判色修正明子颜色 " + colorFixes + " 个");
            }
            // 绘制统一在 GetBoardFromDetection 末尾的 DrawDetectionOverlay 中进行
            // （有裁定图→按裁定模型分色；无裁定图→单色；均受「识别绘制」开关控制），
            // 避免与合并后的叠加重复画框。
            pictureBox_show_result.Image = YoloDisplayBitmap;
            return detections;
        }

        // 单模型绘制：沿用旧行为（红框 + 类名 + 置信度），受绘制开关控制
        void DrawDetectionsSingleColor(List<PieceDetection> detections)
        {
            if (!Settings.DrawOverlay) return;
            int th = Mainform.GetDrawThickness();
            using (Pen pen = new Pen(Mainform.GetDrawChessColor(), th))
            using (Font font = new Font("Arial", 16, GraphicsUnit.Pixel))
            using (SolidBrush brush = new SolidBrush(Mainform.GetDrawChessColor()))
            {
                foreach (var detection in detections)
                {
                    if (Settings.DrawBox) YoloGDI.DrawRectangles(pen, new[] { detection.Rectangle });
                    float centerX = detection.Rectangle.X + detection.Rectangle.Width / 2;
                    float centerY = detection.Rectangle.Y + detection.Rectangle.Height / 2;
                    YoloGDI.FillEllipse(Brushes.Lime, new RectangleF(centerX - 2, centerY - 2, 4, 4));
                    if (Settings.DrawClassName || Settings.DrawConfidence)
                    {
                        YoloGDI.DrawString(FormatLabel(detection.Name, detection.Score),
                            font, brush, new PointF(detection.Rectangle.X - 3, detection.Rectangle.Y - 23));
                    }
                }
            }
        }

        // 叠加文字：按「绘制类名 / 绘制置信度」开关拼装
        static string FormatLabel(string className, float score)
        {
            string s = Settings.DrawClassName ? className : "";
            if (Settings.DrawConfidence)
            {
                string c = Math.Round(score, 2).ToString();
                s = string.IsNullOrEmpty(s) ? c : (s + " " + c);
            }
            return s;
        }

        // 只画框 + 中心点（颜色=裁定色）
        void DrawBoxOnly(RectangleF box, Color color, int th)
        {
            if (!Settings.DrawBox) return;
            using (Pen pen = new Pen(color, th))
            {
                YoloGDI.DrawRectangles(pen, new[] { box });
            }
            float cx = box.X + box.Width / 2f;
            float cy = box.Y + box.Height / 2f;
            YoloGDI.FillEllipse(Brushes.Lime, new RectangleF(cx - 2, cy - 2, 4, 4));
        }

        // 在指定位置画一行「类名 置信度」（受绘制开关控制；越出画布顶部时钳到 0）
        void DrawTextAt(string label, float score, float x, float y, Color color, Font font)
        {
            if (!Settings.DrawClassName && !Settings.DrawConfidence) return;
            string s = FormatLabel(label, score);
            if (string.IsNullOrEmpty(s)) return;
            if (y < 0f) y = 0f;
            using (SolidBrush brush = new SolidBrush(color))
            {
                YoloGDI.DrawString(s, font, brush, new PointF(x - 3, y));
            }
        }

        // 双模型分色绘制（第十七轮重写）：按 9x10 棋盘格遍历本帧双模型证据。
        //  - 同一格两个模型都识别到：分上下两行 —— 揭棋模型（暗子模型）在上、象棋模型（明子模型）在下；
        //    confidence 一律用各模型**原始**分数（不是合并后的分数），因此与单独识别时完全一致。
        //  - 只有一个模型识别到：按正常位置单行绘制，不分上下。
        //  - 颜色：两模型一致且开启「高度可信不验证」→ 「框」用绿色；其他情况框按裁定模型色（象棋=红/揭棋=蓝/无=灰）。
        //    类名/置信度文字一律用各自模型色（绿只表示一致，不改变文字分色），便于区分哪一行来自谁。
        // 必须在合并/着色/剪枝之后调用（拿本帧证据与最终裁定）。
        void DrawDetectionOverlay(List<PieceDetection> detections, RectangleF board, float gridWidth, float gridHeight, string[,] finalBoard)
        {
            if (!Settings.DrawOverlay || YoloGDI == null) return;
            // 只有「揭棋模式 + 交叉验证开启 + 本帧确有双模型证据」才走双模型绘制；否则沿用单色旧行为
            // 第二十五轮：高置信裁定开启时**强制双行**绘制（上揭棋、下象棋，都画类名+原始置信度），
            // 因为该模式只是「选谁进盘」，两模型各自的识别参数仍要完整呈现给用户。
            var cells = (Settings.JieqiMode && Settings.DualModelValidate) ? Utils.LastDualCells : null;
            if (cells == null) { DrawDetectionsSingleColor(detections); return; }
            int th = Mainform.GetDrawThickness();
            Color agreeColor = Mainform.GetDrawAgreeColor();
            using (Font font = new Font("Arial", 16, GraphicsUnit.Pixel))
            {
                for (int gy = 0; gy < 10; gy++)
                {
                    for (int gx = 0; gx < 9; gx++)
                    {
                        Utils.DualCell c = cells[gx, gy];
                        if (c == null || (!c.HasJieqi && !c.HasChess)) continue;
                        // 只画最终盘面采纳了棋子的格子：被超量剔除/剪枝的框不再残留，
                        // 否则画面会出现「框里有棋子、实际没送进引擎」的假象。
                        if (finalBoard == null || finalBoard[gx, gy] == null) continue;
                        RectangleF box = c.HasJieqi ? c.JieqiBox : c.ChessBox;
                        // 第二十四轮：框优先取「最终裁定模型」的原始框（高置信翻盘后框与颜色一致）
                        if (c.Authority == Utils.DetectionAuthority.ChessModel && c.ChessBox.Width > 0f && c.ChessBox.Height > 0f) box = c.ChessBox;
                        if (c.Authority == Utils.DetectionAuthority.JieqiModel && c.JieqiBox.Width > 0f && c.JieqiBox.Height > 0f) box = c.JieqiBox;
                        if (box.Width <= 0f || box.Height <= 0f)
                        {
                            // 该模型未给出可用框（缺框/几何异常）：按格心 + 格尺寸补一个
                            float bw = Math.Min(gridWidth, gridHeight) * 0.92f;
                            float cx0 = board.X + gx * gridWidth;
                            float cy0 = board.Y + gy * gridHeight;
                            box = new RectangleF(cx0 - bw / 2f, cy0 - bw / 2f, bw, bw);
                        }
                        bool green = (c.Authority == Utils.DetectionAuthority.BothAgree);
                        Color boxColor = green ? agreeColor : Mainform.GetAuthorityColor(c.Authority);
                        DrawBoxOnly(box, boxColor, th);
                        if (!Settings.DrawClassName && !Settings.DrawConfidence) continue;
                        // 第二十五轮：两模型都识别到的格子始终双行绘制（上揭棋、下象棋）；仅单模型识别则单行。
                        if (c.HasJieqi && c.HasChess)
                        {
                            float yTop = box.Y - 34f;
                            float yBottom = box.Y - 17f;
                            if (yTop < 0f)   // 上方放不下则整体改到框下方，避免文字被画布裁掉
                            {
                                yTop = box.Y + box.Height + 3f;
                                yBottom = yTop + 17f;
                            }
                            // 绿色只用于「框」：两模型一致时文字仍按模型分色（揭棋蓝在上、象棋红在下）
                            DrawTextAt(c.JieqiLabel, c.JieqiScore, box.X, yTop,
                                Mainform.GetDrawJieqiColor(), font);
                            DrawTextAt(c.ChessLabel, c.ChessScore, box.X, yBottom,
                                Mainform.GetDrawChessColor(), font);
                        }
                        else
                        {
                            string label = c.HasJieqi ? c.JieqiLabel : c.ChessLabel;
                            float score = c.HasJieqi ? c.JieqiScore : c.ChessScore;
                            // 绿色只用于「框」；单模型文字按识别模型色
                            Color tc = c.HasJieqi ? Mainform.GetDrawJieqiColor() : Mainform.GetDrawChessColor();
                            DrawTextAt(label, score, box.X, box.Y - 23f, tc, font);
                        }
                    }
                }
            }
        }

        bool GetBoardFromDetection(List<PieceDetection> detections, bool refreshBoard = false, List<PieceDetection> auxDetections = null)
        {
            RectangleF board = new Rectangle(-1, -1, -1, -1);
            int totalChessmanCount = 0;
            double totalChessmanWidth = 0, totalChessmanHeight = 0;
            foreach (var detection in detections)
            {
                if (detection.Name == "board")
                {
                    board = detection.Rectangle;
                }
            }
            // 兜底：本帧未检出 board 类时，用棋子包围盒反推棋盘范围。
            // 修复"画面里已识别到棋子、状态栏却反复提示识别不到棋盘"的死循环
            // （board 类被漏检/置信度过滤时，旧逻辑直接 return false 并把已框定范围清掉）。
            if (board.X == -1 && !refreshBoard && BoardArea.X != -1 && BoardAreaRaw.Width > 0 && BoardAreaRaw.Height > 0)
            {
                // 兜底 A（最精确）：棋盘区域已建立时，按已知裁切几何直接还原棋盘框。
                // 裁切图原点 = BoardArea 左上角；棋盘在截图坐标中的位置 = BoardAreaRaw。
                board = new RectangleF(BoardAreaRaw.X - BoardArea.X, BoardAreaRaw.Y - BoardArea.Y,
                                       BoardAreaRaw.Width, BoardAreaRaw.Height);
                Debug.WriteLine("board 类未检出，按已知裁切几何还原棋盘框: " + board);
            }
            if (board.X == -1)
            {
                double pMinX = double.MaxValue, pMinY = double.MaxValue;
                double pMaxX = double.MinValue, pMaxY = double.MinValue;
                int pieceCount = 0;
                foreach (var detection in detections)
                {
                    if (detection.Name == "board" || detection.Name == "obstacle") continue;
                    double chessRatio = detection.Rectangle.Width / detection.Rectangle.Height;
                    if (chessRatio > 1.3 || chessRatio < 0.7) continue;
                    double centerX = detection.Rectangle.X + detection.Rectangle.Width / 2.0;
                    double centerY = detection.Rectangle.Y + detection.Rectangle.Height / 2.0;
                    if (centerX < pMinX) pMinX = centerX;
                    if (centerY < pMinY) pMinY = centerY;
                    if (centerX > pMaxX) pMaxX = centerX;
                    if (centerY > pMaxY) pMaxY = centerY;
                    pieceCount++;
                }
                if (pieceCount >= 10)
                {
                    double spanX = pMaxX - pMinX;
                    double spanY = pMaxY - pMinY;
                    if (spanX > 0 && spanY > 0)
                    {
                        float estGridW = (float)(spanX / 8.0);   // 横向 9 条线相差 8 格
                        float estGridH = (float)(spanY / 9.0);   // 纵向 10 条线相差 9 格
                        float gridRatio = estGridW / estGridH;
                        if (estGridW > 4f && estGridH > 4f && gridRatio > 0.6f && gridRatio < 1.6f)
                        {
                            // 注意：坐标系原点必须与"board 类框"一致——左/上边界=最外棋子中心所在线。
                            // 不能向外扩张半格/一格，否则后续网格映射 (中心-board.X)/grid 整体偏移，FEN 全错。
                            board = new RectangleF((float)pMinX, (float)pMinY, (float)spanX, (float)spanY);
                            Debug.WriteLine("board 类未检出，已由 " + pieceCount + " 个棋子反推棋盘: " + board);
                        }
                    }
                }
            }
            foreach (var detection in detections)
            {
                if (detection.Name != "board")
                {
                    double chessRatio = detection.Rectangle.Width / detection.Rectangle.Height;
                    if (chessRatio > 1.3 || chessRatio < 0.7) continue;
                    totalChessmanCount++;
                    totalChessmanWidth += detection.Rectangle.Width;
                    totalChessmanHeight += detection.Rectangle.Height;
                }
            }
            List<PieceDetection> validPredictions = new List<PieceDetection>();
            foreach (var detection in detections)
            {
                if (detection.Name == "board" || detection.Name == "obstacle") continue;
                double chessRatio = detection.Rectangle.Width / detection.Rectangle.Height;
                if (chessRatio > 1.3 || chessRatio < 0.7) continue;
                var rect = detection.Rectangle;
                if (board.Contains(rect.X, rect.Y) || board.Contains(rect.Right, rect.Top) || board.Contains(rect.Left, rect.Bottom) || board.Contains(rect.Right, rect.Bottom))
                {
                    validPredictions.Add(detection);
                }
            }
            double minDistense = int.MaxValue;
            for (int i = 0; i < validPredictions.Count; i++)
            {
                var prediction = validPredictions[i];
                for (int j = i + 1; j < validPredictions.Count; j++)
                {
                    double d = Math.Sqrt(Math.Pow((prediction.Rectangle.X - validPredictions[j].Rectangle.X), 2) + Math.Pow((prediction.Rectangle.Y - validPredictions[j].Rectangle.Y), 2));
                    if (d < minDistense) minDistense = d;
                }
            }
            double minX = int.MaxValue, minY = int.MaxValue, maxX = 0, maxY = 0;
            foreach (var prediction in validPredictions)
            {
                float centerX = prediction.Rectangle.X + prediction.Rectangle.Width / 2;
                float centerY = prediction.Rectangle.Y + prediction.Rectangle.Height / 2;
                if (centerX < minX) minX = centerX;
                if (centerY < minY) minY = centerY;
                if (centerX > maxX) maxX = centerX;
                if (centerY > maxY) maxY = centerY;
            }
            double boardWidth = maxX - minX;
            double boardHeight = maxY - minY;
            float gridWidth = board.Width / 8f;
            float gridHeight = board.Height / 9f;

            if (boardHeight >= minDistense * 8.6 && boardWidth >= minDistense * 7.6)
            {
                board.Width = (int)boardWidth;
                board.Height = (int)boardHeight;
                board.X = (int)minX;
                board.Y = (int)minY;
                gridWidth = (int)boardWidth / 8;
                gridHeight = (int)boardHeight / 9;
                YoloGDI.DrawRectangle(Pens.Blue, board.X, board.Y, board.Width, board.Height);
                SafeInvoke(new Action(() => {
                    pictureBox_show_result.Refresh();
                }));
            }

            double avgChessmanWidth = totalChessmanWidth / totalChessmanCount;
            double avgChessmanHeight = totalChessmanHeight / totalChessmanCount;
            string[,] tmpBoard = new string[9, 10];
            float[,] cellScores = new float[9, 10];   // 每格检测置信度（暗子超量剔除用）
            // 第十七轮：逐格双模型证据（仅「揭棋模式 + 交叉验证」时建立）。
            // 保存两个模型的**原始**类名/置信度/框，供识别画面上下两行分色绘制；
            // 绝不能用合并后的 cellScores 顶替任一方，否则会出现「单独识别与交叉验证置信度不一致」。
            Utils.DualCell[,] dualCells = null;
            bool dualActive = Settings.JieqiMode && Settings.DualModelValidate && auxDetections != null && auxDetections.Count > 0;
            if (dualActive)
            {
                dualCells = new Utils.DualCell[9, 10];
                for (int y = 0; y < 10; y++)
                    for (int x = 0; x < 9; x++) dualCells[x, y] = new Utils.DualCell();
            }
            for (int y = 0; y < 10; y++)
            {
                for (int x = 0; x < 9; x++)
                {
                    tmpBoard[x, y] = null;
                }
            }
            if (board.X == -1) return false;
            if (board.Width / board.Height > 1.3 || board.Width / board.Height < 0.7) return false;
            if (board.Width < avgChessmanWidth * 7 || board.Height < avgChessmanHeight * 8) return false;
            if (refreshBoard)
            {
                BoardAreaRaw = new Rectangle((int)board.X, (int)board.Y, (int)board.Width, (int)board.Height);
            }

            for (int y = 0; y < 10; y++)
            {
                for (int x = 0; x < 9; x++)
                {
                    double pos_x = board.X + x * gridWidth;
                    double pos_y = board.Y + y * gridHeight;
                    ClickPositionMap[x, y] = new Point((int)pos_x + BoardArea.X, (int)pos_y + BoardArea.Y);
                }
            }
           
            foreach (var detection in detections)
            {
                if (detection.Name == "board" || detection.Name == "obstacle") continue;
                double chessRatio = detection.Rectangle.Width / detection.Rectangle.Height;
                if (chessRatio > 1.3 || chessRatio < 0.7) continue;
                float centerX = detection.Rectangle.X + detection.Rectangle.Width / 2;
                float centerY = detection.Rectangle.Y + detection.Rectangle.Height / 2;
                float offsetX = centerX - board.X; // offset from the board
                float offsetY = centerY - board.Y;
                int xPos = (int)Math.Round(offsetX / gridWidth);
                int yPos = (int)Math.Round(offsetY / gridHeight);
                if (xPos >= 0 && xPos <= 8 && yPos >= 0 && yPos <= 9)
                {
                    // 同格同模型多检时取最高置信，避免低分框覆盖高分框
                    if (tmpBoard[xPos, yPos] == null || detection.Score > cellScores[xPos, yPos])
                    {
                        tmpBoard[xPos, yPos] = detection.Name;
                        cellScores[xPos, yPos] = detection.Score;
                    }
                    if (dualCells != null)
                    {
                        Utils.DualCell c = dualCells[xPos, yPos];
                        if (!c.HasJieqi || detection.Score > c.JieqiScore)
                        {
                            c.HasJieqi = true;
                            c.JieqiLabel = detection.Name;
                            c.JieqiScore = detection.Score;
                            c.JieqiBox = detection.Rectangle;
                        }
                    }
                }

            }
            // 视角判定（第十四轮，投票制）：上下九宫内两模型的将/帅按置信度共同投票。
            // 旧逻辑取单个最高置信 r_jiang——误标框（如黑将被标成 r_jiang 且分数更高）会翻转视角、整盘被误判非法。
            // 平票或无候选（开局全暗子）时保持现有 RedSide。必须在合并/着色/剔除之前完成，这些步骤都依赖 RedSide。
            bool? redSideVote = Utils.DecideRedSide(detections, auxDetections, board, gridWidth, gridHeight, out string redSideVoteDetail);
            if (redSideVote.HasValue)
            {
                RedSide = redSideVote.Value;
                Debug.WriteLine("视角投票: " + redSideVoteDetail + " → RedSide=" + RedSide);
            }

            // 交叉验证（揭棋模式）：象棋模型网格 → 与揭棋模型网格合并（暗子以揭棋模型为准，明子补漏检/仲裁误判）
            if (Settings.JieqiMode && auxDetections != null && auxDetections.Count > 0)
            {
                string[,] auxBoard = new string[9, 10];
                float[,] auxScores = new float[9, 10];
                foreach (var detection in auxDetections)
                {
                    if (detection.Name == "board" || detection.Name == "obstacle") continue;
                    double chessRatio = detection.Rectangle.Width / detection.Rectangle.Height;
                    if (chessRatio > 1.3 || chessRatio < 0.7) continue;
                    float cx = detection.Rectangle.X + detection.Rectangle.Width / 2;
                    float cy = detection.Rectangle.Y + detection.Rectangle.Height / 2;
                    int xPos = (int)Math.Round((cx - board.X) / gridWidth);
                    int yPos = (int)Math.Round((cy - board.Y) / gridHeight);
                    if (xPos >= 0 && xPos <= 8 && yPos >= 0 && yPos <= 9)
                    {
                        if (auxBoard[xPos, yPos] == null || detection.Score > auxScores[xPos, yPos])
                        {
                            auxBoard[xPos, yPos] = detection.Name;
                            auxScores[xPos, yPos] = detection.Score;
                        }
                        if (dualCells != null)
                        {
                            Utils.DualCell c = dualCells[xPos, yPos];
                            if (!c.HasChess || detection.Score > c.ChessScore)
                            {
                                c.HasChess = true;
                                c.ChessLabel = detection.Name;
                                c.ChessScore = detection.Score;
                                c.ChessBox = detection.Rectangle;
                            }
                        }
                    }
                }
                int dualFixes = Utils.MergeDualModelGridsEx(tmpBoard, cellScores, auxBoard, auxScores, RedSide,
                    Settings.DualChessConfThreshold, Settings.DualJieqiDarkConfThreshold,
                    Settings.DualAgreeSkipVerify, Settings.DualVisibleByMaxConf, dualCells, out DualAuthorityGrid);

                if (dualFixes > 0)
                {
                    Debug.WriteLine("双模型交叉验证修正 " + dualFixes + " 格");
                    LogJieqiCorrection("[校正] 双模型交叉验证修正 " + dualFixes + " 格");
                }
            }
            else
            {
                // 未走双模型路径：必须清掉证据与裁定图，
                // 否则上一帧的双行文字 / 绿色框会串进单独的象棋模型识别画面。
                DualAuthorityGrid = null;
                Utils.SetLastDualCells(null);
            }

            if (Settings.JieqiMode)
            {
                // 揭棋：裸 dark 着色 —— 颜色记忆（上一检测帧）→ 移动匹配 → 半区回退 → 数量校正。
                // 仅按半区着色会把“过河暗子”染成对方颜色（揭棋暗子移动后保持暗置、可过河），
                // 造成一方暗子计数超 15，被 CheckBoardValid 误判为“当前棋盘不合法”。
                // 先剔除“超量低置信暗子”（全盘物理上限 30）：模型偶发多检一个暗子时不再把合法局面判非法。
                int prunedDarks = Utils.PruneExcessBareDarks(tmpBoard, cellScores, 30);
                if (prunedDarks > 0)
                {
                    Debug.WriteLine("揭棋：剔除超量低置信暗子 " + prunedDarks + " 个");
                    LogJieqiCorrection("[校正] 剔除超量低置信暗子 " + prunedDarks + " 个（全盘上限30）");
                }
                string[,] prevColored = LastValidPendingBoard;   // 只用“校验通过”的帧做颜色记忆，避免非法帧污染
                if (!Utils.HasResolvedDark(prevColored)) prevColored = null;
                if (prevColored == null)
                {
                    if (Utils.HasResolvedDark(LastBoard)) prevColored = LastBoard;
                    else if (Utils.HasResolvedDark(CurrentBoard)) prevColored = CurrentBoard;
                }
                // R26：换边（RedSide 翻转）后旧方向的暗子颜色记忆不可信——否则会把将帅两侧的暗子染成
                // 相反归属，生成「暗子归属矛盾」毒盘（实测会让引擎 0xC0000005 崩溃）。
                if (prevColored != null && _lastColoringRedSide != RedSide) prevColored = null;
                Utils.ResolveDarkColors(tmpBoard, prevColored, RedSide);
                _lastColoringRedSide = RedSide;
                // 明子超量剔除：模型把明子标错前缀/重复检出时（如双红帅、三黑车），
                // 按每方每类型上限保留高置信者（帅优先保留九宫内的），把小幅超额救回。
                int prunedVisible = Utils.PruneExcessVisible(tmpBoard, cellScores, RedSide);
                if (prunedVisible > 0)
                {
                    Debug.WriteLine("揭棋：剔除超量明子 " + prunedVisible + " 个");
                    LogJieqiCorrection("[校正] 剔除超量明子 " + prunedVisible + " 个");
                }
            }
            PendingBoard = tmpBoard;
            // 分色绘制：合并/着色/剪枝全部完成后再画，颜色才能对应本帧最终的裁定模型。
            DrawDetectionOverlay(detections, board, gridWidth, gridHeight, tmpBoard);
            if (Settings.DrawOverlay)
            {
                // 位图内容变化不会自动触发 PictureBox 重绘：显式请求刷新，否则新框会晚一帧出现
                SafeInvoke(new Action(() => { pictureBox_show_result.Refresh(); }));
            }
            return true;
        }

        // 校正类日志限流（每 3 秒最多一条，避免刷屏）
        static void LogJieqiCorrection(string msg)
        {
            if ((DateTime.Now - LastCorrectionLogTime).TotalSeconds < 3) return;
            LastCorrectionLogTime = DateTime.Now;
            try
            {
                File.AppendAllText("jieqi_invalid.log", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + msg + "\r\n");
            }
            catch { }
        }

        List<YoloPrediction> ModelPredict(Bitmap image)
        {
            // 释放上一帧绘图上下文（避免每帧泄漏 GDI 句柄；旧位图由 GC 回收）
            if (YoloGDI != null) { YoloGDI.Dispose(); YoloGDI = null; }
            YoloDisplayBitmap = new Bitmap(image.Width, image.Height);
            YoloGDI = Graphics.FromImage(YoloDisplayBitmap);
            YoloGDI.DrawImage(image, 0, 0);
            DateTime st = DateTime.Now;
            List<YoloPrediction> predictions = Model.Predict(image);
            Debug.WriteLine("模型耗时: " + Math.Round((DateTime.Now - st).TotalSeconds, 2).ToString() + "s");
            // 绘制统一在 GetBoardFromDetection 末尾的 DrawDetectionOverlay 中进行（受绘制开关控制），
            // 避免此处旧手绘与叠加层重复画框、且不遵守「识别绘制」开关。
            pictureBox_show_result.Image = YoloDisplayBitmap;
            return predictions;
        }

        async void StartAnalyze(string fen)
        {
            MultiPvClear();   // 新一次思考：清空上一轮 MultiPV 候选箭头
            _lastInfoLogKey = "";   // 第二十一轮：新搜索重置日志节流键
            _lastCandLogKey = "";
            await Task.Run(new Action(() =>
            {
                if (BestMoveList.Count >= 6)
                {
                    if (BestMoveList[BestMoveList.Count-2] == BestMoveList[BestMoveList.Count - 4] && 
                    BestMoveList[BestMoveList.Count - 4] == BestMoveList[BestMoveList.Count - 6])
                    {
                        Engine.BanMoves = BestMoveList[BestMoveList.Count - 2];
                        DisplayStatus("禁止长打，禁止走法: " + Engine.BanMoves);
                    }
                }
                if (Settings.UseOpenBook)
                {
                    try
                    {
                        var resultList = OpenBookHelper.QueryAll(OpenBookList, fen);
                        int index = 0;
                        if (resultList.Count > 0)
                        {
                            if (Settings.OpenbookMode == ProgramSettings.OpenBookMode.Random)
                            {
                                index = Rand.Next(resultList.Count);
                            }
                            else
                            {
                                // sort 
                                resultList.Sort((q1, q2) =>
                                {
                                    return q2.Score - q1.Score;
                                });
                                index = 0;
                            }
                            var result = resultList[index];
                            DisplayStatus("命中 " + result.Book + " 开局库");
                            SafeInvoke(new Action(() =>
                            {
                                if (result.Score > 20000)
                                {
                                    textBox_engine_log.SelectionColor = MateColor;
                                }
                                else if (result.Score >= 0)
                                {
                                    textBox_engine_log.SelectionColor = PositiveColor;
                                }
                                else
                                {
                                    textBox_engine_log.SelectionColor = NegativeColor;
                                }
                                textBox_engine_log.AppendText(
                                    $"开局库 {result.Book} {result.Memo}\r\n" +
                                    $"得分: {result.Score}\r\n" + Utils.FenToChina(EngineAnalyzingBoard, new string[] { result.Move },RedSide) +
                                    "\r\n"
                                    );
                                if (checkBox_auto_scroll.Checked)
                                {
                                    textBox_engine_log.ScrollToCaret();
                                }
                                EngineAnalyzeCount++;
                                ReceiveBestMove(result.Move, "", ResultSource.Openbook, fen);
                            }));
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        SafeInvoke(new Action(() =>
                        {
                            textBox_engine_log.AppendText("查询开局库时出错: " + ex.Message);
                        }));
                    }
                }
                if (Settings.UseChessDB)
                {
                    try
                    {
                        DisplayStatus("检索云库中");
                        var result = ChessDBHelper.GetPV(fen);
                        if (result != null)
                        {
                            DisplayStatus("命中云库");
                            SafeInvoke(new Action(() =>
                            {
                                if (result.Score > 20000)
                                {
                                    textBox_engine_log.SelectionColor = MateColor;
                                }
                                else if (result.Score >= 0)
                                {
                                    textBox_engine_log.SelectionColor = PositiveColor;
                                }
                                else
                                {
                                    textBox_engine_log.SelectionColor = NegativeColor;
                                }
                                textBox_engine_log.AppendText($"云库 深度: {result.Depth} 得分: {result.Score}\r\n");
                                textBox_engine_log.SelectionColor = Color.Black;
                                textBox_engine_log.AppendText(Utils.FenToChina(EngineAnalyzingBoard, result.PV.ToArray(),RedSide));
                                textBox_engine_log.AppendText("\r\n\r\n");
                                if (checkBox_auto_scroll.Checked)
                                {
                                    textBox_engine_log.ScrollToCaret();
                                }
                                string bestMove = result.PV[0];
                                string ponderMove = "";
                                if (result.PV.Count >= 2) ponderMove = result.PV[1];
                                EngineAnalyzeCount++;
                                ReceiveBestMove(bestMove, ponderMove, ResultSource.ChessDB, fen);
                            }));
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        SafeInvoke(new Action(() =>
                        {
                            textBox_engine_log.AppendText("查询云库时出错: " + ex.Message);
                        }));
                    }
                }

                DisplayStatus("开始引擎计算");
                // 第十五轮：引擎崩溃（揭棋分支在“暗子落在将/帅原格”的局面会段错误）且
                // 自动重启失败时，StartAnalyze 会抛异常 —— 必须在此接住，否则 Task.Run 的
                // 异常经 async void 直接击穿到 UI 线程，整个程序崩溃。
                try
                {
                    Engine.StartAnalyze(fen, Settings.EngineStepTime, Settings.EngineDepth);
                }
                catch (Exception ex)
                {
                    DisplayStatus("引擎异常，本次暂停出招（已记录日志）");
                    SafeInvoke(new Action(() =>
                    {
                        textBox_engine_log.AppendText("引擎异常，本次暂停出招: " + ex.Message + "\r\n");
                    }));
                    try { File.AppendAllText("err.log", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                        + "  [引擎] StartAnalyze 失败: " + ex.Message + " FEN=" + fen + "\r\n"); } catch { }
                }
            }));
        }
       
        // R34：落子失败后的主动自愈——解除去重并按当前盘面强制重新发令。
        // 背景：ResetDetection → ReloadBoard 的路径要求「盘面发生变化」，而点击未生效时盘面
        // 恰恰不变，恢复链因此断裂（实测症状：一次没落下，此后整局不再落子）。
        // 此处不依赖盘面变化，直接把当前分析盘重新交给引擎。
        void TryReissueAfterClickFail()
        {
            try
            {
                if (Engine == null) return;
                Engine.ClearAnalyzeState();   // 解除同 FEN 去重，否则重发会被 SKIP
                if (EngineAnalyzingBoard == null) return;
                string guardReason;
                if (!Utils.IsFenSafeForJieqiEngine(EngineAnalyzingBoard, out guardReason))
                {
                    DisplayStatus("恢复分析被拦截（引擎不安全局面）：" + guardReason);
                    return;
                }
                string fen = Utils.BoardToFen(EngineAnalyzingBoard, RedSide ? "w" : "b", RedSide ? "w" : "b");
                EngineAnalyzeCount++;
                StartAnalyze(fen);
                EngineHelper.IoLog("REISSUE", "落子失败后重新发令（盘面未变）");
            }
            catch (Exception ex) { Debug.WriteLine("TryReissueAfterClickFail: " + ex.Message); }
        }

        // R42：黑名单到期后的延迟补发——引擎崩溃被短暂拉黑后自动重试同一局面（≤3 次），
        // 防止「崩溃→拉黑→整局哑火」；局面变化、引擎正常出招或新开局时状态复位。
        void BlacklistRetryCheck()
        {
            try
            {
                if (_blacklistRetryAt == DateTime.MinValue) return;
                if (DateTime.Now < _blacklistRetryAt) return;
                if (ClickingSuspended) return;                          // R43：暂停自动出招期间不补发（暂停优先；恢复靠局面变化/重开连线）
                if (EngineAnalyzeCount > 0) return;                     // 有搜索在飞，让它先跑
                if (BackgroundAnalyzing) return;
                if (Engine == null) return;
                if (Engine.EngineDead || Engine.SafeHasExited())
                {
                    // R43：引擎已死——先尝试重启（EnsureAlive 自带限流）；成功则本次继续往下补发，失败则稍后再来
                    try { if (!Engine.EnsureAlive()) { _blacklistRetryAt = DateTime.Now.AddSeconds(30); return; } }
                    catch { _blacklistRetryAt = DateTime.Now.AddSeconds(30); return; }
                }
                if (EngineAnalyzingBoard == null) { _blacklistRetryAt = DateTime.MinValue; return; }
                string retGuard;
                if (!Utils.IsFenSafeForJieqiEngine(EngineAnalyzingBoard, out retGuard)) { _blacklistRetryAt = DateTime.MinValue; return; }   // R42：与其余发令点同规——不安全局面不补发
                string fen = Utils.BoardToFen(EngineAnalyzingBoard, RedSide ? "w" : "b", RedSide ? "w" : "b");
                if (EngineHelper.IsBoardBlacklisted(fen))
                {
                    _blacklistRetryAt = DateTime.Now.AddSeconds(30);    // 仍在拉黑（TTL 未到或又崩过）——稍后再试
                    return;
                }
                _blacklistRetryAt = DateTime.MinValue;
                _blacklistRetryCount++;
                EngineAnalyzeCount++;
                StartAnalyze(fen);
                EngineHelper.IoLog("REISSUE", "黑名单到期后延迟补发（第 " + _blacklistRetryCount + " 次）");
                DisplayStatus("崩溃局面已恢复，重新开始计算");
            }
            catch (Exception ex) { Debug.WriteLine("BlacklistRetryCheck: " + ex.Message); }
        }

        void ReloadBoard()
        {
            if (CurrentBoard == null) return;
            var compareWithLast = Utils.CompareBoard(LastBoard, CurrentBoard);
            var compareWithExpected = Utils.CompareBoard(ExpectedSelfGoBoard, CurrentBoard, Settings.JieqiMode);
            if (compareWithLast.DiffCount == 0) return;
            string opponentSymbol =RedSide ? "b_" : "r_";
            string mySymbol = RedSide ? "r_" : "b_";
            Debug.WriteLine($"Diff Result: {compareWithLast.DiffCount} R: {compareWithLast.RedDiff} B: {compareWithLast.BlackDiff}");
            string startingFen = "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR";
            string currentFen = Utils.BoardToFen(CurrentBoard, RedSide ? "w" : "b", RedSide ? "w" : "b");
            if (currentFen.StartsWith(startingFen) && !RedSide)
            {
                TurnToOpponent = true;
            }

            if (TurnToOpponent && !Settings.AnalyzingMode)
            {
                TurnToOpponent = false;
                LastBoard = (string[,])CurrentBoard.Clone();
                ExpectedSelfGoBoard = null;
                ExpectedMove = "";
                RenderDisplayBoard();
                DisplayStatus("从对手开始，跳过当前局面");
                return;
            }
            if (compareWithLast.BlackDiff > 1 || compareWithLast.RedDiff > 1)
            {
                InvalidCount++;
                DisplayStatus($"棋盘变化数异常: 黑棋消失: {compareWithLast.BlackDiff} 红棋消失:{compareWithLast.RedDiff} 判断为动画中，跳过 | 错误次数: {InvalidCount}");
                if (InvalidCount > 2)
                {
                    DisplayStatus("错误次数过多，自动重置");
                    ResetDetection();
                }
                return;
            }
            if (compareWithLast.DiffCount != 2 && compareWithExpected.DiffCount != 2 && compareWithLast.DiffCount < 10)
            {
                InvalidCount++;
                DisplayStatus($"棋盘变化数异常: {compareWithLast.DiffCount} 判断为动画中，跳过 | 错误次数: {InvalidCount}");
                if (InvalidCount > 2)
                {
                    DisplayStatus("错误次数过多，自动重置");
                    ResetDetection();
                }
                return;
            }
            if (compareWithLast.DiffCount == 2 && compareWithLast.Chess != null)
            {
                if (compareWithLast.Chess.Contains(mySymbol))
                {
                    // 我方着法计数（时间设置「前 N 回合使用步时」判定用）
                    Mainform.SelfMoveCount++;
                    // 落子确认：仅当新棋盘与该手棋的预期棋盘一致时，才把暂存评分画入趋势（揭棋含暗子翻开模糊匹配）
                    if (compareWithExpected.DiffCount == 0) ConfirmTrendPoint();
                    RenderDisplayBoard();
                    if (Settings.AnalyzingMode)
                    {
                        InvalidCount = 0;
                        LastBoard = (string[,])CurrentBoard.Clone();
                        // R43：补齐全库最后一处漏检的发送前守卫——将帅均离位+暗子的盘面经此会把必崩 FEN 发给引擎
                        string oppoGuardReason;
                        if (!Utils.IsFenSafeForJieqiEngine(CurrentBoard, out oppoGuardReason))
                        {
                            DisplayStatus("检测到引擎不安全局面，已拦截：" + oppoGuardReason);
                            LogJieqiCorrection("[拦截] 引擎不安全局面：" + oppoGuardReason);
                            return;
                        }
                        EngineAnalyzeCount++;
                        DisplayStatus("开始引擎计算");
                        string oppofen = Utils.BoardToFen(CurrentBoard, RedSide ? "w" : "b", RedSide ? "b" : "w");
                        EngineAnalyzingBoard = (string[,])CurrentBoard.Clone();
                        Engine.StopAnalyze();
                        StartAnalyze(oppofen);
                    }
                    else
                    {
                        InvalidCount = 0;
                        LastBoard = (string[,])CurrentBoard.Clone();
                        DisplayStatus("己方棋子变化，跳过分析");
                        ExpectedMove = "";
                    }
                    return;
                }
                else
                {
                    TurnToOpponent = false;
                }
            }
            if (compareWithExpected.DiffCount == 0 && !Settings.AnalyzingMode)
            {
                Mainform.SelfMoveCount++;   // 我方落子确认（时间设置「前 N 回合使用步时」判定用）
                ConfirmTrendPoint();   // 预期棋盘命中：落子确认（与“我方棋子出现”分支同一确认语义）
                LastBoard = (string[,])CurrentBoard.Clone();
                ExpectedMove = "";
                RenderDisplayBoard();
                string revealText = Settings.JieqiMode ? Utils.DescribeReveals(ExpectedSelfGoBoard, CurrentBoard) : "";
                DisplayStatus("和预期棋盘一样，跳过" + (revealText != "" ? ("，暗子翻开: " + revealText) : ""));
                return;
            }
            if ((compareWithLast.BlackDiff > 1 || compareWithLast.RedDiff > 1) && compareWithLast.DiffCount < 10)
            {
                InvalidCount++;
                DisplayStatus($"差别过大，可能有动画，跳过 | 错误次数: {InvalidCount}");
                if (InvalidCount > 2)
                {
                    DisplayStatus("错误次数过多，自动重置");
                    ResetDetection();
                }
                return;
            }
            RenderDisplayBoard();
            InvalidCount = 0;
            EngineAnalyzeCount++;
            LastBoard = (string[,])CurrentBoard.Clone();

            if (Settings.AnalyzingMode)
            {
                InvalidCount = 0;
                string analyzeFen;
                if (TurnToOpponent)
                {
                    analyzeFen = Utils.BoardToFen(CurrentBoard, RedSide ? "w" : "b", RedSide ? "b" : "w");
                }
                else
                {
                    analyzeFen = Utils.BoardToFen(CurrentBoard, RedSide ? "w" : "b", RedSide ? "w" : "b");
                }
                if (!Utils.IsFenSafeForJieqiEngine(CurrentBoard, out string analyzeGuardReason))
                {
                    DisplayStatus("检测到引擎不安全局面，已拦截：" + analyzeGuardReason);
                    LogJieqiCorrection("[拦截] 引擎不安全局面：" + analyzeGuardReason);
                    try { EngineAnalyzeCount--; if (EngineAnalyzeCount < 0) EngineAnalyzeCount = 0; } catch { }   // R43：拦截即归还本次计数（防看门狗误判有搜索在飞）
                    return;
                }
                EngineAnalyzingBoard = (string[,])CurrentBoard.Clone();   // 先切盘：旧局面迟到的 bestmove 必判过期
                Engine.StopAnalyze();
                StartAnalyze(analyzeFen);
                return;
            }

            string fen = Utils.BoardToFen(CurrentBoard,RedSide ? "w" : "b",RedSide ? "w" : "b");
            string fenGuardReason;
            if (!Utils.IsFenSafeForJieqiEngine(CurrentBoard, out fenGuardReason))
            {
                // 第十五轮：将/帅原位出现暗子会让揭棋引擎 0xc0000005 崩溃（引擎侧已修，
                // 此处为发送前拦截，保护任何未修复/外部引擎）。保留上一合法盘面，等下一帧。
                DisplayStatus("检测到引擎不安全局面，已拦截：" + fenGuardReason);
                LogJieqiCorrection("[拦截] 引擎不安全局面：" + fenGuardReason + "（保留上一合法盘面）");
                try { EngineAnalyzeCount--; if (EngineAnalyzeCount < 0) EngineAnalyzeCount = 0; } catch { }   // R43：拦截即归还本次计数（防看门狗误判有搜索在飞）
                try { File.AppendAllText("err.log", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    + "  [引擎] 拒绝将帅原位暗子局面: " + fenGuardReason + "\r\n"); } catch { }
                return;
            }
            if (BackgroundAnalyzing)
            {
                BackgroundAnalyzing = false;
                var compareWithPonder = Utils.CompareBoard(PonderBoard, CurrentBoard, Settings.JieqiMode);
                if (compareWithPonder.DiffCount == 0)
                {
                    // R23：命中。引擎若已提前算好（暂存）则直接采用，避免等待引擎空转；否则请求 ponderhit
                    string prMove = PonderReadyMove;
                    string prPonder = PonderReadyPonder;
                    string prFen = PonderReadyFen;
                    PonderReadyMove = "";
                    PonderReadyPonder = "";
                    PonderReadyFen = "";
                    if (!string.IsNullOrEmpty(prMove))
                    {
                        DisplayStatus("后台思考命中（预存着法直接出招）");
                        try { if (Engine.AnalyzeQueue.Contains(prFen)) Engine.SkipList.Add(prFen); } catch { }   // 仅当该着法仍在途时抑制尾随输出（防残留条目误吞同 FEN 的后续着法）
                        Engine.PonderHit();
                        ReceiveBestMove(prMove, prPonder, ResultSource.Engine, prFen);
                    }
                    else
                    {
                        DisplayStatus("后台思考命中");
                        Engine.PonderHit();
                    }
                }
                else
                {
                    // R23：未命中——作废暂存，按对手实际着法重算
                    PonderReadyMove = "";
                    PonderReadyPonder = "";
                    PonderReadyFen = "";
                    DisplayStatus("开始引擎计算");
                    Engine.PonderMiss(Utils.BoardToFen(PonderBoard, RedSide));
                    EngineAnalyzingBoard = (string[,])CurrentBoard.Clone();
                    StartAnalyze(fen);
                }
            }
            else
            {
                // 引擎计算期间持续识别（T5b）：先把分析盘切到最新局面（旧局面的迟到 bestmove 必判过期丢弃），
                // 再结束旧局面的思考，最后按最新局面重新开始思考。
                EngineAnalyzingBoard = (string[,])CurrentBoard.Clone();
                Engine.StopAnalyze();
                StartAnalyze(fen);
            }
        }

        private void Engine_InfoEvent(string cmd, Dictionary<string, string> infos)
        {
            // MultiPV（第二十轮）：主线（multipv<=1 或未带 multipv）开启新批次候选；
            // 仅主线驱动自动停/预期盘/落子，非主线只更新候选箭头与日志。
            int mpvRank = 1;
            if (infos.ContainsKey("multipv"))
            {
                int parsedRank;
                if (int.TryParse(infos["multipv"], out parsedRank) && parsedRank >= 1) mpvRank = parsedRank;
            }
            bool mainLine = (mpvRank <= 1);
            if (mainLine && infos.ContainsKey("pv")) MultiPvClear();   // 仅带 pv 的主线行才开启新一批候选

            bool didLog = false;
            string info_str = "";
            if (infos.ContainsKey("score"))
            {
                //SafeInvoke(new Action(() =>
                //{
                //    progressBar_score.Value = int.Parse(infos["score"]);
                //}));
                if (Settings.StopWhenMate && mainLine && infos.ContainsKey("pv"))
                {
                    int currScore = 0;
                    if (infos["score"].Contains("绝杀") && int.Parse(infos["score"].Split('(').Last().Split(')').First()) > 0)
                    {
                        if (FirstMate)
                        {
                            FirstMate = false;
                            string fen = Utils.BoardToFen(CurrentBoard, RedSide);
                            Engine.StopAnalyzeAndSkip(fen);
                            string[] moves = infos["pv"].Split(' ');
                            string ponderMove = "";
                            if (moves.Length >= 2) ponderMove = moves[1];
                            ReceiveBestMove(moves[0], ponderMove, ResultSource.Openbook, fen);
                        }                            
                    }
                    else if (int.TryParse(infos["score"], out currScore) && currScore > Settings.StopScore)
                    {
                        if (FirstMate)
                        {
                            FirstMate = false;
                            string fen = Utils.BoardToFen(CurrentBoard, RedSide);
                            Engine.StopAnalyzeAndSkip(fen);
                            string[] moves = infos["pv"].Split(' ');
                            string ponderMove = "";
                            if (moves.Length >= 2) ponderMove = moves[1];
                            ReceiveBestMove(moves[0], ponderMove, ResultSource.Openbook, fen);
                        }
                    }
                    else
                    {
                        FirstMate = true;
                    }
                }
                double depthVal = 0;
                if (infos.ContainsKey("depth")) double.TryParse(infos["depth"], out depthVal);
                if (infos["score"].Contains("绝杀") && (Math.Abs(depthVal % 10) > 0.0001))
                {
                    return;
                }
                // 日志节流键（第二十一轮）：同一 (深度,候选名次) 只写一次，避免 MultiPV 高频刷日志卡 UI
                string infoKey = (infos.ContainsKey("depth") ? infos["depth"] : "0") + "|" + mpvRank;
                bool logDue = infoKey != _lastInfoLogKey || infos["score"].Contains("绝杀");
                if (logDue) _lastInfoLogKey = infoKey;
                int score;
                if (int.TryParse(infos["score"], out score))
                {
                    if (score >= 0)
                    {
                        if (logDue) SafeInvoke(new Action(() => textBox_engine_log.SelectionColor = PositiveColor));
                    }
                    else
                    {
                        if (logDue) SafeInvoke(new Action(() => textBox_engine_log.SelectionColor = NegativeColor));
                    }
                }
                else
                {
                    if (infos["score"].Contains("绝杀"))
                    {
                        if (logDue) SafeInvoke(new Action(() => textBox_engine_log.SelectionColor = MateColor));
                    }
                }
                info_str = "";
                if (BackgroundAnalyzing)
                {
                    info_str += "[后台思考] ";
                }
                string depthStr = infos.ContainsKey("depth") ? infos["depth"] : "0";
                double timeVal = 0, npsVal = 0;
                if (infos.ContainsKey("time")) double.TryParse(infos["time"], out timeVal);
                if (infos.ContainsKey("nps")) double.TryParse(infos["nps"], out npsVal);
                // 胜率：由评分换算（50% 为均势；绝杀按方向取 100%/0%）
                string winRateStr = "--";
                if (infos["score"].Contains("绝杀"))
                {
                    double mateN = 0;
                    if (double.TryParse(infos["score"].Split('(').Last().Split(')').First(), out mateN))
                    {
                        winRateStr = (mateN > 0 ? "100.0%" : "0.0%");
                        if (mainLine) pendingTrendScore = (float)(mateN > 0 ? 10000 : -10000);   // 趋势曲线：绝杀映射为 ±10000
                    }
                }
                else
                {
                    double cpVal = 0;
                    string cpRaw = infos["score"];
                    if (cpRaw.Length > 0 && (cpRaw[0] == '≥' || cpRaw[0] == '≤')) cpRaw = cpRaw.Substring(1);   // bound 前缀剥离
                    if (double.TryParse(cpRaw, out cpVal))
                    {
                        winRateStr = Math.Round(100.0 / (1.0 + Math.Pow(10, -cpVal / 400.0)), 1) + "%";
                        if (mainLine) pendingTrendScore = (float)cpVal;
                    }
                }
                if (!mainLine) info_str += "候选#" + mpvRank + " ";
                info_str += (
                    $"深度: {depthStr} " +
                    $"得分: {infos["score"]} " +
                    $"时间: {Math.Round(timeVal / 1000, 1)}秒 " +
                    $"nps: {Math.Round(npsVal / 1000)}K " +
                    "胜率: " + winRateStr +
                    "\r\n"
                    );
                if (logDue)
                {
                    didLog = true;
                    SafeInvoke(new Action(() =>
                    {
                        textBox_engine_log.AppendText(info_str);
                    }));
                }
            }
            if (infos.ContainsKey("pv"))
            {
                string[] moves = infos["pv"].Split(' ');
                if (moves.Length == 0 || string.IsNullOrEmpty(moves[0]) || moves[0].Length < 4)
                {
                    return;   // 畸形 pv：无法解析出着法，整行丢弃
                }
                // MultiPV：本线候选入快照（主线在下方继续走原有逻辑）
                MultiPvPut(mpvRank, moves[0], infos.ContainsKey("score") ? infos["score"] : "");
                if (!mainLine)
                {
                    // 非主线候选（第二十轮）：只更新候选箭头与简明日志，不驱动预期盘/自动停/落子
                    // 日志节流（第二十一轮）：同一 (深度,候选名次) 只写一次
                    string candKey = (infos.ContainsKey("depth") ? infos["depth"] : "0") + "|c" + mpvRank;
                    if (candKey != _lastCandLogKey)
                    {
                        _lastCandLogKey = candKey;
                        try
                        {
                            string s2 = "候选#" + mpvRank + "  " + Utils.FenToChina(EngineAnalyzingBoard, new string[] { moves[0] }, RedSide);
                            didLog = true;
                            SafeInvoke(new Action(() =>
                            {
                                textBox_engine_log.SelectionColor = NormalColor;
                                textBox_engine_log.AppendText(s2 + "\r\n");
                            }));
                        }
                        catch { }
                    }
                    RenderBoardFromEngineInfo();
                    return;
                }
                string fen = Utils.BoardToFen(EngineAnalyzingBoard, RedSide ? "w" : "b",RedSide ? "w" : "b");
                string bestMove = moves[0];
                Point fromPoint = Utils.Move2Point(bestMove.Substring(0, 2),RedSide);
                Point toPoint = Utils.Move2Point(bestMove.Substring(2, 2),RedSide);
                try
                {
                    info_str = Utils.FenToChina(EngineAnalyzingBoard, moves,RedSide) + "\r\n";
                    didLog = true;
                    SafeInvoke(new Action(() =>
                    {
                        textBox_engine_log.SelectionColor = NormalColor;
                        textBox_engine_log.AppendText(info_str + "\r\n");
                    }));
                }
                catch (Exception ex)
                {
                    // 解析/渲染异常不再弹窗打断对局（第二十轮）：记日志便于追因
                    Debug.WriteLine("pv 中文转换失败: " + ex);
                    try { File.AppendAllText("err.log", DateTime.Now.ToString() + "  [拦截] pv 中文转换异常: " + ex + Environment.NewLine); } catch { }
                }
                Point ponderFrom = new Point(-1, -1);
                Point ponderTo = new Point(-1, -1);
                if (moves.Length > 1 && moves[1].Length >= 4)
                {
                    string ponderMove = moves[1];
                    ponderFrom = Utils.Move2Point(ponderMove.Substring(0, 2), RedSide);
                    ponderTo = Utils.Move2Point(ponderMove.Substring(2, 2), RedSide);
                }
                // 把思考中的局面加入预期局面，如果对手走了就能快速确认，无需等待
                string[,] expectedSelfBoard = (string[,])EngineAnalyzingBoard.Clone();
                expectedSelfBoard[toPoint.X, toPoint.Y] = expectedSelfBoard[fromPoint.X, fromPoint.Y];
                expectedSelfBoard[fromPoint.X, fromPoint.Y] = null;
                string boardKey = fen + " moves " + bestMove;
                if (!ExpectedBoardMap.ContainsKey(boardKey))
                {
                    ExpectedBoardMap.Add(boardKey, expectedSelfBoard);
                }
                if (moves.Length > 1)
                {
                    string[,] expectedPonderBoard = (string[,])expectedSelfBoard.Clone();
                    expectedPonderBoard[ponderTo.X, ponderTo.Y] = expectedPonderBoard[ponderFrom.X, ponderFrom.Y];
                    expectedPonderBoard[ponderFrom.X, ponderFrom.Y] = null;
                    boardKey += " " + moves[1];
                    if (!ExpectedBoardMap.ContainsKey(boardKey))
                    {
                        ExpectedBoardMap.Add(boardKey, expectedPonderBoard);
                    }
                }
                if (!BackgroundAnalyzing)
                {
                    BackgroundAnalysisMove = new ChessMove(new Point(-1, -1), new Point(-1, -1));
                    BestMove = new ChessMove(fromPoint, toPoint);
                    if (moves.Length > 1)
                    {
                        PonderMove = new ChessMove(ponderFrom, ponderTo);
                    }
                }
                else
                {
                    BackgroundAnalysisMove = new ChessMove(fromPoint, toPoint);
                }

                RenderBoardFromEngineInfo();
            }
            if (didLog)
            {
                SafeInvoke(new Action(() =>
                {
                    if (checkBox_auto_scroll.Checked)
                    {
                        textBox_engine_log.ScrollToCaret();
                    }
                }));
            }
        }

        void ReceiveBestMove(string bestMove, string ponderMove, ResultSource source, string sourceFen="")
        {
            // 第十四轮崩溃加固：识别重置与引擎回调并发时关键字段可能正被清空，着法回调是可丢弃事件——
            // 拦截一切异常避免整进程崩溃（实测 02:02:28 崩溃点），异常全文记入 err.log 便于追因。
            try
            {
                ReceiveBestMoveCore(bestMove, ponderMove, source, sourceFen);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ReceiveBestMove 异常已拦截: " + ex);
                try { File.AppendAllText("err.log", DateTime.Now.ToString() + "  [拦截] 着法回调异常: " + ex + Environment.NewLine + Environment.NewLine); } catch { }
            }
        }

        void ReceiveBestMoveCore(string bestMove, string ponderMove, ResultSource source, string sourceFen="")
        {
            if (bestMove == null) return;   // 引擎线程空数据防护
            if (source == ResultSource.Engine) { _blacklistRetryAt = DateTime.MinValue; _blacklistRetryCount = 0; }   // R42：引擎正常出招 → 清除黑名单补发状态
            if (source == ResultSource.Engine)
            {
                FirstMate = true;
                // R22：分析队列已清空（检测重置/引擎重启）后到达的孤儿 bestmove——丢弃，避免按其旧盘补点（僵尸点击）。
                if (string.IsNullOrEmpty(sourceFen))
                {
                    DisplayStatus("忽略孤儿引擎着法（分析队列已清空）");
                    EngineHelper.IoLog("DROP", "orphan bestmove=" + bestMove);
                    return;
                }
                if (EngineAnalyzingBoard == null)
                {
                    // 识别已重置（分析局面被清空）时，迟到着法无处安放，直接丢弃
                    DisplayStatus("忽略引擎着法（识别已重置）");
                    return;
                }
                // 引擎计算期间持续识别（T5b）：局面已更新时丢弃属于旧局面的迟到着法
                if (!string.IsNullOrEmpty(sourceFen) && EngineAnalyzingBoard != null)
                {
                    string srcBoard = sourceFen.Split(' ')[0];
                    string curBoard = Utils.BoardToFen(EngineAnalyzingBoard, RedSide ? "w" : "b", RedSide ? "w" : "b");
                    int sp = curBoard.IndexOf(' ');
                    if (sp > 0) curBoard = curBoard.Substring(0, sp);
                    if (srcBoard != curBoard)
                    {
                        DisplayStatus("忽略过期引擎着法（局面已更新）");
                        EngineHelper.IoLog("DROP", "stale bestmove=" + bestMove);
                        // 断链恢复（第二十一轮）：无新搜索在飞时，对当前分析盘补发一次分析，
                        // 避免引擎空闲后无人重新启动（表现为「续不上、不能自己下棋」）。
                        try
                        {
                            if (Engine.AnalyzeQueue.Count == 0 && !ClickingSuspended)
                            {
                                string reGuard;
                                if (Utils.IsFenSafeForJieqiEngine(EngineAnalyzingBoard, out reGuard))
                                {
                                    EngineAnalyzeCount++;
                                    StartAnalyze(Utils.BoardToFen(EngineAnalyzingBoard, RedSide ? "w" : "b", RedSide ? "w" : "b"));
                                    DisplayStatus("补发引擎分析（前次着法已过期）");
                                    EngineHelper.IoLog("REISSUE", "已按当前盘补发（前次着法过期）");
                                }
                            }
                        }
                        catch (Exception rex) { Debug.WriteLine("补发分析失败: " + rex.Message); }
                        return;
                    }
                }
            }
            bestMove = bestMove.Trim('\0', ' ');
            ponderMove = ponderMove.Trim('\0', ' ');
            // R23：后台思考（ponder）期间引擎自行结束搜索提前返回的着法不是「对手已走出预判着法」的确认，
            // 绝不能直接点击（会凭空走子并把引擎拖入连锁紊乱）。暂存等待命中时采用；未命中作废。
            if (source == ResultSource.Engine && BackgroundAnalyzing)
            {
                try
                {
                    string sBoard = string.IsNullOrEmpty(sourceFen) ? "" : sourceFen.Split(' ')[0];
                    string cBoard = EngineAnalyzingBoard == null ? "" : Utils.BoardToFen(EngineAnalyzingBoard, RedSide ? "w" : "b", RedSide ? "w" : "b");
                    int csp = cBoard.IndexOf(' ');
                    if (csp > 0) cBoard = cBoard.Substring(0, csp);
                    if (bestMove.Length == 4 && sBoard != "" && sBoard == cBoard)
                    {
                        PonderReadyMove = bestMove;
                        PonderReadyPonder = ponderMove ?? "";
                        PonderReadyFen = sourceFen;
                        EngineHelper.IoLog("STASH", "后台思考提前完成，暂存着法 " + bestMove + "（命中时采用）");
                    }
                    else
                    {
                        EngineHelper.IoLog("DROP", "后台思考期间收到非当前局面着法 " + bestMove);
                    }
                }
                catch { }
                return;
            }
            BestMoveList.Add(bestMove);   // R23：只把真实出招记入长打列表
            string mySideStr = RedSide ? "r" : "b";
            if (bestMove.Length != 4) return;
            Point fromPoint = Utils.Move2Point(bestMove.Substring(0, 2),RedSide);
            Point toPoint = Utils.Move2Point(bestMove.Substring(2, 2),RedSide);
            // 非法着法串防护：畸形输出（字母越界等）会产生越界坐标，直接丢弃，避免数组越界/点击越界
            if (fromPoint.X < 0 || fromPoint.X > 8 || fromPoint.Y < 0 || fromPoint.Y > 9 ||
                toPoint.X < 0 || toPoint.X > 8 || toPoint.Y < 0 || toPoint.Y > 9)
            {
                Debug.WriteLine("忽略越界引擎着法: " + bestMove);
                return;
            }
            Point ponderFrom = new Point(-1, -1);
            Point ponderTo = new Point(-1, -1);
            BestMove = new ChessMove(fromPoint, toPoint);
            if (ponderMove != null && ponderMove.Length >= 4)
            {
                ponderFrom = Utils.Move2Point(ponderMove.Substring(0, 2),RedSide);
                ponderTo = Utils.Move2Point(ponderMove.Substring(2, 2),RedSide);
                PonderMove = new ChessMove(ponderFrom, ponderTo);
            }
            else
            {
                PonderMove = new ChessMove(new Point(-1, -1), new Point(-1, -1));
            }
            if (!BackgroundAnalyzing)
            {
                BackgroundAnalysisMove = new ChessMove(new Point(-1, -1), new Point(-1, -1));
            }
            RenderDisplayBoard();
            string fen = "";
            if (!string.IsNullOrEmpty(sourceFen))
            {
                fen = sourceFen;
            }
            else
            {
                fen = Utils.BoardToFen(CurrentBoard, RedSide);
            }
            if (!string.IsNullOrEmpty(fen))
            {
                string boardKey = fen + " moves " + bestMove;
                ExpectedSelfGoBoard = (string[,])EngineAnalyzingBoard.Clone();
                ExpectedSelfGoBoard[toPoint.X, toPoint.Y] = ExpectedSelfGoBoard[fromPoint.X, fromPoint.Y];
                ExpectedSelfGoBoard[fromPoint.X, fromPoint.Y] = null;
                if (!ExpectedBoardMap.ContainsKey(boardKey))
                {
                    ExpectedBoardMap.Add(boardKey, ExpectedSelfGoBoard);
                }
                ExpectedMove = bestMove;
                if (ponderMove != "")
                {
                    boardKey += " " + ponderMove;
                    string[,] expectedPonderBoard = (string[,])ExpectedSelfGoBoard.Clone();
                    expectedPonderBoard[ponderTo.X, ponderTo.Y] = expectedPonderBoard[ponderFrom.X, ponderFrom.Y];
                    expectedPonderBoard[ponderFrom.X, ponderFrom.Y] = null;
                    if (!ExpectedBoardMap.ContainsKey(boardKey))
                    {
                        ExpectedBoardMap.Add(boardKey, expectedPonderBoard);
                    }
                }
            }

            bool moveIsMine = EngineAnalyzingBoard[fromPoint.X, fromPoint.Y] != null && EngineAnalyzingBoard[fromPoint.X, fromPoint.Y].StartsWith(mySideStr + "_");
            if (Settings.AutoGo && !moveIsMine && !BackgroundAnalyzing && source == ResultSource.Engine)
            {
                DisplayStatus("引擎着法不属于我方棋子，已忽略（若持续出现请反馈日志）");
                EngineHelper.IoLog("DROP", "not-mine bestmove=" + bestMove);
            }
            // 第二十四轮：终点不得为己方棋子（防「自己吃自己」；对引擎盘面做一致性校验）。
            string dstPiece = EngineAnalyzingBoard != null ? EngineAnalyzingBoard[toPoint.X, toPoint.Y] : null;
            if (Settings.AutoGo && Utils.IsOwnDestination(EngineAnalyzingBoard, toPoint, RedSide) && !BackgroundAnalyzing && source == ResultSource.Engine)
            {
                DisplayStatus("引擎着法终点为己方棋子，已拦截（防自己吃自己；若持续出现请反馈日志）");
                EngineHelper.IoLog("DROP", "own-destination bestmove=" + bestMove + " dst=" + dstPiece);
                return;
            }
            if (Settings.AutoGo && moveIsMine)
            {
                if (source == ResultSource.ChessDB || source == ResultSource.Openbook)
                {
                    // 开局库/云库出招间隔：设了区间用区间随机，未设（0-0）沿用旧版「最短思考时间」
                    int bookGap = EngineHelper.PickGapMs(Settings.BookMoveGapMin, Settings.BookMoveGapMax);
                    if (bookGap > 0)
                    {
                        Thread.Sleep(bookGap);
                    }
                    else if (Settings.MinTimeUsingOpenbook > 0)
                    {
                        Thread.Sleep((int)(Settings.MinTimeUsingOpenbook * 1000));
                    }
                }
                // 趋势曲线：暂存本次出招评分（我方视角）；待检测确认“落子成功”后再绘制。
                // 后台思考（ponder）不得覆盖真实出招的暂存分；开局库/云库没有评分，清除旧暂存避免落到别的着法上。
                if (source == ResultSource.Engine && !BackgroundAnalyzing)
                {
                    float trendVal = pendingTrendScore;
                    if (!string.IsNullOrEmpty(sourceFen))
                    {
                        string[] fenParts = sourceFen.Split(' ');
                        string mySideChar = RedSide ? "w" : "b";
                        if (fenParts.Length >= 2 && fenParts[1] != mySideChar)
                        {
                            trendVal = -trendVal;
                        }
                    }
                    StashTrendPoint(trendVal);
                }
                else if (source != ResultSource.Engine)
                {
                    DiscardTrendPoint();
                }
                ChangeDetectedAfterClick = false;
                // 引擎最短出招间隔：算出着法后到点击落子之间随机等待 [min,max] 毫秒（0-0=不等待）
                if (source == ResultSource.Engine)
                {
                    int engineGap = EngineHelper.PickGapMs(Settings.EngineMoveGapMin, Settings.EngineMoveGapMax);
                    if (engineGap > 0)
                    {
                        Thread.Sleep(engineGap);
                    }
                }
                // 第二十四轮：点击前终审——等待/间隔之后复查分析盘未被换新（防「等待期间新确认」导致的错位点击）。
                // 仅对引擎着法启用（有来源 FEN 可比对；开局库/云库保持原行为）。
                try
                {
                    string preClickFen = Utils.BoardToFen(EngineAnalyzingBoard, RedSide ? "w" : "b", RedSide ? "w" : "b");
                    string preClickBoard = preClickFen.Split(' ')[0];
                    string receiveBoard = !string.IsNullOrEmpty(fen) ? fen.Split(' ')[0] : preClickBoard;
                    if (source == ResultSource.Engine && preClickBoard != receiveBoard)
                    {
                        DisplayStatus("着法已过期（等待期间局面已更新），放弃本次点击");
                        EngineHelper.IoLog("DROP", "pre-click stale bestmove=" + bestMove);
                        return;
                    }
                }
                catch { }
                // 第二十四轮：点击前复查起点——起点必须仍有我方棋子（防等待期间起点被吃/被换导致错点）。
                try
                {
                    string[,] srcRefBoard = source == ResultSource.Engine ? EngineAnalyzingBoard : CurrentBoard;
                    string preSrc = srcRefBoard != null ? srcRefBoard[fromPoint.X, fromPoint.Y] : null;
                    if (preSrc == null || !preSrc.StartsWith(mySideStr + "_"))
                    {
                        DisplayStatus("点击前复查失败：起点已无我方棋子，放弃本次点击");
                        EngineHelper.IoLog("DROP", "pre-click no-source bestmove=" + bestMove);
                        return;
                    }
                }
                catch { }
                // R38：暂停期间不点击（防「点了也没用」的空转；含迟到 bestmove 兜底拦截）
                if (ClickingSuspended)
                {
                    DisplayStatus("自动出招已暂停（连续落子未确认），本次着法未点击；手动走一手、切换鼠标模式或重新开始连线后恢复。");
                    EngineHelper.IoLog("DROP", "suspended skip click bestmove=" + bestMove);
                    return;
                }
                // 第二十五轮：点击结果决定后续——投递失败时不得记 PLAY、不得基于未落子启动后台思考
                // （原 async void 会让这里无条件继续，制造「程序以为走了、棋软没走」的状态）。
                bool clicked = PlayChess(bestMove);
                if (!clicked)
                {
                    DisplayStatus("落子点击未投递成功（窗口/坐标异常），本次不计入已走棋");
                    EngineHelper.IoLog("DROP", "click-not-delivered bestmove=" + bestMove);
                    return;
                }
                // 第二十五轮：点击耗时可能跨帧——点击期间若确认盘已被换新（新一帧确认），
                // 说明这次点击可能落在旧坐标上，不得据此启动后台思考（避免"以为走了"连锁）。
                string afterClickBoard = EngineAnalyzingBoard != null
                    ? Utils.BoardToFen(EngineAnalyzingBoard, RedSide ? "w" : "b", RedSide ? "w" : "b").Split(' ')[0]
                    : "";
                string srcBoardBefore = string.IsNullOrEmpty(fen) ? "" : fen.Split(' ')[0];
                if (srcBoardBefore != "" && afterClickBoard != srcBoardBefore)
                {
                    DisplayStatus("点击期间局面已更新，跳过本次后台思考（等待新一帧确认）");
                    EngineHelper.IoLog("DROP", "post-click board-changed bestmove=" + bestMove);
                    return;
                }
                EngineHelper.IoLog("PLAY", bestMove);
                if (Settings.BackgroundAnalysis && ExpectedSelfGoBoard != null && ponderMove != "" && !string.IsNullOrEmpty(fen) && source == ResultSource.Engine)
                {
                    PonderReadyMove = "";   // R23：新一轮后台思考，旧暂存作废
                    PonderReadyPonder = "";
                    PonderReadyFen = "";
                    PonderBoard = (string[,])ExpectedSelfGoBoard.Clone();
                    PonderBoard[ponderTo.X, ponderTo.Y] = PonderBoard[ponderFrom.X, ponderFrom.Y];
                    PonderBoard[ponderFrom.X, ponderFrom.Y] = null;
                    string pGuard;
                    if (!Utils.IsFenSafeForJieqiEngine(PonderBoard, out pGuard))   // R22：后台思考同样过安全守卫（此前漏检）
                    {
                        EngineHelper.IoLog("SKIP", "后台思考跳过（引擎不安全局面）：" + pGuard);
                    }
                    else
                    {
                        string ponderFen = Utils.BoardToFen(PonderBoard, RedSide);
                        double pSec; int pDep; bool pDepthOnly;
                        EngineHelper.ResolveGoParams(Settings, true, out pSec, out pDep, out pDepthOnly);   // R22：后台思考也遵循「时间设置」
                        // R26：返回布尔——黑名单/引擎不可用时不进入后台思考状态，也不切换分析盘
                        if (Engine.StartAnalyzePonder(ponderFen, pSec, pDep, pDepthOnly))
                        {
                            EngineAnalyzingBoard = (string[,])PonderBoard.Clone();
                            BackgroundAnalyzing = true;
                        }
                        else
                        {
                            EngineHelper.IoLog("SKIP", "后台思考未启动（黑名单/引擎不可用）");
                        }
                    }
                }
            }
        }

        private void Engine_BestMove_Event(string sourceFen, string bestMove, string ponderMove)
        {
            EngineAnalyzeCount--;
            if (EngineAnalyzeCount < 0) EngineAnalyzeCount = 0;   // 第二十一轮：计数防负漂移
            ReceiveBestMove(bestMove, ponderMove, ResultSource.Engine, sourceFen);
        }


        // 第二十五轮：由 async void 改同步 bool——原实现立刻返回，调用方会马上记 PLAY、更新预期盘并启动
        // 后台思考，而此刻两次点击可能还没发出去（甚至被守卫跳过多）。现在阻塞到两次点击真正投递完毕，
        // 并把结果返回给调用方（false 时调用方不得记 PLAY、不得基于未落子启动 ponder）。
        public bool PlayChess(string move)
        {
            if (move == null || move.Length != 4) return false;
            Point fromPoint = Utils.Move2Point(move.Substring(0, 2), RedSide);
            Point toPoint = Utils.Move2Point(move.Substring(2, 2), RedSide);
            if (fromPoint.X < 0 || fromPoint.X > 8 || fromPoint.Y < 0 || fromPoint.Y > 9 ||
                toPoint.X < 0 || toPoint.X > 8 || toPoint.Y < 0 || toPoint.Y > 9) return false;
            Point fromClickPoint = ClickPositionMap[fromPoint.X, fromPoint.Y];
            Point toClickPoint = ClickPositionMap[toPoint.X, toPoint.Y];
            ChangeDetectedAfterClick = false;
            return Mainform.MouseLeftClient_2PointSafe(fromClickPoint.X, fromClickPoint.Y, toClickPoint.X, toClickPoint.Y);
        }
    }
}
