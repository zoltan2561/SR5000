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
    public partial class TimerSetting : Form
    {
        DesignForm form;

        public TimerSetting(DesignForm form)
        {
            InitializeComponent();
            this.form = form;
            textBoxTimer.Text = form.Timer.ToString();
        }

        private void buttonTimer_Click(object sender, EventArgs e)
        {
            form.Timer = Int32.Parse(textBoxTimer.Text);
            LineChanger($"timer={textBoxTimer.Text}", "settings.ini", 3);
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
