using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace VinXiangQi
{
    public partial class EngineSettingsForm : Form
    {
        public EngineSettings CurrentEngineSettings;
        public EngineHelper Engine;

        readonly ToolTip toolTip_option = new ToolTip();

        // 竖版表格引用（宽度随视口、高度按内容）
        TableLayoutPanel engineTable;
        // 共享粗体字体（表头/选项名；随窗体释放）
        Font boldFont;
        // 列宽重排防重入
        bool layoutGuard = false;

        // 滚动/重绘时自底向上合成绘制，消除拖动滚动条时的花屏残影（WS_EX_COMPOSITED）
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000;
                return cp;
            }
        }

        public EngineSettingsForm(EngineHelper engine, EngineSettings currentEngine)
        {
            Engine = engine;
            CurrentEngineSettings = currentEngine;
            InitializeComponent();
            toolTip_option.AutoPopDelay = 15000;
            toolTip_option.InitialDelay = 300;
            toolTip_option.ReshowDelay = 200;
            toolTip_option.ShowAlways = true;
            boldFont = new Font(this.Font, FontStyle.Bold);
            panel_scroll.ClientSizeChanged += delegate { RelayoutColumns(); };
        }

        // 单个引擎选项的解析结果
        class EngineOption
        {
            public string Name;
            public string Type;                 // check / spin / combo / button / string
            public string Default;
            public string Min;
            public string Max;
            public List<string> Choices = new List<string>();
        }

        // 常见引擎选项的中文说明（悬停提示；未知选项给通用说明，含类型/默认值/范围）
        static string DescribeOption(EngineOption o)
        {
            string range = "";
            if (o.Type == "spin" && o.Min != null && o.Max != null) range = "，范围 " + o.Min + " ~ " + o.Max;
            string def = o.Default != null ? "，默认 " + o.Default : "";
            string choices = o.Choices.Count > 0 ? "，可选：" + string.Join("/", o.Choices.ToArray()) : "";
            switch (o.Name)
            {
                case "Threads":
                    return "线程数：引擎搜索使用的 CPU 线程。调大=搜索更快但占用 CPU 更多（过大可能拖慢点击/识别）；调小=棋力下降。建议 4~8" + def + range;
                case "Hash":
                    return "哈希表大小(MB)：引擎局面缓存容量。调大=缓存更多局面、棋力略升（占内存）；调小=反复重算。建议 128~1024" + def + range;
                case "Clear Hash":
                    return "清空哈希：立即清除引擎的局面缓存（不影响其他设置）";
                case "Ponder":
                    return "后台思考：轮到对手时引擎继续计算，对手快出招时我方响应更快" + def;
                case "MultiPV":
                    return "多线路分析：同时显示多个候选着法及评分。调大=看得多但更费性能" + def + range;
                case "Skill Level":
                    return "棋力等级(0~20)：调小=故意走弱（陪练用）；20=全力" + def + range;
                case "Move Overhead":
                    return "走子延迟补偿(ms)：为落子/网络延迟预留的时间。调小=棋力强但可能超时；调大=更保守" + def + range;
                case "Slow Mover":
                    return "慢速系数：调大=时间分配更保守、走得更慢" + def + range;
                case "UCI_LimitStrength":
                    return "限制棋力开关：开启后按下面 Elo 设置出招（陪练用）" + def;
                case "UCI_Elo":
                    return "限制的棋力(Elo)：配合限制棋力开关使用，数值越小越弱" + def + range;
                case "EvalFile":
                    return "评估文件：NNUE 权重文件路径（一般不用改）" + def;
                case "NumaPolicy":
                    return "NUMA 策略：多路 CPU 的亲和性设置（普通配置不用改）" + def;
                case "Debug Log File":
                    return "调试日志文件：引擎运行日志路径（一般不用改）" + def;
            }
            string typeCn = o.Type == "check" ? "开关" : (o.Type == "spin" ? "数值" : (o.Type == "combo" ? "选项" : "操作"));
            return "引擎选项：" + o.Name + "（" + typeCn + "）" + def + range + choices;
        }

        private void EngineSettingsForm_Load(object sender, EventArgs e)
        {
            // 重建时先释放旧控件（避免句柄/内存累积）
            panel_scroll.SuspendLayout();
            while (panel_scroll.Controls.Count > 0)
            {
                Control old = panel_scroll.Controls[0];
                panel_scroll.Controls.RemoveAt(0);
                old.Dispose();
            }
            engineTable = null;   // 旧表已释放：置空，避免后续回调访问失效引用

            // ---------- 1) 解析引擎上报的全部选项 ----------
            // 快照一份：引擎输出线程可能仍在追加（避免枚举期间集合被修改）
            List<string> rawOptions = new List<string>(Engine.OptionList);
            List<EngineOption> options = new List<EngineOption>();
            bool prunedButtonKeys = false;
            foreach (string line in rawOptions)
            {
                string[] args = line.Split(' ');
                if (args.Length < 5 || args[0] != "option") continue;
                int name_end_index;
                if (Engine.EngineType == "ucci")
                {
                    name_end_index = 1;
                    for (int i = 1; i < args.Length; i++)
                    {
                        if (args[i] == "type") name_end_index = i - 1;
                    }
                }
                else
                {
                    name_end_index = 2;
                    for (int i = 2; i < args.Length; i++)
                    {
                        if (args[i] == "type") name_end_index = i - 1;
                    }
                }
                if (name_end_index + 2 >= args.Length) continue;
                EngineOption o = new EngineOption();
                o.Name = string.Join(" ", args.Skip(2).Take(name_end_index - 1));
                o.Type = args[name_end_index + 2];
                string[] p = args.Skip(name_end_index + 3).ToArray();
                if (o.Type == "check" && p.Length >= 2)
                {
                    o.Default = p[1];
                }
                else if (o.Type == "spin" && p.Length >= 6)
                {
                    o.Default = p[1];
                    o.Min = p[3];
                    o.Max = p[5];
                }
                else if (o.Type == "combo" && p.Length >= 2)
                {
                    o.Default = p[1];
                    for (int i = 3; i < p.Length; i += 2)
                    {
                        o.Choices.Add(p[i]);
                    }
                }
                else if (o.Type == "button")
                {
                    o.Default = "";
                    if (CurrentEngineSettings.Configs.Remove(o.Name)) prunedButtonKeys = true;   // 清理旧配置里残留的按钮项
                }
                else if (p.Length >= 2)
                {
                    o.Default = p[1];
                }
                options.Add(o);
            }
            if (prunedButtonKeys) Mainform.SaveSettings();   // 按钮项清理结果落盘（避免下次启动回放空值）

            // ---------- 2) 竖向表格：每行一个选项；列 = 选项 / 设置 / 默认值 / 最小值 / 最大值 ----------
            if (options.Count == 0)
            {
                Label empty = new Label();
                empty.Text = "（该引擎未上报可配置选项）";
                empty.AutoSize = true;
                empty.ForeColor = Color.FromArgb(90, 90, 90);
                empty.Margin = new Padding(8);
                panel_scroll.Controls.Add(empty);
                panel_scroll.ResumeLayout(false);
                panel_scroll.PerformLayout();
                return;
            }

            // 行高/列宽按当前字体折算，保证高缩放（如 150%）下仍可读
            int headerH = this.Font.Height + 16;
            int rowH = this.Font.Height + 26;
            double scale = Math.Max(1.0, this.Font.Height / 15.0);
            int avail0 = panel_scroll.ClientSize.Width - panel_scroll.Padding.Horizontal;
            if (avail0 < 260) avail0 = 260;
            int nameW, infoW;                 // 「选项」列 / 「默认值/最小值/最大值」列
            ComputeColumnWidths(avail0, scale, out nameW, out infoW);

            TableLayoutPanel table = new EngineBufferedTable();
            table.SuspendLayout();
            table.AutoSize = true;
            table.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            table.Dock = DockStyle.Top;   // 宽度永远随视口（结构上不可能出现横向滚动），高度按内容自动
            table.CellBorderStyle = TableLayoutPanelCellBorderStyle.Single;
            table.BackColor = Color.White;
            table.ColumnCount = 5;
            table.RowCount = options.Count + 1;
            // 「设置」列自适应吸收剩余宽度：高 DPI/窗口缩放下不产生横向滚动
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, nameW));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, infoW));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, infoW));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, infoW));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, headerH));
            for (int i = 0; i < options.Count; i++)
            {
                table.RowStyles.Add(new RowStyle(SizeType.Absolute, rowH));
            }

            // 表头行（列义）
            string[] headers = new string[] { "选项", "设置", "默认值", "最小值", "最大值" };
            for (int c = 0; c < headers.Length; c++)
            {
                Label h = new Label();
                h.Text = headers[c];
                h.Dock = DockStyle.Fill;
                h.TextAlign = ContentAlignment.MiddleCenter;
                h.Font = boldFont;
                h.BackColor = Color.FromArgb(235, 235, 235);
                table.Controls.Add(h, c, 0);
            }

            for (int i = 0; i < options.Count; i++)
            {
                EngineOption o = options[i];
                int row = i + 1;

                // 第一列：选项名（悬停有说明）
                Label lblName = new Label();
                lblName.Text = o.Name;
                lblName.Dock = DockStyle.Fill;
                lblName.TextAlign = ContentAlignment.MiddleLeft;
                lblName.AutoEllipsis = true;
                lblName.Padding = new Padding(6, 0, 0, 0);
                lblName.Font = boldFont;
                toolTip_option.SetToolTip(lblName, DescribeOption(o));
                table.Controls.Add(lblName, 0, row);

                // 第二行：调整控件（按类型自动选择 勾选框 / 数值输入 / 下拉选择 / 按钮）
                string cur = o.Default;
                if (CurrentEngineSettings.Configs.ContainsKey(o.Name))
                {
                    cur = CurrentEngineSettings.Configs[o.Name];
                }
                Control ctl = null;
                if (o.Type == "check")
                {
                    CheckBox cb = new CheckBox();
                    cb.Text = "";
                    cb.Checked = cur == "true";
                    cb.Dock = DockStyle.Fill;
                    cb.CheckAlign = ContentAlignment.MiddleCenter;
                    cb.CheckedChanged += new EventHandler(((object s2, EventArgs e2) =>
                    {
                        CurrentEngineSettings.Configs[o.Name] = cb.Checked ? "true" : "false";
                        Engine.SetOption(o.Name, cb.Checked ? "true" : "false");
                        Mainform.SaveSettings();
                    }));
                    ctl = cb;
                }
                else if (o.Type == "spin")
                {
                    decimal min = 0, max = 1000000;
                    decimal.TryParse(o.Min, out min);
                    if (!decimal.TryParse(o.Max, out max) || max <= min)
                    {
                        max = min + 1000000;
                    }
                    NumericUpDown num = new NumericUpDown();
                    num.Minimum = min;
                    num.Maximum = max;
                    decimal dv;
                    if (!decimal.TryParse(cur, out dv)) dv = min;
                    if (dv < min) dv = min;
                    if (dv > max) dv = max;
                    num.Value = dv;
                    num.Dock = DockStyle.Fill;
                    num.Margin = new Padding(10, 8, 10, 8);
                    num.ValueChanged += new EventHandler(((object s2, EventArgs e2) =>
                    {
                        CurrentEngineSettings.Configs[o.Name] = num.Value.ToString();
                        Engine.SetOption(o.Name, num.Value.ToString());
                        Mainform.SaveSettings();
                    }));
                    ctl = num;
                }
                else if (o.Type == "combo")
                {
                    ComboBox combo = new ComboBox();
                    combo.DropDownStyle = ComboBoxStyle.DropDownList;
                    combo.Dock = DockStyle.Fill;
                    combo.Margin = new Padding(8, 8, 8, 8);
                    combo.Items.AddRange(o.Choices.ToArray());
                    int idx = combo.Items.IndexOf(cur);
                    if (idx >= 0) combo.SelectedIndex = idx;
                    else if (combo.Items.Count > 0) combo.SelectedIndex = 0;
                    combo.SelectedIndexChanged += new EventHandler(((object s2, EventArgs e2) =>
                    {
                        if (combo.SelectedItem == null) return;
                        CurrentEngineSettings.Configs[o.Name] = combo.SelectedItem.ToString();
                        Engine.SetOption(o.Name, combo.SelectedItem.ToString());
                        Mainform.SaveSettings();
                    }));
                    ctl = combo;
                }
                else if (o.Type == "button")
                {
                    Button btn = new Button();
                    btn.Text = "执行";
                    btn.Dock = DockStyle.Fill;
                    btn.Margin = new Padding(10, 6, 10, 6);
                    btn.Click += new EventHandler(((object s2, EventArgs e2) =>
                    {
                        Engine.SetOption(o.Name, "");   // 按钮类选项只发送命令，不写入配置表
                    }));
                    ctl = btn;
                }
                else
                {
                    // 字符串等其余类型：文本框编辑，回车或失焦时生效
                    TextBox tb = new TextBox();
                    tb.Text = cur ?? "";
                    tb.Dock = DockStyle.Fill;
                    tb.Margin = new Padding(8, 8, 8, 8);
                    EventHandler apply = ((object s2, EventArgs e2) =>
                    {
                        CurrentEngineSettings.Configs[o.Name] = tb.Text;
                        Engine.SetOption(o.Name, tb.Text);
                        Mainform.SaveSettings();
                    });
                    tb.Leave += apply;
                    tb.KeyDown += ((object s2, KeyEventArgs e2) =>
                    {
                        if (e2.KeyCode == Keys.Enter)
                        {
                            e2.SuppressKeyPress = true;
                            apply(s2, EventArgs.Empty);
                        }
                    });
                    ctl = tb;
                }
                if (ctl != null)
                {
                    toolTip_option.SetToolTip(ctl, DescribeOption(o));
                    table.Controls.Add(ctl, 1, row);
                }

                // 第三~五列：默认值 / 最小值 / 最大值（列义由表头标明）
                TableAddInfo(table, string.IsNullOrEmpty(o.Default) ? "-" : o.Default, 2, row);
                TableAddInfo(table, o.Type == "spin" ? o.Min : "-", 3, row);
                TableAddInfo(table, o.Type == "spin" ? o.Max : "-", 4, row);
            }

            table.ResumeLayout(false);
            panel_scroll.Controls.Add(table);
            engineTable = table;
            RelayoutColumns();
            panel_scroll.ResumeLayout(false);
            panel_scroll.PerformLayout();
        }

        // 列宽：正常宽度用标准值；窄窗口按比例收缩（带下限），保证五列可读
        void ComputeColumnWidths(int avail, double scale, out int nameW, out int infoW)
        {
            nameW = (int)(170 * scale);
            infoW = (int)(80 * scale);
            int settingsMin = (int)(110 * scale);
            int fixedSum = nameW + 3 * infoW;
            if (avail < fixedSum + settingsMin)
            {
                double f = (double)(avail - settingsMin) / fixedSum;
                if (f < 0.5) f = 0.5;
                nameW = Math.Max((int)(84 * scale), (int)(nameW * f));
                infoW = Math.Max((int)(40 * scale), (int)(infoW * f));
            }
        }

        // 按当前视口宽度重排列宽（只改固定列；「设置」列百分比自动吸收剩余）
        void RelayoutColumns()
        {
            if (layoutGuard) return;   // 改列宽会触发布局回调 → 防重入
            layoutGuard = true;
            try
            {
                if (engineTable == null || panel_scroll == null) return;
                if (engineTable.ColumnStyles.Count < 5) return;
                int avail = panel_scroll.ClientSize.Width - panel_scroll.Padding.Horizontal;
                if (avail < 260) avail = 260;
                double scale = Math.Max(1.0, this.Font.Height / 15.0);
                int nameW, infoW;
                ComputeColumnWidths(avail, scale, out nameW, out infoW);
                engineTable.ColumnStyles[0].Width = nameW;
                engineTable.ColumnStyles[2].Width = infoW;
                engineTable.ColumnStyles[3].Width = infoW;
                engineTable.ColumnStyles[4].Width = infoW;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("RelayoutColumns: " + ex.Message);
            }
            finally
            {
                layoutGuard = false;
            }
        }

        static void TableAddInfo(TableLayoutPanel table, string text, int col, int row)
        {
            Label l = new Label();
            l.Text = text;
            l.Dock = DockStyle.Fill;
            l.TextAlign = ContentAlignment.MiddleCenter;
            l.ForeColor = Color.FromArgb(90, 90, 90);
            table.Controls.Add(l, col, row);
        }

        private void EngineSettingsForm_FormClosing(object sender, FormClosingEventArgs e)
        {

        }
    }

    // 双缓冲滚动容器：改善拖动滚动条时的重绘效果（0.3 修复）
    internal sealed class EngineScrollPanel : Panel
    {
        public EngineScrollPanel()
        {
            DoubleBuffered = true;
        }
    }

    internal sealed class EngineBufferedTable : TableLayoutPanel
    {
        public EngineBufferedTable()
        {
            DoubleBuffered = true;
        }
    }
}
