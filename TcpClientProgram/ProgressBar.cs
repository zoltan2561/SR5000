using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TcpClientProgram
{
    public partial class ProgressBar : Form
    {
        public ProgressBar()
        {
            InitializeComponent();
        }

        public void SetProgressBarValue(int value)
        {
            progressBar_.Value = value;
        }

        public int GetProgressBarValue()
        {
            return progressBar_.Value;
        }

        private void progressBar__Click(object sender, EventArgs e)
        {

        }
    }
}
