using System;
using System.Windows.Forms;

namespace RiotSwitcherMinimal
{
    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            // Ensure working directory is correct for relative paths (startup fix)
            System.IO.Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                MessageBox.Show($"Unhandled Exception: {((Exception)e.ExceptionObject).Message}", "Crash", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };

            try
            {
                Application.Run(new RiotSwitcherAppContext());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Application failed to start: {ex.Message}\n{ex.StackTrace}", "Startup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
