using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Windows;

namespace NexDirect
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {
        MainWindow parent;
        bool loaded = false;

        public Settings(MainWindow _parent)
        {
            InitializeComponent();

            parent = _parent;

            overlayModeCheckbox.IsChecked = parent.overlayMode;
            audioPreviewsCheckbox.IsChecked = parent.audioPreviews;
            mirrorTextBox.Text = parent.beatmapMirror;
            launchOsuCheckbox.IsChecked = parent.launchOsu;
            officialDownloadBox.IsChecked = parent.useOfficialOsu;
            if (_parent.fallbackActualOsu) officialDownloadBox.IsChecked = true;
            loaded = true;
        }

        private void changeFolderButton_Click(object sender, RoutedEventArgs e)
        {
            parent.osuFolder = "forced_update";
            parent.CheckOrPromptForSongsDir();
        }

        private void overlayModeCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            if (!loaded) return;
            Properties.Settings.Default.overlayMode = true;
            Properties.Settings.Default.Save();
            MessageBox.Show("Overlay mode has been enabled. A restart of NexDirect is required for changes to take effect.", "NexDirect - Updated");
        }

        private void overlayModeCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!loaded) return;
            Properties.Settings.Default.overlayMode = false;
            Properties.Settings.Default.Save();
            MessageBox.Show("Overlay mode has been disabled. A restart of NexDirect is required for changes to take effect.", "NexDirect - Updated");
        }

        private void audioPreviewsCheckbox_Toggled(object sender, RoutedEventArgs e)
        {
            if (!loaded) return;
            parent.audioPreviews = (bool)audioPreviewsCheckbox.IsChecked; // IsChecked is already the new value
            Properties.Settings.Default.audioPreviews = parent.audioPreviews;
            Properties.Settings.Default.Save();
        }

        private void mirrorSaveButton_Click(object sender, RoutedEventArgs e)
        {
            parent.beatmapMirror = mirrorTextBox.Text;
            Properties.Settings.Default.beatmapMirror = parent.beatmapMirror;
            Properties.Settings.Default.Save();
            MessageBox.Show("New mirror has been saved.", "NexDirect - Updated");
        }

        private void setCustomBgButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            CommonFileDialogResult result = dialog.ShowDialog();

            string filename;
            try
            {
                if (string.IsNullOrEmpty(dialog.FileName))
                {
                    MessageBox.Show("Invalid file selected. Please try again.", "NexDirect - Error");
                    return;
                }
                filename = dialog.FileName;
            }
            catch
            {
                // take it as clear - catch dialog.FileName cancel button
                filename = "";
            }

            parent.uiBackground = filename;
            Properties.Settings.Default.customBgPath = parent.uiBackground;
            Properties.Settings.Default.Save();

            if (!string.IsNullOrEmpty(filename)) MessageBox.Show("New custom background saved.", "NexDirect - Updated");
            else MessageBox.Show("Custom background removed.", "NexDirect - Updated");
            parent.SetFormCustomBackground(parent.uiBackground);
        }

        private void clearBgButton_Click(object sender, RoutedEventArgs e)
        {
            parent.uiBackground = "";
            Properties.Settings.Default.customBgPath = parent.uiBackground;
            Properties.Settings.Default.Save();
            MessageBox.Show("Custom background cleared.", "NexDirect - Updated");
            parent.SetFormCustomBackground(null);
        }

        private void launchOsuCheckbox_Toggled(object sender, RoutedEventArgs e)
        {
            if (!loaded) return;
            parent.launchOsu = (bool)launchOsuCheckbox.IsChecked;
            Properties.Settings.Default.launchOsu = parent.launchOsu;
            Properties.Settings.Default.Save();
        }

        private const string regUriSubKey = @"Software\Classes\nexdirect";
        private void registerUriButton_Click(object sender, RoutedEventArgs e)
        {
            string appLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;

            try
            {
                // https://msdn.microsoft.com/en-AU/library/h5e7chcf.aspx
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(regUriSubKey))
                {
                    key.SetValue("", "NexDirect Handling Protocol"); // "" = (default)
                    key.SetValue("URL Protocol", "");

                    using (RegistryKey iconKey = key.CreateSubKey(@"DefaultIcon"))
                    {
                        iconKey.SetValue("", string.Format("{0},1", appLocation));
                    }

                    using (RegistryKey shellOpenKey = key.CreateSubKey(@"shell\open\command"))
                    {
                        shellOpenKey.SetValue("", string.Format("\"{0}\" \"%1\"", appLocation));
                    }
                }

                MessageBox.Show("The URI Scheme handler was registered successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("An error occured whilst registering the handler..."), ex.ToString());
            }
        }

        private void officialDownloadBox_Checked(object sender, RoutedEventArgs e)
        {
            if (!loaded) return;
            (new Dialogs.OsuLogin(parent)).ShowDialog();
            if (!parent.useOfficialOsu)
            {
                loaded = false; // just stop it from running handler
                officialDownloadBox.IsChecked = false;
                loaded = true;
            }
        }

        private void officialDownloadBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!loaded) return;
            parent.useOfficialOsu = false;
            parent.officialOsuCookies = null;
            if (parent.fallbackActualOsu) parent.fallbackActualOsu = false;
            Properties.Settings.Default.useOfficialOsu = false;
            Properties.Settings.Default.officialOsuCookies = null;
            Properties.Settings.Default.officialOsuUsername = "";
            Properties.Settings.Default.officialOsuPassword = "";
            Properties.Settings.Default.Save();
            MessageBox.Show("Disabled official osu! server downloads.");
        }
    }
}
