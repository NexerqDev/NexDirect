using Microsoft.Win32;
using NexDirectLib;
using System;
using System.Windows;

namespace NexDirect.Dialogs
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {
        MainWindow _mw;
        bool loaded = false;

        public Settings(MainWindow mw)
        {
            InitializeComponent();

            _mw = mw;

            overlayModeCheckbox.IsChecked = _mw.overlayMode;
            audioPreviewsCheckbox.IsChecked = _mw.audioPreviews;
            mirrorTextBox.Text = _mw.beatmapMirror;
            launchOsuCheckbox.IsChecked = _mw.launchOsu;
            officialDownloadBox.IsChecked = _mw.useOfficialOsu;
            useTrayCheckbox.IsChecked = _mw.minimizeToTray;

            if (_mw.fallbackActualOsu)
                officialDownloadBox.IsChecked = true;

            if ((bool)officialDownloadBox.IsChecked)
            {
                officialLoggedInAs.Visibility = Visibility.Visible;
                officialLoggedInAs.Content = "Currently logged in as: " + Properties.Settings.Default.officialOsuUsername;
                if (_mw.fallbackActualOsu)
                    officialLoggedInAs.Content += " (falling back to Bloodcat)";
            }

            loaded = true;
        }

        private void changeFolderButton_Click(object sender, RoutedEventArgs e)
        {
            _mw.osuFolder = "forced_update";
            _mw.CheckOrPromptForSongsDir();
        }

        private void overlayModeCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            if (!loaded)
                return;
            Properties.Settings.Default.overlayMode = true;
            Properties.Settings.Default.Save();
            MessageBox.Show("Overlay mode has been enabled. A restart of NexDirect is required for changes to take effect.", "NexDirect - Updated");
        }

        private void overlayModeCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!loaded)
                return;
            Properties.Settings.Default.overlayMode = false;
            Properties.Settings.Default.Save();
            MessageBox.Show("Overlay mode has been disabled. A restart of NexDirect is required for changes to take effect.", "NexDirect - Updated");
        }

        private void audioPreviewsCheckbox_Toggled(object sender, RoutedEventArgs e)
        {
            if (!loaded)
                return;
            _mw.audioPreviews = (bool)audioPreviewsCheckbox.IsChecked; // IsChecked is already the new value
            Properties.Settings.Default.audioPreviews = _mw.audioPreviews;
            Properties.Settings.Default.Save();
        }

        private void mirrorSaveButton_Click(object sender, RoutedEventArgs e)
        {
            _mw.beatmapMirror = mirrorTextBox.Text;
            Properties.Settings.Default.beatmapMirror = _mw.beatmapMirror;
            Properties.Settings.Default.Save();
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

            _mw.uiBackground = filename;
            Properties.Settings.Default.customBgPath = _mw.uiBackground;
            Properties.Settings.Default.Save();

            if (!string.IsNullOrEmpty(filename))
                MessageBox.Show("New custom background saved.", "NexDirect - Updated");
            else
                MessageBox.Show("Custom background removed.", "NexDirect - Updated");

            _mw.SetFormCustomBackground(_mw.uiBackground);
        }

        private void clearBgButton_Click(object sender, RoutedEventArgs e)
        {
            _mw.uiBackground = "";
            Properties.Settings.Default.customBgPath = _mw.uiBackground;
            Properties.Settings.Default.Save();
            MessageBox.Show("Custom background cleared.", "NexDirect - Updated");
            _mw.SetFormCustomBackground(null);
        }

        private void launchOsuCheckbox_Toggled(object sender, RoutedEventArgs e)
        {
            if (!loaded)
                return;
            _mw.launchOsu = (bool)launchOsuCheckbox.IsChecked;
            Properties.Settings.Default.launchOsu = _mw.launchOsu;
            Properties.Settings.Default.Save();
        }

        private const string regUriSubKey = @"Software\Classes\nexdirect";
        private void registerUriButton_Click(object sender, RoutedEventArgs e)
        {
            string appLocation = Tools.GetExecLocation();

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

        private void officialDownloadBox_Checked(object sender, RoutedEventArgs e)
        {
            if (!loaded)
                return;
            (new Dialogs.OsuLogin(this, _mw)).ShowDialog();

            loaded = false; // just stop it from running handler
            if (!_mw.fallbackActualOsu)
                officialDownloadBox.IsChecked = false;
            else
                officialDownloadBox.IsChecked = true;
            loaded = true;
        }

        private void officialDownloadBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!loaded)
                return;
            _mw.useOfficialOsu = false;
            _mw.officialOsuCookies = null;
            if (_mw.fallbackActualOsu) _mw.fallbackActualOsu = false;
            officialLoggedInAs.Visibility = Visibility.Hidden;
            Properties.Settings.Default.useOfficialOsu = false;
            Properties.Settings.Default.officialOsuCookies = null;
            Properties.Settings.Default.officialOsuUsername = "";
            Properties.Settings.Default.officialOsuPassword = "";
            Properties.Settings.Default.Save();
            MessageBox.Show("Disabled official osu! server downloads.");
        }

        private void useTrayCheckbox_Toggled(object sender, RoutedEventArgs e)
        {
            if (!loaded)
                return;
            _mw.minimizeToTray = (bool)useTrayCheckbox.IsChecked;
            Properties.Settings.Default.minimizeToTray = _mw.minimizeToTray;
            Properties.Settings.Default.Save();
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
    }
}
