namespace TcpClientProgram
{
    partial class MailSetting
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
            this.textBoxMailAddressList = new System.Windows.Forms.TextBox();
            this.buttonMailAddressList = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // textBoxMailAddressList
            // 
            this.textBoxMailAddressList.Location = new System.Drawing.Point(13, 13);
            this.textBoxMailAddressList.Name = "textBoxMailAddressList";
            this.textBoxMailAddressList.Size = new System.Drawing.Size(685, 20);
            this.textBoxMailAddressList.TabIndex = 0;
            // 
            // buttonMailAddressList
            // 
            this.buttonMailAddressList.Location = new System.Drawing.Point(704, 11);
            this.buttonMailAddressList.Name = "buttonMailAddressList";
            this.buttonMailAddressList.Size = new System.Drawing.Size(75, 23);
            this.buttonMailAddressList.TabIndex = 1;
            this.buttonMailAddressList.Text = "Set";
            this.buttonMailAddressList.UseVisualStyleBackColor = true;
            this.buttonMailAddressList.Click += new System.EventHandler(this.buttonMailAddressList_Click);
            // 
            // MailSetting
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 48);
            this.Controls.Add(this.buttonMailAddressList);
            this.Controls.Add(this.textBoxMailAddressList);
            this.Name = "MailSetting";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "MailSetting";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBoxMailAddressList;
        private System.Windows.Forms.Button buttonMailAddressList;
    }
}