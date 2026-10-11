namespace VinXiangQi
{
    partial class MouseCaptureSettingsForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
                if (toolTip_form != null)
                {
                    toolTip_form.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.groupBox6 = new System.Windows.Forms.GroupBox();
            this.numericUpDown_screenshot_retry = new System.Windows.Forms.NumericUpDown();
            this.numericUpDown_click_interval = new System.Windows.Forms.NumericUpDown();
            this.numericUpDown_click_hold = new System.Windows.Forms.NumericUpDown();
            this.numericUpDown_click_prewait = new System.Windows.Forms.NumericUpDown();
            this.numericUpDown_click_retry = new System.Windows.Forms.NumericUpDown();
            this.comboBox_bg_strategy = new System.Windows.Forms.ComboBox();
            this.label_ms1 = new System.Windows.Forms.Label();
            this.label_ms2 = new System.Windows.Forms.Label();
            this.label_ms3 = new System.Windows.Forms.Label();
            this.label_hint = new System.Windows.Forms.Label();
            this.button_save = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.groupBox5.SuspendLayout();
            this.groupBox6.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_screenshot_retry)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_click_interval)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_click_hold)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_click_prewait)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_click_retry)).BeginInit();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.numericUpDown_screenshot_retry);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(184, 62);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "截图重试次数";
            // 
            // numericUpDown_screenshot_retry
            // 
            this.numericUpDown_screenshot_retry.Location = new System.Drawing.Point(23, 24);
            this.numericUpDown_screenshot_retry.Maximum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.numericUpDown_screenshot_retry.Minimum = new decimal(new int[] {
            0,
            0,
            0,
            0});
            this.numericUpDown_screenshot_retry.Name = "numericUpDown_screenshot_retry";
            this.numericUpDown_screenshot_retry.Size = new System.Drawing.Size(121, 25);
            this.numericUpDown_screenshot_retry.TabIndex = 0;
            this.numericUpDown_screenshot_retry.Value = new decimal(new int[] {
            2,
            0,
            0,
            0});
            this.numericUpDown_screenshot_retry.ValueChanged += new System.EventHandler(this.numericUpDown_screenshot_retry_ValueChanged);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.label_ms1);
            this.groupBox2.Controls.Add(this.numericUpDown_click_interval);
            this.groupBox2.Location = new System.Drawing.Point(216, 12);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(184, 62);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "点击间隔";
            // 
            // numericUpDown_click_interval
            // 
            this.numericUpDown_click_interval.Increment = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.numericUpDown_click_interval.Location = new System.Drawing.Point(23, 24);
            this.numericUpDown_click_interval.Maximum = new decimal(new int[] {
            2000,
            0,
            0,
            0});
            this.numericUpDown_click_interval.Minimum = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.numericUpDown_click_interval.Name = "numericUpDown_click_interval";
            this.numericUpDown_click_interval.Size = new System.Drawing.Size(96, 25);
            this.numericUpDown_click_interval.TabIndex = 0;
            this.numericUpDown_click_interval.Value = new decimal(new int[] {
            300,
            0,
            0,
            0});
            this.numericUpDown_click_interval.ValueChanged += new System.EventHandler(this.numericUpDown_click_interval_ValueChanged);
            // 
            // label_ms1
            // 
            this.label_ms1.AutoSize = true;
            this.label_ms1.Location = new System.Drawing.Point(125, 27);
            this.label_ms1.Name = "label_ms1";
            this.label_ms1.Size = new System.Drawing.Size(22, 15);
            this.label_ms1.TabIndex = 1;
            this.label_ms1.Text = "ms";
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.label_ms2);
            this.groupBox3.Controls.Add(this.numericUpDown_click_hold);
            this.groupBox3.Location = new System.Drawing.Point(12, 82);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(184, 62);
            this.groupBox3.TabIndex = 2;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "点击按住时长";
            // 
            // numericUpDown_click_hold
            // 
            this.numericUpDown_click_hold.Increment = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.numericUpDown_click_hold.Location = new System.Drawing.Point(23, 24);
            this.numericUpDown_click_hold.Maximum = new decimal(new int[] {
            500,
            0,
            0,
            0});
            this.numericUpDown_click_hold.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numericUpDown_click_hold.Name = "numericUpDown_click_hold";
            this.numericUpDown_click_hold.Size = new System.Drawing.Size(96, 25);
            this.numericUpDown_click_hold.TabIndex = 0;
            this.numericUpDown_click_hold.Value = new decimal(new int[] {
            40,
            0,
            0,
            0});
            this.numericUpDown_click_hold.ValueChanged += new System.EventHandler(this.numericUpDown_click_hold_ValueChanged);
            // 
            // label_ms2
            // 
            this.label_ms2.AutoSize = true;
            this.label_ms2.Location = new System.Drawing.Point(125, 27);
            this.label_ms2.Name = "label_ms2";
            this.label_ms2.Size = new System.Drawing.Size(22, 15);
            this.label_ms2.TabIndex = 1;
            this.label_ms2.Text = "ms";
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.label_ms3);
            this.groupBox4.Controls.Add(this.numericUpDown_click_prewait);
            this.groupBox4.Location = new System.Drawing.Point(216, 82);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(184, 62);
            this.groupBox4.TabIndex = 3;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "点击前等待";
            // 
            // numericUpDown_click_prewait
            // 
            this.numericUpDown_click_prewait.Increment = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numericUpDown_click_prewait.Location = new System.Drawing.Point(23, 24);
            this.numericUpDown_click_prewait.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.numericUpDown_click_prewait.Minimum = new decimal(new int[] {
            0,
            0,
            0,
            0});
            this.numericUpDown_click_prewait.Name = "numericUpDown_click_prewait";
            this.numericUpDown_click_prewait.Size = new System.Drawing.Size(96, 25);
            this.numericUpDown_click_prewait.TabIndex = 0;
            this.numericUpDown_click_prewait.Value = new decimal(new int[] {
            30,
            0,
            0,
            0});
            this.numericUpDown_click_prewait.ValueChanged += new System.EventHandler(this.numericUpDown_click_prewait_ValueChanged);
            // 
            // label_ms3
            // 
            this.label_ms3.AutoSize = true;
            this.label_ms3.Location = new System.Drawing.Point(125, 27);
            this.label_ms3.Name = "label_ms3";
            this.label_ms3.Size = new System.Drawing.Size(22, 15);
            this.label_ms3.TabIndex = 1;
            this.label_ms3.Text = "ms";
            // 
            // groupBox5
            // 
            this.groupBox5.Controls.Add(this.numericUpDown_click_retry);
            this.groupBox5.Location = new System.Drawing.Point(12, 152);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Size = new System.Drawing.Size(184, 62);
            this.groupBox5.TabIndex = 4;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "落子失败重试";
            // 
            // numericUpDown_click_retry
            // 
            this.numericUpDown_click_retry.Location = new System.Drawing.Point(23, 24);
            this.numericUpDown_click_retry.Maximum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.numericUpDown_click_retry.Minimum = new decimal(new int[] {
            0,
            0,
            0,
            0});
            this.numericUpDown_click_retry.Name = "numericUpDown_click_retry";
            this.numericUpDown_click_retry.Size = new System.Drawing.Size(121, 25);
            this.numericUpDown_click_retry.TabIndex = 0;
            this.numericUpDown_click_retry.Value = new decimal(new int[] {
            2,
            0,
            0,
            0});
            this.numericUpDown_click_retry.ValueChanged += new System.EventHandler(this.numericUpDown_click_retry_ValueChanged);
            // 
            // groupBox6
            // 
            this.groupBox6.Controls.Add(this.comboBox_bg_strategy);
            this.groupBox6.Location = new System.Drawing.Point(216, 152);
            this.groupBox6.Name = "groupBox6";
            this.groupBox6.Size = new System.Drawing.Size(184, 62);
            this.groupBox6.TabIndex = 5;
            this.groupBox6.TabStop = false;
            this.groupBox6.Text = "后台点击序列";
            // 
            // comboBox_bg_strategy
            // 
            this.comboBox_bg_strategy.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_bg_strategy.FormattingEnabled = true;
            this.comboBox_bg_strategy.Items.AddRange(new object[] {
            "兼容序列",
            "标准序列",
            "激活式"});
            this.comboBox_bg_strategy.Location = new System.Drawing.Point(18, 24);
            this.comboBox_bg_strategy.Name = "comboBox_bg_strategy";
            this.comboBox_bg_strategy.Size = new System.Drawing.Size(148, 23);
            this.comboBox_bg_strategy.TabIndex = 0;
            this.comboBox_bg_strategy.SelectedIndexChanged += new System.EventHandler(this.comboBox_bg_strategy_SelectedIndexChanged);
            // 
            // label_hint
            // 
            this.label_hint.AutoSize = false;
            this.label_hint.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(90)))), ((int)(((byte)(90)))));
            this.label_hint.Location = new System.Drawing.Point(12, 228);
            this.label_hint.Name = "label_hint";
            this.label_hint.Size = new System.Drawing.Size(224, 15);
            this.label_hint.TabIndex = 7;
            this.label_hint.Text = "参数即时保存；越界钳制。";
            // 
            // button_save
            // 
            this.button_save.Location = new System.Drawing.Point(240, 220);
            this.button_save.Name = "button_save";
            this.button_save.Size = new System.Drawing.Size(160, 32);
            this.button_save.TabIndex = 6;
            this.button_save.Text = "关闭窗口";
            this.button_save.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.button_save.UseVisualStyleBackColor = true;
            this.button_save.Click += new System.EventHandler(this.button_save_Click);
            // 
            // MouseCaptureSettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(412, 266);
            this.MinimumSize = new System.Drawing.Size(428, 305);
            this.Controls.Add(this.groupBox6);
            this.Controls.Add(this.groupBox5);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.label_hint);
            this.Controls.Add(this.button_save);
            this.Name = "MouseCaptureSettingsForm";
            this.Text = "截图与鼠标设置";
            this.Load += new System.EventHandler(this.MouseCaptureSettingsForm_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.groupBox5.ResumeLayout(false);
            this.groupBox6.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_screenshot_retry)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_click_interval)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_click_hold)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_click_prewait)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_click_retry)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.GroupBox groupBox5;
        private System.Windows.Forms.GroupBox groupBox6;
        private System.Windows.Forms.NumericUpDown numericUpDown_screenshot_retry;
        private System.Windows.Forms.NumericUpDown numericUpDown_click_interval;
        private System.Windows.Forms.NumericUpDown numericUpDown_click_hold;
        private System.Windows.Forms.NumericUpDown numericUpDown_click_prewait;
        private System.Windows.Forms.NumericUpDown numericUpDown_click_retry;
        private System.Windows.Forms.ComboBox comboBox_bg_strategy;
        private System.Windows.Forms.Label label_ms1;
        private System.Windows.Forms.Label label_ms2;
        private System.Windows.Forms.Label label_ms3;
        private System.Windows.Forms.Label label_hint;
        private System.Windows.Forms.Button button_save;
    }
}
