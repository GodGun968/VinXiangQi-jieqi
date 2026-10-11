using System;
using System.Windows.Forms;

namespace VinXiangQi
{
    // 「截图与鼠标」详细设置：截图重试、点击时序、后台点击序列。
    // 从「识别设置」弹窗迁入（识别类参数留在识别设置里）。
    public partial class MouseCaptureSettingsForm : Form
    {
        public MouseCaptureSettingsForm()
        {
            InitializeComponent();
            InitToolTips();
        }

        // 各参数的悬停说明（功能 / 数值过调后果 / 默认值）
        readonly ToolTip toolTip_form = new ToolTip();
        bool loading = false;

        void InitToolTips()
        {
            toolTip_form.AutoPopDelay = 15000;
            toolTip_form.InitialDelay = 400;
            toolTip_form.ReshowDelay = 200;
            toolTip_form.ShowAlways = true;
            toolTip_form.SetToolTip(numericUpDown_screenshot_retry, "截图重试次数：抓到无效帧（黑屏/空帧）时的自动重试次数。调大=更稳，但失败时更慢；0=不重试。范围 0~5。默认：2");
            toolTip_form.SetToolTip(numericUpDown_click_interval, "点击间隔(ms)：同一着法两次点击（起点→终点）之间的等待。过小=目标来不及响应；过大=走子慢。范围 50~2000。默认：300");
            toolTip_form.SetToolTip(numericUpDown_click_hold, "点击按住时长(ms)：每次点击从按下到抬起的间隔。过小=个别程序识别不到点击；过大=稍显迟钝。范围 10~500。默认：40");
            toolTip_form.SetToolTip(numericUpDown_click_prewait, "点击前等待(ms)：移动光标后、按下前等待（给目标窗口就绪时间）。过小=可能点空；过大=走子慢。范围 0~1000。默认：30");
            toolTip_form.SetToolTip(numericUpDown_click_retry, "落子失败重试：点击后未检测到落子时的自动重试次数，超过后重置检测。调大=更稳，但失败时更慢；0=不重试。范围 0~5。默认：2");
            toolTip_form.SetToolTip(comboBox_bg_strategy, "后台点击序列：向目标窗口发送的鼠标消息序列。兼容序列=兼容性最好（默认）；标准序列=坐标严格一致；激活式=先发送激活消息再点击（个别程序需要）。逐个试，选能正常落子的那个");
            toolTip_form.SetToolTip(button_save, "关闭窗口（所有参数即时生效并已保存）");
            Mainform.SyncNumericToolTips(this, toolTip_form);
        }

        private void MouseCaptureSettingsForm_Load(object sender, EventArgs e)
        {
            loading = true;
            numericUpDown_screenshot_retry.Value = (decimal)Mainform.Settings.ScreenshotRetryCount;
            numericUpDown_click_interval.Value = (decimal)Mainform.Settings.ClickInterval;
            numericUpDown_click_hold.Value = (decimal)Mainform.Settings.ClickHoldMs;
            numericUpDown_click_prewait.Value = (decimal)Mainform.Settings.ClickPreWaitMs;
            numericUpDown_click_retry.Value = (decimal)Mainform.Settings.ClickRetryCount;
            int s = Mainform.Settings.BackgroundClickStrategy;
            if (s < 0 || s > 2) s = 0;
            comboBox_bg_strategy.SelectedIndex = s;
            loading = false;
        }

        private void numericUpDown_screenshot_retry_ValueChanged(object sender, EventArgs e)
        {
            if (loading) return;
            Mainform.Settings.ScreenshotRetryCount = (int)numericUpDown_screenshot_retry.Value;
            Mainform.SaveSettings();
        }

        private void numericUpDown_click_interval_ValueChanged(object sender, EventArgs e)
        {
            if (loading) return;
            Mainform.Settings.ClickInterval = (int)numericUpDown_click_interval.Value;
            Mainform.SaveSettings();
        }

        private void numericUpDown_click_hold_ValueChanged(object sender, EventArgs e)
        {
            if (loading) return;
            Mainform.Settings.ClickHoldMs = (int)numericUpDown_click_hold.Value;
            Mainform.SaveSettings();
        }

        private void numericUpDown_click_prewait_ValueChanged(object sender, EventArgs e)
        {
            if (loading) return;
            Mainform.Settings.ClickPreWaitMs = (int)numericUpDown_click_prewait.Value;
            Mainform.SaveSettings();
        }

        private void numericUpDown_click_retry_ValueChanged(object sender, EventArgs e)
        {
            if (loading) return;
            Mainform.Settings.ClickRetryCount = (int)numericUpDown_click_retry.Value;
            Mainform.SaveSettings();
        }

        private void comboBox_bg_strategy_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (loading) return;
            Mainform.Settings.BackgroundClickStrategy = comboBox_bg_strategy.SelectedIndex;
            Mainform.SaveSettings();
        }

        private void button_save_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
