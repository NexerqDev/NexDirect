using NexDirectLib;
using NexDirectLib.Structures;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace NexDirect.Dialogs
{
    /// <summary>
    /// Interaction logic for DirectDownload.xaml
    /// </summary>
    public partial class DirectDownload : Window
    {
        BeatmapSet set;
        MainWindow _mw;

        public DirectDownload(MainWindow mw, BeatmapSet bs)
        {
            InitializeComponent();

            set = bs;
            _mw = mw;

            Title = $"{set.Artist} - {set.Title} <{set.Mapper}> :: NexDirect Download";
            previewImage.Source = new BitmapImage(bs.PreviewImage);
            songInfoLabel.Content = $"{set.Artist} - {set.Title}";
            mapperInfoLabel.Content = $"(mapped by {set.Mapper})";

            if (SettingManager.Get("overlayMode")) // overlay & only if osu! is open.
                initOverlayMode();
        }

        private void initOverlayMode()
        {
            Opacity = 0.88;
            Topmost = true;
            WindowStyle = WindowStyle.None;
            Height = SystemParameters.PrimaryScreenHeight;
            AllowsTransparency = true;

            // Reposition to very right - wpf only has left/top properties so calculate.
            Top = 0;
            Left = SystemParameters.PrimaryScreenWidth; // start outside
            // animate the coming in
            var animate = new DoubleAnimation(SystemParameters.PrimaryScreenWidth - Width, new Duration(TimeSpan.FromMilliseconds(500)));
            BeginAnimation(Window.LeftProperty, animate);
        }

        private void downloadBtn_Click(object sender, RoutedEventArgs e)
            => onDownloadClick();

        private void downloadViewBtn_Click(object sender, RoutedEventArgs e)
            => onDownloadClick(true);

        private void previewBtn_Click(object sender, RoutedEventArgs e)
            => Osu.PlayPreviewAudio(set);

        private void button_Click(object sender, RoutedEventArgs e)
        {
            string url = $"https://osu.ppy.sh/s/{set.Id}";
            Process proc;
            if (String.IsNullOrEmpty(SettingManager.Get("linkerDefaultBrowser")))
                proc = Process.Start(url);
            else
                proc = Process.Start(new ProcessStartInfo(SettingManager.Get("linkerDefaultBrowser"), url));

            WinTools.SetHandleForeground(proc.Handle);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            AudioManager.ForceStopPreview();

            // fade out
            if (SettingManager.Get("overlayMode"))
            {
                e.Cancel = true; // interrupt event to fadeclose
                var animate = new DoubleAnimation(SystemParameters.PrimaryScreenWidth, new Duration(TimeSpan.FromMilliseconds(150)));
                animate.Completed += (o, e1) => Close(); // close on done
                BeginAnimation(Window.LeftProperty, animate);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
            => Activate();

        private void onDownloadClick(bool restore = false)
        {
            AudioManager.ForceStopPreview();
            DownloadManagement.DownloadBeatmapSet(set);
            if (restore)
                _mw.RestoreWindow();
            Close();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
            => Close();
    }
}
