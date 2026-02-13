using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace TcpClientProgram
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            SplashScreen splashScreen = new SplashScreen();
            splashScreen.Show();
            DesignForm designForm = new DesignForm(splashScreen);
            Application.Run(designForm);
        }
    }
}
