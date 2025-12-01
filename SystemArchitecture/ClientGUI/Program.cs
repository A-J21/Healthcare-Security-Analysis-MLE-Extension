using System;
using System.Windows.Forms;

namespace MLE_GUI
{
    /// Program: Entry point for the MLE GUI application.
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            // Application.Run(new MainForm());
            Application.Run(new MainFormModern()); //Updated version of the GUI
        }
    }
}

