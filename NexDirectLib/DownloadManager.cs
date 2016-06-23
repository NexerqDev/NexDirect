using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace NexDirectLib
{
    public static class DownloadManager
    {
        public static ObservableCollection<Structures.BeatmapDownload> Downloads = new ObservableCollection<Structures.BeatmapDownload>();

        public static async Task DownloadSet(Structures.BeatmapDownload download)
        {
            download.Client.DownloadFileCompleted += (o, e) => Downloads.Remove(download);
            Downloads.Add(download);
            await download.Client.DownloadFileTaskAsync(download.Location, download.TempPath);
        }

        public static void CancelDownload(Structures.BeatmapDownload download)
        {
            download.Cancelled = true;
            download.Client.CancelAsync();
        }
    }
}
