namespace VinXiangQi
{
    partial class AboutForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AboutForm));
            this.pictureBox_about = new System.Windows.Forms.PictureBox();
            this.label_credit = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_about)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBox_about
            // 
            this.pictureBox_about.BackColor = System.Drawing.Color.White;
            this.pictureBox_about.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pictureBox_about.Image = global::VinXiangQi.Properties.Resources.about_vinxiangqi;
            this.pictureBox_about.Location = new System.Drawing.Point(0, 0);
            this.pictureBox_about.Name = "pictureBox_about";
            this.pictureBox_about.Size = new System.Drawing.Size(800, 450);
            this.pictureBox_about.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox_about.TabIndex = 0;
            this.pictureBox_about.TabStop = false;
            // 
            // label_credit
            // 
            this.label_credit.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.label_credit.BackColor = System.Drawing.Color.White;
            this.label_credit.Font = new System.Drawing.Font("微软雅黑", 10F);
            this.label_credit.Location = new System.Drawing.Point(0, 418);
            this.label_credit.Name = "label_credit";
            this.label_credit.Size = new System.Drawing.Size(800, 32);
            this.label_credit.TabIndex = 1;
            this.label_credit.Text = "揭棋模式和优化由GodGun968制作";
            this.label_credit.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // AboutForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.pictureBox_about);
            this.Controls.Add(this.label_credit);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "AboutForm";
            this.Text = "关于 Vin象棋";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_about)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox_about;
        private System.Windows.Forms.Label label_credit;
    }
}