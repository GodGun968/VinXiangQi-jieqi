namespace VinXiangQi
{
    partial class TimeSettingsForm
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
            this.groupBox_time = new System.Windows.Forms.GroupBox();
            this.chk_fixed = new System.Windows.Forms.CheckBox();
            this.num_fixed_sec = new System.Windows.Forms.NumericUpDown();
            this.lbl_fixed_unit = new System.Windows.Forms.Label();
            this.chk_depth = new System.Windows.Forms.CheckBox();
            this.num_depth = new System.Windows.Forms.NumericUpDown();
            this.lbl_depth_unit = new System.Windows.Forms.Label();
            this.chk_open = new System.Windows.Forms.CheckBox();
            this.num_open_moves = new System.Windows.Forms.NumericUpDown();
            this.lbl_open_moves_unit = new System.Windows.Forms.Label();
            this.num_open_sec = new System.Windows.Forms.NumericUpDown();
            this.lbl_open_sec_unit = new System.Windows.Forms.Label();
            this.chk_game = new System.Windows.Forms.CheckBox();
            this.num_game_min = new System.Windows.Forms.NumericUpDown();
            this.lbl_game_min_unit = new System.Windows.Forms.Label();
            this.num_game_sec = new System.Windows.Forms.NumericUpDown();
            this.lbl_game_sec_unit = new System.Windows.Forms.Label();
            this.groupBox_gap = new System.Windows.Forms.GroupBox();
            this.lbl_engine_gap = new System.Windows.Forms.Label();
            this.num_engine_min = new System.Windows.Forms.NumericUpDown();
            this.lbl_engine_dash = new System.Windows.Forms.Label();
            this.num_engine_max = new System.Windows.Forms.NumericUpDown();
            this.lbl_engine_unit = new System.Windows.Forms.Label();
            this.lbl_book_gap = new System.Windows.Forms.Label();
            this.num_book_min = new System.Windows.Forms.NumericUpDown();
            this.lbl_book_dash = new System.Windows.Forms.Label();
            this.num_book_max = new System.Windows.Forms.NumericUpDown();
            this.lbl_book_unit = new System.Windows.Forms.Label();
            this.label_hint = new System.Windows.Forms.Label();
            this.button_save = new System.Windows.Forms.Button();
            this.groupBox_time.SuspendLayout();
            this.groupBox_gap.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.num_fixed_sec)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_depth)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_open_moves)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_open_sec)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_game_min)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_game_sec)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_engine_min)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_engine_max)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_book_min)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_book_max)).BeginInit();
            this.SuspendLayout();
            // 
            // groupBox_time
            // 
            this.groupBox_time.Controls.Add(this.chk_fixed);
            this.groupBox_time.Controls.Add(this.num_fixed_sec);
            this.groupBox_time.Controls.Add(this.lbl_fixed_unit);
            this.groupBox_time.Controls.Add(this.chk_depth);
            this.groupBox_time.Controls.Add(this.num_depth);
            this.groupBox_time.Controls.Add(this.lbl_depth_unit);
            this.groupBox_time.Controls.Add(this.chk_open);
            this.groupBox_time.Controls.Add(this.num_open_moves);
            this.groupBox_time.Controls.Add(this.lbl_open_moves_unit);
            this.groupBox_time.Controls.Add(this.num_open_sec);
            this.groupBox_time.Controls.Add(this.lbl_open_sec_unit);
            this.groupBox_time.Controls.Add(this.chk_game);
            this.groupBox_time.Controls.Add(this.num_game_min);
            this.groupBox_time.Controls.Add(this.lbl_game_min_unit);
            this.groupBox_time.Controls.Add(this.num_game_sec);
            this.groupBox_time.Controls.Add(this.lbl_game_sec_unit);
            this.groupBox_time.Location = new System.Drawing.Point(12, 8);
            this.groupBox_time.Name = "groupBox_time";
            this.groupBox_time.Size = new System.Drawing.Size(500, 148);
            this.groupBox_time.TabIndex = 0;
            this.groupBox_time.TabStop = false;
            this.groupBox_time.Text = "出招时限";
            // 
            // chk_fixed
            // 
            this.chk_fixed.AutoSize = true;
            this.chk_fixed.Location = new System.Drawing.Point(14, 25);
            this.chk_fixed.Name = "chk_fixed";
            this.chk_fixed.Size = new System.Drawing.Size(80, 19);
            this.chk_fixed.TabIndex = 0;
            this.chk_fixed.Text = "每步固定";
            this.chk_fixed.UseVisualStyleBackColor = true;
            this.chk_fixed.CheckedChanged += new System.EventHandler(this.chk_fixed_CheckedChanged);
            // 
            // num_fixed_sec
            // 
            this.num_fixed_sec.DecimalPlaces = 1;
            this.num_fixed_sec.Increment = new decimal(new int[] { 5, 0, 0, 65536 });
            this.num_fixed_sec.Location = new System.Drawing.Point(150, 22);
            this.num_fixed_sec.Maximum = new decimal(new int[] { 9999, 0, 0, 0 });
            this.num_fixed_sec.Minimum = new decimal(new int[] { 1, 0, 0, 65536 });
            this.num_fixed_sec.Name = "num_fixed_sec";
            this.num_fixed_sec.Size = new System.Drawing.Size(80, 25);
            this.num_fixed_sec.TabIndex = 1;
            this.num_fixed_sec.Value = new decimal(new int[] { 20, 0, 0, 65536 });
            this.num_fixed_sec.ValueChanged += new System.EventHandler(this.num_fixed_sec_ValueChanged);
            // 
            // lbl_fixed_unit
            // 
            this.lbl_fixed_unit.AutoSize = true;
            this.lbl_fixed_unit.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(90)))), ((int)(((byte)(90)))));
            this.lbl_fixed_unit.Location = new System.Drawing.Point(236, 26);
            this.lbl_fixed_unit.Name = "lbl_fixed_unit";
            this.lbl_fixed_unit.Size = new System.Drawing.Size(20, 15);
            this.lbl_fixed_unit.TabIndex = 2;
            this.lbl_fixed_unit.Text = "秒";
            // 
            // chk_depth
            // 
            this.chk_depth.AutoSize = true;
            this.chk_depth.Location = new System.Drawing.Point(14, 53);
            this.chk_depth.Name = "chk_depth";
            this.chk_depth.Size = new System.Drawing.Size(80, 19);
            this.chk_depth.TabIndex = 3;
            this.chk_depth.Text = "计算深度";
            this.chk_depth.UseVisualStyleBackColor = true;
            this.chk_depth.CheckedChanged += new System.EventHandler(this.chk_depth_CheckedChanged);
            // 
            // num_depth
            // 
            this.num_depth.Location = new System.Drawing.Point(150, 50);
            this.num_depth.Maximum = new decimal(new int[] { 200, 0, 0, 0 });
            this.num_depth.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.num_depth.Name = "num_depth";
            this.num_depth.Size = new System.Drawing.Size(80, 25);
            this.num_depth.TabIndex = 4;
            this.num_depth.Value = new decimal(new int[] { 200, 0, 0, 0 });
            this.num_depth.ValueChanged += new System.EventHandler(this.num_depth_ValueChanged);
            // 
            // lbl_depth_unit
            // 
            this.lbl_depth_unit.AutoSize = true;
            this.lbl_depth_unit.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(90)))), ((int)(((byte)(90)))));
            this.lbl_depth_unit.Location = new System.Drawing.Point(236, 54);
            this.lbl_depth_unit.Name = "lbl_depth_unit";
            this.lbl_depth_unit.Size = new System.Drawing.Size(20, 15);
            this.lbl_depth_unit.TabIndex = 5;
            this.lbl_depth_unit.Text = "层";
            // 
            // chk_open
            // 
            this.chk_open.AutoSize = true;
            this.chk_open.Location = new System.Drawing.Point(14, 81);
            this.chk_open.Name = "chk_open";
            this.chk_open.Size = new System.Drawing.Size(40, 19);
            this.chk_open.TabIndex = 6;
            this.chk_open.Text = "前";
            this.chk_open.UseVisualStyleBackColor = true;
            this.chk_open.CheckedChanged += new System.EventHandler(this.chk_open_CheckedChanged);
            // 
            // num_open_moves
            // 
            this.num_open_moves.Location = new System.Drawing.Point(70, 78);
            this.num_open_moves.Maximum = new decimal(new int[] { 999, 0, 0, 0 });
            this.num_open_moves.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.num_open_moves.Name = "num_open_moves";
            this.num_open_moves.Size = new System.Drawing.Size(58, 25);
            this.num_open_moves.TabIndex = 7;
            this.num_open_moves.Value = new decimal(new int[] { 10, 0, 0, 0 });
            this.num_open_moves.ValueChanged += new System.EventHandler(this.num_open_moves_ValueChanged);
            // 
            // lbl_open_moves_unit
            // 
            this.lbl_open_moves_unit.AutoSize = true;
            this.lbl_open_moves_unit.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(90)))), ((int)(((byte)(90)))));
            this.lbl_open_moves_unit.Location = new System.Drawing.Point(134, 82);
            this.lbl_open_moves_unit.Name = "lbl_open_moves_unit";
            this.lbl_open_moves_unit.Size = new System.Drawing.Size(90, 15);
            this.lbl_open_moves_unit.TabIndex = 8;
            this.lbl_open_moves_unit.Text = "回合使用步时";
            // 
            // num_open_sec
            // 
            this.num_open_sec.DecimalPlaces = 1;
            this.num_open_sec.Increment = new decimal(new int[] { 5, 0, 0, 65536 });
            this.num_open_sec.Location = new System.Drawing.Point(262, 78);
            this.num_open_sec.Maximum = new decimal(new int[] { 9999, 0, 0, 0 });
            this.num_open_sec.Minimum = new decimal(new int[] { 1, 0, 0, 65536 });
            this.num_open_sec.Name = "num_open_sec";
            this.num_open_sec.Size = new System.Drawing.Size(80, 25);
            this.num_open_sec.TabIndex = 9;
            this.num_open_sec.Value = new decimal(new int[] { 20, 0, 0, 65536 });
            this.num_open_sec.ValueChanged += new System.EventHandler(this.num_open_sec_ValueChanged);
            // 
            // lbl_open_sec_unit
            // 
            this.lbl_open_sec_unit.AutoSize = true;
            this.lbl_open_sec_unit.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(90)))), ((int)(((byte)(90)))));
            this.lbl_open_sec_unit.Location = new System.Drawing.Point(348, 82);
            this.lbl_open_sec_unit.Name = "lbl_open_sec_unit";
            this.lbl_open_sec_unit.Size = new System.Drawing.Size(45, 15);
            this.lbl_open_sec_unit.TabIndex = 10;
            this.lbl_open_sec_unit.Text = "秒每步";
            // 
            // chk_game
            // 
            this.chk_game.AutoSize = true;
            this.chk_game.Location = new System.Drawing.Point(14, 115);
            this.chk_game.Name = "chk_game";
            this.chk_game.Size = new System.Drawing.Size(40, 19);
            this.chk_game.TabIndex = 11;
            this.chk_game.Text = "局时";
            this.chk_game.UseVisualStyleBackColor = true;
            this.chk_game.CheckedChanged += new System.EventHandler(this.chk_game_CheckedChanged);
            // 
            // num_game_min
            // 
            this.num_game_min.Location = new System.Drawing.Point(90, 112);
            this.num_game_min.Maximum = new decimal(new int[] { 999, 0, 0, 0 });
            this.num_game_min.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.num_game_min.Name = "num_game_min";
            this.num_game_min.Size = new System.Drawing.Size(58, 25);
            this.num_game_min.TabIndex = 12;
            this.num_game_min.Value = new decimal(new int[] { 10, 0, 0, 0 });
            this.num_game_min.ValueChanged += new System.EventHandler(this.num_game_min_ValueChanged);
            // 
            // lbl_game_min_unit
            // 
            this.lbl_game_min_unit.AutoSize = true;
            this.lbl_game_min_unit.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(90)))), ((int)(((byte)(90)))));
            this.lbl_game_min_unit.Location = new System.Drawing.Point(154, 116);
            this.lbl_game_min_unit.Name = "lbl_game_min_unit";
            this.lbl_game_min_unit.Size = new System.Drawing.Size(90, 15);
            this.lbl_game_min_unit.TabIndex = 13;
            this.lbl_game_min_unit.Text = "分钟后使用步时";
            // 
            // num_game_sec
            // 
            this.num_game_sec.DecimalPlaces = 1;
            this.num_game_sec.Increment = new decimal(new int[] { 5, 0, 0, 65536 });
            this.num_game_sec.Location = new System.Drawing.Point(296, 112);
            this.num_game_sec.Maximum = new decimal(new int[] { 9999, 0, 0, 0 });
            this.num_game_sec.Minimum = new decimal(new int[] { 1, 0, 0, 65536 });
            this.num_game_sec.Name = "num_game_sec";
            this.num_game_sec.Size = new System.Drawing.Size(80, 25);
            this.num_game_sec.TabIndex = 14;
            this.num_game_sec.Value = new decimal(new int[] { 20, 0, 0, 65536 });
            this.num_game_sec.ValueChanged += new System.EventHandler(this.num_game_sec_ValueChanged);
            // 
            // lbl_game_sec_unit
            // 
            this.lbl_game_sec_unit.AutoSize = true;
            this.lbl_game_sec_unit.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(90)))), ((int)(((byte)(90)))));
            this.lbl_game_sec_unit.Location = new System.Drawing.Point(382, 116);
            this.lbl_game_sec_unit.Name = "lbl_game_sec_unit";
            this.lbl_game_sec_unit.Size = new System.Drawing.Size(45, 15);
            this.lbl_game_sec_unit.TabIndex = 15;
            this.lbl_game_sec_unit.Text = "秒每步";
            // 
            // groupBox_gap
            // 
            this.groupBox_gap.Controls.Add(this.lbl_engine_gap);
            this.groupBox_gap.Controls.Add(this.num_engine_min);
            this.groupBox_gap.Controls.Add(this.lbl_engine_dash);
            this.groupBox_gap.Controls.Add(this.num_engine_max);
            this.groupBox_gap.Controls.Add(this.lbl_engine_unit);
            this.groupBox_gap.Controls.Add(this.lbl_book_gap);
            this.groupBox_gap.Controls.Add(this.num_book_min);
            this.groupBox_gap.Controls.Add(this.lbl_book_dash);
            this.groupBox_gap.Controls.Add(this.num_book_max);
            this.groupBox_gap.Controls.Add(this.lbl_book_unit);
            this.groupBox_gap.Location = new System.Drawing.Point(12, 160);
            this.groupBox_gap.Name = "groupBox_gap";
            this.groupBox_gap.Size = new System.Drawing.Size(500, 92);
            this.groupBox_gap.TabIndex = 1;
            this.groupBox_gap.TabStop = false;
            this.groupBox_gap.Text = "最短出招间隔";
            // 
            // lbl_engine_gap
            // 
            this.lbl_engine_gap.AutoSize = true;
            this.lbl_engine_gap.Location = new System.Drawing.Point(14, 27);
            this.lbl_engine_gap.Name = "lbl_engine_gap";
            this.lbl_engine_gap.Size = new System.Drawing.Size(110, 15);
            this.lbl_engine_gap.TabIndex = 0;
            this.lbl_engine_gap.Text = "引擎最短出招间隔";
            // 
            // num_engine_min
            // 
            this.num_engine_min.DecimalPlaces = 1;
            this.num_engine_min.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
            this.num_engine_min.Location = new System.Drawing.Point(170, 24);
            this.num_engine_min.Maximum = new decimal(new int[] { 600, 0, 0, 0 });
            this.num_engine_min.Name = "num_engine_min";
            this.num_engine_min.Size = new System.Drawing.Size(80, 25);
            this.num_engine_min.TabIndex = 1;
            this.num_engine_min.Value = new decimal(new int[] { 0, 0, 0, 0 });
            this.num_engine_min.ValueChanged += new System.EventHandler(this.num_engine_min_ValueChanged);
            // 
            // lbl_engine_dash
            // 
            this.lbl_engine_dash.AutoSize = true;
            this.lbl_engine_dash.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(90)))), ((int)(((byte)(90)))));
            this.lbl_engine_dash.Location = new System.Drawing.Point(256, 28);
            this.lbl_engine_dash.Name = "lbl_engine_dash";
            this.lbl_engine_dash.Size = new System.Drawing.Size(15, 15);
            this.lbl_engine_dash.TabIndex = 2;
            this.lbl_engine_dash.Text = "到";
            // 
            // num_engine_max
            // 
            this.num_engine_max.DecimalPlaces = 1;
            this.num_engine_max.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
            this.num_engine_max.Location = new System.Drawing.Point(292, 24);
            this.num_engine_max.Maximum = new decimal(new int[] { 600, 0, 0, 0 });
            this.num_engine_max.Name = "num_engine_max";
            this.num_engine_max.Size = new System.Drawing.Size(80, 25);
            this.num_engine_max.TabIndex = 3;
            this.num_engine_max.Value = new decimal(new int[] { 0, 0, 0, 0 });
            this.num_engine_max.ValueChanged += new System.EventHandler(this.num_engine_max_ValueChanged);
            // 
            // lbl_engine_unit
            // 
            this.lbl_engine_unit.AutoSize = true;
            this.lbl_engine_unit.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(90)))), ((int)(((byte)(90)))));
            this.lbl_engine_unit.Location = new System.Drawing.Point(378, 28);
            this.lbl_engine_unit.Name = "lbl_engine_unit";
            this.lbl_engine_unit.Size = new System.Drawing.Size(20, 15);
            this.lbl_engine_unit.TabIndex = 4;
            this.lbl_engine_unit.Text = "秒";
            // 
            // lbl_book_gap
            // 
            this.lbl_book_gap.AutoSize = true;
            this.lbl_book_gap.Location = new System.Drawing.Point(14, 57);
            this.lbl_book_gap.Name = "lbl_book_gap";
            this.lbl_book_gap.Size = new System.Drawing.Size(125, 15);
            this.lbl_book_gap.TabIndex = 5;
            this.lbl_book_gap.Text = "开局库最短出招间隔";
            // 
            // num_book_min
            // 
            this.num_book_min.DecimalPlaces = 1;
            this.num_book_min.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
            this.num_book_min.Location = new System.Drawing.Point(184, 54);
            this.num_book_min.Maximum = new decimal(new int[] { 600, 0, 0, 0 });
            this.num_book_min.Name = "num_book_min";
            this.num_book_min.Size = new System.Drawing.Size(80, 25);
            this.num_book_min.TabIndex = 6;
            this.num_book_min.Value = new decimal(new int[] { 0, 0, 0, 0 });
            this.num_book_min.ValueChanged += new System.EventHandler(this.num_book_min_ValueChanged);
            // 
            // lbl_book_dash
            // 
            this.lbl_book_dash.AutoSize = true;
            this.lbl_book_dash.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(90)))), ((int)(((byte)(90)))));
            this.lbl_book_dash.Location = new System.Drawing.Point(270, 58);
            this.lbl_book_dash.Name = "lbl_book_dash";
            this.lbl_book_dash.Size = new System.Drawing.Size(15, 15);
            this.lbl_book_dash.TabIndex = 7;
            this.lbl_book_dash.Text = "到";
            // 
            // num_book_max
            // 
            this.num_book_max.DecimalPlaces = 1;
            this.num_book_max.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
            this.num_book_max.Location = new System.Drawing.Point(306, 54);
            this.num_book_max.Maximum = new decimal(new int[] { 600, 0, 0, 0 });
            this.num_book_max.Name = "num_book_max";
            this.num_book_max.Size = new System.Drawing.Size(80, 25);
            this.num_book_max.TabIndex = 8;
            this.num_book_max.Value = new decimal(new int[] { 0, 0, 0, 0 });
            this.num_book_max.ValueChanged += new System.EventHandler(this.num_book_max_ValueChanged);
            // 
            // lbl_book_unit
            // 
            this.lbl_book_unit.AutoSize = true;
            this.lbl_book_unit.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(90)))), ((int)(((byte)(90)))));
            this.lbl_book_unit.Location = new System.Drawing.Point(392, 58);
            this.lbl_book_unit.Name = "lbl_book_unit";
            this.lbl_book_unit.Size = new System.Drawing.Size(20, 15);
            this.lbl_book_unit.TabIndex = 9;
            this.lbl_book_unit.Text = "秒";
            // 
            // label_hint
            // 
            this.label_hint.AutoSize = false;
            this.label_hint.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(90)))), ((int)(((byte)(90)))));
            this.label_hint.Location = new System.Drawing.Point(12, 266);
            this.label_hint.Name = "label_hint";
            this.label_hint.Size = new System.Drawing.Size(368, 36);
            this.label_hint.TabIndex = 2;
            this.label_hint.Text = "参数即时保存；全不勾选时按默认步时出招。\r\n线程数与 Hash 在「引擎设置」分组框调整。";
            // 
            // button_save
            // 
            this.button_save.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button_save.AutoSize = false;
            this.button_save.Location = new System.Drawing.Point(392, 264);
            this.button_save.Name = "button_save";
            this.button_save.Size = new System.Drawing.Size(120, 36);
            this.button_save.TabIndex = 3;
            this.button_save.Text = "关闭窗口";
            this.button_save.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.button_save.UseVisualStyleBackColor = true;
            this.button_save.Click += new System.EventHandler(this.button_save_Click);
            // 
            // TimeSettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(524, 314);
            this.Controls.Add(this.groupBox_time);
            this.Controls.Add(this.groupBox_gap);
            this.Controls.Add(this.label_hint);
            this.Controls.Add(this.button_save);
            this.MinimumSize = new System.Drawing.Size(540, 354);
            this.Name = "TimeSettingsForm";
            this.Text = "时间设置";
            this.Load += new System.EventHandler(this.TimeSettingsForm_Load);
            this.groupBox_time.ResumeLayout(false);
            this.groupBox_time.PerformLayout();
            this.groupBox_gap.ResumeLayout(false);
            this.groupBox_gap.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.num_fixed_sec)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_depth)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_open_moves)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_open_sec)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_game_min)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_game_sec)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_engine_min)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_engine_max)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_book_min)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_book_max)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox_time;
        private System.Windows.Forms.CheckBox chk_fixed;
        private System.Windows.Forms.NumericUpDown num_fixed_sec;
        private System.Windows.Forms.Label lbl_fixed_unit;
        private System.Windows.Forms.CheckBox chk_depth;
        private System.Windows.Forms.NumericUpDown num_depth;
        private System.Windows.Forms.Label lbl_depth_unit;
        private System.Windows.Forms.CheckBox chk_open;
        private System.Windows.Forms.NumericUpDown num_open_moves;
        private System.Windows.Forms.Label lbl_open_moves_unit;
        private System.Windows.Forms.NumericUpDown num_open_sec;
        private System.Windows.Forms.Label lbl_open_sec_unit;
        private System.Windows.Forms.CheckBox chk_game;
        private System.Windows.Forms.NumericUpDown num_game_min;
        private System.Windows.Forms.Label lbl_game_min_unit;
        private System.Windows.Forms.NumericUpDown num_game_sec;
        private System.Windows.Forms.Label lbl_game_sec_unit;
        private System.Windows.Forms.GroupBox groupBox_gap;
        private System.Windows.Forms.Label lbl_engine_gap;
        private System.Windows.Forms.NumericUpDown num_engine_min;
        private System.Windows.Forms.Label lbl_engine_dash;
        private System.Windows.Forms.NumericUpDown num_engine_max;
        private System.Windows.Forms.Label lbl_engine_unit;
        private System.Windows.Forms.Label lbl_book_gap;
        private System.Windows.Forms.NumericUpDown num_book_min;
        private System.Windows.Forms.Label lbl_book_dash;
        private System.Windows.Forms.NumericUpDown num_book_max;
        private System.Windows.Forms.Label lbl_book_unit;
        private System.Windows.Forms.Label label_hint;
        private System.Windows.Forms.Button button_save;
    }
}
