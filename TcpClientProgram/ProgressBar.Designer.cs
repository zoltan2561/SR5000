namespace TcpClientProgram
{
    partial class ProgressBar
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
            this.progressBar_ = new System.Windows.Forms.ProgressBar();
            this.SuspendLayout();
            // 
            // progressBar_
            // 
            this.progressBar_.Location = new System.Drawing.Point(0, 0);
            this.progressBar_.Name = "progressBar_";
            this.progressBar_.Size = new System.Drawing.Size(452, 31);
            this.progressBar_.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.progressBar_.TabIndex = 0;
            this.progressBar_.Click += new System.EventHandler(this.progressBar__Click);
            // 
            // ProgressBar
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(452, 31);
            this.ControlBox = false;
            this.Controls.Add(this.progressBar_);
            this.Name = "ProgressBar";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ProgressBar progressBar_;
    }
}