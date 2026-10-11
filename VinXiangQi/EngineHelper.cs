using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using System.IO;
using System.Windows.Forms;

namespace VinXiangQi
{
    public class EngineHelper
    {
        // Deal with engines with UCCI Protocol for Chess Game.
        public string EngineType = "uci";
        public string EnginePath = "";
        public string EngineName = "";
        public string EngineAuthor = "";
        public Process Engine;
        public string LastBestMove = "";
        public string LastPonderMove = "";
        public string BanMoves = "";
        public event Action<string, string, string> BestMoveEvent;
        public event Action<string, Dictionary<string, string>> InfoEvent;
        public List<string> OptionList = new List<string>();
        public Thread ThreadHandleOutput;
        public Dictionary<string, string> Configs = new Dictionary<string, string>();
        public Queue<string> AnalyzeQueue = new Queue<string>();
        public List<string> SkipList = new List<string>();
        public DateTime LastOutputTime = new DateTime();
        public string IgnoreMove = "";
        public bool Initialized = false;
        // 引擎握手完成标志（收到 uciok/ucciok）——Init 等到握手完成再返回，
        // 避免"选项只收到一半就建表"导致的显示不全/枚举竞态
        public bool HandshakeDone = false;

        // ---- 第十五轮：引擎进程健康（揭棋引擎段错误会导致永久哑火）----
        // 揭棋分支 pikafish 在特定局面（暗子落在将/帅原格）会 0xc0000005 崩溃；
        // 旧实现没有任何 Exited 监听，引擎一死就再也收不到 bestmove，表现为"无法点击"。
        public volatile bool EngineDead = false;          // 进程已退出（Exited 回调置位）
        public event Action EngineExitedEvent;            // 供上层（Mainform）提示与复位
        public int RestartCount = 0;                      // 累计自动重启次数（用于限流）
        public DateTime LastRestartTime = DateTime.MinValue;
        static readonly object RestartLock = new object();// 多识别线程并发重启互斥

        // ---- R26：崩溃局面黑名单（切断「崩→重启→重发同帧→再崩」死循环）----
        // 实测 2026-10-09 21:05-21:06：换局/换边抖动生成的「暗子归属矛盾」盘使揭棋引擎
        // 0xC0000005 崩溃；崩溃恢复把同一盘面原样重发 4 次 → 60 秒内 3 连崩触发限流 →
        // 之后所有分析报「引擎不可用（已崩溃且未能重启）」、整局瘫痪且 err.log 被刷屏。
        // 对策：真实崩溃时记录「最后发出的盘面（FEN 第一段）」，同盘面短暂拉黑（到期自动重试）；
        // 用户重置识别（ResetDetection）时清空。
        public static readonly HashSet<string> CrashFens = new HashSet<string>();
        // R42：黑名单带 TTL——实测同一盘面早晚都能正常分析，崩溃取决于引擎当时状态而非盘面；
        // 永久拉黑合法盘会整局哑火（恢复补发始终被跳过）；到期自动放行，另配延迟补发恢复出招。
        static readonly Dictionary<string, DateTime> CrashFenTimes = new Dictionary<string, DateTime>();
        public const int CrashFenTtlSeconds = 60;
        static readonly object CrashFenLock = new object();
        string _lastSentFenBoard = "";
        string _lastBlacklistNotice = "";
        static DateTime _throttleLogTime = DateTime.MinValue;
        public static int CrashFenCount { get { lock (CrashFenLock) return CrashFens.Count; } }
        // 取 FEN 的盘面部分（第一段）作为黑名单键
        public static string FenBoardPart(string fen)
        {
            if (string.IsNullOrEmpty(fen)) return "";
            int i = fen.IndexOf(' ');
            return i > 0 ? fen.Substring(0, i) : fen;
        }
        public static bool IsBoardBlacklisted(string fen)
        {
            string p0 = FenBoardPart(fen);
            if (p0 == "") return false;
            lock (CrashFenLock)
            {
                if (!CrashFens.Contains(p0)) return false;
                DateTime t;
                if (CrashFenTimes.TryGetValue(p0, out t) &&
                    (DateTime.Now - t).TotalSeconds > CrashFenTtlSeconds)
                {
                    CrashFens.Remove(p0);   // R42：超过 TTL 放行重发
                    CrashFenTimes.Remove(p0);
                    return false;
                }
                return true;   // 无时间戳的手工条目（测试用）视为有效
            }
        }
        public static void ClearCrashFens()
        {
            lock (CrashFenLock) { CrashFens.Clear(); CrashFenTimes.Clear(); }
        }

