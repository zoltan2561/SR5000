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
    public partial class CountSetting : Form
    {
        DesignForm form;

        public CountSetting(DesignForm form)
        {
            InitializeComponent();
            this.form = form;
            textBoxCount.Text = form.ScanCount;
        }

        private void buttonSet_Click(object sender, EventArgs e)
        {
            int test = 0;
            if(!int.TryParse(textBoxCount.Text,out test))
            {
                textBoxCount.Text = form.ScanCount;
                MessageBox.Show("Number value only!");
            }
            if(Int32.Parse(textBoxCount.Text) > 256)
            {
                textBoxCount.Text = form.ScanCount;
                MessageBox.Show("Please enter a number between 1 and 256");
            }
            if (Int32.Parse(textBoxCount.Text) < 1)
            {
                textBoxCount.Text = form.ScanCount;
                MessageBox.Show("Please enter a number between 1 and 256");
            }
            form.ScanCount = textBoxCount.Text;
            LineChanger($"scancount={textBoxCount.Text}", "settings.ini", 7);
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
