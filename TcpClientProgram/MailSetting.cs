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
    public partial class MailSetting : Form
    {
        DesignForm form;

        public MailSetting(DesignForm form)
        {
            InitializeComponent();
            this.form = form;
            textBoxMailAddressList.Text = form.MailList;
        }

        private void buttonMailAddressList_Click(object sender, EventArgs e)
        {
            form.MailList = textBoxMailAddressList.Text;
            LineChanger($"mail={textBoxMailAddressList.Text}", "settings.ini", 4);
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
