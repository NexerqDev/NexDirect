using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace NexDirectLib
{
    public static class DownloadManager
    {
        public static ObservableCollection<Structures.BeatmapDownload> Downloads = new ObservableCollection<Structures.BeatmapDownload>();

        public static async Task DownloadSet(Structures.BeatmapDownload download)
        {
            Downloads.Add(download);
            await download.Client.DownloadFileTaskAsync(download.Location, download.TempPath);
            Downloads.Remove(download);
            AudioManager.OnDownloadComplete();
        }

        public static void CancelDownload(Structures.BeatmapDownload download)
        {
            download.Cancelled = true;
            download.Client.CancelAsync();
        }
    }
}
