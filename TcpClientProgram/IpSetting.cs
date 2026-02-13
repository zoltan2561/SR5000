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
    public partial class IpSetting : Form
    {
        DesignForm form;

        public IpSetting(DesignForm form)
        {
            InitializeComponent();
            this.form = form;
            textBoxIp.Text = form.Ip;
            textBoxPort.Text = form.Port.ToString();
        }

        private void buttonSet_Click(object sender, EventArgs e)
        {
            form.Ip = textBoxPort.Text;
            form.Port = Int32.Parse(textBoxPort.Text);
            LineChanger($"ip={textBoxIp.Text}", "settings.ini", 5);
            LineChanger($"port={textBoxPort.Text}", "settings.ini", 6);
            this.Close();
        }

        private void LineChanger(string newText, string fileName, int line_to_edit)
        {
            string[] arrLine = File.ReadAllLines(fileName);
            arrLine[line_to_edit - 1] = newText;
            File.WriteAllLines(fileName, arrLine);
        }
    }
}
