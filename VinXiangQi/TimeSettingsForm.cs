using System;
using System.Windows.Forms;

namespace VinXiangQi
{
    // 「时间设置」：出招时限（每步固定/计算深度/前N回合步时/局时N分钟后步时）
    // 与最短出招间隔（引擎/开局库）。线程数与 Hash 在主界面「引擎设置」分组框。
    // 全部不勾选且间隔为 0 时，出招行为与旧版完全一致。
    public partial class TimeSettingsForm : Form
    {
        public TimeSettingsForm()
        {
            InitializeComponent();
            InitToolTips();
        }

        readonly ToolTip toolTip_form = new ToolTip();
        bool loading = false;

        void InitToolTips()
        {
            toolTip_form.AutoPopDelay = 15000;
            toolTip_form.InitialDelay = 400;
            toolTip_form.ReshowDelay = 200;
            toolTip_form.ShowAlways = true;
            toolTip_form.SetToolTip(chk_fixed, "每步固定：每步都用固定秒数思考。与「计算深度」同时勾选时本项优先。默认：关");
            toolTip_form.SetToolTip(num_fixed_sec, "每步固定的思考秒数。默认：2.0");
            toolTip_form.SetToolTip(chk_depth, "计算深度：只限制搜索层数、不用时间上限。与「每步固定」同时勾选时以后者为准。默认：关");
            toolTip_form.SetToolTip(num_depth, "最大搜索深度（层）。默认：200");
            toolTip_form.SetToolTip(chk_open, "前 N 回合使用步时：连线开始后的前 N 个己方着法，每步改用右侧秒数。默认：关");
            toolTip_form.SetToolTip(num_open_moves, "开局覆盖的己方着法数。默认：10");
            toolTip_form.SetToolTip(num_open_sec, "开局阶段每步思考秒数。默认：2.0");
            toolTip_form.SetToolTip(chk_game, "局时 N 分钟后使用步时：本局开始 N 分钟后，每步改用右侧秒数。默认：关");
            toolTip_form.SetToolTip(num_game_min, "局时阈值（分钟，从连线开始计时）。默认：10");
            toolTip_form.SetToolTip(num_game_sec, "超过局时后每步思考秒数。默认：2.0");
            toolTip_form.SetToolTip(num_engine_min, "引擎最短出招间隔下限（秒）：引擎算出着法到点击落子之间的随机等待。0=不等待。默认：0");
            toolTip_form.SetToolTip(num_engine_max, "引擎最短出招间隔上限（秒）。0=不等待。默认：0");
            toolTip_form.SetToolTip(num_book_min, "开局库最短出招间隔下限（秒）：走开局库/云库时的随机等待。0=沿用「最短思考时间」设置。默认：0");
            toolTip_form.SetToolTip(num_book_max, "开局库最短出招间隔上限（秒）。0=沿用旧设置。默认：0");
            toolTip_form.SetToolTip(label_hint, "参数即时生效并自动保存。");
            toolTip_form.SetToolTip(button_save, "关闭窗口（所有参数即时生效并已保存）");
            Mainform.SyncNumericToolTips(this, toolTip_form);
        }

        private void TimeSettingsForm_Load(object sender, EventArgs e)
        {
            loading = true;
            ProgramSettings s = Mainform.Settings;
            num_fixed_sec.Value = Clamp(num_fixed_sec, (decimal)s.FixedStepSeconds);
            num_depth.Value = Clamp(num_depth, s.TimeModeDepthValue);
            num_open_moves.Value = Clamp(num_open_moves, s.OpeningStepMoves);
            num_open_sec.Value = Clamp(num_open_sec, (decimal)s.OpeningStepSeconds);
            num_game_min.Value = Clamp(num_game_min, s.GameTimeMinutes);
            num_game_sec.Value = Clamp(num_game_sec, (decimal)s.GameTimeStepSeconds);
            num_engine_min.Value = Clamp(num_engine_min, (decimal)s.EngineMoveGapMin);
            num_engine_max.Value = Clamp(num_engine_max, (decimal)s.EngineMoveGapMax);
            num_book_min.Value = Clamp(num_book_min, (decimal)s.BookMoveGapMin);
            num_book_max.Value = Clamp(num_book_max, (decimal)s.BookMoveGapMax);
            chk_fixed.Checked = s.TimeModeFixed;
            chk_depth.Checked = s.TimeModeDepth;
            chk_open.Checked = s.OpeningStepEnabled;
            chk_game.Checked = s.GameTimeEnabled;
            loading = false;
        }

        static decimal Clamp(NumericUpDown n, decimal v)
        {
            if (v < n.Minimum) v = n.Minimum;
            if (v > n.Maximum) v = n.Maximum;
            return v;
        }

        void Save()
        {
            Mainform.SaveSettings();
        }

        private void chk_fixed_CheckedChanged(object sender, EventArgs e) { if (!loading) { Mainform.Settings.TimeModeFixed = chk_fixed.Checked; Save(); } }
        private void num_fixed_sec_ValueChanged(object sender, EventArgs e) { if (!loading) { Mainform.Settings.FixedStepSeconds = (double)num_fixed_sec.Value; Save(); } }
        private void chk_depth_CheckedChanged(object sender, EventArgs e) { if (!loading) { Mainform.Settings.TimeModeDepth = chk_depth.Checked; Save(); } }
        private void num_depth_ValueChanged(object sender, EventArgs e) { if (!loading) { Mainform.Settings.TimeModeDepthValue = (int)num_depth.Value; Save(); } }
        private void chk_open_CheckedChanged(object sender, EventArgs e) { if (!loading) { Mainform.Settings.OpeningStepEnabled = chk_open.Checked; Save(); } }
        private void num_open_moves_ValueChanged(object sender, EventArgs e) { if (!loading) { Mainform.Settings.OpeningStepMoves = (int)num_open_moves.Value; Save(); } }
        private void num_open_sec_ValueChanged(object sender, EventArgs e) { if (!loading) { Mainform.Settings.OpeningStepSeconds = (double)num_open_sec.Value; Save(); } }
        private void chk_game_CheckedChanged(object sender, EventArgs e) { if (!loading) { Mainform.Settings.GameTimeEnabled = chk_game.Checked; Save(); } }
        private void num_game_min_ValueChanged(object sender, EventArgs e) { if (!loading) { Mainform.Settings.GameTimeMinutes = (int)num_game_min.Value; Save(); } }
        private void num_game_sec_ValueChanged(object sender, EventArgs e) { if (!loading) { Mainform.Settings.GameTimeStepSeconds = (double)num_game_sec.Value; Save(); } }
        private void num_engine_min_ValueChanged(object sender, EventArgs e) { if (!loading) { Mainform.Settings.EngineMoveGapMin = (double)num_engine_min.Value; Save(); } }
        private void num_engine_max_ValueChanged(object sender, EventArgs e) { if (!loading) { Mainform.Settings.EngineMoveGapMax = (double)num_engine_max.Value; Save(); } }
        private void num_book_min_ValueChanged(object sender, EventArgs e) { if (!loading) { Mainform.Settings.BookMoveGapMin = (double)num_book_min.Value; Save(); } }
        private void num_book_max_ValueChanged(object sender, EventArgs e) { if (!loading) { Mainform.Settings.BookMoveGapMax = (double)num_book_max.Value; Save(); } }

        private void button_save_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
