using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using WPFFolderBrowser;

namespace NexDirect.Dialogs
{
    /// <summary>
    /// Interaction logic for FirstTimeInstall.xaml
    /// </summary>
    public partial class FirstTimeInstall : Window
    {
        MainWindow _mw;
        public FirstTimeInstall(MainWindow mw)
        {
            InitializeComponent();
            _mw = mw;
        }

        string osuDir;
        bool triedDetected = false;
        private void mainButton_Click(object sender, RoutedEventArgs e)
        {
            if (triedDetected)
            {
                // manual selection
                if ((osuDir = manualSelectDir()) == null)
                    return;

                statusLabel.Content = $"Manually selected osu! at: {osuDir}";
                statusLabel.Foreground = Brushes.Green;
                startButton.IsEnabled = true;
                return;
            }


            osuDir = tryAutoRetrieveOsuDir();
            if (osuDir != null)
            {
                statusLabel.Content = $"We were able to auto-detect your osu! folder at: {osuDir}";
                statusLabel.Foreground = Brushes.Green;
            }
            else
            {
                statusLabel.Content = $"We were not able to auto-detect your osu! folder. Please manually select it.";
                statusLabel.Foreground = Brushes.Red;
                startButton.IsEnabled = false;
            }

            startButton.Visibility = Visibility.Visible;
            mainButton.Content = "Manual Select osu! folder";
            triedDetected = true;
        }

        private void startButton_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.osuFolder = osuDir;
            Properties.Settings.Default.Save();
            _mw.osuFolder = osuDir;
        }

        private const string OSU_REG_KEY = @"SOFTWARE\Classes\osu!";
        private Regex osuValueRegex = new Regex(@"""(.*)\\osu!\.exe"" ""%1""");
        private string tryAutoRetrieveOsuDir()
        {
            // attempt auto-detection via osu! uri registry keys
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(OSU_REG_KEY, false))
            {
                if (key != null && key.GetValue("", "").ToString() == "osu! beatmap") // make sure key exists and the GetValue() of the default is indeed for osu! beatmap
                {
                    using (RegistryKey cmdKey = key.OpenSubKey(@"shell\open\command"))
                    {
                        string osuValue;
                        if ((osuValue = (string)cmdKey.GetValue("", null)) != null)
                        {
                            Match m = osuValueRegex.Match(osuValue);
                            if (!String.IsNullOrEmpty(m.Value))
                                return m.Groups[1].ToString();
                        }
                    }
                }

                return null;
            }
        }

        private string manualSelectDir()
        {
            var dialog = new WPFFolderBrowserDialog();
            if (!(bool)dialog.ShowDialog())
                return null;

            if (!string.IsNullOrEmpty(dialog.FileName))
            {
                if (File.Exists(Path.Combine(dialog.FileName, "osu!.exe")))
                    return dialog.FileName;
                else
                    return null;
            }
            else
            {
                return null;
            }
        }
    }
}
