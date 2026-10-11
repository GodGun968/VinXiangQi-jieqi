namespace VinXiangQi
{
    partial class DetectionSettingsForm
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
            if (disposing && (components != null))
            {
                components.Dispose();
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DetectionSettingsForm));
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.numericUpDown_detection_confirm = new System.Windows.Forms.NumericUpDown();
            this.numericUpDown_detection_interval = new System.Windows.Forms.NumericUpDown();
            this.numericUpDown_jieqi_conf = new System.Windows.Forms.NumericUpDown();
            this.numericUpDown_jieqi_nms = new System.Windows.Forms.NumericUpDown();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.numericUpDown_invalid_reset = new System.Windows.Forms.NumericUpDown();
            this.checkBox_dual_validate = new System.Windows.Forms.CheckBox();
this.checkBox_dual_agree = new System.Windows.Forms.CheckBox();
            this.checkBox_dual_visible = new System.Windows.Forms.CheckBox();
            this.groupBox_dual = new System.Windows.Forms.GroupBox();
            this.label_dual_chess_model = new System.Windows.Forms.Label();
            this.comboBox_dual_chess_model = new System.Windows.Forms.ComboBox();
            this.label_dual_chess_conf = new System.Windows.Forms.Label();
            this.numericUpDown_dual_chess_conf = new System.Windows.Forms.NumericUpDown();
            this.label_dual_jieqi_model = new System.Windows.Forms.Label();
            this.comboBox_dual_jieqi_model = new System.Windows.Forms.ComboBox();
            this.label_dual_jieqi_dark_conf = new System.Windows.Forms.Label();
            this.numericUpDown_dual_jieqi_dark_conf = new System.Windows.Forms.NumericUpDown();
            this.groupBox_draw = new System.Windows.Forms.GroupBox();
            this.checkBox_draw_overlay = new System.Windows.Forms.CheckBox();
            this.checkBox_draw_box = new System.Windows.Forms.CheckBox();
            this.checkBox_draw_class = new System.Windows.Forms.CheckBox();
            this.checkBox_draw_conf = new System.Windows.Forms.CheckBox();
            this.label_draw_thickness = new System.Windows.Forms.Label();
            this.numericUpDown_draw_thickness = new System.Windows.Forms.NumericUpDown();
            this.label_draw_chess_color = new System.Windows.Forms.Label();
            this.button_draw_chess_color = new System.Windows.Forms.Button();
            this.label_draw_jieqi_color = new System.Windows.Forms.Label();
            this.button_draw_jieqi_color = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.button_save = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.groupBox5.SuspendLayout();
            this.groupBox_dual.SuspendLayout();
            this.groupBox_draw.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_detection_confirm)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_detection_interval)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_jieqi_conf)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_jieqi_nms)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_invalid_reset)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_dual_chess_conf)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_dual_jieqi_dark_conf)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_draw_thickness)).BeginInit();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.numericUpDown_detection_confirm);
            this.groupBox1.Location = new System.Drawing.Point(19, 15);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(176, 72);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "识别确认次数";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Controls.Add(this.numericUpDown_detection_interval);
            this.groupBox2.Location = new System.Drawing.Point(19, 95);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(176, 72);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "识别间隔";
            // 
            // numericUpDown_detection_confirm
            // 
            this.numericUpDown_detection_confirm.Location = new System.Drawing.Point(23, 24);
            this.numericUpDown_detection_confirm.Maximum = new decimal(new int[] {
            20,
            0,
            0,
            0});
            this.numericUpDown_detection_confirm.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDown_detection_confirm.Name = "numericUpDown_detection_confirm";
            this.numericUpDown_detection_confirm.Size = new System.Drawing.Size(121, 25);
            this.numericUpDown_detection_confirm.TabIndex = 0;
            this.numericUpDown_detection_confirm.Value = new decimal(new int[] {
            2,
            0,
            0,
            0});
            this.numericUpDown_detection_confirm.ValueChanged += new System.EventHandler(this.numericUpDown_detection_confirm_ValueChanged);
            // 
            // numericUpDown_detection_interval
            // 
            this.numericUpDown_detection_interval.Increment = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.numericUpDown_detection_interval.Location = new System.Drawing.Point(23, 24);
            this.numericUpDown_detection_interval.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.numericUpDown_detection_interval.Name = "numericUpDown_detection_interval";
            this.numericUpDown_detection_interval.Size = new System.Drawing.Size(96, 25);
            this.numericUpDown_detection_interval.TabIndex = 1;
            this.numericUpDown_detection_interval.Value = new decimal(new int[] {
            550,
            0,
            0,
            0});
            this.numericUpDown_detection_interval.ValueChanged += new System.EventHandler(this.numericUpDown_detection_interval_ValueChanged);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.numericUpDown_jieqi_conf);
            this.groupBox3.Location = new System.Drawing.Point(19, 175);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(176, 72);
            this.groupBox3.TabIndex = 3;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "揭棋检测置信度";
            // 
            // numericUpDown_jieqi_conf
            // 
            this.numericUpDown_jieqi_conf.DecimalPlaces = 2;
            this.numericUpDown_jieqi_conf.Increment = new decimal(new int[] {
            5,
            0,
            0,
            131072});
            this.numericUpDown_jieqi_conf.Location = new System.Drawing.Point(23, 24);
            this.numericUpDown_jieqi_conf.Maximum = new decimal(new int[] {
            95,
            0,
            0,
            131072});
            this.numericUpDown_jieqi_conf.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            131072});
            this.numericUpDown_jieqi_conf.Name = "numericUpDown_jieqi_conf";
            this.numericUpDown_jieqi_conf.Size = new System.Drawing.Size(121, 25);
            this.numericUpDown_jieqi_conf.TabIndex = 0;
            this.numericUpDown_jieqi_conf.Value = new decimal(new int[] {
            50,
            0,
            0,
            131072});
            this.numericUpDown_jieqi_conf.ValueChanged += new System.EventHandler(this.numericUpDown_jieqi_conf_ValueChanged);
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.numericUpDown_jieqi_nms);
            this.groupBox4.Location = new System.Drawing.Point(19, 255);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(176, 72);
            this.groupBox4.TabIndex = 4;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "揭棋NMS阈值";
            // 
            // numericUpDown_jieqi_nms
            // 
            this.numericUpDown_jieqi_nms.DecimalPlaces = 2;
            this.numericUpDown_jieqi_nms.Increment = new decimal(new int[] {
            5,
            0,
            0,
            131072});
            this.numericUpDown_jieqi_nms.Location = new System.Drawing.Point(23, 24);
            this.numericUpDown_jieqi_nms.Maximum = new decimal(new int[] {
            90,
            0,
            0,
            131072});
            this.numericUpDown_jieqi_nms.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            131072});
            this.numericUpDown_jieqi_nms.Name = "numericUpDown_jieqi_nms";
            this.numericUpDown_jieqi_nms.Size = new System.Drawing.Size(121, 25);
            this.numericUpDown_jieqi_nms.TabIndex = 0;
            this.numericUpDown_jieqi_nms.Value = new decimal(new int[] {
            45,
            0,
            0,
            131072});
            this.numericUpDown_jieqi_nms.ValueChanged += new System.EventHandler(this.numericUpDown_jieqi_nms_ValueChanged);
            // 
            // groupBox5
            // 
            this.groupBox5.Controls.Add(this.numericUpDown_invalid_reset);
            this.groupBox5.Location = new System.Drawing.Point(19, 335);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Size = new System.Drawing.Size(176, 72);
            this.groupBox5.TabIndex = 5;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "不合法重置次数";
            // 
            // numericUpDown_invalid_reset
            // 
            this.numericUpDown_invalid_reset.Location = new System.Drawing.Point(23, 24);
            this.numericUpDown_invalid_reset.Maximum = new decimal(new int[] {
            20,
            0,
            0,
            0});
            this.numericUpDown_invalid_reset.Minimum = new decimal(new int[] {
            2,
            0,
            0,
            0});
            this.numericUpDown_invalid_reset.Name = "numericUpDown_invalid_reset";
            this.numericUpDown_invalid_reset.Size = new System.Drawing.Size(121, 25);
            this.numericUpDown_invalid_reset.TabIndex = 0;
            this.numericUpDown_invalid_reset.Value = new decimal(new int[] {
            6,
            0,
            0,
            0});
            this.numericUpDown_invalid_reset.ValueChanged += new System.EventHandler(this.numericUpDown_invalid_reset_ValueChanged);
            // 
            // groupBox_dual
            // 
            this.groupBox_dual.Controls.Add(this.checkBox_dual_agree);
            this.groupBox_dual.Controls.Add(this.checkBox_dual_visible);
            this.groupBox_dual.Controls.Add(this.checkBox_dual_validate);
            this.groupBox_dual.Controls.Add(this.label_dual_chess_model);
            this.groupBox_dual.Controls.Add(this.comboBox_dual_chess_model);
            this.groupBox_dual.Controls.Add(this.label_dual_chess_conf);
            this.groupBox_dual.Controls.Add(this.numericUpDown_dual_chess_conf);
            this.groupBox_dual.Controls.Add(this.label_dual_jieqi_model);
            this.groupBox_dual.Controls.Add(this.comboBox_dual_jieqi_model);
            this.groupBox_dual.Controls.Add(this.label_dual_jieqi_dark_conf);
            this.groupBox_dual.Controls.Add(this.numericUpDown_dual_jieqi_dark_conf);
            this.groupBox_dual.Location = new System.Drawing.Point(213, 12);
            this.groupBox_dual.Name = "groupBox_dual";
            this.groupBox_dual.Size = new System.Drawing.Size(400, 278);
            this.groupBox_dual.TabIndex = 5;
            this.groupBox_dual.TabStop = false;
            this.groupBox_dual.Text = "交叉验证";
            // 
            // checkBox_dual_validate
            // 
            this.checkBox_dual_validate.AutoSize = false;
            this.checkBox_dual_validate.Location = new System.Drawing.Point(16, 26);
            this.checkBox_dual_validate.Name = "checkBox_dual_validate";
            this.checkBox_dual_validate.Size = new System.Drawing.Size(173, 22);
            this.checkBox_dual_validate.TabIndex = 0;
            this.checkBox_dual_validate.Text = "启用交叉验证";
            this.checkBox_dual_validate.UseVisualStyleBackColor = true;
            this.checkBox_dual_validate.CheckedChanged += new System.EventHandler(this.checkBox_dual_validate_CheckedChanged);
            // 
            // checkBox_dual_agree
            // 
            this.checkBox_dual_agree.AutoSize = false;
            this.checkBox_dual_agree.Location = new System.Drawing.Point(16, 52);
            this.checkBox_dual_agree.Name = "checkBox_dual_agree";
            this.checkBox_dual_agree.Size = new System.Drawing.Size(360, 22);
            this.checkBox_dual_agree.TabIndex = 1;
            this.checkBox_dual_agree.Text = "高度可信不验证（两模型一致，绘绿色）";
            this.checkBox_dual_agree.ForeColor = System.Drawing.Color.Green;
            this.checkBox_dual_agree.UseVisualStyleBackColor = true;
            this.checkBox_dual_agree.CheckedChanged += new System.EventHandler(this.checkBox_dual_agree_CheckedChanged);
            // 
            // checkBox_dual_visible
            // 
            this.checkBox_dual_visible.AutoSize = false;
            this.checkBox_dual_visible.Location = new System.Drawing.Point(16, 78);
            this.checkBox_dual_visible.Name = "checkBox_dual_visible";
            this.checkBox_dual_visible.Size = new System.Drawing.Size(370, 22);
            this.checkBox_dual_visible.TabIndex = 9;
            this.checkBox_dual_visible.Text = "置信度优先裁定（两个阈值将禁用）";
            this.checkBox_dual_visible.UseVisualStyleBackColor = true;
            this.checkBox_dual_visible.CheckedChanged += new System.EventHandler(this.checkBox_dual_visible_CheckedChanged);
            // 
            // label_dual_chess_model
            // 
            this.label_dual_chess_model.AutoSize = false;
            this.label_dual_chess_model.Location = new System.Drawing.Point(16, 110);
            this.label_dual_chess_model.Name = "label_dual_chess_model";
            this.label_dual_chess_model.Size = new System.Drawing.Size(160, 20);
            this.label_dual_chess_model.TabIndex = 1;
            this.label_dual_chess_model.Text = "交叉验证用象棋模型";
            // 
            // comboBox_dual_chess_model
            // 
            this.comboBox_dual_chess_model.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_dual_chess_model.Location = new System.Drawing.Point(16, 130);
            this.comboBox_dual_chess_model.Name = "comboBox_dual_chess_model";
            this.comboBox_dual_chess_model.Size = new System.Drawing.Size(227, 30);
            this.comboBox_dual_chess_model.TabIndex = 2;
            this.comboBox_dual_chess_model.SelectedIndexChanged += new System.EventHandler(this.comboBox_dual_chess_model_SelectedIndexChanged);
            // 
            // label_dual_chess_conf
            // 
            this.label_dual_chess_conf.AutoSize = false;
            this.label_dual_chess_conf.Location = new System.Drawing.Point(256, 112);
            this.label_dual_chess_conf.Name = "label_dual_chess_conf";
            this.label_dual_chess_conf.Size = new System.Drawing.Size(107, 20);
            this.label_dual_chess_conf.TabIndex = 3;
            this.label_dual_chess_conf.Text = "棋子置信度";
            // 
            // numericUpDown_dual_chess_conf
            // 
            this.numericUpDown_dual_chess_conf.DecimalPlaces = 2;
            this.numericUpDown_dual_chess_conf.Increment = new decimal(new int[] {
            5,
            0,
            0,
            131072});
            this.numericUpDown_dual_chess_conf.Location = new System.Drawing.Point(256, 132);
            this.numericUpDown_dual_chess_conf.Maximum = new decimal(new int[] {
            100,
            0,
            0,
            131072});
            this.numericUpDown_dual_chess_conf.Minimum = new decimal(new int[] {
            50,
            0,
            0,
            131072});
            this.numericUpDown_dual_chess_conf.Name = "numericUpDown_dual_chess_conf";
            this.numericUpDown_dual_chess_conf.Size = new System.Drawing.Size(85, 30);
            this.numericUpDown_dual_chess_conf.TabIndex = 4;
            this.numericUpDown_dual_chess_conf.Value = new decimal(new int[] {
            85,
            0,
            0,
            131072});
            this.numericUpDown_dual_chess_conf.ValueChanged += new System.EventHandler(this.numericUpDown_dual_chess_conf_ValueChanged);
            // 
            // label_dual_jieqi_model
            // 
            this.label_dual_jieqi_model.AutoSize = false;
            this.label_dual_jieqi_model.Location = new System.Drawing.Point(16, 172);
            this.label_dual_jieqi_model.Name = "label_dual_jieqi_model";
            this.label_dual_jieqi_model.Size = new System.Drawing.Size(160, 20);
            this.label_dual_jieqi_model.TabIndex = 5;
            this.label_dual_jieqi_model.Text = "交叉验证用揭棋模型";
            // 
            // comboBox_dual_jieqi_model
            // 
            this.comboBox_dual_jieqi_model.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_dual_jieqi_model.Location = new System.Drawing.Point(16, 192);
            this.comboBox_dual_jieqi_model.Name = "comboBox_dual_jieqi_model";
            this.comboBox_dual_jieqi_model.Size = new System.Drawing.Size(227, 30);
            this.comboBox_dual_jieqi_model.TabIndex = 6;
            this.comboBox_dual_jieqi_model.SelectedIndexChanged += new System.EventHandler(this.comboBox_dual_jieqi_model_SelectedIndexChanged);
            // 
            // label_dual_jieqi_dark_conf
            // 
            this.label_dual_jieqi_dark_conf.AutoSize = false;
            this.label_dual_jieqi_dark_conf.Location = new System.Drawing.Point(256, 174);
            this.label_dual_jieqi_dark_conf.Name = "label_dual_jieqi_dark_conf";
            this.label_dual_jieqi_dark_conf.Size = new System.Drawing.Size(107, 20);
            this.label_dual_jieqi_dark_conf.TabIndex = 7;
            this.label_dual_jieqi_dark_conf.Text = "暗子置信度";
            // 
            // numericUpDown_dual_jieqi_dark_conf
            // 
            this.numericUpDown_dual_jieqi_dark_conf.DecimalPlaces = 2;
            this.numericUpDown_dual_jieqi_dark_conf.Increment = new decimal(new int[] {
            5,
            0,
            0,
            131072});
            this.numericUpDown_dual_jieqi_dark_conf.Location = new System.Drawing.Point(256, 194);
            this.numericUpDown_dual_jieqi_dark_conf.Maximum = new decimal(new int[] {
            100,
            0,
            0,
            131072});
            this.numericUpDown_dual_jieqi_dark_conf.Minimum = new decimal(new int[] {
            50,
            0,
            0,
            131072});
            this.numericUpDown_dual_jieqi_dark_conf.Name = "numericUpDown_dual_jieqi_dark_conf";
            this.numericUpDown_dual_jieqi_dark_conf.Size = new System.Drawing.Size(85, 30);
            this.numericUpDown_dual_jieqi_dark_conf.TabIndex = 8;
            this.numericUpDown_dual_jieqi_dark_conf.Value = new decimal(new int[] {
            95,
            0,
            0,
            131072});
            this.numericUpDown_dual_jieqi_dark_conf.ValueChanged += new System.EventHandler(this.numericUpDown_dual_jieqi_dark_conf_ValueChanged);
            // 
            // groupBox_draw
            // 
            this.groupBox_draw.Controls.Add(this.checkBox_draw_overlay);
            this.groupBox_draw.Controls.Add(this.checkBox_draw_box);
            this.groupBox_draw.Controls.Add(this.checkBox_draw_class);
            this.groupBox_draw.Controls.Add(this.checkBox_draw_conf);
            this.groupBox_draw.Controls.Add(this.label_draw_thickness);
            this.groupBox_draw.Controls.Add(this.numericUpDown_draw_thickness);
            this.groupBox_draw.Controls.Add(this.label_draw_chess_color);
            this.groupBox_draw.Controls.Add(this.button_draw_chess_color);
            this.groupBox_draw.Controls.Add(this.label_draw_jieqi_color);
            this.groupBox_draw.Controls.Add(this.button_draw_jieqi_color);
            this.groupBox_draw.Location = new System.Drawing.Point(213, 296);
            this.groupBox_draw.Name = "groupBox_draw";
            this.groupBox_draw.Size = new System.Drawing.Size(400, 212);
            this.groupBox_draw.TabIndex = 6;
            this.groupBox_draw.TabStop = false;
            this.groupBox_draw.Text = "识别绘制";
            // 
            // checkBox_draw_overlay
            // 
            this.checkBox_draw_overlay.AutoSize = false;
            this.checkBox_draw_overlay.Location = new System.Drawing.Point(16, 30);
            this.checkBox_draw_overlay.Name = "checkBox_draw_overlay";
            this.checkBox_draw_overlay.Size = new System.Drawing.Size(160, 25);
            this.checkBox_draw_overlay.TabIndex = 0;
            this.checkBox_draw_overlay.Text = "启用识别绘制";
            this.checkBox_draw_overlay.UseVisualStyleBackColor = true;
            this.checkBox_draw_overlay.CheckedChanged += new System.EventHandler(this.checkBox_draw_overlay_CheckedChanged);
            // 
            // checkBox_draw_box
            // 
            this.checkBox_draw_box.AutoSize = false;
            this.checkBox_draw_box.Location = new System.Drawing.Point(16, 62);
            this.checkBox_draw_box.Name = "checkBox_draw_box";
            this.checkBox_draw_box.Size = new System.Drawing.Size(140, 25);
            this.checkBox_draw_box.TabIndex = 1;
            this.checkBox_draw_box.Text = "绘制检测框";
            this.checkBox_draw_box.UseVisualStyleBackColor = true;
            this.checkBox_draw_box.CheckedChanged += new System.EventHandler(this.checkBox_draw_box_CheckedChanged);
            // 
            // checkBox_draw_class
            // 
            this.checkBox_draw_class.AutoSize = false;
            this.checkBox_draw_class.Location = new System.Drawing.Point(16, 92);
            this.checkBox_draw_class.Name = "checkBox_draw_class";
            this.checkBox_draw_class.Size = new System.Drawing.Size(140, 25);
            this.checkBox_draw_class.TabIndex = 2;
            this.checkBox_draw_class.Text = "绘制类名";
            this.checkBox_draw_class.UseVisualStyleBackColor = true;
            this.checkBox_draw_class.CheckedChanged += new System.EventHandler(this.checkBox_draw_class_CheckedChanged);
            // 
            // checkBox_draw_conf
            // 
            this.checkBox_draw_conf.AutoSize = false;
            this.checkBox_draw_conf.Location = new System.Drawing.Point(171, 62);
            this.checkBox_draw_conf.Name = "checkBox_draw_conf";
            this.checkBox_draw_conf.Size = new System.Drawing.Size(147, 25);
            this.checkBox_draw_conf.TabIndex = 3;
            this.checkBox_draw_conf.Text = "绘制置信度";
            this.checkBox_draw_conf.UseVisualStyleBackColor = true;
            this.checkBox_draw_conf.CheckedChanged += new System.EventHandler(this.checkBox_draw_conf_CheckedChanged);
            // 
            // label_draw_thickness
            // 
            this.label_draw_thickness.AutoSize = false;
            this.label_draw_thickness.Location = new System.Drawing.Point(16, 128);
            this.label_draw_thickness.Name = "label_draw_thickness";
            this.label_draw_thickness.Size = new System.Drawing.Size(93, 20);
            this.label_draw_thickness.TabIndex = 4;
            this.label_draw_thickness.Text = "框线粗细";
            // 
            // numericUpDown_draw_thickness
            // 
            this.numericUpDown_draw_thickness.Location = new System.Drawing.Point(16, 150);
            this.numericUpDown_draw_thickness.Maximum = new decimal(new int[] {
            8,
            0,
            0,
            0});
            this.numericUpDown_draw_thickness.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDown_draw_thickness.Name = "numericUpDown_draw_thickness";
            this.numericUpDown_draw_thickness.Size = new System.Drawing.Size(85, 30);
            this.numericUpDown_draw_thickness.TabIndex = 5;
            this.numericUpDown_draw_thickness.Value = new decimal(new int[] {
            2,
            0,
            0,
            0});
            this.numericUpDown_draw_thickness.ValueChanged += new System.EventHandler(this.numericUpDown_draw_thickness_ValueChanged);
            // 
            // label_draw_chess_color
            // 
            this.label_draw_chess_color.AutoSize = false;
            this.label_draw_chess_color.Location = new System.Drawing.Point(117, 128);
            this.label_draw_chess_color.Name = "label_draw_chess_color";
            this.label_draw_chess_color.Size = new System.Drawing.Size(128, 20);
            this.label_draw_chess_color.TabIndex = 6;
            this.label_draw_chess_color.Text = "象棋裁定颜色";
            // 
            // button_draw_chess_color
            // 
            this.button_draw_chess_color.Location = new System.Drawing.Point(117, 150);
            this.button_draw_chess_color.Name = "button_draw_chess_color";
            this.button_draw_chess_color.Size = new System.Drawing.Size(117, 30);
            this.button_draw_chess_color.TabIndex = 7;
            this.button_draw_chess_color.Text = "选择颜色";
            this.button_draw_chess_color.UseVisualStyleBackColor = false;
            this.button_draw_chess_color.Click += new System.EventHandler(this.button_draw_chess_color_Click);
            // 
            // label_draw_jieqi_color
            // 
            this.label_draw_jieqi_color.AutoSize = false;
            this.label_draw_jieqi_color.Location = new System.Drawing.Point(251, 128);
            this.label_draw_jieqi_color.Name = "label_draw_jieqi_color";
            this.label_draw_jieqi_color.Size = new System.Drawing.Size(128, 20);
            this.label_draw_jieqi_color.TabIndex = 8;
            this.label_draw_jieqi_color.Text = "揭棋裁定颜色";
            // 
            // button_draw_jieqi_color
            // 
            this.button_draw_jieqi_color.Location = new System.Drawing.Point(251, 150);
            this.button_draw_jieqi_color.Name = "button_draw_jieqi_color";
            this.button_draw_jieqi_color.Size = new System.Drawing.Size(117, 30);
            this.button_draw_jieqi_color.TabIndex = 9;
            this.button_draw_jieqi_color.Text = "选择颜色";
            this.button_draw_jieqi_color.UseVisualStyleBackColor = false;
            this.button_draw_jieqi_color.Click += new System.EventHandler(this.button_draw_jieqi_color_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("宋体", 10.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label1.Location = new System.Drawing.Point(125, 26);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(26, 18);
            this.label1.TabIndex = 2;
            this.label1.Text = "ms";
            // 
            // button_save
            // 
            this.button_save.Location = new System.Drawing.Point(493, 526);
            this.button_save.Name = "button_save";
            this.button_save.Size = new System.Drawing.Size(160, 38);
            this.button_save.TabIndex = 2;
            this.button_save.Text = "保存";
            this.button_save.UseVisualStyleBackColor = true;
            this.button_save.Click += new System.EventHandler(this.button_save_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = false;
            this.label2.Location = new System.Drawing.Point(19, 521);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(460, 100);
            this.label2.TabIndex = 3;
            this.label2.Text = "请不要随意调整识别间隔，\r\n识别间隔如果太小可能会导致识别到错误的中间局面。\r\n如果连线的是JJ象棋等动画时间较短的游戏，\r\n可酌情将识别确认次数调整为1次，\r\n" +
    "如果连线的是天天象棋等动画时间较长的游戏，\r\n则识别确认次数至少需要两次。";
            // 
            // DetectionSettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(667, 626);
            this.Controls.Add(this.groupBox_draw);
            this.Controls.Add(this.groupBox_dual);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.button_save);
            this.Controls.Add(this.groupBox5);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "DetectionSettingsForm";
            this.Text = "识别设置";
            this.Load += new System.EventHandler(this.DetectionSettingsForm_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox4.ResumeLayout(false);
            this.groupBox5.ResumeLayout(false);
            this.groupBox_dual.ResumeLayout(false);
            this.groupBox_dual.PerformLayout();
            this.groupBox_draw.ResumeLayout(false);
            this.groupBox_draw.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_detection_confirm)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_detection_interval)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_jieqi_conf)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_jieqi_nms)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_invalid_reset)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_dual_chess_conf)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_dual_jieqi_dark_conf)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_draw_thickness)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.NumericUpDown numericUpDown_detection_confirm;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown numericUpDown_detection_interval;
        private System.Windows.Forms.Button button_save;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.NumericUpDown numericUpDown_jieqi_conf;
        private System.Windows.Forms.NumericUpDown numericUpDown_jieqi_nms;
        private System.Windows.Forms.GroupBox groupBox5;
        private System.Windows.Forms.NumericUpDown numericUpDown_invalid_reset;
        private System.Windows.Forms.CheckBox checkBox_dual_validate;
        private System.Windows.Forms.CheckBox checkBox_dual_agree;
        private System.Windows.Forms.CheckBox checkBox_dual_visible;
        private System.Windows.Forms.GroupBox groupBox_dual;
        private System.Windows.Forms.Label label_dual_chess_model;
        private System.Windows.Forms.ComboBox comboBox_dual_chess_model;
        private System.Windows.Forms.Label label_dual_chess_conf;
        private System.Windows.Forms.NumericUpDown numericUpDown_dual_chess_conf;
        private System.Windows.Forms.Label label_dual_jieqi_model;
        private System.Windows.Forms.ComboBox comboBox_dual_jieqi_model;
        private System.Windows.Forms.Label label_dual_jieqi_dark_conf;
        private System.Windows.Forms.NumericUpDown numericUpDown_dual_jieqi_dark_conf;
        private System.Windows.Forms.GroupBox groupBox_draw;
        private System.Windows.Forms.CheckBox checkBox_draw_overlay;
        private System.Windows.Forms.CheckBox checkBox_draw_box;
        private System.Windows.Forms.CheckBox checkBox_draw_class;
        private System.Windows.Forms.CheckBox checkBox_draw_conf;
        private System.Windows.Forms.Label label_draw_thickness;
        private System.Windows.Forms.NumericUpDown numericUpDown_draw_thickness;
        private System.Windows.Forms.Label label_draw_chess_color;
        private System.Windows.Forms.Button button_draw_chess_color;
        private System.Windows.Forms.Label label_draw_jieqi_color;
        private System.Windows.Forms.Button button_draw_jieqi_color;
    }
}