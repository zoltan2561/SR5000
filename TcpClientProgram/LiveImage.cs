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
        private bool redirectedToImage;

        public LiveImage()
        {
            GetIp();
            InitializeComponent();
            var url = string.IsNullOrWhiteSpace(ip) ? "http://10.8.253.207/" : $"http://{ip}/";
            this.webBrowser.Url = new System.Uri(url);
        }

        private void webBrowser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            if (redirectedToImage || webBrowser.Document == null)
            {
                return;
            }

            try
            {
                foreach (HtmlElement img in webBrowser.Document.GetElementsByTagName("img"))
                {
                    var src = img.GetAttribute("src");
                    if (string.IsNullOrWhiteSpace(src))
                    {
                        continue;
                    }

                    Uri imageUri;
                    if (!Uri.TryCreate(src, UriKind.Absolute, out imageUri))
                    {
                        Uri.TryCreate(webBrowser.Url, src, out imageUri);
                    }

                    if (imageUri != null)
                    {
                        redirectedToImage = true;
                        webBrowser.Navigate(imageUri);
                        return;
                    }
                }
            }
            catch
            {
                // ignore and keep root page
            }
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
