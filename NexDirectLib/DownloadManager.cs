using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace NexDirectLib
{
    using Structures;
    using System.IO;
    public static class DownloadManager
    {
        public static ObservableCollection<BeatmapDownload> Downloads = new ObservableCollection<BeatmapDownload>();
        public static float Speed = 0;

        public class SpeedUpdatedEventArgs : EventArgs
        {
            public float Speed;
            public SpeedUpdatedEventArgs(float _speed) { Speed = _speed; }
        }
        public delegate void SpeedUpdatedHandler(SpeedUpdatedEventArgs e);
        public static event SpeedUpdatedHandler SpeedUpdated;

        static DownloadManager()
        {
            // hack?! idk
            Downloads.CollectionChanged += (o, e) =>
            {
                if (Downloads.Count > 0 && e.NewItems != null)
                {
                    foreach (var _d in e.NewItems)
                    {
                        var d = _d as BeatmapDownload;
                        if (d == null)
                            continue;
                        d.PropertyChanged += (o1, e1) =>
                        {
                            if (e1.PropertyName == "Speed")
                            {
                                Downloads_SpeedUpdateHandler();
                            }
                        };
                    }
                }
                Downloads_SpeedUpdateHandler();
            };
        }

        public static async Task DownloadSet(BeatmapDownload download, bool playAudioNotif = true)
        {
            Downloads.Add(download);
            download.SpeedTracker.Start(); // start the speed tracking

            try
            {
                await download.Client.DownloadFileTaskAsync(download.Location, download.TempPath);
            }
            catch (Exception ex)
            {
                if (download.Cancelled)
                {
                    File.Delete(download.TempPath);
                    return;
                }
                throw ex;
            }
            finally
            {
                Downloads.Remove(download); // cleanup regardless

                if (Downloads.Count < 1)
                    MapsManager.Reload(); // reload only when theres nothing left
            }
                
            if (playAudioNotif)
                AudioManager.OnDownloadComplete();
        }

        public static void CancelDownload(BeatmapDownload download)
        {
            download.Cancelled = true;
            download.Client.CancelAsync();
            Downloads.Remove(download);
        }

        private static void Downloads_SpeedUpdateHandler()
        {
            if (Downloads.Count > 0)
                Speed = Downloads.Select(d => d.Speed).Sum() / Downloads.Count;
            else
                Speed = 0;

            SpeedUpdated(new SpeedUpdatedEventArgs(Speed));
        }
    }
}
