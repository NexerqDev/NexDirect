using Microsoft.Win32;
using NexDirectLib;
using System;
using System.Windows;

using NexDirect;
using System.Windows.Media;

namespace NexDirect.Dialogs
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {
        bool loaded = false;

        MainWindow _mw;

        public Settings(MainWindow mw)
        {
            InitializeComponent();

            this._mw = mw; // legacy reference

            overlayModeCheckbox.IsChecked = SettingManager.Get("overlayMode");
            audioPreviewsCheckbox.IsChecked = SettingManager.Get("audioPreviews");
            mirrorTextBox.Text = SettingManager.Get("beatmapMirror");
            launchOsuCheckbox.IsChecked = SettingManager.Get("launchOsu");
            officialDownloadBox.IsChecked = SettingManager.Get("useOfficialOsu");
            useTrayCheckbox.IsChecked = SettingManager.Get("minimizeToTray");
            novidCheckbox.IsChecked = SettingManager.Get("novidDownload");
            previewVolumeSlider.Value = SettingManager.Get("previewVolume") * 100;

            if (SettingManager.Get("fallbackActualOsu"))
                officialDownloadBox.IsChecked = true;

            if ((bool)officialDownloadBox.IsChecked)
            {
                officialLoggedInAs.Visibility = Visibility.Visible;
                officialLoggedInAs.Content = "Currently logged in as: " + SettingManager.Get("officialOsuUsername");
                if (SettingManager.Get("fallbackActualOsu"))
                    officialLoggedInAs.Content += " (falling back to Bloodcat)";
            }

            buildDataLabel.Content = $"running NexDirect {(BuildMeta.IsDebug ? "debug" : "v" + WinTools.GetGitStyleVersion())} - built {BuildMeta.BuildDateTime.ToString()} ({BuildMeta.BuildBranch}#{BuildMeta.BuildCommit})";
            if (BuildMeta.IsDebug)
                buildDataLabel.Foreground = new SolidColorBrush(Color.FromRgb(255, 0, 0));

            loaded = true;
        }

        private void changeFolderButton_Click(object sender, RoutedEventArgs e)
        {
            SettingManager.Set("osuFolder", "");
            MessageBox.Show("osu! folder setting reset. Restart NexDirect to run through the first time folder setup again.", "NexDirect - Updated");
        }

        private void overlayModeCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            if (!loaded)
                return;
            SettingManager.Set("overlayMode", true);
            MessageBox.Show("Overlay mode has been enabled. A restart of NexDirect is required for changes to take effect.", "NexDirect - Updated");
        }

        private void overlayModeCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!loaded)
                return;
            SettingManager.Set("overlayMode", false);
            MessageBox.Show("Overlay mode has been disabled. A restart of NexDirect is required for changes to take effect.", "NexDirect - Updated");
        }

        private void audioPreviewsCheckbox_Toggled(object sender, RoutedEventArgs e)
        {
            if (!loaded)
                return;
            SettingManager.Set("audioPreviews", (bool)audioPreviewsCheckbox.IsChecked); // IsChecked is already the new value
        }

        private void mirrorSaveButton_Click(object sender, RoutedEventArgs e)
        {
            SettingManager.Set("beatmapMirror", mirrorTextBox.Text);
            MessageBox.Show("New mirror has been saved.", "NexDirect - Updated");
        }

        private void setCustomBgButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();

            string filename;
            if (!(bool)dialog.ShowDialog())
            {
                // take it as clear - catch dialog.FileName cancel button
                filename = "";
            }
            else
            {
                if (string.IsNullOrEmpty(dialog.FileName))
                {
                    MessageBox.Show("Invalid file selected. Please try again.", "NexDirect - Error");
                    return;
                }
                filename = dialog.FileName;
            }

            SettingManager.Set("customBgPath", filename);

            if (!string.IsNullOrEmpty(filename))
                MessageBox.Show("New custom background saved.", "NexDirect - Updated");
            else
                MessageBox.Show("Custom background removed.", "NexDirect - Updated");

            _mw.SetFormCustomBackground(filename);
        }

        private void clearBgButton_Click(object sender, RoutedEventArgs e)
        {
            SettingManager.Set("customBgPath", "");
            MessageBox.Show("Custom background cleared.", "NexDirect - Updated");
            _mw.SetFormCustomBackground(null);
        }

        private void launchOsuCheckbox_Toggled(object sender, RoutedEventArgs e)
        {
            if (!loaded)
                return;
            SettingManager.Set("launchOsu", (bool)launchOsuCheckbox.IsChecked);
        }

        private const string regUriSubKey = @"Software\Classes\nexdirect";
        private void registerUriButton_Click(object sender, RoutedEventArgs e)
        {
            string appLocation = WinTools.GetExecLocation();

            try
            {
                // https://msdn.microsoft.com/en-AU/library/h5e7chcf.aspx
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(regUriSubKey))
                {
                    key.SetValue("", "NexDirect Handling Protocol"); // "" = (default)
                    key.SetValue("URL Protocol", "");

                    using (RegistryKey iconKey = key.CreateSubKey(@"DefaultIcon"))
                        iconKey.SetValue("", $"{appLocation},1");

                    using (RegistryKey shellOpenKey = key.CreateSubKey(@"shell\open\command"))
                        shellOpenKey.SetValue("", $"\"{appLocation}\" \"%1\"");
                }

                MessageBox.Show("The URI Scheme handler was registered successfully.");
            }
            catch (Exception ex) {  MessageBox.Show($"An error occured whilst registering the handler...\n\n{ex.ToString()}"); }
        }

        private async void officialDownloadBox_Checked(object sender, RoutedEventArgs e)
        {
            if (!loaded)
                return;
            (new OsuLoginPop(this)).ShowDialog();

            if (SettingManager.Get("useOfficialOsu"))
                await OsuCredsOnLaunch.TestCookies(); // shitty but reuse code
        }

        private void officialDownloadBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!loaded)
                return;

            if (SettingManager.Get("fallbackActualOsu"))
                SettingManager.Set("fallbackActualOsu", false);

            officialLoggedInAs.Visibility = Visibility.Hidden;

            SettingManager.Set("useOfficialOsu", false);
            SettingManager.Set("officialOsuCookies", null);
            SettingManager.Set("officialOsuUsername", "");
            SettingManager.Set("officialOsuPassword", "");
            MessageBox.Show("Disabled official osu! server downloads.");
        }

        private void useTrayCheckbox_Toggled(object sender, RoutedEventArgs e)
        {
            if (!loaded)
                return;
            SettingManager.Set("minimizeToTray", (bool)useTrayCheckbox.IsChecked);
        }

        private void registerLinkerButton_Click(object sender, RoutedEventArgs e)
        {
            if (!WinUacHelper.IsProcessElevated)
            {
                MessageBox.Show("Please restart NexDirect as Admin (right click -> Run as Administrator) as this process requires this to be set up!");
                return;
            }

            MessageBoxResult m = MessageBox.Show("Are you sure you want to set up NexDirect as a system browser?", "NexDirect", MessageBoxButton.YesNo);
            if (m == MessageBoxResult.No)
                return;

            LinkerSetup.Setup();
        }

        private void novidCheckbox_Toggled(object sender, RoutedEventArgs e)
        {
            if (!loaded)
                return;
            SettingManager.Set("novidDownload", (bool)novidCheckbox.IsChecked);
        }

        private void previewVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!loaded)
                return;

            float v = (float)(previewVolumeSlider.Value / 100);
            SettingManager.Set("previewVolume", v);
            AudioManager.SetPreviewVolume(v);
        }
    }
}
