using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TcpClientProgram
{
    public partial class DesignForm : Form
    {
        private TcpClientLogic tcpClientLogic;
        private TimerSetting timerSetting;
        private MailSetting mailSetting;
        private LiveImage liveImage;
        private IpSetting ipSetting;
        private CountSetting countSetting;
        private Login login;
        private ProgressBar progressBar;
        private BackgroundWorker backgroundWorker;
        private System.Windows.Forms.Timer countdownTimer;
        private System.Windows.Forms.Timer progressBarTimer;
        private int remainingMilliseconds;
        private int remainingMillisecondsProgressBar;
        private bool pointerStatus;
        private int timer;
        private string mailList;
        private string ip;
        private int port;
        private string scancount;
        private int autoupload;
        private int blockcommand;
        private string language;
        private string currentShelfCode;
        private Label shelfCodeLabel;
        private TextBox shelfCodeTextBox;
        public static ResourceManager rm;
        public bool disconnect;
        public string Ip { get => ip; set => ip = value; }
        public int Port { get => port; set => port = value; }
        public int Timer { get => timer; set => timer = value; }
        public string MailList { get => mailList; set => mailList = value; }
        public string ScanCount { get => scancount; set => scancount = value; }
        public int AutoUpload { get => autoupload; set => autoupload = value; }
        public int BlockCommand { get => blockcommand; set => blockcommand = value; }
        public bool Disconnect { get => disconnect; set => disconnect = value; }
        public string Langauge { get => language; set => language = value; }
        public string CurrentShelfCode { get => currentShelfCode; set => currentShelfCode = value; }
        


        public DesignForm()
        {
            //CreateLogFile();
            InitializeComponent();
            SetUp();

        }

        public DesignForm(SplashScreen splash)
        {
            //CreateLogFile();
            InitializeComponent();
            SetUp();
            splash.Close();

        }

        public void DisplayMessage(string message, Color color = default(Color))
        {
            if (outputBox.InvokeRequired)
            {
                Invoke(new Action(() => DisplayMessage(message, color)));
            }
            else
            {
                outputBox.AppendText(message + Environment.NewLine, color);
                TrimOutputBuffer(maxLines: 2000);
            }
        }


        private void DesignForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            tcpClientLogic.StopClient();

        }

        private void connectButton_Click_1(object sender, EventArgs e)
        {
            this.disconnect = false;
            tcpClientLogic.StartClientAsync();
            buttonShoot.Enabled = true;
            buttonSend.Enabled = true;
            if(this.autoupload != 1)
            {
                autouploadToolStripMenuItem.Text = $"{rm.GetString("setting_autoupload")} [{rm.GetString("off")}]";
                buttonUpload.Text = rm.GetString("upload");
                buttonUpload.Enabled = true;
                autouploadToolStripMenuItem.Checked = false;
            }
            else
            {
                autouploadToolStripMenuItem.Text = $"{rm.GetString("setting_autoupload")} [{rm.GetString("on")}]";
                buttonUpload.Text = "AUTO";
                autouploadToolStripMenuItem.Checked = true;
            }
            buttonConnect.Enabled = false;
            buttonDisconnect.Enabled = true;
            liveImageToolStripMenuItem.Enabled = true;

        }

        private void outputBox_TextChanged(object sender, EventArgs e)
        {
            outputBox.SelectionStart = outputBox.Text.Length;
            outputBox.ScrollToCaret();
        }

        private void TrimOutputBuffer(int maxLines)
        {
            if (outputBox.Lines.Length <= maxLines) return;

            int removeLines = outputBox.Lines.Length - maxLines;
            int removeIndex = 0;

            for (int i = 0; i < removeLines; i++)
            {
                int next = outputBox.Text.IndexOf(Environment.NewLine, removeIndex, StringComparison.Ordinal);
                if (next < 0) break;
                removeIndex = next + Environment.NewLine.Length;
            }

            if (removeIndex > 0 && removeIndex < outputBox.TextLength)
            {
                outputBox.Select(0, removeIndex);
                outputBox.SelectedText = string.Empty;
            }
        }

        private void uploadButton_Click(object sender, EventArgs e)
        {
            tcpClientLogic.ConnectToMysql();
        }

        private void sendButton_Click_1(object sender, EventArgs e)
        {
            tcpClientLogic.SendMessage(inputBox.Text);
            inputBox.Clear();
        }

        private void buttonShoot_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CurrentShelfCode) || CurrentShelfCode == "0")
            {
                MessageBox.Show("Kérlek előbb olvasd be a polc számát a szkennelés előtt!", "Hiányzó polc", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                shelfCodeTextBox.Focus();
                shelfCodeTextBox.SelectAll();
                return;
            }

            //SetTimer(Timer);
            buttonShoot.Enabled = false;
            buttonShoot.BackColor = Color.Gold;
            tcpClientLogic.SendMessage("LON");
            SetTimer(Timer);
            SetProgressTimer(Timer);
            progressBar.ShowDialog();
            //Thread.Sleep(Timer);


        }

        private void timerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GetTimerValue();
            timerSetting.ShowDialog();

        }

        private void SetTimer(int milliseconds)
        {
            // Initialize the Timer
            countdownTimer = new System.Windows.Forms.Timer();
            countdownTimer.Tick += async (sender, e) => await CountdownTimer_Tick(sender, e);

            // Set the remaining milliseconds
            remainingMilliseconds = milliseconds;

            countdownTimer.Interval = 1000;

            // Start the timer
            countdownTimer.Start();
        }

        private void SetProgressTimer(int milliseconds)
        {
            // Initialize the Timer
            progressBarTimer = new System.Windows.Forms.Timer();
            progressBarTimer.Tick += async (sender, e) => await progressBarTimer_Tick(sender, e);

            // Set the remaining milliseconds
            remainingMillisecondsProgressBar = milliseconds;

            progressBarTimer.Interval = timer / 100;

            // Start the timer
            progressBarTimer.Start();
        }

        private async Task progressBarTimer_Tick(object sender, EventArgs e)
        {
            // Update the remaining milliseconds
            remainingMillisecondsProgressBar -= progressBarTimer.Interval;

            // Check if the countdown is completed
            if (remainingMillisecondsProgressBar <= 0)
            {
                progressBar.SetProgressBarValue(100);
                progressBarTimer.Stop();
            }
            else
            {
                calculateProgress();
                //UpdateDisplayProgressBar();
            }

            await TaskEx.Delay(0);
        }

        private async Task CountdownTimer_Tick(object sender, EventArgs e)
        {
            // Update the remaining milliseconds
            remainingMilliseconds -= countdownTimer.Interval;

            // Check if the countdown is completed
            if (remainingMilliseconds <= 0)
            {
                progressBarTimer.Stop();
                progressBar.SetProgressBarValue(100);
                buttonShoot.Enabled = true;
                // Stop the timer
                countdownTimer.Stop();
                tcpClientLogic.SendMessage("LOFF");

                // VÁRUNK, hogy a LOFF után a beolvasás + parse + qty frissítés lefusson
                await TaskEx.Delay(1200); // állítható: 800-1500ms

                int targetQty = 0;
                int.TryParse((this.scancount ?? "").Trim(), out targetQty);

                int actualQty = tcpClientLogic.Qty; // ekkor már stabil

                labelTimer.Text = $"Last read {actualQty} barcodes at: {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}";

                if (actualQty < targetQty)
                {
                    DialogResult dr = MessageBox.Show(
                        $"Count of readed QR ({actualQty}) is less than SET count ({targetQty}). Do you still want to upload to DB?",
                        "Warning",
                        MessageBoxButtons.YesNo);

                    if (dr == DialogResult.Yes)
                    {
                        tcpClientLogic.ConnectToMysql();
                    }
                }
                else
                {
                    if (this.autoupload == 1)
                    {
                        tcpClientLogic.ConnectToMysql();
                    }
                }

                buttonShoot.BackColor = Color.FromArgb(59, 130, 246);
                progressBar.Close();
                progressBar.SetProgressBarValue(0);
                ResetShelfCodeAfterScan();
                // Perform any actions you want when the countdown is finished

                // You can add more logic here based on your requirements
            }
            else
            {
                // Update the display with the remaining time
                UpdateDisplay();
                // 100 - Timer
                // current - remainingMilliseconds
            }

            await TaskEx.Delay(0);
        }

        private void UpdateDisplay()
        {
            // Convert remaining milliseconds to seconds and milliseconds
            int seconds = remainingMilliseconds / 1000;
            int milliseconds = remainingMilliseconds % 1000;

            // Display the remaining time (you can customize this based on your UI)
            labelTimer.Text = $"Shooting in: {seconds:D2} seconds";
            //DisplayMessage(labelTimer.Text);

        }

        private void UpdateDisplayProgressBar()
        {
            // Convert remaining milliseconds to seconds and milliseconds
            int seconds = remainingMillisecondsProgressBar / 1000;
            int milliseconds = remainingMillisecondsProgressBar % 1000;

            // Display the remaining time (you can customize this based on your UI)
            //labelTimer.Text = $"Shooting in: {seconds:D2} seconds";
            //DisplayMessage(labelTimer.Text);

        }

        private void RestartTimer(int milliseconds)
        {
            // Stop the existing timer if it's running
            countdownTimer?.Stop();

            // Set the new timer with the specified milliseconds
            SetTimer(milliseconds);
        }

        private void RestartTimerProgressBar(int milliseconds)
        {
            // Stop the existing timer if it's running
            progressBarTimer?.Stop();

            // Set the new timer with the specified milliseconds
            SetProgressTimer(milliseconds);
        }

        private void buttonDisconnect_Click(object sender, EventArgs e)
        {
            Kill();
        }

        private void buttonClear_Click(object sender, EventArgs e)
        {
            outputBox.Text = "";
        }

        private void GetTimerValue()
        {
            String line;
            try
            {
                StreamReader sr = new StreamReader("settings.ini");
                line = sr.ReadLine();
                while (line != null)
                {
                    if (line.StartsWith("timer"))
                    {
                        this.timer = Int32.Parse(line.Split('=')[1]);
                        sr.Close();
                        return;
                    }
                    line = sr.ReadLine();
                }
                sr.Close();
            }
            catch (Exception ex)
            {
                //
            }
        }

        private void GetLanguage()
        {
            String line;
            this.language = "en";
            try
            {
                StreamReader sr = new StreamReader("settings.ini");
                line = sr.ReadLine();
                while (line != null)
                {
                    if (line.StartsWith("language"))
                    {
                        this.language = line.Split('=')[1];
                        if(this.language == "en")
                        {
                            แบบไทยToolStripMenuItem.Checked = false;
                            tagalogToolStripMenuItem.Checked = false;
                            englishToolStripMenuItem.Checked = true;
                            magyarToolStripMenuItem.Checked = false;
                        }
                        else if(this.language == "hu")
                        {
                            แบบไทยToolStripMenuItem.Checked = false;
                            tagalogToolStripMenuItem.Checked = false;
                            englishToolStripMenuItem.Checked = false;
                            magyarToolStripMenuItem.Checked = true;
                        }
                        else if(this.language == "th")
                        {
                            แบบไทยToolStripMenuItem.Checked = true;
                            tagalogToolStripMenuItem.Checked = false;
                            englishToolStripMenuItem.Checked = false;
                            magyarToolStripMenuItem.Checked = false;
                        }
                        else if(this.language == "tl")
                        {
                            แบบไทยToolStripMenuItem.Checked = false;
                            tagalogToolStripMenuItem.Checked = true;
                            englishToolStripMenuItem.Checked = false;
                            magyarToolStripMenuItem.Checked = false;
                        }
                        rm = new ResourceManager($"TcpClientProgram.{this.language}_local", Assembly.GetExecutingAssembly());
                        sr.Close();
                        return;
                    }
                    line = sr.ReadLine();
                }
                sr.Close();
            }
            catch (Exception ex)
            {
                //
            }
        }

        private void GetScanCount()
        {
            String line;
            this.scancount = "40";
            try
            {
                StreamReader sr = new StreamReader("settings.ini");
                line = sr.ReadLine();
                while (line != null)
                {
                    if (line.StartsWith("scancount"))
                    {
                        this.scancount = line.Split('=')[1];
                        sr.Close();
                        return;
                    }
                    line = sr.ReadLine();
                }
                sr.Close();
            }
            catch (Exception ex)
            {
                //
            }
        }

        private void GetIpValue()
        {
            String line;
            try
            {
                StreamReader sr = new StreamReader("settings.ini");
                line = sr.ReadLine();
                while (line != null)
                {
                    if (line.StartsWith("ip"))
                    {
                        this.ip = line.Split('=')[1];
                        sr.Close();
                        return;
                    }
                    line = sr.ReadLine();
                }
                sr.Close();
            }
            catch (Exception ex)
            {
                //
            }
        }

        private void GetPortValue()
        {
            String line;
            try
            {
                StreamReader sr = new StreamReader("settings.ini");
                line = sr.ReadLine();
                while (line != null)
                {
                    if (line.StartsWith("port"))
                    {
                        this.port = Int32.Parse(line.Split('=')[1]);
                        sr.Close();
                        return;
                    }
                    line = sr.ReadLine();
                }
                sr.Close();
            }
            catch (Exception ex)
            {
                //
            }
        }

        private void GetAutoUploadValue()
        {
            String line;
            this.autoupload = 1;
            try
            {
                StreamReader sr = new StreamReader("settings.ini");
                line = sr.ReadLine();
                while (line != null)
                {
                    if (line.StartsWith("autoupload"))
                    {
                        this.autoupload = Int32.Parse(line.Split('=')[1]);
                        sr.Close();
                        return;
                    }
                    line = sr.ReadLine();
                }
                sr.Close();
            }
            catch (Exception ex)
            {
                //
            }
        }

        private void GetBlockCommandValue()
        {
            String line;
            try
            {
                StreamReader sr = new StreamReader("settings.ini");
                line = sr.ReadLine();
                while (line != null)
                {
                    if (line.StartsWith("blockcommand"))
                    {
                        this.blockcommand = Int32.Parse(line.Split('=')[1]);
                        sr.Close();
                        return;
                    }
                    line = sr.ReadLine();
                }
                sr.Close();
            }
            catch (Exception ex)
            {
                //
            }
        }

        private void GetAddressList()
        {
            String line;
            try
            {
                StreamReader sr = new StreamReader("settings.ini");
                line = sr.ReadLine();
                while (line != null)
                {
                    if (line.StartsWith("mail"))
                    {
                        this.mailList = line.Split('=')[1];
                        sr.Close();
                        return;
                    }
                    line = sr.ReadLine();
                }
                sr.Close();
            }
            catch (Exception ex)
            {
                //
            }

        }

        private void addressListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GetAddressList();
            mailSetting.ShowDialog();
        }

        private void calibrateToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void pointerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (pointerStatus)
            {
                tcpClientLogic.SendMessage("AMOFF");
                pointerToolStripMenuItem.Text = "Pointer (OFF)";
                pointerToolStripMenuItem.Checked = false;
                pointerStatus = false;
            }
            else
            {
                tcpClientLogic.SendMessage("AMON");
                pointerToolStripMenuItem.Text = "Pointer (ON)";
                pointerToolStripMenuItem.Checked = true;
                pointerStatus = true;
            }
        }

        private void CreateLogFile()
        {
            string today = DateTime.Now.ToString("yyyyMMdd");
            string path = $".\\log\\{today}.log";
            try
            {
                using (var stream = File.Open(path, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
                {
                }
            }
            catch(IOException ex)
            {
                var myFile = File.Create(path);
                myFile.Close();
            }

            //using (StreamWriter w = File.AppendText(path)) { }
        }

        private void openLogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string path = ".\\log";
            Process.Start("explorer.exe", path);
        }

        private void liveImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                liveImage.Show();
            }catch(Exception ex)
            {
                liveImage = new LiveImage();
                liveImage.Show();
            }
            
        }

        private void connectionParametersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ipSetting.ShowDialog();
        }

        private void scanCountToolStripMenuItem_Click(object sender, EventArgs e)
        {
            countSetting.ShowDialog();
        }

        private void autouploadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(this.autoupload != 1)
            {
                autouploadToolStripMenuItem.Text = $"{rm.GetString("setting_autoupload")} [{rm.GetString("on")}]";
                buttonUpload.Text = "AUTO";
                autouploadToolStripMenuItem.Checked = true;
                buttonUpload.Enabled = false;
                this.autoupload = 1;
            }
            else
            {
                autouploadToolStripMenuItem.Text = $"{rm.GetString("setting_autoupload")} [{rm.GetString("off")}]";
                autouploadToolStripMenuItem.Checked = false;
                buttonUpload.Text = rm.GetString("upload");
                buttonUpload.Enabled = true;
                this.autoupload = 0;
            }
            LineChanger($"autoupload={this.autoupload}", "settings.ini", 8);
        }

        private void directCommandToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.blockcommand != 1)
            {
                directCommandToolStripMenuItem.Text = $"{rm.GetString("setting_command")} [{rm.GetString("off")}]";
                directCommandToolStripMenuItem.Checked = false;
                this.blockcommand = 1;
                inputBox.Enabled = false;
                buttonSend.Enabled = false;
            }
            else
            {
                directCommandToolStripMenuItem.Text = $"{rm.GetString("setting_command")} [{rm.GetString("on")}]";
                directCommandToolStripMenuItem.Checked = true;
                this.blockcommand = 0;
                inputBox.Enabled = true;
                buttonSend.Enabled = true;
            }
            LineChanger($"blockcommand={this.blockcommand}", "settings.ini", 10);
        }

        private void LineChanger(string newText, string fileName, int line_to_edit)
        {
            string[] arrLine = File.ReadAllLines(fileName);
            arrLine[line_to_edit - 1] = newText;
            File.WriteAllLines(fileName, arrLine);
        }

        private void loginAsAdminToolStripMenuItem_Click(object sender, EventArgs e)
        {
            login.ShowDialog();
        }

        private void SetUp()
        {
            EnsureSettingsFile();
            GetLanguage();
            GetTimerValue();
            GetAddressList();
            GetIpValue();
            GetPortValue();
            GetScanCount();
            GetAutoUploadValue();
            GetBlockCommandValue();
            pointerStatus = false;
            pointerToolStripMenuItem.Text = "Pointer (OFF)";
            progressBar = new ProgressBar();
            tcpClientLogic = new TcpClientLogic(this);
            tcpClientLogic.GetIp();
            tcpClientLogic.GetPort();
            timerSetting = new TimerSetting(this);
            mailSetting = new MailSetting(this);
            ipSetting = new IpSetting(this);
            liveImage = new LiveImage();
            countSetting = new CountSetting(this);
            backgroundWorker = new BackgroundWorker();
            backgroundWorker.WorkerReportsProgress = true;
            login = new Login();
            buttonShoot.Enabled = false;
            buttonSend.Enabled = false;
            buttonUpload.Enabled = false;
            buttonDisconnect.Enabled = false;
            liveImageToolStripMenuItem.Enabled = false;
            SetLanguageText();
            InitializeResponsiveLayout();
            InitializeShelfControls();
            ApplyModernUiTheme();
            ResetShelfCodeAfterScan();
            if (this.autoupload == 1)
            {
                autouploadToolStripMenuItem.Text = $"{rm.GetString("setting_autoupload")} [{rm.GetString("on")}]";
                buttonUpload.Text = "AUTO";
                autouploadToolStripMenuItem.Checked = true;
            }
            else
            {
                autouploadToolStripMenuItem.Text = $"{rm.GetString("setting_autoupload")} [{rm.GetString("off")}]";
                autouploadToolStripMenuItem.Checked = false;
                buttonUpload.Text = rm.GetString("upload");
            }
            if (this.blockcommand == 1)
            {
                directCommandToolStripMenuItem.Text = $"{rm.GetString("setting_command")} [{rm.GetString("off")}]";
                inputBox.Enabled = false;
                buttonSend.Enabled = false;
                directCommandToolStripMenuItem.Checked = false;
            }
            else
            {
                directCommandToolStripMenuItem.Text = $"{rm.GetString("setting_command")} [{rm.GetString("on")}]";
                inputBox.Enabled = true;
                buttonSend.Enabled = true;
                directCommandToolStripMenuItem.Checked = true;
            }
        }


        private void InitializeResponsiveLayout()
        {
            MinimumSize = new Size(760, 480);

            tableLayoutPanel2.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            tableLayoutPanel1.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            buttonShoot.Dock = DockStyle.Fill;
            buttonUpload.Dock = DockStyle.Fill;
            buttonClear.Dock = DockStyle.Fill;
            buttonConnect.Dock = DockStyle.Fill;
            buttonDisconnect.Dock = DockStyle.Fill;
            buttonSend.Dock = DockStyle.Fill;

            outputBox.Location = new Point(outputBox.Location.X, 90);
            outputBox.Size = new Size(outputBox.Size.Width, outputBox.Size.Height - 30);
            tableLayoutPanel2.Location = new Point(tableLayoutPanel2.Location.X, 57);

            Resize += DesignForm_Resize;
            DesignForm_Resize(this, EventArgs.Empty);
        }

        private void DesignForm_Resize(object sender, EventArgs e)
        {
            int top = outputBox.Top;
            int bottomLimit = tableLayoutPanel1.Top - 8;
            int availableHeight = Math.Max(120, bottomLimit - top);
            outputBox.Height = availableHeight;

            labelTimer.Top = tableLayoutPanel1.Bottom + 6;
            labelTimer.Left = outputBox.Left;
        }

        private void EnsureSettingsFile()
        {
            const string settingsPath = "settings.ini";
            string[] defaultLines = new[]
            {
                "client=SR5000",
                "reader=keyence",
                "timer=5000",
                "mail=",
                "ip=127.0.0.1",
                "port=9004",
                "scancount=40",
                "autoupload=1",
                "autoconnect=0",
                "blockcommand=0",
                "language=en"
            };

            if (!File.Exists(settingsPath))
            {
                File.WriteAllLines(settingsPath, defaultLines);
                return;
            }

            string[] current = File.ReadAllLines(settingsPath);
            bool rewrite = current.Length < defaultLines.Length;

            if (!rewrite)
            {
                string[] scanCountParts = current[6].Split('=');
                if (!current[6].StartsWith("scancount=", StringComparison.OrdinalIgnoreCase) ||
                    scanCountParts.Length < 2 ||
                    string.IsNullOrWhiteSpace(scanCountParts[1]))
                {
                    current[6] = "scancount=40";
                }

                current[7] = "autoupload=1";
                File.WriteAllLines(settingsPath, current);
                return;
            }

            var merged = new string[defaultLines.Length];
            for (int i = 0; i < defaultLines.Length; i++)
            {
                merged[i] = i < current.Length && !string.IsNullOrWhiteSpace(current[i])
                    ? current[i]
                    : defaultLines[i];
            }

            merged[6] = "scancount=40";
            merged[7] = "autoupload=1";
            File.WriteAllLines(settingsPath, merged);
        }

        private void InitializeShelfControls()
        {
            shelfCodeLabel = new Label
            {
                AutoSize = true,
                Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Bold),
                Text = "Polc szám:",
                Location = new Point(12, 33)
            };

            shelfCodeTextBox = new TextBox
            {
                Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Regular),
                Location = new Point(108, 30),
                Width = ClientSize.Width - 120,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            shelfCodeTextBox.TextChanged += shelfCodeTextBox_TextChanged;

            Controls.Add(shelfCodeLabel);
            Controls.Add(shelfCodeTextBox);
            shelfCodeTextBox.BringToFront();
            shelfCodeLabel.BringToFront();
        }

        private void ApplyModernUiTheme()
        {
            BackColor = Color.FromArgb(245, 247, 250);
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

            outputBox.BackColor = Color.White;
            outputBox.BorderStyle = BorderStyle.FixedSingle;

            StyleActionButton(buttonShoot, Color.FromArgb(59, 130, 246), Color.White);
            StyleActionButton(buttonUpload, Color.FromArgb(16, 185, 129), Color.White);
            StyleActionButton(buttonConnect, Color.FromArgb(14, 165, 233), Color.White);
            StyleActionButton(buttonDisconnect, Color.FromArgb(239, 68, 68), Color.White);
            StyleActionButton(buttonClear, Color.FromArgb(107, 114, 128), Color.White);
            StyleActionButton(buttonSend, Color.FromArgb(79, 70, 229), Color.White);

            shelfCodeLabel.ForeColor = Color.FromArgb(31, 41, 55);
            shelfCodeLabel.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point);

            shelfCodeTextBox.BorderStyle = BorderStyle.FixedSingle;
            shelfCodeTextBox.BackColor = Color.White;
            shelfCodeTextBox.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);

            labelTimer.ForeColor = Color.FromArgb(55, 65, 81);
            labelTimer.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
        }

        private void StyleActionButton(Button button, Color backColor, Color foreColor)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = backColor;
            button.ForeColor = foreColor;
            button.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point);
            button.Height = Math.Max(button.Height, 34);
        }

        private void ResetShelfCodeAfterScan()
        {
            CurrentShelfCode = "0";
            shelfCodeTextBox.Text = "0";
            shelfCodeTextBox.SelectionStart = shelfCodeTextBox.Text.Length;
        }

        private void Kill()
        {
            tcpClientLogic.StopClient();
            buttonShoot.Enabled = false;
            buttonSend.Enabled = false;
            buttonUpload.Enabled = false;
            buttonUpload.Text = rm.GetString("upload");
            buttonDisconnect.Enabled = false;
            buttonConnect.Enabled = true;
            this.disconnect = true;
            liveImageToolStripMenuItem.Enabled = false;
            timerSetting.Dispose();
            mailSetting.Dispose();
            ipSetting.Dispose();
            liveImage.Dispose();
            countSetting.Dispose();
            login.Dispose();
            tcpClientLogic.Dispose();
        }
        
        private void SetLanguageText()
        {
            buttonShoot.Text = rm.GetString("shoot");
            buttonClear.Text = rm.GetString("clear");
            buttonConnect.Text = rm.GetString("connect");
            buttonDisconnect.Text = rm.GetString("disconnect");
            settingsToolStripMenuItem.Text = rm.GetString("settings");
            loginAsAdminToolStripMenuItem.Text = rm.GetString("login");
            liveImageToolStripMenuItem.Text = rm.GetString("live");
            buttonSend.Text = rm.GetString("send");
            languageToolStripMenuItem.Text = rm.GetString("setting_language");
            calibrateToolStripMenuItem.Text = rm.GetString("setting_calibrate");
            timerToolStripMenuItem.Text = rm.GetString("setting_timer");
            openLogToolStripMenuItem.Text = rm.GetString("setting_log");
            addressListToolStripMenuItem.Text = rm.GetString("setting_list");
            connectionParametersToolStripMenuItem.Text = rm.GetString("setting_ip");
            autoconnectToolStripMenuItem.Text = rm.GetString("setting_autoconnect");
            scanCountToolStripMenuItem.Text = rm.GetString("setting_count");

        }
        
        private void magyarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            แบบไทยToolStripMenuItem.Checked = false;
            tagalogToolStripMenuItem.Checked = false;
            englishToolStripMenuItem.Checked = false;
            magyarToolStripMenuItem.Checked = true;
            LineChanger($"language=hu", "settings.ini", 11);
            GetLanguage();
            MessageBox.Show(rm.GetString("restart"));
            Application.Restart();
            Environment.Exit(0);
        }

        private void englishToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            แบบไทยToolStripMenuItem.Checked = false;
            tagalogToolStripMenuItem.Checked = false;
            englishToolStripMenuItem.Checked = true;
            magyarToolStripMenuItem.Checked = false;
            LineChanger($"language=en", "settings.ini", 11);
            GetLanguage();
            MessageBox.Show(rm.GetString("restart"));
            Application.Restart();
            Environment.Exit(0);
        }

        private void แบบไทยToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            แบบไทยToolStripMenuItem.Checked = true;
            tagalogToolStripMenuItem.Checked = false;
            englishToolStripMenuItem.Checked = false;
            magyarToolStripMenuItem.Checked = false;
            LineChanger($"language=th", "settings.ini", 11);
            GetLanguage();
            MessageBox.Show(rm.GetString("restart"));
            Application.Restart();
            Environment.Exit(0);
        }

        private void tagalogToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            แบบไทยToolStripMenuItem.Checked = false;
            tagalogToolStripMenuItem.Checked = true;
            englishToolStripMenuItem.Checked = false;
            magyarToolStripMenuItem.Checked = false;
            LineChanger($"language=tl", "settings.ini", 11);
            GetLanguage();
            MessageBox.Show(rm.GetString("restart"));
            Application.Restart();
            Environment.Exit(0);
        }

        private void calculateProgress()
        {
            //timer - 100%
            int step = 1;
            if (progressBar.GetProgressBarValue() + step > 100)
            {
                progressBar.SetProgressBarValue(100);
            }
            else
            {
                progressBar.SetProgressBarValue(progressBar.GetProgressBarValue() + step);
            }
        }

        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void shelfCodeTextBox_TextChanged(object sender, EventArgs e)
        {
            CurrentShelfCode = shelfCodeTextBox.Text.Trim();
        }
    }
}
