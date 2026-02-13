namespace TcpClientProgram
{
    partial class TimerSetting
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TimerSetting));
            this.textBoxTimer = new System.Windows.Forms.TextBox();
            this.buttonTimer = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // textBoxTimer
            // 
            this.textBoxTimer.Location = new System.Drawing.Point(3, 9);
            this.textBoxTimer.Name = "textBoxTimer";
            this.textBoxTimer.Size = new System.Drawing.Size(100, 20);
            this.textBoxTimer.TabIndex = 0;
            // 
            // buttonTimer
            // 
            this.buttonTimer.Location = new System.Drawing.Point(109, 7);
            this.buttonTimer.Name = "buttonTimer";
            this.buttonTimer.Size = new System.Drawing.Size(34, 23);
            this.buttonTimer.TabIndex = 1;
            this.buttonTimer.Text = "Set";
            this.buttonTimer.UseVisualStyleBackColor = true;
            this.buttonTimer.Click += new System.EventHandler(this.buttonTimer_Click);
            // 
            // TimerSetting
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(150, 35);
            this.Controls.Add(this.buttonTimer);
            this.Controls.Add(this.textBoxTimer);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "TimerSetting";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "TimerSetting";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBoxTimer;
        private System.Windows.Forms.Button buttonTimer;
    }
}