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
            // focus
            if (MainWindow.WindowState == WindowState.Minimized)
            {
                MainWindow.WindowState = WindowState.Normal;
            }

            MainWindow.Activate();

            args.RemoveAt(0); // args includes the executing path, lets remove that
            MainWindow _MainWindow = (MainWindow)Current.MainWindow; // get the mainwin instance
            _MainWindow.handleURIArgs(args);
            return true;
        }

        #endregion
    }
}
