namespace VinXiangQi
{
    partial class EngineSettingsForm
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
                if (toolTip_option != null)
                {
                    toolTip_option.Dispose();
                }
                if (boldFont != null)
                {
                    boldFont.Dispose();
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EngineSettingsForm));
            this.panel_scroll = new EngineScrollPanel();
            this.SuspendLayout();
            // 
            // panel_scroll
            // 
            this.panel_scroll.AutoScroll = true;
            this.panel_scroll.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel_scroll.Location = new System.Drawing.Point(0, 0);
            this.panel_scroll.Name = "panel_scroll";
            this.panel_scroll.Padding = new System.Windows.Forms.Padding(6);
            this.panel_scroll.Size = new System.Drawing.Size(800, 450);
            this.panel_scroll.TabIndex = 0;
            // 
            // EngineSettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1024, 480);
            this.MinimumSize = new System.Drawing.Size(600, 340);
            this.Controls.Add(this.panel_scroll);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "EngineSettingsForm";
            this.Text = "引擎设置";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.EngineSettingsForm_FormClosing);
            this.Load += new System.EventHandler(this.EngineSettingsForm_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private EngineScrollPanel panel_scroll;
    }
}