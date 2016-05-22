using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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
            loaded = true;
        }

        private void changeFolderButton_Click(object sender, RoutedEventArgs e)
        {
            parent.osuFolder = "forced_update";
            parent.checkOrPromptSongsDir();
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

            if (string.IsNullOrEmpty(dialog.FileName))
            {
                MessageBox.Show("No file selected. Please try again.", "NexDirect - Error");
                return;
            }

            parent.uiBackground = dialog.FileName;
            Properties.Settings.Default.customBgPath = parent.uiBackground;
            Properties.Settings.Default.Save();
            MessageBox.Show("New custom background saved.", "NexDirect - Updated");
            parent.setCustomBackground(parent.uiBackground);
        }

        private void clearBgButton_Click(object sender, RoutedEventArgs e)
        {
            parent.uiBackground = "";
            Properties.Settings.Default.customBgPath = parent.uiBackground;
            Properties.Settings.Default.Save();
            MessageBox.Show("Custom background cleared.", "NexDirect - Updated");
            parent.setCustomBackground(null);
        }

        private void launchOsuCheckbox_Toggled(object sender, RoutedEventArgs e)
        {
            if (!loaded) return;
            parent.launchOsu = (bool)launchOsuCheckbox.IsChecked;
            Properties.Settings.Default.launchOsu = parent.launchOsu;
            Properties.Settings.Default.Save();
        }
    }
}
