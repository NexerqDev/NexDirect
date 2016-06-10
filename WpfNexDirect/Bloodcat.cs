using NAudio.Wave;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Windows;

namespace NexDirect
{
    public static class Bloodcat
    {
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
        /// Standardizes Bloodcat JSON data to our central structure
        /// </summary>
        public static Structures.BeatmapSet StandardizeToSetStruct(MainWindow _this, JObject bloodcatData)
        {
            var difficulties = new Dictionary<string, string>();
            foreach (var d in (JArray)bloodcatData["beatmaps"])
            {
                difficulties.Add(d["name"].ToString(), d["mode"].ToString());
            }

            return new Structures.BeatmapSet(_this,
                bloodcatData["id"].ToString(), bloodcatData["artist"].ToString(),
                bloodcatData["title"].ToString(), bloodcatData["creator"].ToString(),
                bloodcatData["status"].ToString(), difficulties, bloodcatData);
        }

        /// <summary>
        /// Shorthand to resolve a set ID to a BeatmapSet object
        /// </summary>
        public static async Task<Structures.BeatmapSet> ResolveSetId(MainWindow _this, string beatmapSetId)
        {
            JArray results = await Search(beatmapSetId, null, null, "s");
            JObject map = results.Children<JObject>().FirstOrDefault(r => r["id"].ToString() == beatmapSetId);
            if (map == null) return null;
            return StandardizeToSetStruct(_this, map);
        }

        /// <summary>
        /// Downloads a set from bloodcat or mirror if defined
        /// </summary>
        public static async Task DownloadSet(Structures.BeatmapSet set, string mirror, ObservableCollection<Structures.BeatmapDownload> downloadProgress, string osuFolder, WaveOut doongPlayer, bool launchOsu)
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
                var download = new Structures.BeatmapDownload(set, client, osuFolder, doongPlayer, launchOsu);
                downloadProgress.Add(download);

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
    }
}
