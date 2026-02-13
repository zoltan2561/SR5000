using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TcpClientProgram
{
    public partial class LiveImage : Form
    {
        private string ip;

        public LiveImage()
        {
            GetIp();
            InitializeComponent();
            string targetIp = string.IsNullOrWhiteSpace(this.ip) ? "10.8.253.207" : this.ip;
            this.webBrowser.Url = new System.Uri(string.Format("http://{0}", targetIp));
        }

        private void webBrowser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {

        }

        private void GetIp()
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

        private void LiveImage_Load(object sender, EventArgs e)
        {

        }

    }
}
