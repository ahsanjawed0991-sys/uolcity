using System;
using System.Windows.Forms;

namespace UOLCity
{
    /// <summary>
    /// Application entry point for UOL CITY — Designed to Evolve.
    /// Initializes Windows Forms visual styles and launches the MainForm town screen.
    /// </summary>
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.Run(new MainForm());
        }
    }
}