        // R26：新开一局时调用——清空崩溃黑名单、重启节流与最后发送盘面记录（全新开始不背旧账）
        public void OnNewGameReset()
        {
            ClearCrashFens();
            RestartCount = 0;
            LastRestartTime = DateTime.MinValue;
            _lastSentFenBoard = "";
        }
        // 黑名单命中时的统一提示（同一盘面只提示一次，避免刷屏）
        void NotifyBlacklisted(string fen)
        {
            string key = FenBoardPart(fen);
            if (_lastBlacklistNotice == key) return;
            _lastBlacklistNotice = key;
            try { Mainform.StatusPub("该局面曾使引擎崩溃，已暂时跳过重发（稍后自动重试）"); } catch { }
        }


        // ---- R22 遥测：引擎 I/O 落盘（engine_io.log）----
        // 记录每条 position/go/stop/ponderhit 与 bestmove（含时间戳、进程号），
        // 配合识别循环每 10 秒的状态心跳（HB），用于离线复盘「一直思考/不落子」类问题。
        static readonly object IoLogLock = new object();
        static int _ioPid = 0;
        // 记录仪总开关（默认关；见 ProgramSettings.EngineIoLog）。关闭时直接返回：零磁盘写入。
        public static bool IsIoLogEnabled()
        {
            try { return Mainform.Settings != null && Mainform.Settings.EngineIoLog; }
            catch { return false; }
        }

        public static void IoLog(string tag, string text)
        {
            if (!IsIoLogEnabled()) return;
            try
            {
                lock (IoLogLock)
                {
                    if (_ioPid == 0) { try { _ioPid = Process.GetCurrentProcess().Id; } catch { _ioPid = -1; } }
                    File.AppendAllText("engine_io.log",
                        DateTime.Now.ToString("HH:mm:ss.fff") + " [" + _ioPid + "] " + tag + " " + text + "\r\n");
                }
            }
            catch { }
        }
        public DateTime LastGoTime = DateTime.MinValue;   // R22：最近一次 go 的时间（超时保护用）
        public bool LastGoWasPonder = false;              // R22：最近一次 go 是否为后台思考

        public EngineHelper(string enginePath, Dictionary<string, string> configs = null)
        {
            EnginePath = enginePath;
            if (configs != null)
            {
                Configs = configs;
            }
        }

