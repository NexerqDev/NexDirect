using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;

namespace NexDirectLib
{
    public class Structures
    {
        /// <summary>
        /// Beatmap Set class
        /// </summary>
        public class BeatmapSet
        {
            public string Id { get; set; }
            public string Artist { get; set; }
            public string Title { get; set; }
            public string Mapper { get; set; }
            public string RankingStatus { get; set; }
            public IEnumerable<Difficulty> Difficulties { get; set; }
            public bool AlreadyHave { get; set; }
            public Uri PreviewImage { get; set; }
            public bool IsBloodcat { get; set; }
            public JObject BloodcatData { get; set; }

            public BeatmapSet(string id, string artist, string title, string mapper, string rankStatus, Dictionary<string, string> difficulties, JObject bloodcatRaw)
            {
                Id = id;
                Artist = artist;
                Title = title;
                Mapper = mapper;
                RankingStatus = rankStatus;
                PreviewImage = new Uri($"http://b.ppy.sh/thumb/{Id}l.jpg");
                AlreadyHave = MapsManager.Maps.Any(b => b.Contains(Id + " "));
                Difficulties = difficulties.Select(d => new Difficulty(d.Key, d.Value));

                if (bloodcatRaw != null)
                {
                    BloodcatData = bloodcatRaw;
                    IsBloodcat = true;
                }
            }

            public class Difficulty
            {
                public string Name { get; set; }
                public string Mode { get; set; }
                public Uri ModeImage { get; set; }

                public Difficulty(string name, string mode)
                {
                    Name = name;
                    Mode = mode;

                    string _image;
                    switch (mode)
                    {
                        case "1":
                            _image = "pack://application:,,,/Resources/mode-taiko-small.png"; break;
                        case "2":
                            _image = "pack://application:,,,/Resources/mode-fruits-small.png"; break;
                        case "3":
                            _image = "pack://application:,,,/Resources/mode-mania-small.png"; break;
                        default:
                            _image = "pack://application:,,,/Resources/mode-osu-small.png"; break;
                    }
                    ModeImage = new Uri(_image);
                }
            }
        }

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


            private string _percent;
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
            public string Percent
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
                Percent = "0";
                Client = new WebClient();
                SpeedTracker = new Stopwatch();
                Speed = 0;
                Location = uri;

                // Attach events
                Client.DownloadProgressChanged += (o, e) =>
                {
                    if (SpeedTracker.Elapsed.Seconds > 0)
                        Speed = (e.BytesReceived / 1000) / SpeedTracker.Elapsed.Seconds;

                    Percent = e.ProgressPercentage.ToString();
                };
                Client.DownloadFileCompleted += (o, e) =>
                {
                    Client.Dispose();
                    SpeedTracker.Stop();
                };
            }
        }

        // Key/Value pair items, for utility
        public class KVItem
        {
            public string Key { get; set; }
            public string Value { get; set; }
            public KVItem(string k, string v)
            {
                Key = k;
                Value = v;
            }

            public override string ToString()
            {
                return Key;
            }
        }
    }
}
