using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VinXiangQi
{
    public class ProgramSettings
    {
        public Dictionary<string, EngineSettings> EngineList = new Dictionary<string, EngineSettings>();
        public string SelectedEngine = "";
        public float ScaleFactor = 1.0f;
        public bool AutoGo = true;
        public double EngineStepTime = 2.0;
        public int StopScore = 2000;
        public int EngineDepth = 200;
        public int ThreadCount = 4;
        public int DetectionConfirmCount = 1;
        public int DetectionInterval = 550;
        public bool KeepDetecting = false;
        public bool UniversalMode = false;
        public bool UniversalMouse = false;
        public string SelectedSolution = "天天象棋";
        public bool AnalyzingMode = false;
        public bool AutoClick = false;
        public bool StopWhenMate = false;
        public string YoloModel = "nano.onnx";
        public bool JieqiMode = false;
        public Dictionary<string, string> LayoutPositions = new Dictionary<string, string>();
        public Dictionary<string, string> LayoutSplits = new Dictionary<string, string>();
        public int ScoreTrendMode = 0;   // 趋势曲线显示：0=分数 1=胜率
        // 布局版本：控件增删/重排后 +1；旧版本保存的 LayoutPositions 一律丢弃，
        // 避免恢复旧矩形把已删除控件的位置套到新控件上（用户反馈的「组件重叠/重复」根因）。
        public int LayoutVersion = 0;
        public const int CurrentLayoutVersion = 2;
        public int ClickHoldMs = 40;     // 单次点击按下-抬起间隔(ms)
        public int ClickPreWaitMs = 30;  // 点击前光标到位等待(ms)
        public int ClickRetryCount = 2;  // 落子失败自动重试次数（检测到未落子时）
        public int BackgroundClickStrategy = 0;  // 后台点击序列：0=兼容(默认) 1=标准 2=激活式
        public int InvalidResetCount = 6;  // 连续多少次识别不合法后重置识别范围（T5a）
        public bool DualModelValidate = true;  // 交叉验证：揭棋模式双模型协作（象棋模型认明子，揭棋模型判暗子）
        // ---- 交叉验证（第十六轮）----
        // 阈值口径：某格置信度达到阈值时，该模型对该格有裁定权。
        //   象棋模型 ≥ DualChessConfThreshold → 该格明子以象棋模型为准（用户口径：0.85）
        //   揭棋模型判暗子且 ≥ DualJieqiDarkConfThreshold → 该格判为暗子（用户口径：0.95）
        //   两者同时达标且揭棋判为暗子 → 揭棋模型为主（历史口径：识别到明子的地方被写了暗子则以揭棋模型为主）
        public float DualChessConfThreshold = 0.85f;      // 象棋模型棋子置信度阈值
        public float DualJieqiDarkConfThreshold = 0.95f;  // 揭棋模型暗子置信度阈值
        // 高度可信不验证：同一格两模型结果一致时跳过阈值比较、直接采用，识别画面画绿色。
        // 默认开（两个模型都确认同一结果时直接信任；可在「识别设置」关闭）。
        public bool DualAgreeSkipVerify = true;
        // 明子按置信度高者裁定（第二十四轮）：同一格两模型都识别为明子时，比较原始置信度，
        // 谁高以谁为准（识别画面按胜出模型着色）。揭棋判暗子达阈值时仍优先，不受此选项影响。
        // 默认关（保持旧行为：明子按象棋模型阈值仲裁）。
        public bool DualVisibleByMaxConf = false;
        public string DualChessModel = "";    // 交叉验证用象棋模型文件名（空=跟随主模型 YoloModel）
        public string DualJieqiModel = "";    // 交叉验证用揭棋模型文件名（空=跟随揭棋检测器当前模型）
        // ---- 识别绘制（第十六轮）----
        // 按“裁定模型”给识别画面上的框/类名/置信度分色：象棋模型裁定=红，揭棋模型裁定=蓝，两模型一致=绿。
        public bool DrawOverlay = true;          // 总开关：识别画面叠加绘制
        public bool DrawBox = true;              // 绘制检测框
        public bool DrawClassName = true;        // 绘制类名
        public bool DrawConfidence = true;       // 绘制置信度
        public int DrawBoxThickness = 2;         // 框线粗细（1~8）
        public int DrawChessColorArgb = unchecked((int)0xFFFF0000);  // 象棋模型裁定颜色（默认纯红）
        public int DrawJieqiColorArgb = unchecked((int)0xFF0000FF);  // 揭棋模型裁定颜色（默认纯蓝）
        public int DrawAgreeColorArgb = unchecked((int)0xFF00AA00);  // 两模型一致（高度可信）颜色（默认绿）

        // 可调检测/交互参数（默认值与旧硬编码一致；旧 settings.json 缺字段时自动取默认值）
        public float JieqiConfThreshold = 0.5f;
        public float JieqiNmsThreshold = 0.45f;
        public int ScreenshotRetryCount = 2;
        public int ClickInterval = 300;
        public bool UseOpenBook = false;
        public bool UseChessDB = false;
        public bool BackgroundAnalysis = false;
        // ---- 引擎记录仪（第二十二轮补；默认关）----
        // 开启后把引擎通信（position/go/stop/bestmove）与 10 秒状态心跳写入程序目录 engine_io.log，
        // 用于排查「一直思考/不落子」类问题。默认关：避免频繁写盘；排查时在主界面「引擎设置」
        // 勾选「引擎记录」再复现问题。
        public bool EngineIoLog = false;
        public double MinTimeUsingOpenbook = 1.0;
        public OpenBookMode OpenbookMode = OpenBookMode.HighestScore;
        // ---- 时间设置（第六轮新增；全部默认关闭，关闭时出招行为与旧版完全一致）----
        // 每步固定：勾选后强制用 FixedStepSeconds 作为 movetime（与「计算深度」互斥，本项优先）
        public bool TimeModeFixed = false;
        public double FixedStepSeconds = 2.0;
        // 计算深度模式：勾选后发 `go depth N`（不传 movetime，由深度决定停走）
        public bool TimeModeDepth = false;
        public int TimeModeDepthValue = 200;
        // 前 N 回合使用步时：开局自走的前 N 回合，用 OpeningStepSeconds 覆盖默认步时
        public bool OpeningStepEnabled = false;
        public int OpeningStepMoves = 10;
        public double OpeningStepSeconds = 2.0;
        // 局时 N 分钟后使用步时：本局开始 N 分钟后，改用 GameTimeStepSeconds 覆盖默认步时
        public bool GameTimeEnabled = false;
        public int GameTimeMinutes = 10;
        public double GameTimeStepSeconds = 2.0;
        // 引擎最短出招间隔（毫秒）：引擎算出着法到点击落子之间的随机等待区间，0-0 = 不等待
        public double EngineMoveGapMin = 0;   // 引擎最短出招间隔（秒，0=不等待）
        public double EngineMoveGapMax = 0;
        // 开局库最短出招间隔（毫秒）：走开局库/云库分支的随机等待区间，0-0 = 沿用 MinTimeUsingOpenbook
        public double BookMoveGapMin = 0;     // 开局库最短出招间隔（秒，0=沿用 MinTimeUsingOpenbook）
        public double BookMoveGapMax = 0;
       
        public enum OpenBookMode
        {
            HighestScore,
            Random
        }

        public EngineSettings CurrentEngine
        {
            get
            {
                if (EngineList.ContainsKey(SelectedEngine))
                {
                    return EngineList[SelectedEngine];
                }
                return null;
            }
        }
    }

    public class EngineSettings
    {
        public string ExePath = "";
        public string Name = "";
        public string Author = "";
        public Dictionary<string, string> Configs = new Dictionary<string, string>();
    }
}
