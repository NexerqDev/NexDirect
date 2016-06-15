using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;

namespace NexDirect
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

            public BeatmapSet(MainWindow _this, string id, string artist, string title, string mapper, string rankStatus, Dictionary<string, string> difficulties, JObject bloodcatRaw)
            {
                Id = id;
                Artist = artist;
                Title = title;
                Mapper = mapper;
                RankingStatus = rankStatus;
                PreviewImage = new Uri(string.Format("http://b.ppy.sh/thumb/{0}l.jpg", Id));
                AlreadyHave = _this.alreadyDownloaded.Any(b => b.Contains(Id + " "));
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
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(propName));
                }
            }
            #endregion


            private string _percent;

            public string BeatmapSetName { get; set; }
            public string ProgressPercent
            {
                get { return _percent; }
                set
                {
                    _percent = value;
                    Notify("ProgressPercent");
                }
            }
            public string BeatmapSetId { get; set; }
            public WebClient DownloadClient { get; set; }
            public string DownloadFileName { get; set; }
            public string TempDownloadPath { get; set; }
            public bool DownloadCancelled { get; set; }

            public BeatmapDownload(BeatmapSet set, WebClient client, string osuFolder, NAudio.Wave.WaveOut doongPlayer, bool launchOsuAfter)
            {
                BeatmapSetName = string.Format("{0} ({1})", set.Title, set.Mapper);
                ProgressPercent = "0";
                BeatmapSetId = set.Id;
                DownloadClient = client;
                DownloadFileName = Tools.sanitizeFilename(string.Format("{0} {1} - {2}.osz", set.Id, set.Artist, set.Title));
                TempDownloadPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DownloadFileName + ".nexd");

                // Attach events
                DownloadClient.DownloadProgressChanged += (o, e) =>
                {
                    ProgressPercent = e.ProgressPercentage.ToString();
                };
                DownloadClient.DownloadFileCompleted += (o, e) =>
                {
                    if (e.Cancelled)
                    {
                        File.Delete(TempDownloadPath);
                        return;
                    }

                    if (launchOsuAfter && Process.GetProcessesByName("osu!").Length > 0)
                    {
                        string newPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DownloadFileName);
                        File.Move(TempDownloadPath, newPath); // rename to .osz
                        Process.Start(Path.Combine(osuFolder, "osu!.exe"), newPath);
                    }
                    else
                    {
                        File.Move(TempDownloadPath, Path.Combine(osuFolder, "Songs", DownloadFileName));
                    }

                    doongPlayer.Play();
                };
            }
        }
    }
}
