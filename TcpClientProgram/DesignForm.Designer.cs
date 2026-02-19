namespace TcpClientProgram
{
    partial class DesignForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DesignForm));
            this.inputBox = new System.Windows.Forms.TextBox();
            this.buttonSend = new System.Windows.Forms.Button();
            this.outputBox = new System.Windows.Forms.RichTextBox();
            this.buttonConnect = new System.Windows.Forms.Button();
            this.buttonUpload = new System.Windows.Forms.Button();
            this.buttonClear = new System.Windows.Forms.Button();
            this.buttonShoot = new System.Windows.Forms.Button();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.timerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.calibrateToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pointerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openLogToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.connectionParametersToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.scanCountToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.autouploadToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.directCommandToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.languageToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.englishToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.magyarToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.แบบไทยToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tagalogToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addressListToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.autoconnectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loginAsAdminToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.liveImageToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.labelTimer = new System.Windows.Forms.Label();
            this.buttonDisconnect = new System.Windows.Forms.Button();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.menuStrip1.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // inputBox
            // 
            this.inputBox.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.inputBox.Location = new System.Drawing.Point(2, 3);
            this.inputBox.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.inputBox.Name = "inputBox";
            this.inputBox.Size = new System.Drawing.Size(645, 20);
            this.inputBox.TabIndex = 0;
            // 
            // buttonSend
            // 
            this.buttonSend.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.buttonSend.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.buttonSend.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.buttonSend.Location = new System.Drawing.Point(651, 3);
            this.buttonSend.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.buttonSend.Name = "buttonSend";
            this.buttonSend.Size = new System.Drawing.Size(159, 21);
            this.buttonSend.TabIndex = 1;
            this.buttonSend.Text = "Send";
            this.buttonSend.UseVisualStyleBackColor = true;
            this.buttonSend.Click += new System.EventHandler(this.sendButton_Click_1);
            // 
            // outputBox
            // 
            this.outputBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.outputBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.outputBox.Location = new System.Drawing.Point(12, 60);
            this.outputBox.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.outputBox.Name = "outputBox";
            this.outputBox.ReadOnly = true;
            this.outputBox.Size = new System.Drawing.Size(812, 459);
            this.outputBox.TabIndex = 2;
            this.outputBox.Text = "";
            this.outputBox.TextChanged += new System.EventHandler(this.outputBox_TextChanged);
            // 
            // buttonConnect
            // 
            this.buttonConnect.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.buttonConnect.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.buttonConnect.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.buttonConnect.ImageAlign = System.Drawing.ContentAlignment.TopCenter;
            this.buttonConnect.Location = new System.Drawing.Point(490, 3);
            this.buttonConnect.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.buttonConnect.Name = "buttonConnect";
            this.buttonConnect.Size = new System.Drawing.Size(153, 31);
            this.buttonConnect.TabIndex = 3;
            this.buttonConnect.Text = "connect";
            this.buttonConnect.UseVisualStyleBackColor = true;
            this.buttonConnect.Click += new System.EventHandler(this.connectButton_Click_1);
            // 
            // buttonUpload
            // 
            this.buttonUpload.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.buttonUpload.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.buttonUpload.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.buttonUpload.Location = new System.Drawing.Point(164, 3);
            this.buttonUpload.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.buttonUpload.Name = "buttonUpload";
            this.buttonUpload.Size = new System.Drawing.Size(158, 31);
            this.buttonUpload.TabIndex = 4;
            this.buttonUpload.Text = "Upload";
            this.buttonUpload.UseVisualStyleBackColor = true;
            this.buttonUpload.Click += new System.EventHandler(this.uploadButton_Click);
            // 
            // buttonClear
            // 
            this.buttonClear.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.buttonClear.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.buttonClear.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.buttonClear.Location = new System.Drawing.Point(327, 3);
            this.buttonClear.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.buttonClear.Name = "buttonClear";
            this.buttonClear.Size = new System.Drawing.Size(155, 31);
            this.buttonClear.TabIndex = 5;
            this.buttonClear.Text = "Clear";
            this.buttonClear.UseVisualStyleBackColor = true;
            this.buttonClear.Click += new System.EventHandler(this.buttonClear_Click);
            // 
            // buttonShoot
            // 
            this.buttonShoot.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.buttonShoot.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.buttonShoot.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.buttonShoot.Location = new System.Drawing.Point(2, 3);
            this.buttonShoot.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.buttonShoot.Name = "buttonShoot";
            this.buttonShoot.Size = new System.Drawing.Size(158, 31);
            this.buttonShoot.TabIndex = 6;
            this.buttonShoot.Text = "Shoot";
            this.buttonShoot.UseVisualStyleBackColor = true;
            this.buttonShoot.Click += new System.EventHandler(this.buttonShoot_Click);
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.settingsToolStripMenuItem,
            this.loginAsAdminToolStripMenuItem,
            this.liveImageToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(836, 24);
            this.menuStrip1.TabIndex = 7;
            this.menuStrip1.Text = "menuStrip1";
            this.menuStrip1.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.menuStrip1_ItemClicked);
            // 
            // settingsToolStripMenuItem
            // 
            this.settingsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.timerToolStripMenuItem,
            this.calibrateToolStripMenuItem,
            this.openLogToolStripMenuItem,
            this.connectionParametersToolStripMenuItem,
            this.scanCountToolStripMenuItem,
            this.autouploadToolStripMenuItem,
            this.directCommandToolStripMenuItem,
            this.languageToolStripMenuItem,
            this.addressListToolStripMenuItem,
            this.autoconnectToolStripMenuItem});
            this.settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            this.settingsToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
            this.settingsToolStripMenuItem.Text = "Settings";
            // 
            // timerToolStripMenuItem
            // 
            this.timerToolStripMenuItem.Image = global::TcpClientProgram.Properties.Resources.stopwatch1;
            this.timerToolStripMenuItem.Name = "timerToolStripMenuItem";
            this.timerToolStripMenuItem.Size = new System.Drawing.Size(198, 22);
            this.timerToolStripMenuItem.Text = "Timer";
            this.timerToolStripMenuItem.ToolTipText = "Set trigget timer (milliseconds)";
            this.timerToolStripMenuItem.Click += new System.EventHandler(this.timerToolStripMenuItem_Click);
            // 
            // calibrateToolStripMenuItem
            // 
            this.calibrateToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.pointerToolStripMenuItem});
            this.calibrateToolStripMenuItem.Image = global::TcpClientProgram.Properties.Resources.target1;
            this.calibrateToolStripMenuItem.Name = "calibrateToolStripMenuItem";
            this.calibrateToolStripMenuItem.Size = new System.Drawing.Size(198, 22);
            this.calibrateToolStripMenuItem.Text = "Calibrate";
            this.calibrateToolStripMenuItem.Click += new System.EventHandler(this.calibrateToolStripMenuItem_Click);
            // 
            // pointerToolStripMenuItem
            // 
            this.pointerToolStripMenuItem.Image = global::TcpClientProgram.Properties.Resources.target1;
            this.pointerToolStripMenuItem.Name = "pointerToolStripMenuItem";
            this.pointerToolStripMenuItem.Size = new System.Drawing.Size(112, 22);
            this.pointerToolStripMenuItem.Text = "Pointer";
            this.pointerToolStripMenuItem.Click += new System.EventHandler(this.pointerToolStripMenuItem_Click);
            // 
            // openLogToolStripMenuItem
            // 
            this.openLogToolStripMenuItem.Image = global::TcpClientProgram.Properties.Resources.folder1;
            this.openLogToolStripMenuItem.Name = "openLogToolStripMenuItem";
            this.openLogToolStripMenuItem.Size = new System.Drawing.Size(198, 22);
            this.openLogToolStripMenuItem.Text = "Open log directory";
            this.openLogToolStripMenuItem.Click += new System.EventHandler(this.openLogToolStripMenuItem_Click);
            // 
            // connectionParametersToolStripMenuItem
            // 
            this.connectionParametersToolStripMenuItem.Image = global::TcpClientProgram.Properties.Resources.more1;
            this.connectionParametersToolStripMenuItem.Name = "connectionParametersToolStripMenuItem";
            this.connectionParametersToolStripMenuItem.Size = new System.Drawing.Size(198, 22);
            this.connectionParametersToolStripMenuItem.Text = "Connection parameters";
            this.connectionParametersToolStripMenuItem.Click += new System.EventHandler(this.connectionParametersToolStripMenuItem_Click);
            // 
            // scanCountToolStripMenuItem
            // 
            this.scanCountToolStripMenuItem.Image = global::TcpClientProgram.Properties.Resources.view1;
            this.scanCountToolStripMenuItem.Name = "scanCountToolStripMenuItem";
            this.scanCountToolStripMenuItem.Size = new System.Drawing.Size(198, 22);
            this.scanCountToolStripMenuItem.Text = "Scan count";
            this.scanCountToolStripMenuItem.Click += new System.EventHandler(this.scanCountToolStripMenuItem_Click);
            // 
            // autouploadToolStripMenuItem
            // 
            this.autouploadToolStripMenuItem.Image = global::TcpClientProgram.Properties.Resources.cloud_computing1;
            this.autouploadToolStripMenuItem.Name = "autouploadToolStripMenuItem";
            this.autouploadToolStripMenuItem.Size = new System.Drawing.Size(198, 22);
            this.autouploadToolStripMenuItem.Text = "Autoupload";
            this.autouploadToolStripMenuItem.Click += new System.EventHandler(this.autouploadToolStripMenuItem_Click);
            // 
            // directCommandToolStripMenuItem
            // 
            this.directCommandToolStripMenuItem.Image = global::TcpClientProgram.Properties.Resources.server1;
            this.directCommandToolStripMenuItem.Name = "directCommandToolStripMenuItem";
            this.directCommandToolStripMenuItem.Size = new System.Drawing.Size(198, 22);
            this.directCommandToolStripMenuItem.Text = "Direct command";
            this.directCommandToolStripMenuItem.Click += new System.EventHandler(this.directCommandToolStripMenuItem_Click);
            // 
            // languageToolStripMenuItem
            // 
            this.languageToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.englishToolStripMenuItem,
            this.magyarToolStripMenuItem,
            this.แบบไทยToolStripMenuItem,
            this.tagalogToolStripMenuItem});
            this.languageToolStripMenuItem.Image = global::TcpClientProgram.Properties.Resources.internet1;
            this.languageToolStripMenuItem.Name = "languageToolStripMenuItem";
            this.languageToolStripMenuItem.Size = new System.Drawing.Size(198, 22);
            this.languageToolStripMenuItem.Text = "Language";
            // 
            // englishToolStripMenuItem
            // 
            this.englishToolStripMenuItem.Checked = true;
            this.englishToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.englishToolStripMenuItem.Image = global::TcpClientProgram.Properties.Resources.MicrosoftTeams_image;
            this.englishToolStripMenuItem.Name = "englishToolStripMenuItem";
            this.englishToolStripMenuItem.Size = new System.Drawing.Size(116, 22);
            this.englishToolStripMenuItem.Text = "English";
            this.englishToolStripMenuItem.Click += new System.EventHandler(this.englishToolStripMenuItem_Click_1);
            // 
            // magyarToolStripMenuItem
            // 
            this.magyarToolStripMenuItem.Image = global::TcpClientProgram.Properties.Resources.MicrosoftTeams_image__3_;
            this.magyarToolStripMenuItem.Name = "magyarToolStripMenuItem";
            this.magyarToolStripMenuItem.Size = new System.Drawing.Size(116, 22);
            this.magyarToolStripMenuItem.Text = "Magyar";
            this.magyarToolStripMenuItem.Click += new System.EventHandler(this.magyarToolStripMenuItem_Click);
            // 
            // แบบไทยToolStripMenuItem
            // 
            this.แบบไทยToolStripMenuItem.Image = global::TcpClientProgram.Properties.Resources.MicrosoftTeams_image__1_;
            this.แบบไทยToolStripMenuItem.Name = "แบบไทยToolStripMenuItem";
            this.แบบไทยToolStripMenuItem.Size = new System.Drawing.Size(116, 22);
            this.แบบไทยToolStripMenuItem.Text = "แบบไทย";
            this.แบบไทยToolStripMenuItem.Click += new System.EventHandler(this.แบบไทยToolStripMenuItem_Click_1);
            // 
            // tagalogToolStripMenuItem
            // 
            this.tagalogToolStripMenuItem.Image = global::TcpClientProgram.Properties.Resources.MicrosoftTeams_image__2_;
            this.tagalogToolStripMenuItem.Name = "tagalogToolStripMenuItem";
            this.tagalogToolStripMenuItem.Size = new System.Drawing.Size(116, 22);
            this.tagalogToolStripMenuItem.Text = "Tagalog";
            this.tagalogToolStripMenuItem.Click += new System.EventHandler(this.tagalogToolStripMenuItem_Click_1);
            // 
            // addressListToolStripMenuItem
            // 
            this.addressListToolStripMenuItem.Enabled = false;
            this.addressListToolStripMenuItem.Name = "addressListToolStripMenuItem";
            this.addressListToolStripMenuItem.Size = new System.Drawing.Size(198, 22);
            this.addressListToolStripMenuItem.Text = "Address list";
            this.addressListToolStripMenuItem.Click += new System.EventHandler(this.addressListToolStripMenuItem_Click);
            // 
            // autoconnectToolStripMenuItem
            // 
            this.autoconnectToolStripMenuItem.Enabled = false;
            this.autoconnectToolStripMenuItem.Name = "autoconnectToolStripMenuItem";
            this.autoconnectToolStripMenuItem.Size = new System.Drawing.Size(198, 22);
            this.autoconnectToolStripMenuItem.Text = "Autoconnect";
            // 
            // loginAsAdminToolStripMenuItem
            // 
            this.loginAsAdminToolStripMenuItem.Enabled = false;
            this.loginAsAdminToolStripMenuItem.Name = "loginAsAdminToolStripMenuItem";
            this.loginAsAdminToolStripMenuItem.Overflow = System.Windows.Forms.ToolStripItemOverflow.AsNeeded;
            this.loginAsAdminToolStripMenuItem.Size = new System.Drawing.Size(100, 20);
            this.loginAsAdminToolStripMenuItem.Text = "Login as admin";
            this.loginAsAdminToolStripMenuItem.Click += new System.EventHandler(this.loginAsAdminToolStripMenuItem_Click);
            // 
            // liveImageToolStripMenuItem
            // 
            this.liveImageToolStripMenuItem.Name = "liveImageToolStripMenuItem";
            this.liveImageToolStripMenuItem.Size = new System.Drawing.Size(76, 20);
            this.liveImageToolStripMenuItem.Text = "Live image";
            this.liveImageToolStripMenuItem.Click += new System.EventHandler(this.liveImageToolStripMenuItem_Click);
            // 
            // labelTimer
            // 
            this.labelTimer.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.labelTimer.AutoSize = true;
            this.labelTimer.Location = new System.Drawing.Point(12, 531);
            this.labelTimer.Name = "labelTimer";
            this.labelTimer.Size = new System.Drawing.Size(0, 13);
            this.labelTimer.TabIndex = 8;
            // 
            // buttonDisconnect
            // 
            this.buttonDisconnect.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.buttonDisconnect.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.buttonDisconnect.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.buttonDisconnect.ImageAlign = System.Drawing.ContentAlignment.TopCenter;
            this.buttonDisconnect.Location = new System.Drawing.Point(650, 3);
            this.buttonDisconnect.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.buttonDisconnect.Name = "buttonDisconnect";
            this.buttonDisconnect.Size = new System.Drawing.Size(160, 31);
            this.buttonDisconnect.TabIndex = 9;
            this.buttonDisconnect.Text = "disconnect";
            this.buttonDisconnect.UseVisualStyleBackColor = true;
            this.buttonDisconnect.Click += new System.EventHandler(this.buttonDisconnect_Click);
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 5;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.Controls.Add(this.buttonShoot, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.buttonDisconnect, 4, 0);
            this.tableLayoutPanel1.Controls.Add(this.buttonClear, 2, 0);
            this.tableLayoutPanel1.Controls.Add(this.buttonUpload, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.buttonConnect, 3, 0);
            this.tableLayoutPanel1.Location = new System.Drawing.Point(12, 491);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(812, 37);
            this.tableLayoutPanel1.TabIndex = 10;
            this.tableLayoutPanel1.Paint += new System.Windows.Forms.PaintEventHandler(this.tableLayoutPanel1_Paint);
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 2;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 80F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel2.Controls.Add(this.inputBox, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.buttonSend, 1, 0);
            this.tableLayoutPanel2.Location = new System.Drawing.Point(12, 27);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 1;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(812, 27);
            this.tableLayoutPanel2.TabIndex = 11;
            // 
            // DesignForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoValidate = System.Windows.Forms.AutoValidate.EnablePreventFocusChange;
            this.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.ClientSize = new System.Drawing.Size(836, 553);
            this.Controls.Add(this.tableLayoutPanel2);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.labelTimer);
            this.Controls.Add(this.outputBox);
            this.Controls.Add(this.menuStrip1);
            this.Cursor = System.Windows.Forms.Cursors.Default;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.Name = "DesignForm";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "SR5000 Interface PZ";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox inputBox;
        private System.Windows.Forms.Button buttonSend;
        private System.Windows.Forms.RichTextBox outputBox;
        private System.Windows.Forms.Button buttonConnect;
        private System.Windows.Forms.Button buttonUpload;
        private System.Windows.Forms.Button buttonClear;
        private System.Windows.Forms.Button buttonShoot;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem settingsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem timerToolStripMenuItem;
        private System.Windows.Forms.Label labelTimer;
        private System.Windows.Forms.Button buttonDisconnect;
        private System.Windows.Forms.ToolStripMenuItem calibrateToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem addressListToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem pointerToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openLogToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem liveImageToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem connectionParametersToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem scanCountToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem autouploadToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem directCommandToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loginAsAdminToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem languageToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem englishToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem magyarToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem autoconnectToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem แบบไทยToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem tagalogToolStripMenuItem;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
    }
}

