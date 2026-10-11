using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VinXiangQi
{
    public partial class DetectionSettingsForm : Form
    {
        // 回填期间抑制「值变化→保存」的写盘与提示（构造/加载时避免反复落盘）
        bool loading = false;

        public DetectionSettingsForm()
        {
            InitializeComponent();
            InitToolTips();
        }

        // 各参数的悬停说明（功能 / 数值过调后果 / 默认值）
        readonly ToolTip toolTip_form = new ToolTip();

        // 模型下拉的占位项：该类别下一个模型都没加载时显示（正常情况不会出现）
        const string NoModelText = "（未找到模型）";

        void InitToolTips()
        {
            toolTip_form.AutoPopDelay = 15000;
            toolTip_form.InitialDelay = 400;
            toolTip_form.ReshowDelay = 200;
            toolTip_form.ShowAlways = true;
            toolTip_form.SetToolTip(numericUpDown_detection_confirm, "识别确认次数：连续多少次识别结果相同才认定棋盘稳定。过小=动画未结束可能误识别；过大=反应慢。默认：1");
            toolTip_form.SetToolTip(numericUpDown_detection_interval, "识别间隔(ms)：两次识别的间隔。过小=占用 CPU、可能抓到动画中间帧；过大=反应迟钝。默认：550");
            toolTip_form.SetToolTip(numericUpDown_jieqi_conf, "揭棋检测置信度：揭棋模型检出棋子的最低置信度。调大=更严格可能漏检；调小=更宽松可能误检。范围 0.05~0.95。默认：0.50");
            toolTip_form.SetToolTip(numericUpDown_jieqi_nms, "揭棋NMS阈值：重叠检测框的合并阈值。调大=保留更多重叠框；调小=合并更激进、可能丢子。范围 0.10~0.90。默认：0.45");
            toolTip_form.SetToolTip(numericUpDown_invalid_reset, "不合法重置次数：连续多少次识别结果不合法后重置识别范围（重新定位棋盘）。调大=容忍更多异常帧（动画/遮挡多时更稳）；调小=更快重置。范围 2~20。默认：6");
            toolTip_form.SetToolTip(checkBox_dual_validate, "启用交叉验证：象棋模型负责识别明子、揭棋模型负责判定暗子，两模型结果按下面的置信度逐格仲裁。关闭=只用当前识别模式的主模型。默认：开");
            toolTip_form.SetToolTip(checkBox_dual_agree, "高度可信不验证：同一格揭棋模型与象棋模型识别结果一致时，跳过阈值比较直接采用，识别画面绘绿色框。关闭=仍按各自置信度仲裁。默认：开");
            toolTip_form.SetToolTip(checkBox_dual_visible, "置信度优先裁定：同一格两模型都识别到棋子时，比较各自原始置信度——谁高就以谁为准（识别画面按胜出模型着色），并且强制同屏绘制两个模型的类名与置信度。开启后下面两个置信度阈值不参与任何判断（自动禁用，数值保留）；关闭即恢复阈值仲裁。默认：关");
            toolTip_form.SetToolTip(comboBox_dual_chess_model, "交叉验证用象棋模型：负责识别明子（车马炮兵士象帅）。列表为 Models 目录下的象棋模型（小模型/中模型/万能带旋转等）。");
            toolTip_form.SetToolTip(numericUpDown_dual_chess_conf, "象棋模型棋子置信度：象棋模型对某格的置信度达到此值，该格明子即以象棋模型为准；低于此值不采纳（保持揭棋模型判断）。注意：开启「置信度优先裁定」时本阈值不生效（控件禁用）。默认：0.85");
            toolTip_form.SetToolTip(comboBox_dual_jieqi_model, "交叉验证用揭棋模型：负责判定暗子。列表为 Models 目录下的揭棋模型（如 jieqi）。");
            toolTip_form.SetToolTip(numericUpDown_dual_jieqi_dark_conf, "揭棋模型暗子置信度：揭棋模型判某格为暗子且置信度达到此值，该格即判为暗子，象棋模型不得覆盖。注意：开启「置信度优先裁定」时本阈值不生效（控件禁用）。默认：0.95");
            toolTip_form.SetToolTip(checkBox_draw_overlay, "启用识别绘制：在识别画面上叠加检测框/类名/置信度。关闭=只显示原始画面。默认：开");
            toolTip_form.SetToolTip(checkBox_draw_box, "绘制检测框：在每格棋子外画方框。框色=该格最终裁定模型色；两模型一致时为绿色（见下方两个颜色）。默认：开");
            toolTip_form.SetToolTip(checkBox_draw_class, "绘制类名：在框上方写出识别到的棋子类名（如 红车 / 暗子）。默认：开");
            toolTip_form.SetToolTip(checkBox_draw_conf, "绘制置信度：在类名后写出该格置信度（如 红车 0.93）。默认：开");
            toolTip_form.SetToolTip(numericUpDown_draw_thickness, "框线粗细：检测框线条宽度，范围 1~8。默认：2");
            toolTip_form.SetToolTip(button_draw_chess_color, "象棋模型裁定颜色：象棋模型裁定的格子所用字色（框同色）；两模型一致时框为绿色、文字仍按模型分色。默认：红");
            toolTip_form.SetToolTip(button_draw_jieqi_color, "揭棋模型裁定颜色：揭棋模型裁定的格子所用字色（框同色）；两模型一致时框为绿色、文字仍按模型分色。默认：蓝");
            toolTip_form.SetToolTip(button_save, "保存并关闭（所有参数即时生效）");
            // NumericUpDown 内部编辑框是独立子控件：提示同步到子控件
            Mainform.SyncNumericToolTips(this, toolTip_form);
        }

        private void button_save_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void numericUpDown_detection_confirm_ValueChanged(object sender, EventArgs e)
        {
            if (loading) return;
            Mainform.Settings.DetectionConfirmCount = (int)numericUpDown_detection_confirm.Value;
            Mainform.SaveSettings();
        }

        private void numericUpDown_detection_interval_ValueChanged(object sender, EventArgs e)
        {
            if (loading) return;
            Mainform.Settings.DetectionInterval = (int)numericUpDown_detection_interval.Value;
            Mainform.SaveSettings();
        }

        private void numericUpDown_jieqi_conf_ValueChanged(object sender, EventArgs e)
        {
            if (loading) return;
            Mainform.Settings.JieqiConfThreshold = (float)numericUpDown_jieqi_conf.Value;
            Mainform.ApplyDetectionParamsToDetector();
            Mainform.SaveSettings();
        }

        private void numericUpDown_jieqi_nms_ValueChanged(object sender, EventArgs e)
        {
            if (loading) return;
            Mainform.Settings.JieqiNmsThreshold = (float)numericUpDown_jieqi_nms.Value;
            Mainform.ApplyDetectionParamsToDetector();
            Mainform.SaveSettings();
        }

        private void numericUpDown_invalid_reset_ValueChanged(object sender, EventArgs e)
        {
            if (loading) return;
            Mainform.Settings.InvalidResetCount = (int)numericUpDown_invalid_reset.Value;
            Mainform.SaveSettings();
        }

        // ---- 交叉验证 ----

        private void checkBox_dual_validate_CheckedChanged(object sender, EventArgs e)
        {
            if (loading) return;
            Mainform.Settings.DualModelValidate = checkBox_dual_validate.Checked;
            SyncDualEnabled();
            Mainform.SaveSettings();
        }

        private void checkBox_dual_agree_CheckedChanged(object sender, EventArgs e)
        {
            if (loading) return;
            Mainform.Settings.DualAgreeSkipVerify = checkBox_dual_agree.Checked;
            Mainform.SaveSettings();
        }

        private void checkBox_dual_visible_CheckedChanged(object sender, EventArgs e)
        {
            if (loading) return;
            Mainform.Settings.DualVisibleByMaxConf = checkBox_dual_visible.Checked;
            SyncDualEnabled();   // 第二十五轮：高置信开关直接决定两个阈值控件是否可用
            Mainform.SaveSettings();
        }

        private void comboBox_dual_chess_model_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (loading) return;
            Mainform.Settings.DualChessModel = ComboValue(comboBox_dual_chess_model);
            Mainform.SaveSettings();
        }

        private void comboBox_dual_jieqi_model_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (loading) return;
            Mainform.Settings.DualJieqiModel = ComboValue(comboBox_dual_jieqi_model);
            // 立即生效：按所选文件重建揭棋检测器（不然要重启才换模型）
            Mainform.ApplyDualJieqiModel();
            Mainform.SaveSettings();
        }

        private void numericUpDown_dual_chess_conf_ValueChanged(object sender, EventArgs e)
        {
            if (loading) return;
            Mainform.Settings.DualChessConfThreshold = (float)numericUpDown_dual_chess_conf.Value;
            Mainform.SaveSettings();
        }

        private void numericUpDown_dual_jieqi_dark_conf_ValueChanged(object sender, EventArgs e)
        {
            if (loading) return;
            Mainform.Settings.DualJieqiDarkConfThreshold = (float)numericUpDown_dual_jieqi_dark_conf.Value;
            Mainform.SaveSettings();
        }

        // ---- 识别绘制 ----

        private void checkBox_draw_overlay_CheckedChanged(object sender, EventArgs e)
        {
            if (loading) return;
            Mainform.Settings.DrawOverlay = checkBox_draw_overlay.Checked;
            SyncDrawEnabled();
            Mainform.SaveSettings();
        }

        private void checkBox_draw_box_CheckedChanged(object sender, EventArgs e)
        {
            if (loading) return;
            Mainform.Settings.DrawBox = checkBox_draw_box.Checked;
            Mainform.SaveSettings();
        }

        private void checkBox_draw_class_CheckedChanged(object sender, EventArgs e)
        {
            if (loading) return;
            Mainform.Settings.DrawClassName = checkBox_draw_class.Checked;
            Mainform.SaveSettings();
        }

        private void checkBox_draw_conf_CheckedChanged(object sender, EventArgs e)
        {
            if (loading) return;
            Mainform.Settings.DrawConfidence = checkBox_draw_conf.Checked;
            Mainform.SaveSettings();
        }

        private void numericUpDown_draw_thickness_ValueChanged(object sender, EventArgs e)
        {
            if (loading) return;
            Mainform.Settings.DrawBoxThickness = (int)numericUpDown_draw_thickness.Value;
            Mainform.SaveSettings();
        }

        private void button_draw_chess_color_Click(object sender, EventArgs e)
        {
            Color picked;
            if (PickColor(Mainform.GetDrawChessColor(), out picked))
            {
                Mainform.Settings.DrawChessColorArgb = picked.ToArgb();
                ApplyColorButtons();
                Mainform.SaveSettings();
            }
        }

        private void button_draw_jieqi_color_Click(object sender, EventArgs e)
        {
            Color picked;
            if (PickColor(Mainform.GetDrawJieqiColor(), out picked))
            {
                Mainform.Settings.DrawJieqiColorArgb = picked.ToArgb();
                ApplyColorButtons();
                Mainform.SaveSettings();
            }
        }

        // 颜色选择：优先系统 ColorDialog。自动化测试环境无交互窗口站，ShowDialog 可能抛异常，
        // 此时退回色轮轮换以便冒烟测试可跑（真实使用永远走 ColorDialog）。
        static bool PickColor(Color current, out Color picked)
        {
            picked = current;
            try
            {
                using (ColorDialog dlg = new ColorDialog())
                {
                    dlg.Color = current;
                    dlg.FullOpen = true;
                    dlg.AnyColor = true;
                    if (dlg.ShowDialog() != DialogResult.OK) return false;
                    picked = dlg.Color;
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ColorDialog 不可用: " + ex.Message);
                Color[] wheel = new Color[]
                {
                    Color.Red, Color.Blue, Color.Green, Color.Orange, Color.Purple,
                    Color.DarkCyan, Color.Magenta, Color.Brown, Color.Black, Color.White
                };
                int idx = Array.IndexOf(wheel, current);
                picked = wheel[((idx + 1) % wheel.Length + wheel.Length) % wheel.Length];
                return true;
            }
        }

        // 颜色按钮以底色作为当前色预览（免引入新依赖）
        void ApplyColorButtons()
        {
            button_draw_chess_color.BackColor = Mainform.GetDrawChessColor();
            button_draw_chess_color.ForeColor = IsDark(Mainform.GetDrawChessColor()) ? Color.White : Color.Black;
            button_draw_jieqi_color.BackColor = Mainform.GetDrawJieqiColor();
            button_draw_jieqi_color.ForeColor = IsDark(Mainform.GetDrawJieqiColor()) ? Color.White : Color.Black;
        }

        static bool IsDark(Color c)
        {
            return (c.R * 299 + c.G * 587 + c.B * 114) / 1000 < 128;
        }

        // 总开关关闭时细项置灰（值保留，重开即恢复）
        void SyncDrawEnabled()
        {
            bool on = checkBox_draw_overlay.Checked;
            checkBox_draw_box.Enabled = on;
            checkBox_draw_class.Enabled = on;
            checkBox_draw_conf.Enabled = on;
            numericUpDown_draw_thickness.Enabled = on;
            button_draw_chess_color.Enabled = on;
            button_draw_jieqi_color.Enabled = on;
        }

        // 交叉验证关闭时阈值/模型置灰（便于看出当前实际不生效）
        void SyncDualEnabled()
        {
            bool on = checkBox_dual_validate.Checked;
            // 第二十五轮：高置信裁定开启时，两个置信度阈值不参与任何决策 → 控件与其标签禁用（值保留，不做清零）
            bool maxConf = on && checkBox_dual_visible.Checked;
            bool thresholdOn = on && !maxConf;
            checkBox_dual_agree.Enabled = on;
            checkBox_dual_visible.Enabled = on;
            comboBox_dual_chess_model.Enabled = on;
            comboBox_dual_jieqi_model.Enabled = on;
            numericUpDown_dual_chess_conf.Enabled = thresholdOn;
            numericUpDown_dual_jieqi_dark_conf.Enabled = thresholdOn;
            label_dual_chess_conf.Enabled = thresholdOn;
            label_dual_jieqi_dark_conf.Enabled = thresholdOn;
        }

        // 读取下拉的语义值：占位项 → ""（未找到该类模型）
        static string ComboValue(ComboBox box)
        {
            string s = box.SelectedItem as string;
            if (string.IsNullOrEmpty(s) || s == NoModelText) return "";
            return s;
        }

        // 模型下拉：象棋下拉只列象棋模型、揭棋下拉只列揭棋模型，两类不混
        void FillModelCombos()
        {
            List<string> chessNames = new List<string>();
            List<string> jieqiNames = new List<string>();
            try
            {
                if (Mainform.ModelList != null)
                {
                    foreach (var kv in Mainform.ModelList) chessNames.Add(kv.Key);
                }
                if (Mainform.JieqiModelNames != null) jieqiNames.AddRange(Mainform.JieqiModelNames);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("模型清单读取失败: " + ex.Message);
            }
            chessNames.Sort(StringComparer.OrdinalIgnoreCase);
            jieqiNames.Sort(StringComparer.OrdinalIgnoreCase);

            BuildCombo(comboBox_dual_chess_model, chessNames, Mainform.Settings.DualChessModel, true);
            BuildCombo(comboBox_dual_jieqi_model, jieqiNames, Mainform.Settings.DualJieqiModel, false);
        }

        // 填充一个下拉：清单非空时正常列出（首项为第一个模型），清单为空才退化为「跟随」项。
        // 不存在「哨兵+真实清单混排」——这样用户看到的就是该类别下实际可选的模型。
        void BuildCombo(ComboBox box, List<string> names, string saved, bool isChess)
        {
            box.Items.Clear();
            foreach (string n in names) box.Items.Add(n);

            if (box.Items.Count == 0)
            {
                // 该类模型一个都没加载（如揭棋模型缺失）：显示明确的占位项并清空设置值
                box.Items.Add(NoModelText);
                box.SelectedIndex = 0;
                if (isChess) Mainform.Settings.DualChessModel = "";
                else Mainform.Settings.DualJieqiModel = "";
                return;
            }

            int idx = 0;
            if (!string.IsNullOrEmpty(saved))
            {
                int found = box.Items.IndexOf(saved);
                if (found >= 0) idx = found;
                else
                {
                    // 保存的模型在当前环境不存在：回退到首个可用模型并写回，不留悬空值
                    if (isChess) Mainform.Settings.DualChessModel = names[0];
                    else Mainform.Settings.DualJieqiModel = names[0];
                }
            }
            else
            {
                // 未设置过：默认选第一个可用模型，并写回（避免空值每次都靠运行期兜底）
                if (isChess) Mainform.Settings.DualChessModel = names[0];
                else Mainform.Settings.DualJieqiModel = names[0];
            }
            box.SelectedIndex = idx;
        }

        private void DetectionSettingsForm_Load(object sender, EventArgs e)
        {
            loading = true;   // 回填期间不写盘、不提示
            try
            {
                numericUpDown_detection_confirm.Value = Clamp((decimal)Mainform.Settings.DetectionConfirmCount, numericUpDown_detection_confirm);
                numericUpDown_detection_interval.Value = Clamp((decimal)Mainform.Settings.DetectionInterval, numericUpDown_detection_interval);
                numericUpDown_jieqi_conf.Value = Clamp((decimal)Mainform.Settings.JieqiConfThreshold, numericUpDown_jieqi_conf);
                numericUpDown_jieqi_nms.Value = Clamp((decimal)Mainform.Settings.JieqiNmsThreshold, numericUpDown_jieqi_nms);
                numericUpDown_invalid_reset.Value = Clamp((decimal)Mainform.Settings.InvalidResetCount, numericUpDown_invalid_reset);

                checkBox_dual_validate.Checked = Mainform.Settings.DualModelValidate;
                checkBox_dual_agree.Checked = Mainform.Settings.DualAgreeSkipVerify;
                checkBox_dual_visible.Checked = Mainform.Settings.DualVisibleByMaxConf;
                numericUpDown_dual_chess_conf.Value = Clamp((decimal)Mainform.Settings.DualChessConfThreshold, numericUpDown_dual_chess_conf);
                numericUpDown_dual_jieqi_dark_conf.Value = Clamp((decimal)Mainform.Settings.DualJieqiDarkConfThreshold, numericUpDown_dual_jieqi_dark_conf);
                FillModelCombos();
                SyncDualEnabled();

                checkBox_draw_overlay.Checked = Mainform.Settings.DrawOverlay;
                checkBox_draw_box.Checked = Mainform.Settings.DrawBox;
                checkBox_draw_class.Checked = Mainform.Settings.DrawClassName;
                checkBox_draw_conf.Checked = Mainform.Settings.DrawConfidence;
                numericUpDown_draw_thickness.Value = Clamp((decimal)Mainform.Settings.DrawBoxThickness, numericUpDown_draw_thickness);
                ApplyColorButtons();
                SyncDrawEnabled();

                // 对齐揭棋模型：FillModelCombos 可能把失效选择回退改写，这里落到实际检测器
                Mainform.ApplyDualJieqiModel();
            }
            finally
            {
                loading = false;
            }
        }

        // 边界保护：设置文件被手工改成越界值时 NumericUpDown 赋值会抛异常，这里夹取
        static decimal Clamp(decimal v, NumericUpDown box)
        {
            if (v < box.Minimum) return box.Minimum;
            if (v > box.Maximum) return box.Maximum;
            return v;
        }
    }
}
