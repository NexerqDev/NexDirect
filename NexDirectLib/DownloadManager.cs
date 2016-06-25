using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace NexDirectLib
{
    using static Structures;

    public static class DownloadManager
    {
        public static ObservableCollection<BeatmapDownload> Downloads = new ObservableCollection<BeatmapDownload>();

        public static async Task DownloadSet(BeatmapDownload download)
        {
            Downloads.Add(download);
            await download.Client.DownloadFileTaskAsync(download.Location, download.TempPath);
            Downloads.Remove(download);
            AudioManager.OnDownloadComplete();
        }

        public static void CancelDownload(BeatmapDownload download)
        {
            download.Cancelled = true;
            download.Client.CancelAsync();
        }
    }
}
