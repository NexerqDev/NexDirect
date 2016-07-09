using Microsoft.Shell;
using System;
using System.Collections.Generic;
using System.Windows;

namespace NexDirect
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application, ISingleInstanceApp
    {
        // http://blogs.microsoft.co.il/arik/2010/05/28/wpf-single-instance-application/
        private const string AppUnique = "NexDirect_SIApp";

        [STAThread]
        public static void Main(string[] args)
        {
            // If we need to upgrade the config file, need to do it now.
            if (NexDirect.Properties.Settings.Default.configUpgradeRequired)
            {
                NexDirect.Properties.Settings.Default.Upgrade();
                NexDirect.Properties.Settings.Default.configUpgradeRequired = false;
                NexDirect.Properties.Settings.Default.Save();

                // The osu! folder check is to check if it is a new installation, because if it is then configupgrade will be true, and we dont need to pop this.
                if (!string.IsNullOrEmpty(NexDirect.Properties.Settings.Default.osuFolder))
                {
                    MessageBox.Show($"Your NexDirect installation has been successfully updated to version {WinTools.GetGitStyleVersion()}. Your previous settings have all been upgraded. Welcome back!", "NexDirect - Successful Update", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }

            if (SingleInstance<App>.InitializeAsFirstInstance(AppUnique))
            {
                var app = new App();

                app.InitializeComponent();
                //app.HandleURIArgs(args.ToList());
                app.Run(new MainWindow(args));

                SingleInstance<App>.Cleanup();
            }
        }

        #region ISingleInstanceApp Members

        public bool SignalExternalCommandLineArgs(IList<string> args)
        {
            MainWindow _MainWindow = (MainWindow)Current.MainWindow; // get the mainwin instance
            _MainWindow.RestoreWindow();

            args.RemoveAt(0); // args includes the executing path, lets remove that
            _MainWindow.HandleURIArgs(args);
            return true;
        }

        #endregion
    }
}
