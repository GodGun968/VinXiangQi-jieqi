using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using Yolov5Net.Scorer;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace VinXiangQi
{
    public static class Utils
    {
        public struct BoardCompareResult
        {
            public Point From;
            public Point To;
            public string Chess;
            public string Target;
            public int DiffCount;
            public int RedDiff;
            public int BlackDiff;
        }
        [DllImport("kernel32.dll")]
        public static extern IntPtr LoadLibrary(string dllToLoad);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

        [DllImport("kernel32.dll")]
        public static extern bool FreeLibrary(IntPtr hModule);

        public static string intToChina(int x)
        {
            if (x < 0 || x > 9) return "";
            string[] stringNum = { "零", "一", "二", "三", "四", "五", "六", "七", "八", "九" };
            return stringNum[x];
        }
        
        public static string nameToChina(string name)
        {
            bool isRed = name.Substring(0, 1) == "r";
            name = name.Substring(2);
            if (name == "che")
            {
                name = "车";
            }
            else if (name == "ma")
            {
                name = "马";
            }
            else if (name == "xiang")
            {
                name = isRed ? "相" : "象";
            }
            else if (name == "shi")
            {
                name = isRed ? "仕" : "士";
            }
            else if (name == "jiang")
            {
                name = isRed ? "帅" : "将";
            }
            else if (name == "pao")
            {
                name = "炮";
            }
            else if (name == "bing")
            {
                name = isRed ? "兵" : "卒";
            }
            else if (name == "anzi")
            {
                name = "暗";
            }
            return name;
        }

        public static string ChangeStrToSBC(string str)
        {

            char[] c = str.ToCharArray();
            for (int i = 0; i < c.Length; i++)
            {
                byte[] b = System.Text.Encoding.Unicode.GetBytes(c, i, 1);
                if (b.Length == 2)
                {
                    if (b[1] == 0)
                    {
                        b[0] = (byte)(b[0] - 32);
                        b[1] = 255;
                        c[i] = System.Text.Encoding.Unicode.GetChars(b)[0];
                    }
                }
            }
            //半角  
            string strNew = new string(c);
            return strNew;
        }
        
        public static string FenToChina(string[,] cboard, string[] moves, bool redSide)
        {
            string[,] board = (string[,])cboard.Clone();
            moves = (string[])moves.Clone();
            for (int i = 0; i < moves.Length; i++)
            {
                string ret = "";
                Point fromPoint = Utils.Move2Point(moves[i].Substring(0, 2), redSide);
                Point toPoint = Utils.Move2Point(moves[i].Substring(2, 2), redSide);
                string name = board[fromPoint.X, fromPoint.Y];
                if (name == "" || name == null) continue;
                bool isRed = name.Substring(0, 1) == "r";

                int X1 = fromPoint.X + 1;
                int X2 = toPoint.X + 1;
                int Y = toPoint.Y - fromPoint.Y;
                if (redSide == isRed)
                {
                    X1 = 10 - X1;
                    X2 = 10 - X2;
                    Y = -1 * Y;
                }
                string MoveName = "";

                //车马炮兵,判断前中后
                if (name.Contains("che") || name.Contains("ma") || name.Contains("pao") || name.Contains("bing"))
                {
                    int Front = 0, Back = 0;
                    for (int j = 0; j < 10; j++)
                    {
                        if (board[fromPoint.X, j] == name)
                        {
                            if (j < fromPoint.Y) Front++;
                            if (j > fromPoint.Y) Back++;
                        }
                    }
                    if (Front > 0 || Back > 0)
                    {
                        if (Back == 0 && Front > 0)
                        {
                            MoveName = "后";
                        }
                        else if (Back > 0 && Front == 0)
                        {
                            MoveName = "前";
                        }
                        else if (Back == 1 && Front == 1)
                        {
                            MoveName = "中";
                        }
                        else
                        {
                            MoveName = intToChina(Front + 1);//需要转为大写汉字
                        }
                    }
                }
                string StartStr = "", EndStr = "";
                if (MoveName == "") StartStr = isRed ? intToChina(X1) : ChangeStrToSBC(X1 + "");
                if (isRed)
                {
                    EndStr = intToChina(X2);
                }
                else
                {
                    EndStr = ChangeStrToSBC(X2 + "");
                }
                MoveName += nameToChina(name);
                string MoveDir = "";
                if (Y == 0)
                {
                    MoveDir = "平" + (isRed ? intToChina(X2) : ChangeStrToSBC(X2 + ""));
                }
                else if (Y > 0)
                {
                    MoveDir = "进" + (isRed ? intToChina(Y) : ChangeStrToSBC(Y + ""));
                }
                else
                {
                    MoveDir = "退" + (isRed ? intToChina(-Y) : ChangeStrToSBC(-Y + ""));
                }

                board[fromPoint.X, fromPoint.Y] = "";
                board[toPoint.X, toPoint.Y] = name;

                name = name.Substring(2);
                if (name == "jiang")
                {
                    ret = MoveName + StartStr + MoveDir;
                }
                else if (name == "shi" || name == "xiang" || name == "ma")
                {
                    if (Y > 0)
                    {
                        MoveDir = "进";
                    }
                    else
                    {
                        MoveDir = "退";
                    }
                    ret = MoveName + StartStr + MoveDir + EndStr;
                }
                else
                {
                    ret = MoveName + StartStr + MoveDir;
                }
                moves[i] = ret;

            }
            return string.Join(" ", moves);
        }

        public static BoardCompareResult CompareBoard(string[,] from, string[,] to, bool fuzzyDark = false)
        {
            BoardCompareResult result = new BoardCompareResult();
            int diffCount = 0;
            if (from == null || to == null)
            {
                result.DiffCount = 32;
                return result;
            }
            int bFromCount = 0;
            int bToCount = 0;
            int rFromCount = 0;
            int rToCount = 0;
            for (int y = 0; y < 10; y++)
            {
                for (int x = 0; x < 9; x++)
                {
                    if (from[x, y] != null && from[x, y].Contains("b_"))
                    {
                        bFromCount++;
                    }
                    if (to[x, y] != null && to[x, y].Contains("b_"))
                    {
                        bToCount++;
                    }
                    if (from[x, y] != null && from[x, y].Contains("r_"))
                    {
                        rFromCount++;
                    }
                    if (to[x, y] != null && to[x, y].Contains("r_"))
                    {
                        rToCount++;
                    }
                    bool cellEqual = (from[x, y] == to[x, y]);
                    if (!cellEqual && fuzzyDark && IsDarkPiece(from[x, y]) && !IsDarkPiece(to[x, y]) && SameSide(from[x, y], to[x, y]))
                    {
                        // 揭棋：上一次推断该格为暗子，新检测同格为同色明子（暗子翻开后的真实身份），视为同一棋子，不计入差异
                        cellEqual = true;
                    }
                    if (!cellEqual)
                    {
                        if (to[x, y] == null)
                        {
                            result.From = new Point(x, y);
                            diffCount++;
                        }
                        else
                        {
                            result.To = new Point(x, y);
                            result.Chess = to[x, y];
                            result.Target = from[x, y];
                            diffCount++;
                        }
                    }
                }
            }
            result.RedDiff = rFromCount - rToCount;
            result.BlackDiff = bFromCount - bToCount;
            result.DiffCount = diffCount;
            return result;
        }

        // 是否为暗子格（揭棋：dark / b_anzi / r_anzi）
        public static bool IsDarkPiece(string cell)
        {
            if (string.IsNullOrEmpty(cell)) return false;
            return cell == "dark" || cell.EndsWith("_anzi");
        }

        // 视角判定（第十四轮，投票制）：上下九宫内两模型检出的“将/帅”按置信度投票，决定红方在底还是在顶。
        // 旧逻辑取单个最高置信 r_jiang 判向——揭棋模型偶发把对方将误标成 r_jiang 且分数高于真帅
        // （实测 020208 帧：误标 0.83 压过真帅 0.70），单框独裁会翻转视角、整盘随后被误判非法。
        // 规则：r_jiang 在下九宫 或 b_jiang 在上九宫 → 支持红在底；反之支持红在顶。
        // 平票或无有效候选时返回 null，调用方保持现有 RedSide 不变。
        public static bool? DecideRedSide(List<PieceDetection> jieqiList, List<PieceDetection> auxList, RectangleF board, float gridWidth, float gridHeight, out string voteDetail)
        {
            double redBottom = 0, redTop = 0;
            AccumulateKingVotes(jieqiList, board, gridWidth, gridHeight, ref redBottom, ref redTop);
            AccumulateKingVotes(auxList, board, gridWidth, gridHeight, ref redBottom, ref redTop);
            voteDetail = "红底票=" + Math.Round(redBottom, 2) + " 红顶票=" + Math.Round(redTop, 2);
            if (redBottom == redTop) return null;
            return redBottom > redTop;
        }

        // 累加单个检测列表的视角票：只有落在上/下九宫（列 3-5，行 0-2 或 7-9）的将/帅才计票
        static void AccumulateKingVotes(List<PieceDetection> list, RectangleF board, float gridWidth, float gridHeight, ref double redBottom, ref double redTop)
        {
            if (list == null || gridWidth <= 0f || gridHeight <= 0f) return;
            foreach (PieceDetection d in list)
            {
                if (d == null || (d.Name != "r_jiang" && d.Name != "b_jiang")) continue;
                double ratio = d.Rectangle.Height == 0f ? 0.0 : d.Rectangle.Width / d.Rectangle.Height;
                if (ratio > 1.3 || ratio < 0.7) continue;
                float cx = d.Rectangle.X + d.Rectangle.Width / 2f;
                float cy = d.Rectangle.Y + d.Rectangle.Height / 2f;
                int xp = (int)Math.Round((cx - board.X) / gridWidth);
                int yp = (int)Math.Round((cy - board.Y) / gridHeight);
                if (xp < 3 || xp > 5) continue;
                bool lower = yp >= 7 && yp <= 9;
                bool upper = yp >= 0 && yp <= 2;
                if (!lower && !upper) continue;
                bool supportsBottom = (d.Name == "r_jiang") ? lower : upper;
                if (supportsBottom) redBottom += d.Score; else redTop += d.Score;
            }
        }

        // 两个棋子名是否属于同一方
        public static bool SameSide(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
            return a.Substring(0, 1) == b.Substring(0, 1);
        }

        // 对比上一次推断的棋盘，列出"暗子翻开成什么"（用于状态栏提示）
        public static string DescribeReveals(string[,] from, string[,] to)
        {
            if (from == null || to == null) return "";
            string text = "";
            for (int y = 0; y < 10; y++)
            {
                for (int x = 0; x < 9; x++)
                {
                    if (IsDarkPiece(from[x, y]) && to[x, y] != null && !IsDarkPiece(to[x, y]) && SameSide(from[x, y], to[x, y]))
                    {
                        if (text != "") text += "、";
                        text += "行" + y + "列" + x + "翻开=" + nameToChina(to[x, y]);
                    }
                }
            }
            return text;
        }

        // 剔除超量裸暗子检测：全盘暗子物理上限 = 每方 15（合计 30），多出的按置信度从低到高剔除。
        // 用于把"模型多检出一个暗子"导致的合法局面误判救回（返回实际剔除数量；偏差过大返回 0 不处理）。
        public static int PruneExcessBareDarks(string[,] board, float[,] scores, int maxTotal)
        {
            if (board == null) return 0;
            List<Point> darks = new List<Point>();
            for (int y = 0; y < 10; y++)
            {
                for (int x = 0; x < 9; x++)
                {
                    if (board[x, y] == "dark") darks.Add(new Point(x, y));
                }
            }
            int excess = darks.Count - maxTotal;
            if (excess <= 0) return 0;
            if (excess > 6) return 0;   // 偏差过大视为识别错乱帧，交给常规校验/重置处理，不掩盖
            darks.Sort(delegate(Point a, Point b)
            {
                float sa = scores != null ? scores[a.X, a.Y] : 0f;
                float sb = scores != null ? scores[b.X, b.Y] : 0f;
                return sa.CompareTo(sb);
            });
            for (int i = 0; i < excess; i++)
            {
                board[darks[i].X, darks[i].Y] = null;
            }
            return excess;
        }

        // 像素判色：对"已翻开的明子"按棋子面像素的红/黑特征判色。
        // 背景：揭棋模型偶发把黑子标成 r_*、红子标成 b_*（实测日志出现 双红帅/缺黑将/三黑车），
        // 仅靠类名前缀会误判；真实截图探针实测：红明子红色像素占比 ≥0.449、黑明子 0.000，可分。
        // 返回 true=红 / false=黑 / null=不确定（保持模型原判）。
        public static bool? JudgeVisiblePieceColor(Bitmap img, RectangleF rect)
        {
            if (img == null) return null;
            int x0 = (int)Math.Ceiling(rect.X + rect.Width * 0.28f);
            int x1 = (int)Math.Floor(rect.X + rect.Width * 0.72f);
            int y0 = (int)Math.Ceiling(rect.Y + rect.Height * 0.28f);
            int y1 = (int)Math.Floor(rect.Y + rect.Height * 0.72f);
            if (x0 < 0) x0 = 0;
            if (y0 < 0) y0 = 0;
            if (x1 >= img.Width) x1 = img.Width - 1;
            if (y1 >= img.Height) y1 = img.Height - 1;
            if (x1 <= x0 || y1 <= y0) return null;
            int step = Math.Max(1, Math.Min(x1 - x0, y1 - y0) / 8);
            int total = 0, red = 0;
            for (int y = y0; y <= y1; y += step)
            {
                for (int x = x0; x <= x1; x += step)
                {
                    Color c = img.GetPixel(x, y);
                    total++;
                    if (c.R > 110 && c.R > c.G + 45 && c.R > c.B + 45) red++;
                }
            }
            if (total < 12) return null;
            float frac = (float)red / total;
            if (frac >= 0.20f) return true;    // 阈值 0.20：红≥0.45、黑=0.00，余量充足
            if (frac <= 0.08f) return false;
            return null;                       // 0.08~0.20 模糊带：保持模型原判
        }

        // 批量修正明子颜色前缀（board/obstacle/暗子不参与）；返回修正个数
        public static int CorrectVisiblePieceColors(Bitmap img, List<PieceDetection> detections)
        {
            if (img == null || detections == null) return 0;
            int fixedCount = 0;
            foreach (PieceDetection d in detections)
            {
                string n = d.Name;
                if (string.IsNullOrEmpty(n)) continue;
                if (n == "board" || n == "obstacle" || n == "dark" || n.EndsWith("_anzi")) continue;
                if (!(n.StartsWith("r_") || n.StartsWith("b_"))) continue;
                bool? isRed = JudgeVisiblePieceColor(img, d.Rectangle);
                if (isRed == null) continue;
                string want = (isRed.Value ? "r_" : "b_") + n.Substring(2);
                if (want != n)
                {
                    d.Name = want;
                    fixedCount++;
                }
            }
            return fixedCount;
        }

        // 明子超量剔除：每方每类型按上限保留置信度最高者（帅/将优先保留在九宫内的）。
        // 修复模型把明子前缀标错（如红车标成 b_che）或重复检出导致的"数量超限"误判；
        // 仅处理小幅超额（≤2），偏差过大交给常规校验，避免掩盖真实识别错误。
        public static int PruneExcessVisible(string[,] board, float[,] scores, bool redSide)
        {
            if (board == null) return 0;
            Dictionary<string, int> caps = new Dictionary<string, int>
            {
                { "jiang", 1 }, { "che", 2 }, { "ma", 2 }, { "pao", 2 }, { "xiang", 2 }, { "shi", 2 }, { "bing", 5 }
            };
            int pruned = 0;
            foreach (string side in new string[] { "r", "b" })
            {
                foreach (var kv in caps)
                {
                    List<Point> cells = new List<Point>();
                    for (int y = 0; y < 10; y++)
                    {
                        for (int x = 0; x < 9; x++)
                        {
                            string cell = board[x, y];
                            if (string.IsNullOrEmpty(cell) || cell.Length < 3) continue;
                            if (cell[0] != side[0]) continue;
                            if (cell.EndsWith("_anzi")) continue;
                            string[] parts = cell.Split('_');
                            if (parts.Length < 2 || parts[1] != kv.Key) continue;
                            cells.Add(new Point(x, y));
                        }
                    }
                    int excess = cells.Count - kv.Value;
                    if (excess <= 0 || excess > 2) continue;
                    bool preferPalace = (kv.Key == "jiang");
                    cells.Sort(delegate(Point a, Point b)
                    {
                        int pa = preferPalace ? (InOwnPalace(a.X, a.Y, side, redSide) ? 1 : 0) : 0;
                        int pb = preferPalace ? (InOwnPalace(b.X, b.Y, side, redSide) ? 1 : 0) : 0;
                        if (pa != pb) return pa.CompareTo(pb);   // 非九宫的帅优先被剔除
                        float sa = scores != null ? scores[a.X, a.Y] : 0f;
                        float sb = scores != null ? scores[b.X, b.Y] : 0f;
                        return sa.CompareTo(sb);                 // 低置信者先剔除
                    });
                    for (int i = 0; i < excess; i++)
                    {
                        board[cells[i].X, cells[i].Y] = null;
                        pruned++;
                    }
                }
            }
            return pruned;
        }

        // 该方棋子是否位于本方九宫（redSide=true 表示红方在下）
        static bool InOwnPalace(int x, int y, string side, bool redSide)
        {
            if (x < 3 || x > 5) return false;
            bool atBottom = (side == "r") == redSide;
            int yd = atBottom ? y : 9 - y;
            return yd >= 7;
        }

        // 将/帅九宫先验：非将类恒真；将类要求 x∈[3,5] 且位于本方九宫（复用 InOwnPalace 的方位语义）
        static bool JiangInPalace(string name, int x, int y, bool redAtBottom)
        {
            string[] p = name.Split('_');
            if (p.Length < 2 || p[1] != "jiang") return true;
            if (p[0] != "r" && p[0] != "b") return true;
            return InOwnPalace(x, y, p[0], redAtBottom);
        }

        // 交叉验证（揭棋模式）——用户口径：象棋模型负责明子，揭棋模型负责暗子。
        // 依据真实截图对比实验（象棋模型对暗子格 100% 误报成明子，故暗子格必须由揭棋模型强制覆盖）：
        //  ① 揭棋模型判暗的格 → 最终必为暗子（象棋模型在同一格的明子标签一律丢弃）；
        //  ② 其余格：象棋模型明子标签优先采用（含覆盖揭棋模型的明子标签、含补入空位）；
        //     仅当象棋模型自身超出该子力上限时才不接受（防「三车/双帅」），此时保留揭棋模型原判；
        //  ③ 揭棋模型明子、象棋模型该格为空 → 不补（揭棋模型只负责暗子），仅计日志；
        //  ④ 不做九宫否决：明子一律以象棋模型输出为准（JiangInPalace 仅保留给旧调用点，本函数不再使用）。
        // board/boardScores：终局网格（原地修改，调用方先写入揭棋识别结果）；
        // aux/auxScores：象棋模型网格（明子主来源）；redBottom：保留参数（当前不参与判定）。
        // 返回被修正的格子数。
        // 裁定来源（第十六轮）：某格最终结果由哪个模型决定 —— 分色绘制与日志都以此为准。
        // 铁律：颜色只表达“裁定模型”，不用来表达棋子红黑方（否则一个框两种语义，必然混乱）。
        public enum DetectionAuthority
        {
            None = 0,        // 无裁定（沿用主模型/上一帧/空格）
            ChessModel = 1,  // 象棋模型裁定（明子）→ 默认红
            JieqiModel = 2,  // 揭棋模型裁定（暗子或揭棋明子）→ 默认蓝
            BothAgree = 3    // 两模型结果一致且「高度可信不验证」开启 → 绿色
        }

        // 双模型逐格证据（第十七轮）：必须原样保留两个模型的**原始**类名与置信度。
        // 绘制时不得用合并后的分数顶替任一方，否则会出现「单独识别与交叉验证置信度不一致」。
        public class DualCell
        {
            public bool HasJieqi;                       // 揭棋模型在该格检出明子
            public string JieqiLabel;                   // 原始类名（含红黑前缀）
            public float JieqiScore;                    // 原始置信度
            public RectangleF JieqiBox;                 // 原始检测框（供绘制，缺框时用几何补）
            public bool HasChess;                       // 象棋模型在该格检出明子
            public string ChessLabel;
            public float ChessScore;
            public RectangleF ChessBox;
            public bool Agree;                          // 两模型均检出且类名完全相同（含前缀）
            public string FinalLabel;                   // 最终采用（null = 空格）
            public DetectionAuthority Authority;        // 最终裁定来源
        }

        static DualCell[,] _lastDualCells;
        public static DualCell[,] LastDualCells { get { return _lastDualCells; } }
        // 非双模型路径清空证据（避免上一帧绿框/双行串入单模型画面）
        public static void SetLastDualCells(DualCell[,] cells) { _lastDualCells = cells; }

        // 最近一次双模型合并的裁定来源图（每格一个来源），供识别画面分色绘制消费。
        // 整图原子替换：渲染线程只会读到“上一帧完整的图”，不会读到半新半旧。
        static DetectionAuthority[,] _lastMergeAuthority;
        public static DetectionAuthority[,] LastMergeAuthority { get { return _lastMergeAuthority; } }

        // 带阈值与来源图的合并（第十六轮用户口径）：
        //   ① 揭棋模型判暗子且 conf ≥ jieqiDarkConf      → 该格暗子，裁定=揭棋模型（象棋模型不得覆盖）
        //   ② 象棋模型给明子且 conf ≥ chessConf          → 该格明子，裁定=象棋模型
        //   ③ 两者同时达标且揭棋判暗子                    → 揭棋模型为主（历史口径）
        //   ④ 只一方达标                                  → 用达标那方
        //   ⑤ 都不达标                                    → 沿用旧回退规则（主模型原判/上限约束），来源 None/主模型
        public static int MergeDualModelGrids(string[,] board, float[,] boardScores,
                                              string[,] aux, float[,] auxScores, bool redBottom,
                                              float chessConf, float jieqiDarkConf,
                                              out DetectionAuthority[,] authority)
        {
            authority = new DetectionAuthority[9, 10];
            if (board == null || aux == null) { _lastMergeAuthority = authority; return 0; }
            Dictionary<string, int> caps = new Dictionary<string, int>
            {
                { "che", 2 }, { "ma", 2 }, { "xiang", 2 }, { "shi", 2 }, { "jiang", 1 }, { "pao", 2 }, { "bing", 5 }
            };
            Dictionary<string, int> counts = new Dictionary<string, int>();
            int fixes = 0;
            int darkForced = 0, visibleTook = 0, visibleKept = 0;
            for (int y = 0; y < 10; y++)
            {
                for (int x = 0; x < 9; x++)
                {
                    string j = board[x, y];
                    bool jDark = j != null && IsDarkPiece(j);
                    float jScore = boardScores != null ? boardScores[x, y] : 0f;
                    if (jDark)
                    {
                        // 主模型（揭棋）判为暗子：阈值达标则明确裁定为揭棋模型；否则仍保留暗子但来源置 None
                        counts[j] = counts.ContainsKey(j) ? counts[j] + 1 : 1;
                        authority[x, y] = jScore >= jieqiDarkConf ? DetectionAuthority.JieqiModel : DetectionAuthority.None;
                        continue;
                    }
                    string a = aux[x, y];
                    float aScore = auxScores != null ? auxScores[x, y] : 0f;
                    if (a == null || IsDarkPiece(a) || a == "board" || a == "obstacle")
                    {
                        // 该格无象棋模型明子：保留揭棋原判（若有）
                        if (j != null) { visibleKept++; authority[x, y] = DetectionAuthority.JieqiModel; }
                        continue;
                    }
                    string[] ap = a.Split('_');
                    if (ap.Length < 2 || !caps.ContainsKey(ap[1]))
                    {
                        if (j != null && !jDark) { visibleKept++; authority[x, y] = DetectionAuthority.JieqiModel; }
                        continue;
                    }
                    // 象棋模型给的是合法明子。是否够格裁定？不够格则保持揭棋原判。
                    bool chessQualified = aScore >= chessConf;
                    if (!chessQualified)
                    {
                        if (j != null && !jDark) { visibleKept++; authority[x, y] = jScore >= jieqiDarkConf ? DetectionAuthority.JieqiModel : DetectionAuthority.None; }
                        continue;
                    }
                    int used = counts.ContainsKey(a) ? counts[a] : 0;
                    bool canTake = used < caps[ap[1]];
                    if (!canTake)
                    {
                        if (j != null && !jDark) { visibleKept++; authority[x, y] = DetectionAuthority.JieqiModel; }
                        continue;
                    }
                    if (j != a)
                    {
                        if (j != null) counts[j] = Math.Max(0, (counts.ContainsKey(j) ? counts[j] : 0) - 1);
                        board[x, y] = a;
                        if (boardScores != null) boardScores[x, y] = aScore;
                        fixes++;
                    }
                    counts[a] = used + 1;
                    visibleTook++;
                    authority[x, y] = DetectionAuthority.ChessModel;   // 明子由象棋模型裁定
                }
            }
            darkForced = 0;
            for (int y = 0; y < 10; y++)
                for (int x = 0; x < 9; x++)
                    if (board[x, y] != null && IsDarkPiece(board[x, y])) darkForced++;
            Debug.WriteLine("交叉验证合并: 暗子保底=" + darkForced + " 象棋模型明子=" + visibleTook + " 揭棋明子保留=" + visibleKept + " 修正=" + fixes);
            _lastMergeDarkForced = darkForced;
            _lastMergeVisibleTook = visibleTook;
            _lastMergeVisibleKept = visibleKept;
            _lastMergeAuthority = authority;   // 整图原子替换
            return fixes;
        }

        // 旧签名转发（无阈值语义）：旧调用点/fixture 不带置信度，阈值门槛一律不生效，
        // 保持旧行为完全不变（这是 7 个 dual 测试与 dual_merge_pipeline_test 的既有契约）。
        public static int MergeDualModelGrids(string[,] board, float[,] boardScores, string[,] aux, float[,] auxScores, bool redBottom)
        {
            DetectionAuthority[,] _ignored;
            return MergeDualModelGrids(board, boardScores, aux, auxScores, redBottom, 0f, 0f, out _ignored);
        }

        // 第十七轮：在既有阈值仲裁之上，追加「逐格双模型证据 + 高度可信不验证」。
        // 调用方先填好 cells 的原始字段（HasJieqi/JieqiLabel/JieqiScore/JieqiBox 与 Chess 同）；
        // 本函数负责跑既有仲裁，再标注一致性 FinalLabel/Authority，并在 agreeSkipVerify 开启时
        // 把「两模型结果一致」的格子裁定改写为 BothAgree（识别画面用绿色绘制、不再比阈值）。
        // 注意：一致的格子无论是否勾选，最终标签都与仲裁结果相同（两个模型给出同一个标签），
        // 因此改写裁定只影响绘制颜色，不会改变送入引擎的盘面 —— 这是安全的兜底。
        // 第二十四轮：旧 10 参签名保留（转发），新增 visibleByMaxConf（明子按置信度高者裁定）。
        public static int MergeDualModelGridsEx(string[,] board, float[,] boardScores,
                                                string[,] aux, float[,] auxScores, bool redBottom,
                                                float chessConf, float jieqiDarkConf, bool agreeSkipVerify,
                                                DualCell[,] cells, out DetectionAuthority[,] authority)
        {
            return MergeDualModelGridsEx(board, boardScores, aux, auxScores, redBottom, chessConf, jieqiDarkConf,
                                         agreeSkipVerify, false, cells, out authority);
        }

        public static int MergeDualModelGridsEx(string[,] board, float[,] boardScores,
                                                string[,] aux, float[,] auxScores, bool redBottom,
                                                float chessConf, float jieqiDarkConf, bool agreeSkipVerify,
                                                bool visibleByMaxConf,
                                                DualCell[,] cells, out DetectionAuthority[,] authority)
        {
            bool[,] jieqiDark = new bool[9, 10];
            for (int y = 0; y < 10; y++)
                for (int x = 0; x < 9; x++)
                    jieqiDark[x, y] = board != null && board[x, y] != null && IsDarkPiece(board[x, y]);

            int fixes = MergeDualModelGrids(board, boardScores, aux, auxScores, redBottom, chessConf, jieqiDarkConf, out authority);
            // 纯原始置信度裁定（第二十五轮口径修正）：visibleByMaxConf 开启时**两个阈值都不参与**——
            // 先跑带阈值的 MergeDualModelGrids 会把盘先改掉，再比较就不是原始结果。
            // 覆盖式重判：两模型同格都有结果、标签不同 → 原始分高者胜（含暗子，不再有暗子优先）；
            // 标签相同 → 绿色（一致）并沿用该标签；分差 < 0.0005 → 相等，保持原仲裁不翻。
            // DualCell 的原始标签/分数不改写，供识别画面双行绘制使用。
            int maxConfFlips = 0;
            if (visibleByMaxConf && cells != null)
            {
                for (int y = 0; y < 10; y++)
                {
                    for (int x = 0; x < 9; x++)
                    {
                        DualCell c = cells[x, y];
                        if (c == null || !c.HasJieqi || !c.HasChess) continue;
                        if (string.IsNullOrEmpty(c.JieqiLabel) || string.IsNullOrEmpty(c.ChessLabel)) continue;
                        if (c.JieqiLabel == c.ChessLabel)
                        {
                            authority[x, y] = DetectionAuthority.BothAgree;
                            if (board != null) board[x, y] = c.JieqiLabel;
                            if (boardScores != null) boardScores[x, y] = Math.Max(c.JieqiScore, c.ChessScore);
                            continue;
                        }
                        float diff = c.JieqiScore - c.ChessScore;
                        bool jieqiWins = diff > 0.0005f;
                        bool chessWins = diff < -0.0005f;
                        if (!jieqiWins && !chessWins) continue;   // 相等：保持既有仲裁
                        string winLabel = jieqiWins ? c.JieqiLabel : c.ChessLabel;
                        float winScore = jieqiWins ? c.JieqiScore : c.ChessScore;
                        if (board != null && board[x, y] != winLabel)
                        {
                            board[x, y] = winLabel;
                            if (boardScores != null) boardScores[x, y] = winScore;
                            maxConfFlips++;
                        }
                        authority[x, y] = jieqiWins ? DetectionAuthority.JieqiModel : DetectionAuthority.ChessModel;
                    }
                }
            }
            // 第十七轮收尾（第二十五轮恢复并统一）：标注逐格双模型证据 ——
            //   Agree      两模型都检出、均为明子、标签完全相同（暗对明不算一致）
            //   FinalLabel 最终采用的标签；Authority 最终裁定来源
            // 「高度可信不验证」开启时把一致格裁定改写为 BothAgree（识别画面绘绿框、跳过阈值比较）。
            // 高置信块只改 board/authority，本循环以**最终结果**为准标注，两者不冲突。
            int agreeCount = 0, agreeKept = 0;
            for (int y = 0; y < 10; y++)
            {
                for (int x = 0; x < 9; x++)
                {
                    DualCell c = cells != null ? cells[x, y] : null;
                    if (c == null) continue;
                    bool agree = c.HasJieqi && c.HasChess && !jieqiDark[x, y]
                                 && !string.IsNullOrEmpty(c.JieqiLabel) && c.JieqiLabel == c.ChessLabel;
                    c.Agree = agree;
                    c.FinalLabel = board != null ? board[x, y] : null;
                    c.Authority = authority[x, y];
                    if (agree)
                    {
                        agreeCount++;
                        if (agreeSkipVerify)
                        {
                            authority[x, y] = DetectionAuthority.BothAgree;
                            c.Authority = DetectionAuthority.BothAgree;
                            agreeKept++;
                        }
                    }
                }
            }
            _lastMergeAuthority = authority;
            _lastDualCells = cells;   // 整图原子替换（null 表示本帧无双模型证据）
            Debug.WriteLine("交叉验证证据: 一致格=" + agreeCount + " 其中不验证=" + agreeKept
                + " 修正=" + fixes + " 纯原始分翻盘=" + maxConfFlips);
            return fixes + maxConfFlips;
        }

        // 最近一次合并的统计（供日志/测试读取）
        public static int _lastMergeDarkForced = 0;
        public static int _lastMergeVisibleTook = 0;
        public static int _lastMergeVisibleKept = 0;

        // 统计盘面全部暗子数量：bare 格用解析中的 color，其余格用盘面名（r_anzi/b_anzi）
        static void CountDarkTotals(string[,] board, string[,] color, out int r, out int b)
        {
            r = 0; b = 0;
            for (int y = 0; y < 10; y++)
            {
                for (int x = 0; x < 9; x++)
                {
                    string cc = color[x, y];
                    if (cc == "r") { r++; continue; }
                    if (cc == "b") { b++; continue; }
                    string cell = board[x, y];
                    if (cell == "r_anzi") r++;
                    else if (cell == "b_anzi") b++;
                }
            }
        }

        // 揭棋裸 dark 着色（含过河暗子）：按“移动匹配 + 颜色记忆 + 半区回退 + 数量校正”解析。
        // 引擎语义：暗子移动后保持暗置（可过河），因此仅按半区着色会把过河暗子染成对方颜色，
        // 造成一方暗子计数超 15 → CheckBoardValid 误判“不合法”。
        // prevBoard = 上一检测帧的棋盘（含已解析的 r_anzi/b_anzi），可为 null。
        public static void ResolveDarkColors(string[,] board, string[,] prevBoard, bool redSide)
        {
            if (board == null) return;
            List<Point> bare = new List<Point>();
            for (int y = 0; y < 10; y++)
            {
                for (int x = 0; x < 9; x++)
                {
                    if (board[x, y] == "dark") bare.Add(new Point(x, y));
                }
            }
            if (bare.Count == 0) return;

            string[,] color = new string[9, 10];      // "r" / "b"
            bool[,] confident = new bool[9, 10];

            // ① 移动匹配（必须最先做）：上一帧某暗子格“消失”、本帧某格出现裸暗子 → 视为同一子移动，颜色继承。
            //    若该移动是“吃子”（目标格上一帧放着对方暗子），同格继承会把目标格误染成原占位者的颜色，
            //    因此先做移动匹配、后做同格继承。
            //    贪心按棋盘距离升序配对，只使用来源颜色已知（r_anzi/b_anzi）的格子。
            if (prevBoard != null)
            {
                List<Point> gone = new List<Point>();
                for (int y = 0; y < 10; y++)
                {
                    for (int x = 0; x < 9; x++)
                    {
                        string pc = prevBoard[x, y];
                        bool prevDark = (pc == "r_anzi" || pc == "b_anzi" || pc == "dark");
                        string nc = board[x, y];
                        bool nowDark = (nc == "r_anzi" || nc == "b_anzi" || nc == "dark");
                        if (prevDark && !nowDark) gone.Add(new Point(x, y));   // 上一帧有暗子、本帧该格已无暗子（离开/被吃）
                    }
                }
                List<Point> need = new List<Point>();
                foreach (Point p in bare)
                {
                    if (color[p.X, p.Y] == null) need.Add(p);
                }
                List<int[]> pairs = new List<int[]>();
                for (int i = 0; i < need.Count; i++)
                {
                    for (int j = 0; j < gone.Count; j++)
                    {
                        string pc = prevBoard[gone[j].X, gone[j].Y];
                        if (pc != "r_anzi" && pc != "b_anzi") continue;   // 来源颜色未知（裸 dark）不用于匹配
                        int dx = need[i].X - gone[j].X;
                        int dy = need[i].Y - gone[j].Y;
                        pairs.Add(new int[] { dx * dx + dy * dy, i, j });
                    }
                }
                pairs.Sort(delegate(int[] a, int[] b) { return a[0].CompareTo(b[0]); });
                bool[] usedNeed = new bool[need.Count];
                bool[] usedGone = new bool[gone.Count];
                foreach (int[] pr in pairs)
                {
                    int i = pr[1], j = pr[2];
                    if (usedNeed[i] || usedGone[j]) continue;
                    usedNeed[i] = true;
                    usedGone[j] = true;
                    Point dst = need[i];
                    color[dst.X, dst.Y] = (prevBoard[gone[j].X, gone[j].Y] == "r_anzi") ? "r" : "b";
                    confident[dst.X, dst.Y] = true;
                }
            }

            // ② 同格继承：未能通过移动匹配定色的裸暗子格，若上一帧同格为已解析暗子 → 颜色沿用
            if (prevBoard != null)
            {
                foreach (Point p in bare)
                {
                    if (color[p.X, p.Y] != null) continue;
                    string pc = prevBoard[p.X, p.Y];
                    if (pc == "r_anzi") { color[p.X, p.Y] = "r"; confident[p.X, p.Y] = true; }
                    else if (pc == "b_anzi") { color[p.X, p.Y] = "b"; confident[p.X, p.Y] = true; }
                }
            }

            // ③ 半区回退：无任何来源信息时按半区着色
            foreach (Point p in bare)
            {
                if (color[p.X, p.Y] == null)
                {
                    color[p.X, p.Y] = ((p.Y < 5) != redSide) ? "r" : "b";
                }
            }

            // ④ 数量校正：每方暗子合计 ≤ 15（含盘面已解析色的暗子；超限说明有裸暗子被染错色）。
            //    对“裸暗子中低置信、距本方底线更远”者改判对方——过河暗子恰是这一类，会被改回来。
            int rc, bc;
            CountDarkTotals(board, color, out rc, out bc);
            if (rc > 15)
            {
                List<Point> cand = new List<Point>();
                foreach (Point p in bare) { if (color[p.X, p.Y] == "r") cand.Add(p); }
                cand.Sort(delegate(Point a, Point b)
                {
                    if (confident[a.X, a.Y] != confident[b.X, b.Y]) return confident[a.X, a.Y] ? 1 : -1;
                    // 次键降序：距红方底线越远的先改判
                    int da = redSide ? 9 - a.Y : a.Y;
                    int db = redSide ? 9 - b.Y : b.Y;
                    return db.CompareTo(da);
                });
                int flip = rc - 15;
                if (flip > cand.Count) flip = cand.Count;
                for (int i = 0; i < flip; i++)
                {
                    color[cand[i].X, cand[i].Y] = "b";
                }
            }
            // 重新统计（红方改判会改变黑方数量），再做黑方校正
            CountDarkTotals(board, color, out rc, out bc);
            if (bc > 15)
            {
                List<Point> cand = new List<Point>();
                foreach (Point p in bare) { if (color[p.X, p.Y] == "b") cand.Add(p); }
                cand.Sort(delegate(Point a, Point b)
                {
                    if (confident[a.X, a.Y] != confident[b.X, b.Y]) return confident[a.X, a.Y] ? 1 : -1;
                    // 次键降序：距黑方底线越远的先改判
                    int da = redSide ? a.Y : 9 - a.Y;
                    int db = redSide ? b.Y : 9 - b.Y;
                    return db.CompareTo(da);
                });
                int flip = bc - 15;
                if (flip > cand.Count) flip = cand.Count;
                for (int i = 0; i < flip; i++)
                {
                    color[cand[i].X, cand[i].Y] = "r";
                }
            }

            foreach (Point p in bare)
            {
                board[p.X, p.Y] = (color[p.X, p.Y] == "r") ? "r_anzi" : "b_anzi";
            }
        }

        // prevBoard 中是否已含明确颜色的暗子（r_anzi/b_anzi）——用于判断能否作为颜色记忆源
        public static bool HasResolvedDark(string[,] board)
        {
            if (board == null) return false;
            for (int y = 0; y < 10; y++)
            {
                for (int x = 0; x < 9; x++)
                {
                    string c = board[x, y];
                    if (c == "r_anzi" || c == "b_anzi") return true;
                }
            }
            return false;
        }

        public static string BoardToFen(string[,] Board, bool redSide)
        {
            return BoardToFen(Board, redSide ? "w" : "b", redSide ? "w" : "b");
        }

        public static string BoardToFen(string[,] Board, string myPos, string nextPlayer)
        {
            Dictionary<string, string> FenMap = new Dictionary<string, string>
            {
                { "che",  "r" }, {"ma", "n"}, {"xiang", "b"}, {"shi", "a"}, {"jiang", "k"}, {"pao", "c" }, {"bing", "p" }
            };
            string fen = "";
            int emptyCount = 0;
            if (Board == null) return "";
            if (myPos == "w")
            {
                for (int y = 0; y < 10; y++)
                {
                    for (int x = 0; x < 9; x++)
                    {
                        if (Board[x, y] == null)
                        {
                            emptyCount++;
                        }
                        else
                        {
                            if (emptyCount > 0)
                            {
                                fen += emptyCount.ToString();
                                emptyCount = 0;
                            }
                            string[] nameInfo = Board[x, y].Split('_');
                            if (nameInfo.Length < 2) continue;
                            if (nameInfo[1] == "anzi" || nameInfo[1] == "dark") fen += (nameInfo[0] == "r") ? "X" : "x";
                            else if (nameInfo[0] == "r") fen += FenMap[nameInfo[1]].ToUpper();
                            else fen += FenMap[nameInfo[1]];
                        }
                    }
                    if (emptyCount > 0)
                    {
                        fen += emptyCount.ToString();
                        emptyCount = 0;
                    }
                    fen += "/";
                }
            }
            else
            {
                for (int y = 9; y >= 0; y--)
                {
                    for (int x = 8; x >= 0; x--)
                    {
                        if (Board[x, y] == null)
                        {
                            emptyCount++;
                        }
                        else
                        {
                            if (emptyCount > 0)
                            {
                                fen += emptyCount.ToString();
                                emptyCount = 0;
                            }
                            string[] nameInfo = Board[x, y].Split('_');
                            if (nameInfo.Length < 2) continue;
                            if (nameInfo[1] == "anzi" || nameInfo[1] == "dark") fen += (nameInfo[0] == "r") ? "X" : "x";
                            else if (nameInfo[0] == "r") fen += FenMap[nameInfo[1]].ToUpper();
                            else fen += FenMap[nameInfo[1]];
                        }
                    }
                    if (emptyCount > 0)
                    {
                        fen += emptyCount.ToString();
                        emptyCount = 0;
                    }
                    fen += "/";
                }
            }
            
            fen = fen.Substring(0, fen.Length - 1) + " " + nextPlayer;
            if (Mainform.Settings.JieqiMode)
            {
                // 揭棋：追加暗子池（初始 2/2/2/2/2/5 减盘面明子，按类不低于 0），格式 board side pocket 0 1
                string pocket = GetJieqiPocket(Board);
                if (pocket != "") fen += " " + pocket + " 0 1";
            }
            return fen;
        }

        // ---- 第十五轮：揭棋引擎 FEN 安全守卫 ----
        // 实测（gdb 栈 + 38 例对照）：揭棋分支 pikafish 的 Position::set() 在 FEN 的
        // 将/帅原位（rank0/rank9 的 e 列）出现暗子 X/x 时，会造出一枚"暗的王"，
        // 导致 square<KING>() 指向暗子格、set_check_info 走 CannonMagics 越界
        // → 0xc0000005 段错误（引擎进程直接死掉，程序再也收不到 bestmove）。
        // 引擎侧已修（position.cpp 归一化镜像 + 跳过王位），此处再做一层发送前拦截：
        // 任何外部引擎/未修复引擎遇到这类局面都会被挡下，改为保留上一合法盘面。
        public static bool IsFenSafeForJieqiEngine(string[,] Board, out string reason)
        {
            reason = "";
            if (Board == null) { reason = "盘面为空"; return false; }
            if (!Mainform.Settings.JieqiMode) return true;

            int darkCount = 0;
            for (int y = 0; y < 10; y++)
            {
                for (int x = 0; x < 9; x++)
                {
                    string cell = Board[x, y];
                    if (string.IsNullOrEmpty(cell)) continue;
                    if (IsDarkPiece(cell))
                    {
                        darkCount++;
                        continue;
                    }
                }
            }
            // 规则一：将/帅原位是暗子 —— 引擎会造出第二枚"暗的王"，
            // set_check_info 走 CannonMagics 越界（0xc0000005）。实测：必崩。
            string c0 = Board[4, 0], c9 = Board[4, 9];
            if (!string.IsNullOrEmpty(c0) && IsDarkPiece(c0)) { reason = "黑将原位(4,0)是暗子 " + c0; return false; }
            if (!string.IsNullOrEmpty(c9) && IsDarkPiece(c9)) { reason = "红帅原位(4,9)是暗子 " + c9; return false; }
            // 规则二（实测 2026-10-06，gdb 定位）：将/帅不在原位（九宫外/中路离位）
            // 且盘面存在暗子时，Position::set() 中 put_piece 使用越界的棋子索引
            // （pieceCount[pc]++ 写越界）→ 同样段错误。此构型引擎侧未修完，
            // 发送前拦截，保留上一合法盘面等下一帧。
            bool kingOffHome = true;
            for (int y = 0; y < 10 && kingOffHome; y++)
            {
                for (int x = 0; x < 9; x++)
                {
                    if (y != 0 && y != 9) continue;
                    if (x != 4) continue;
                    string cell = Board[x, y];
                    if (string.IsNullOrEmpty(cell)) continue;
                    string[] args = cell.Split('_');
                    if (args.Length >= 2 && args[1] == "jiang") { kingOffHome = false; break; }
                }
            }
            if (kingOffHome && darkCount > 0)
            {
                reason = "将/帅均不在原位而盘面有 " + darkCount + " 个暗子";
                return false;
            }
            return true;
        }

        // 揭棋暗子池：按类型统计"初始数量 - 盘面明子数"，clamp 0，省略零计数
        public static string GetJieqiPocket(string[,] Board)
        {
            Dictionary<string, string> FenMap = new Dictionary<string, string>
            {
                { "che", "r" }, { "ma", "n" }, { "xiang", "b" }, { "shi", "a" }, { "pao", "c" }, { "bing", "p" }
            };
            Dictionary<string, int> initial = new Dictionary<string, int>
            {
                { "R", 2 }, { "N", 2 }, { "B", 2 }, { "A", 2 }, { "C", 2 }, { "P", 5 },
                { "r", 2 }, { "n", 2 }, { "b", 2 }, { "a", 2 }, { "c", 2 }, { "p", 5 }
            };
            Dictionary<string, int> visible = new Dictionary<string, int>();
            if (Board != null)
            {
                for (int y = 0; y < 10; y++)
                {
                    for (int x = 0; x < 9; x++)
                    {
                        string cell = Board[x, y];
                        if (string.IsNullOrEmpty(cell) || !cell.Contains("_")) continue;
                        string[] nameInfo = cell.Split('_');
                        if (nameInfo.Length < 2 || !FenMap.ContainsKey(nameInfo[1])) continue;
                        string letter = FenMap[nameInfo[1]];
                        if (nameInfo[0] == "r") letter = letter.ToUpper();
                        if (visible.ContainsKey(letter)) visible[letter]++;
                        else visible.Add(letter, 1);
                    }
                }
            }
            string pocket = "";
            foreach (string letter in new string[] { "R", "N", "B", "A", "C", "P", "r", "n", "b", "a", "c", "p" })
            {
                int count = initial[letter] - (visible.ContainsKey(letter) ? visible[letter] : 0);
                if (count < 0) count = 0;
                if (count > 0) pocket += letter + count.ToString();
            }
            return pocket;
        }

        public static string MirrorFenLeftRight(string fen)
        {
            // rnbaka2r/9/1c2b1nc1/p1p1p1p1p/9/2P6/P3P1P1P/1CN4C1/9/R1BAKABNR w - - 0 1
            string[] args = fen.Split(' ');
            string board = args[0];
            string[] rows = board.Split('/');
            List<string> newRows = new List<string>();
            for (int i = 0; i < rows.Length; i++)
            {
                string row = rows[i];
                string newRow = "";
                for (int j = row.Length - 1; j >= 0; j--)
                {
                    newRow += row[j];
                }
                newRows.Add(newRow);
            }
            string newBoard = string.Join("/", newRows);
            return newBoard + " " + string.Join(" ", args.Skip(1));
        }

        public static string MirrorFenRedBlack(string fen)
        {
            // rnbaka2r/9/1c2b1nc1/p1p1p1p1p/9/2P6/P3P1P1P/1CN4C1/9/R1BAKABNR w - - 0 1
            string[] args = fen.Split(' ');
            string board = args[0];
            string[] rows = board.Split('/');
            List<string> newRows = new List<string>();
            for (int i = rows.Length - 1; i >= 0; i--)
            {
                string newRow = "";
                foreach (char c in rows[i])
                {
                    string s = c.ToString();
                    if (c >= 'a' && c <= 'z')
                    {
                        newRow += s.ToUpper();
                    }
                    else
                    {
                        newRow += s.ToLower();
                    }
                }
                newRows.Add(newRow);
            }
            string newBoard = string.Join("/", newRows);
            string nextPlayer = args[1];
            nextPlayer = nextPlayer == "b" ? "w" : "b";
            return newBoard + " " + nextPlayer + " " + string.Join(" ", args.Skip(2));
        }

        public static bool CheckChessmanValid(string chess, int x, int y, bool redSide)
        {
            string[] args = chess.Split('_');
            if (args.Length < 2) return true;   // 名称不完整（模型异常输出如裸 dark）：不据此判非法
            string side = args[0];
            string type = args[1];
            if (Mainform.Settings.JieqiMode && type != "jiang")
            {
                // 揭棋：翻开的士/象/卒可能出现在非标准位置（移动后翻开），跳过站位检查
                return true;
            }
            if (!redSide)
            {
                y = 9 - y;
            }
            if (side == "r")
            {
                if (type == "jiang")
                {
                    if (x >= 3 && x <= 5 && y >= 7 && y <= 9)
                    {
                        return true;
                    }
                    return false;
                }
                else if (type == "shi")
                {
                    if (x == 3 && y == 7 || x == 5 && y == 7 || x == 4 && y == 8 || x == 3 && y == 9 || x == 5 && y == 9)
                    {
                        return true;
                    }
                    return false;
                }
                else if (type == "xiang")
                {
                    if (x == 2 && y == 9 || x == 6 && y == 9 ||
                        x == 0 && y == 7 || x == 4 && y == 7 || x == 8 && y == 7 ||
                        x == 2 && y == 5 || x == 6 && y == 5)
                    {
                        return true;
                    }
                    return false;
                }
                else if (type == "bing")
                {
                    if ((y == 5 || y == 6) && (x == 0 || x == 2 || x == 4 || x == 6 || x == 8))
                    {
                        return true;
                    }
                    else if (y <= 4)
                    {
                        return true;
                    }
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                if (type == "jiang")
                {
                    if (x >= 3 && x <= 5 && y >= 0 && y <= 2)
                    {
                        return true;
                    }
                    return false;
                }
                else if (type == "shi")
                {
                    if (x == 3 && y == 0 || x == 5 && y == 0 || x == 4 && y == 1 || x == 3 && y == 2 || x == 5 && y == 2)
                    {
                        return true;
                    }
                    return false;
                }
                else if (type == "xiang")
                {
                    if (x == 2 && y == 0 || x == 6 && y == 0 || 
                        x == 0 && y == 2 || x == 4 && y == 2 || x == 8 && y == 2 ||
                        x == 2 && y == 4 || x == 6 && y == 4)
                    {
                        return true;
                    }
                    return false;
                }
                else if (type == "bing")
                {
                    if ((y == 3 || y == 4) && (x == 0 || x == 2 || x == 4 || x == 6 || x == 8))
                    {
                        return true;
                    }
                    else if (y >= 5)
                    {
                        return true;
                    }
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        public static bool CheckBoardValid(string[,] board, bool redSide)
        {
            string reason;
            return CheckBoardValid(board, redSide, out reason);
        }

        // 带原因的版盘合法性校验（原因用于状态栏提示与 jieqi_invalid.log）
        public static bool CheckBoardValid(string[,] board, bool redSide, out string reason)
        {
            reason = "";
            Dictionary<string, int> counts = new Dictionary<string, int>();
            Dictionary<string, int> maxCounts = new Dictionary<string, int>
            {
                { "che", 2 },
                { "ma", 2 },
                { "pao", 2 },
                { "xiang", 2 },
                { "shi", 2 },
                { "jiang", 1 },
                { "bing", 5 },
                { "anzi", 15 }
            };
            for (int y = 0; y < 10; y++)
            {
                for (int x = 0; x < 9; x++)
                {
                    if (!string.IsNullOrEmpty(board[x, y]))
                    {
                        string chessMan = board[x, y];
                        if (counts.ContainsKey(chessMan))
                        {
                            counts[chessMan]++;
                        }
                        else
                        {
                            counts.Add(chessMan, 1);
                        }
                        if (!CheckChessmanValid(chessMan, x, y, redSide))
                        {
                            reason = chessMan + " 位置非法@" + x + "," + y;
                            return false;
                        }
                    }
                }
            }
            foreach (var c in counts)
            {
                string[] nameParts = c.Key.Split('_');
                if (nameParts.Length < 2) continue;
                string type = nameParts[1];
                if (maxCounts.ContainsKey(type) && c.Value > maxCounts[type])
                {
                    reason = c.Key + " 数量超限 " + c.Value + ">" + maxCounts[type];
                    return false;
                }
            }
            if (!counts.ContainsKey("b_jiang") || !counts.ContainsKey("r_jiang"))
            {
                reason = "缺少将帅(" + (counts.ContainsKey("r_jiang") ? "" : "红帅 ") + (counts.ContainsKey("b_jiang") ? "" : "黑将") + ")";
                return false;
            }
            return true;
        }

        // 第二十四轮：终点己方子防御（防「自己吃自己」）——到达格已有己方棋子（含暗子）即拒绝；
        // 盘面为 null 视为无法判定（返回 false，不拦截）；越界视为非法（返回 true，拦截）。
        public static bool IsOwnDestination(string[,] board, Point to, bool redSide)
        {
            if (board == null) return false;
            if (to.X < 0 || to.X > 8 || to.Y < 0 || to.Y > 9) return true;
            string pc = board[to.X, to.Y];
            return pc != null && pc.StartsWith(redSide ? "r_" : "b_");
        }

        public static Point Move2Point(string move, bool redSide)
        {
            int x = move[0] - 'a';
            int y = move[1] - '0';
            if (redSide)
            {
                return new Point(x, 9 - y);
            }
            else
            {
                return new Point(8 - x, y);
            }
        }

        public static string Point2Move(Point from, Point to)
        {

            string[] letters = "a b c d e f g h i j k l m n o p q r s t u v w x y z".Split(' ');
            string move = "";
            move += letters[from.X];
            move += (9 - from.Y).ToString();
            move += letters[to.X];
            move += (9 - to.Y).ToString();
            return move;
        }

        public static Rectangle ExpendArea(Rectangle area, Size maxSize)
        {
            float gridWidth = area.Width / 8;
            float gridHeight = area.Height / 9;
            Rectangle newArea = new Rectangle((int)(area.X - gridWidth), (int)(area.Y - gridHeight), (int)(area.Width + gridWidth * 2), (int)(area.Height + gridHeight * 2));
            if (newArea.X < 0) newArea.X = 0;
            if (newArea.X + newArea.Width > maxSize.Width) newArea.Width = maxSize.Width - newArea.X;
            if (newArea.Y < 0) newArea.Y = 0;
            if (newArea.Y + newArea.Height > maxSize.Height) newArea.Height = maxSize.Height - newArea.Y;
            return newArea;
        }

        public static Rectangle RestoreArea(Rectangle cropArea, Rectangle boardArea)
        {
            return new Rectangle(cropArea.X + boardArea.X, cropArea.Y + boardArea.Y, boardArea.Width, boardArea.Height);
        }
    }
}
