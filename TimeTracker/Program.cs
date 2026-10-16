using System;
using System.Windows.Forms;

namespace TimeTracker
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize(); // Configures high DPI, default font, etc.
            Application.Run(new TimeTracker());
        }
    }
}
