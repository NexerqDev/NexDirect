using NAudio.Wave;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Media.Imaging;

namespace NexDirect
{
    public static class Bloodcat
    {
        /// <summary>
        /// Bloodcat Beatmap Set class
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
            public JObject BloodcatData { get; set; }

            public BeatmapSet(MainWindow _this, JObject rawData)
            {
                Id = rawData["id"].ToString();
                Artist = rawData["artist"].ToString();
                Title = rawData["title"].ToString();
                Mapper = rawData["creator"].ToString();
                RankingStatus = Tools.resolveRankingStatus(rawData["status"].ToString());
                PreviewImage = new Uri(string.Format("http://b.ppy.sh/thumb/{0}l.jpg", Id));
                AlreadyHave = _this.alreadyDownloaded.Any(b => b.Contains(Id + " "));
                Difficulties = ((JArray)rawData["beatmaps"]).Select(b => new Difficulty(b["name"].ToString(), b["mode"].ToString()));
                BloodcatData = rawData;
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

            public BeatmapDownload(BeatmapSet set, WebClient client)
            {
                BeatmapSetName = string.Format("{0} ({1})", set.Title, set.Mapper);
                ProgressPercent = "0";
                BeatmapSetId = set.Id;
                DownloadClient = client;
                DownloadFileName = Tools.sanitizeFilename(string.Format("{0} {1} - {2}.osz", set.Id, set.Artist, set.Title));
                TempDownloadPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DownloadFileName + ".nexd");
            }
        }

        /// <summary>
        /// Searches Bloodcat for a string with some params
        /// </summary>
        public static async Task<JArray> Search(string query, string sRankedParam, string mModeParam, string cNumbersParam)
        {
            // build query string -- https://stackoverflow.com/questions/17096201/build-query-string-for-system-net-httpclient-get
            var qs = HttpUtility.ParseQueryString(string.Empty);
            qs["mod"] = "json";
            qs["q"] = query;
            if (sRankedParam != null) qs["s"] = sRankedParam;
            if (mModeParam != null) qs["m"] = mModeParam;
            if (cNumbersParam != null) qs["c"] = cNumbersParam;

            return await Web.GetJson<JArray>("http://bloodcat.com/osu/?" + qs.ToString());
        }

        /// <summary>
        /// Retrieves front page popular data from Bloodcat
        /// </summary>
        public static async Task<JArray> Popular()
        {
            return await Web.GetJson<JArray>("http://bloodcat.com/osu/popular.php?mod=json");
        }

        /// <summary>
        /// Shorthand to resolve a set ID to a BeatmapSet object
        /// </summary>
        public static async Task<BeatmapSet> ResolveSetId(MainWindow _this, string beatmapSetId)
        {
            JArray results = await Search(beatmapSetId, null, null, "s");
            JObject map = results.Children<JObject>().FirstOrDefault(r => r["id"].ToString() == beatmapSetId);
            if (map == null) return null;
            return new BeatmapSet(_this, map);
        }

        /// <summary>
        /// Downloads a set from bloodcat or mirror if defined
        /// </summary>
        public static async Task DownloadSet(BeatmapSet set, string mirror, ObservableCollection<BeatmapDownload> downloadProgress, string osuFolder, WaveOut doongPlayer, bool launchOsu)
        {
            Uri downloadUri;
            if (string.IsNullOrEmpty(mirror))
            {
                downloadUri = new Uri("http://bloodcat.com/osu/s/" + set.Id);
            }
            else
            {
                downloadUri = new Uri(mirror.Replace("%s", set.Id));
            }

            using (var client = new WebClient())
            {
                var download = new BeatmapDownload(set, client);
                downloadProgress.Add(download);

                client.DownloadProgressChanged += (o, e) =>
                {
                    download.ProgressPercent = e.ProgressPercentage.ToString();
                };
                client.DownloadFileCompleted += (o, e) =>
                {
                    if (e.Cancelled)
                    {
                        File.Delete(download.TempDownloadPath);
                        return;
                    }

                    if (launchOsu && Process.GetProcessesByName("osu!").Length > 0) // https://stackoverflow.com/questions/262280/how-can-i-know-if-a-process-is-running - ensure osu! is running dont want to just launch the game lol
                    {
                        string newPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, download.DownloadFileName);
                        File.Move(download.TempDownloadPath, newPath); // rename to .osz
                        Process.Start(Path.Combine(osuFolder, "osu!.exe"), newPath);
                    }
                    else
                    {
                        File.Move(download.TempDownloadPath, Path.Combine(osuFolder, "Songs", download.DownloadFileName));
                    }

                    doongPlayer.Play();
                };

                try { await client.DownloadFileTaskAsync(downloadUri, download.TempDownloadPath); } // appdomain.etc is a WPF way of getting startup dir... stupid :(
                catch (Exception ex)
                {
                    if (download.DownloadCancelled == true) return;
                    MessageBox.Show(string.Format("An error has occured whilst downloading {0} ({1}).\n\n{2}", set.Title, set.Mapper, ex.ToString()));
                }
                finally
                {
                    downloadProgress.Remove(download);
                }
            }
        }

        public static void CancelDownload(BeatmapDownload statusObj)
        {
            statusObj.DownloadCancelled = true;
            statusObj.DownloadClient.CancelAsync();
        }
    }
}
