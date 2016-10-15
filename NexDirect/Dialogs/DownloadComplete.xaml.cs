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

using NexDirectLib.Structures;
using System.Windows.Media.Animation;

namespace NexDirect.Dialogs
{
    /// <summary>
    /// Interaction logic for DownloadComplete.xaml
    /// </summary>
    public partial class DownloadComplete : Window
    {
        public DownloadComplete(BeatmapSet set)
        {
            InitializeComponent();

            // same as DirectDownload init for right align.
            Left = SystemParameters.PrimaryScreenWidth - Width;
            Top = SystemParameters.WorkArea.Height - Height - 12; // workarea = area without start bar then some padding

            image.Source = new BitmapImage(set.PreviewImage);
            songLabel.Content = $"{set.Artist} - {set.Title} <{set.Mapper}>";

            fadeCloseInThree();
        }

        private async void fadeCloseInThree()
        {
            await Task.Delay(3000);

            // lerp animate close
            var animate = new DoubleAnimation(0, new Duration(TimeSpan.FromSeconds(1)));
            animate.Completed += (o, e) => Close(); // close this form on done
            BeginAnimation(UIElement.OpacityProperty, animate);
        }
    }
}