        public void Stop()
        {
            try
            {
                if (Engine != null)
                {
                    ExpectedStop = true;   // 正常停止：Exited 回调不再记成崩溃/触发重启
                    _lastSentFenBoard = ""; // R26：主动停止不记录崩溃局面
                    Engine.Kill();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        // 正常停止标志：区分“用户主动停/换引擎/关窗”与“进程自己崩掉”
        public volatile bool ExpectedStop = false;

        public void Init()
        {
            Initialized = false;
            Debug.WriteLine("Init Engine");
            Engine = new Process();
            EnginePath = EnginePath.Replace("/", "\\");
            if (!File.Exists(EnginePath))
            {
                MessageBox.Show("找不到引擎 " + EnginePath + " !");
                return;
            }
            Engine.StartInfo.FileName = EnginePath;
            Engine.StartInfo.UseShellExecute = false;
            Engine.StartInfo.RedirectStandardInput = true;
            Engine.StartInfo.RedirectStandardOutput = true;
            Engine.StartInfo.RedirectStandardError = true;
            Engine.StartInfo.CreateNoWindow = true;
            string[] pathParams = EnginePath.Split('\\');
            Engine.StartInfo.WorkingDirectory = string.Join("\\", pathParams.Take(pathParams.Length - 1));
            Engine.OutputDataReceived += Engine_OutputDataReceived;
            // 第十五轮：监听进程退出。揭棋引擎在特定局面会 0xc0000005 崩溃，
            // 旧实现没有 Exited 监听 → 引擎死后永远收不到 bestmove → 用户看到"无法点击"。
            Engine.EnableRaisingEvents = true;
            Engine.Exited += Engine_ProcessExited;
            EngineDead = false;
            ExpectedStop = false;   // R26：新启动的进程不再视为「主动停止」，后续真实崩溃可被记录与恢复
            Engine.Start();
            int tryCount = 10;
            bool success = false;
            while (!success && tryCount > 0)
            {
                tryCount--;
                try
                {
                    Engine.PriorityClass = ProcessPriorityClass.BelowNormal;
                    success = true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("加载引擎时失败: " + ex.ToString());
                    Thread.Sleep(500);
                }
            }
            ThreadHandleOutput = new Thread(new ThreadStart(WaitForExit));
            ThreadHandleOutput.IsBackground = true;   // R22：关窗后不再驻留
            ThreadHandleOutput.Start();
            Engine.BeginOutputReadLine();
            HandshakeDone = false;
            OptionList.Clear();
            SendCommand("uci");
            SendCommand("ucci");
            // 等到握手完成（uciok/ucciok）为止：引擎选项是逐行陆续输出的，
            // 只等"收到任意选项"会拿到不完整的选项表（旧实现即如此）。
            // 上限 ~15 秒；个别引擎不回握手信息时到点返回，不阻塞后续流程。
            tryCount = 150;
            while (tryCount > 0 && !HandshakeDone)
            {
                tryCount--;
                Thread.Sleep(100);
            }
            if (OptionList.Count < 0)
            {
                throw new Exception("引擎 " + EnginePath + " 加载超时！");
            }
            foreach (var option in Configs)
            {
                if (IsButtonOption(option.Key)) continue;   // 按钮类选项（如 Clear Hash）不在启动时回放
                SetOption(option.Key, option.Value);
            }
            AnalyzeQueue.Clear();
            SkipList.Clear();                     // 第二十一轮：崩溃重启后清空配对表，避免旧 FEN 吃掉新 bestmove
            Mainform.EngineAnalyzeCount = 0;      // 同上：计数随重启归零
            Initialized = true;
        }

        void WaitForExit()
        {
            Engine.WaitForExit();
        }

        // 引擎进程退出回调（含崩溃）。置死亡标志、清握手状态，并通知上层。
        // 注意：此回调在引擎退出线程上执行，只做轻量操作，不在此处 Init()（重启由发令路径串行做）。
        private void Engine_ProcessExited(object sender, EventArgs e)
        {
            EngineDead = true;
            HandshakeDone = false;
            Initialized = false;
            if (ExpectedStop) return;   // 主动停止（换引擎/关窗）：不是崩溃，不触发告警与重启
            try
            {
                int code = -1;
                try { code = ((Process)sender).ExitCode; } catch { }
                File.AppendAllText("err.log", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    + "  [引擎] 进程已退出 (exitCode=" + code + ") 路径=" + EnginePath + "\r\n");
                // R26/R42：记录最后发出的盘面——同一盘面短暂拉黑（配合延迟补发，不再整局哑火）
                lock (CrashFenLock)
                {
                    if (!string.IsNullOrEmpty(_lastSentFenBoard))
                    {
                        if (CrashFens.Count > 500) { CrashFens.Clear(); CrashFenTimes.Clear(); }
                        CrashFens.Add(_lastSentFenBoard);
                        CrashFenTimes[_lastSentFenBoard] = DateTime.Now;
                        File.AppendAllText("err.log", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                            + "  [引擎] 已记录崩溃局面（短暂拉黑，到期自动恢复）\r\n");
                    }
                }
            }
            catch { }
            try { EngineExitedEvent?.Invoke(); } catch { }
        }

        // 发令前的健康检查：进程已死则串行重启一次（含重新握手 + 回放选项）。
        // 返回 true = 可用（原本活着或已成功重启）。
        public bool EnsureAlive()
        {
            if (Engine != null && !EngineDead && !SafeHasExited()) return true;
            lock (RestartLock)
            {
                // 双检：可能在等锁期间已被别的线程重启好
                if (Engine != null && !EngineDead && !SafeHasExited()) return true;
                // 限流：60 秒内最多重启 3 次，避免崩→重启→再崩的死循环
                if ((DateTime.Now - LastRestartTime).TotalSeconds < 60 && RestartCount >= 3)
                {
                    // R26：限流提示每 30 秒最多记一次（原先每 2 秒刷屏 err.log）
                    if ((DateTime.Now - _throttleLogTime).TotalSeconds > 30)
                    {
                        _throttleLogTime = DateTime.Now;
                        try { File.AppendAllText("err.log", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                            + "  [引擎] 60 秒内已重启 " + RestartCount + " 次，暂停自动重启（可能局面持续触发引擎缺陷）\r\n"); } catch { }
                    }
                    return false;
                }
                if ((DateTime.Now - LastRestartTime).TotalSeconds >= 60) RestartCount = 0;
                LastRestartTime = DateTime.Now;
                RestartCount++;
                try { File.AppendAllText("err.log", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    + "  [引擎] 正在自动重启 (第 " + RestartCount + " 次)\r\n"); } catch { }
                try
                {
                    if (Engine != null) { try { Engine.Exited -= Engine_ProcessExited; } catch { } }
                    Init();
                }
                catch (Exception ex)
                {
                    try { File.AppendAllText("err.log", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                        + "  [引擎] 自动重启失败: " + ex.Message + "\r\n"); } catch { }
                    return false;
                }
                bool ok = Engine != null && Initialized && !EngineDead;
                try { File.AppendAllText("err.log", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    + (ok ? "  [引擎] 自动重启成功\r\n" : "  [引擎] 自动重启后仍未就绪\r\n")); } catch { }
                return ok;
            }
        }

        // HasExited 在进程对象已释放时会抛异常，此处吞掉返回 true（视为不可用）
        public bool SafeHasExited()
        {
            try { return Engine.HasExited; } catch { return true; }
        }

        // 该选项是否为引擎上报的按钮类型（如 Clear Hash）——启动回放时跳过
        bool IsButtonOption(string name)
        {
            string prefix = "option name " + name + " ";
            foreach (string line in OptionList)
            {
                if (line == null) continue;
                if (line.StartsWith(prefix) && line.Substring(prefix.Length).StartsWith("type button")) return true;
            }
            return false;
        }

        private void Engine_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            // 第十四轮崩溃加固：引擎输出线程上的未捕获异常会终止整个进程（着法回调链的源头），逐行隔离
            if (e.Data == null) return;
            try
            {
                Debug.WriteLine("Engine Output: " + e.Data);
                LastOutputTime = DateTime.Now;
                HandleOutputLine(e.Data);
            }
            catch (Exception ex)
            {
                try { File.AppendAllText("err.log", DateTime.Now.ToString() + "  [拦截] 引擎输出处理异常: " + ex + "\r\n\r\n"); } catch { }
            }
        }

        // info 行 → 键值表（第二十轮：抽取为静态方法便于回归测试）。
        // 加固点：① 重复键覆盖不抛异常；② 尾部残缺 token 防护；③ score 支持 cp/mate/lowerbound/upperbound；
        // ④ wdl 四元组；⑤ pv/string/refutation/currline 取余行；⑥ 未知 token 按 key-value / flag 兜底。
        public static readonly System.Collections.Generic.HashSet<string> InfoKeySet =
            new System.Collections.Generic.HashSet<string>(
                "depth seldepth time nodes pv multipv score currmove currmovenumber hashfull nps tbhits cpuload string refutation currline wdl".Split(' '));

        public static System.Collections.Generic.Dictionary<string, string> ParseInfoTokens(string line)
        {
            var Infos = new System.Collections.Generic.Dictionary<string, string>();
            if (string.IsNullOrEmpty(line)) return Infos;
            string[] args = line.Trim().Split(' ');
            if (args.Length == 0 || args[0] != "info") return Infos;
            for (int i = 1; i < args.Length; i++)
            {
                string key = args[i];
                if (key == "pv" || key == "string" || key == "refutation" || key == "currline")
                {
                    Infos[key] = string.Join(" ", args.Skip(i + 1)).Trim();
                    break;
                }
                if (key == "score")
                {
                    if (i + 1 >= args.Length) { Infos["score"] = ""; break; }
                    if (args[i + 1] == "cp" && i + 2 < args.Length)
                    {
                        // UCI 标准：score cp <x> [lowerbound|upperbound]（bound 跟在值后面）
                        if (i + 3 < args.Length && (args[i + 3] == "lowerbound" || args[i + 3] == "upperbound"))
                        {
                            Infos["score"] = (args[i + 3] == "lowerbound" ? "≥" : "≤") + args[i + 2];
                            i += 3;
                        }
                        else { Infos["score"] = args[i + 2]; i += 2; }
                    }
                    else if (args[i + 1] == "mate" && i + 2 < args.Length)
                    {
                        if (i + 3 < args.Length && (args[i + 3] == "lowerbound" || args[i + 3] == "upperbound"))
                        {
                            Infos["score"] = (args[i + 3] == "lowerbound" ? "≥" : "≤") + "绝杀 (" + args[i + 2] + ")";
                            i += 3;
                        }
                        else { Infos["score"] = "绝杀 (" + args[i + 2] + ")"; i += 2; }
                    }
                    else if (args[i + 1] == "lowerbound" || args[i + 1] == "upperbound")
                    {
                        // 容错：非标准顺序（bound 在值前）
                        string v = (args[i + 1] == "lowerbound" ? "≥" : "≤");
                        if (i + 2 < args.Length) { v += args[i + 2]; i += 2; } else { i += 1; }
                        Infos["score"] = v;
                    }
                    else { Infos["score"] = args[i + 1]; i += 1; }
                    continue;
                }
                if (key == "wdl" && i + 3 < args.Length)
                {
                    // UCI 标准：wdl <win> <draw> <loss>（3 个千分比整数）
                    Infos["wdl"] = args[i + 1] + "/" + args[i + 2] + "/" + args[i + 3];
                    i += 3;
                    continue;
                }
                if (i + 1 < args.Length && !InfoKeySet.Contains(args[i + 1]))
                {
                    Infos[key] = args[i + 1];
                    i += 1;
                }
                else
                {
                    Infos[key] = "";
                }
            }
            return Infos;
        }

        public void HandleOutputLine(string line)
        {
            if (line == null) return;
            string[] args = line.Split(' ');
            string cmd = args[0];
            if (cmd == "info")
            {
                Dictionary<string, string> Infos = ParseInfoTokens(line);
                InfoEvent?.Invoke(cmd, Infos);
            }
            else if (cmd == "bestmove")
            {
                IoLog("RX", line.Trim());
                if (args.Length < 2) return;   // 畸形输出（仅有 "bestmove"）防护：索引越界会崩线程
                string sourceFen = "";
                if (AnalyzeQueue.Count > 0)
                {
                     sourceFen = AnalyzeQueue.Dequeue();
                }
                if (SkipList.Contains(sourceFen))
                {
                    SkipList.Remove(sourceFen);
                    return;
                }
                if (args.Length > 2)
                {
                    BestMoveEvent?.Invoke(sourceFen, args[1], args[3]);
                }
                else
                {
                    BestMoveEvent?.Invoke(sourceFen, args[1], "");
                }
            }
            else if (cmd == "option")
            {
                OptionList.Add(line);
            }
            else if (cmd == "id")
            {
                if (args.Length >= 3)
                {
                    string type = args[1].ToLower();
                    if (type == "name")
                    {
                        EngineName = string.Join(" ", args.Skip(2));
                    }
                    else if (type == "author")
                    {
                        EngineAuthor = string.Join(" ", args.Skip(2));
                    }
                }
            }
            else if (cmd == "ucciok")
            {
                EngineType = "ucci";
                HandshakeDone = true;
            }
            else if (cmd == "uciok")
            {
                HandshakeDone = true;
            }
        }

        public void SetOption(string key, string value)
        {
            if (EngineType == "ucci")
            {
                SendCommand("setoption " + key + " " + value);
            }
            else
            {
                SendCommand("setoption name " + key + " value " + value);
            }
        }

        // 读取当前引擎的 MultiPV 设置（第二十轮）：未配置/非法返回 1，上限 500 防异常值。
        // 识别结果箭头数量与「引擎输出」多线展示均以此为准，保证两处对引擎参数的处理一致。
        public int GetMultiPvSetting()
        {
            try
            {
                if (Configs != null && Configs.ContainsKey("MultiPV"))
                {
                    int n;
                    if (int.TryParse(Configs["MultiPV"], out n) && n > 0) return Math.Min(n, 500);
                }
            }
            catch { }
            return 1;
        }

        public void StopAnalyze()
        {
            if (EngineDead) return;   // 引擎已死：不向已关闭的 stdin 写命令
            SendCommand("stop");
        }

        // R34：落子失败自愈——清空分析队列与配对表，使同一局面可以重新发令。
        // 死锁成因：落子点击未生效 → 盘面不变 → ReloadBoard 因「盘面未变化」直接返回
        // → 同 FEN 又被 AnalyzeQueue 去重挡下 → 引擎永不再出招。此处提供手动解锁入口。
        public void ClearAnalyzeState()
        {
            try { AnalyzeQueue.Clear(); SkipList.Clear(); } catch { }
        }

        public async void StopAnalyzeAndSkip(string skipFen)
        {
            await Task.Run(new Action(() =>
            {
                SendCommand("stop");
                SkipList.Add(skipFen);
            }));
        }

        public void PonderMiss(string skipFen)
        {
            SendCommand("stop");
            SkipList.Add(skipFen);
        }

        public void PonderHit()
        {
            Debug.WriteLine("Ponder hit");
            SendCommand("ponderhit");
        }

        public void SendCommand(string cmd)
        {
            Debug.WriteLine("Engine Input: " + cmd);
            try
            {
                if (cmd != null)
                {
                    if (cmd.StartsWith("go ")) { LastGoTime = DateTime.Now; LastGoWasPonder = cmd.StartsWith("go ponder"); IoLog("TX", cmd); }
                    else if (cmd == "stop" || cmd == "ponderhit")
                    {
                        if (cmd == "ponderhit") { LastGoTime = DateTime.Now; LastGoWasPonder = false; }
                        IoLog("TX", cmd);
                    }
                    else if (cmd.StartsWith("position ")) IoLog("TX", cmd);
                }
            }
            catch { }
            if (Engine != null && Engine.StandardInput != null && !EngineDead)
            {
                try
                {
                    Engine.StandardInput.WriteLine(cmd);
                }
                catch (Exception ex)
                {
                    // 管道已断（引擎刚崩）：置死亡标志，由发令路径的重启逻辑接管
                    EngineDead = true;
                    Debug.WriteLine("SendCommand 失败: " + ex.Message);
                }
            }
            else
            {
                Debug.WriteLine("Stardard Input is null");
            }
        }

        public bool StartAnalyzePonder(string fen, double time_sec, int depth, bool depthOnly = false)
        {
            // R26：黑名单同样适用于后台思考（毒帧不放行）
            if (IsBoardBlacklisted(fen))
            {
                IoLog("SKIP", "曾使引擎崩溃的局面，已跳过后台思考 FEN=" + fen);
                return false;
            }
            if (!EnsureAlive())
            {
                try { File.AppendAllText("err.log", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    + "  [引擎] 不可用，跳过本次后台思考 FEN=" + fen + "\r\n"); } catch { }
                return false;   // R26：改为布尔返回（不再抛异常，调用方按结果处理）
            }
            _lastSentFenBoard = FenBoardPart(fen);   // R26：仅在真正发令前记录（确保只记录已发送盘面）
            AnalyzeQueue.Enqueue(fen);
            Debug.WriteLine("Start Analyzing Ponder: \n" + fen);
            SendCommand("position fen " + fen);
            if (BanMoves != "")
            {
                SendCommand("banmoves " + BanMoves);
                BanMoves = "";
            }
            if (!depthOnly && time_sec > 0)
            {
                SendCommand("go ponder movetime " + (int)(time_sec * 1000) + " depth " + depth);
            }
            else
            {
                SendCommand("go ponder " + "depth " + depth);
            }
            return true;
        }

        public void StartAnalyze(string fen, double time_sec, int depth)
        {
            // R26：崩溃黑名单——该盘面曾使引擎崩溃则跳过（切断「崩→重启→重发同帧→再崩」死循环）
            if (IsBoardBlacklisted(fen))
            {
                IoLog("SKIP", "曾使引擎崩溃的局面，已跳过不再重发 FEN=" + fen);
                try
                {
                    Mainform.EngineAnalyzeCount--;
                    if (Mainform.EngineAnalyzeCount < 0) Mainform.EngineAnalyzeCount = 0;
                }
                catch { }
                NotifyBlacklisted(fen);
                return;
            }
            // 第十五轮：发令前健康检查。引擎若已崩溃（揭棋分支在特定局面会段错误），
            // 旧实现会一直往死进程写指令 → 永远没有 bestmove → 表现为"无法点击"。
            if (!EnsureAlive())
            {
                try { File.AppendAllText("err.log", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    + "  [引擎] 不可用，跳过本次分析 FEN=" + fen + "\r\n"); } catch { }
                throw new InvalidOperationException("引擎不可用（已崩溃且未能重启）");
            }
            // R23：同一局面已在分析队列（前次发令尚未返回 bestmove）——不重复发令，
            // 防止引擎收到双重 position/go 后状态错乱（实测会拖出「强停不停、长时间无响应」的僵死路径）。
            if (AnalyzeQueue.Contains(fen))
            {
                IoLog("SKIP", "重复分析请求已跳过 FEN=" + fen);
                try { Mainform.EngineAnalyzeCount--; if (Mainform.EngineAnalyzeCount < 0) Mainform.EngineAnalyzeCount = 0; } catch { }   // 计数配平（调用方已 +1）
                return;
            }
            _lastSentFenBoard = FenBoardPart(fen);   // R26：仅在真正发令前记录（确保只记录已发送盘面）
            AnalyzeQueue.Enqueue(fen);
            Debug.WriteLine("Start Analyzing: \n" + fen);
            SendCommand("position fen " + fen);
            if (BanMoves != "")
            {
                SendCommand("banmoves " + BanMoves);
                BanMoves = "";
            }
            bool depthOnly;
            double tSec;
            int dep;
            ResolveGoParams(Mainform.Settings, true, out tSec, out dep, out depthOnly);
            // 调用方显式给出的 time_sec 优先（受「每步固定/计算深度」影响时由 ResolveGoParams 决定）
            if (!depthOnly && tSec <= 0) tSec = time_sec;
            if (!depthOnly && dep <= 0) dep = depth;
            if (depthOnly)
            {
                SendCommand("go depth " + dep);
            }
            else if (tSec > 0)
            {
                SendCommand("go movetime " + (int)(tSec * 1000) + " depth " + dep);
            }
            else
            {
                SendCommand("go depth " + dep);
            }
        }

        // 时间设置：按用户勾选项解析实际发令参数（默认全关 → 与旧版字节一致）
        // 优先级：每步固定 > 计算深度 > 前 N 回合步时 > 局时超时步时 > 默认步时
        // 返回 (timeSec, depth, useDepthOnly)：useDepthOnly=true 时发 `go depth N`（不带 movetime）
        public static void ResolveGoParams(ProgramSettings s, bool isOurMove, out double timeSec, out int depth, out bool depthOnly)
        {
            timeSec = 2.0;
            depth = 200;
            depthOnly = false;
            if (s == null) return;
            timeSec = s.EngineStepTime;
            depth = s.EngineDepth;
            if (s.TimeModeFixed)
            {
                timeSec = s.FixedStepSeconds;
                return;
            }
            if (s.TimeModeDepth)
            {
                depth = s.TimeModeDepthValue;
                depthOnly = true;
                return;
            }
            if (s.OpeningStepEnabled && isOurMove && Mainform.SelfMoveCount < s.OpeningStepMoves)
            {
                timeSec = s.OpeningStepSeconds;
                return;
            }
            if (s.GameTimeEnabled && Mainform.GameStartTime != DateTime.MinValue
                && (DateTime.Now - Mainform.GameStartTime).TotalMinutes >= s.GameTimeMinutes)
            {
                timeSec = s.GameTimeStepSeconds;
                return;
            }
        }

        // 最短出招间隔：返回需等待的毫秒数（区间 0~0 或未设置 → 0）
        // 最短出招间隔（秒）：返回需等待的毫秒数；0 或非法 → 0（不等待）
        public static int PickGapMs(double minSec, double maxSec)
        {
            if (maxSec <= 0) return 0;
            if (minSec < 0) minSec = 0;
            if (minSec > maxSec) minSec = maxSec;
            int lo = (int)Math.Round(minSec * 1000);
            int hi = (int)Math.Round(maxSec * 1000);
            if (hi <= 0) return 0;
            if (lo < 0) lo = 0;
            if (lo > hi) lo = hi;
            lock (RandLock)
            {
                return lo == hi ? lo : Rand.Next(lo, hi + 1);
            }
        }

        static readonly object RandLock = new object();
        static readonly Random Rand = new Random();
    }
}
