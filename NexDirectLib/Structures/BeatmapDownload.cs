using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NexDirectLib.Structures
{
    // i dont even 100% know how this notifypropertychanged works
    // but i get why i need it i guess
    // https://stackoverflow.com/questions/5051530/wpf-gridview-not-updating-on-observable-collection-change
    /// <summary>
    /// Beatmap download tracker object
    /// </summary>
    public class BeatmapDownload : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected void Notify(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
        #endregion


        private int _percent;
        private float _speed;

        public BeatmapSet Set { get; set; }
        public WebClient Client { get; set; }
        public float Speed // kB/s
        {
            get { return _speed; }
            set
            {
                _speed = value;
                Notify("Speed");
            }
        }
        public Stopwatch SpeedTracker { get; set; }
        public Uri Location { get; set; }
        public int Percent
        {
            get { return _percent; }
            set
            {
                _percent = value;
                Notify("Percent");
            }
        }
        public bool Cancelled { get; set; }
        public string FriendlyName => $"{Set.Title} ({Set.Mapper})";
        public string FileName => Tools.SanitizeFilename($"{Set.Id} {Set.Artist} - {Set.Title}.osz");
        public string TempPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FileName + ".nexd");

        public BeatmapDownload(BeatmapSet set, Uri uri)
        {
            Set = set;
            Percent = 0;
            Client = new WebClient();
            SpeedTracker = new Stopwatch();
            Speed = 0;
            Location = uri;

            // Attach events
            Client.DownloadProgressChanged += (o, e) =>
            {
                if (SpeedTracker.Elapsed.Seconds > 0)
                    Speed = (e.BytesReceived / 1000) / SpeedTracker.Elapsed.Seconds;

                Percent = e.ProgressPercentage;
            };
            Client.DownloadFileCompleted += (o, e) =>
            {
                Client.Dispose();
                SpeedTracker.Stop();
            };
        }
    }
}
