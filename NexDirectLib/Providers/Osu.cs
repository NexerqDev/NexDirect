using System.IO;
using System.Net.Http;
using NAudio.Wave;
using System.Net;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections;
using System;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NexDirectLib.Structures;
using System.Text.RegularExpressions;
using NexDirectLib.Management;

namespace NexDirectLib.Providers
{
    public static class Osu
    {
        public static CookieContainer Cookies;

        /// <summary>
        /// Plays preview audio of a specific beatmap set to the waveout interface
        /// </summary>
        public static async void PlayPreviewAudio(BeatmapSet set)
        {
            // kind of a hack.
            AudioManager.PreviewOut.Stop(); // if already playing something just stop it
            await Task.Delay(250);
            if (DownloadManager.Downloads.Any(d => d.Set.Id == set.Id))
                return; // check for if already d/l'ing overlaps

            WaveOut waveOut = AudioManager.PreviewOut;
            using (var client = new HttpClient())
            {
                try
                {
                    var response = await client.GetAsync("http://b.ppy.sh/preview/" + set.Id + ".mp3");
                    response.EnsureSuccessStatusCode();
                    Stream audioData = await response.Content.ReadAsStreamAsync();

                    // https://stackoverflow.com/questions/2488426/how-to-play-a-mp3-file-using-naudio sol #2
                    var reader = new Mp3FileReader(audioData);
                    waveOut.Stop();
                    waveOut.Init(reader);
                    waveOut.Play();
                }
                catch { } // meh audio previews arent that important, and sometimes they dont exist
            }
        }

        /// <summary>
        /// Logs in to the official osu! website with a given username/password and returns cookie container.
        /// </summary>
        public static async Task<CookieContainer> LoginAndGetCookie(string username, string password)
        {
            var _formData = new Dictionary<string, string>();
            _formData.Add("username", username);
            _formData.Add("password", password);
            var formData = new FormUrlEncodedContent(_formData);

            using (var handler = new HttpClientHandler() { UseCookies = true, CookieContainer = new CookieContainer() })
            using (var client = new HttpClient(handler))
            {
                var response = await client.PostAsync("https://osu.ppy.sh/session", formData);
                if (((int)response.StatusCode) == 422)
                    throw new InvalidPasswordException();
                response.EnsureSuccessStatusCode();

                return handler.CookieContainer;
            }
        }

        public class InvalidPasswordException : Exception { }

        /// <summary>
        /// Checks if cookies given as a param are still working and if so, continue to use them in here.
        /// </summary>
        public static async Task CheckPassedLoginCookieElseUseNew(CookieContainer cookies, string username, string password)
        {
            using (var handler = new HttpClientHandler() { CookieContainer = cookies })
            using (var client = new HttpClient(handler))
            {
                var response = await client.GetAsync("https://osu.ppy.sh/home/download-quota-check");
                response.EnsureSuccessStatusCode();
                string str = await response.Content.ReadAsStringAsync();

                if (str.Contains("error"))
                {
                    // try with creds to renew login
                    CookieContainer newCookies = await LoginAndGetCookie(username, password);
                    Cookies = newCookies;
                }
                else
                {
                    Cookies = cookies;
                }
            }
        }

        /// <summary>
        /// Checks if persisted cookies are still working
        /// </summary>
        public static async Task CheckLoginCookie()
        {
            using (var handler = new HttpClientHandler() { CookieContainer = Cookies })
            using (var client = new HttpClient(handler))
            {
                var response = await client.GetAsync("https://osu.ppy.sh/home/download-quota-check");
                response.EnsureSuccessStatusCode();
                string str = await response.Content.ReadAsStringAsync();

                if (str.Contains("error"))
                    throw new CookiesExpiredException();
            }
        }

        /// <summary>
        /// Searches the official beatmap listing for beatmaps.
        /// </summary>
        public static async Task<SearchResultSet> Search(string query, SearchFilters.OsuRankStatus rankedFilter = SearchFilters.OsuRankStatus.All, SearchFilters.OsuModes modeFilter = SearchFilters.OsuModes.All, int page = 1)
        {
            // hmm, this isnt exactly ideal now, but lets just roll with it i guess
            // there is no actual way to tell if cookies are expired are not unfortunately
            if (page == 1)
                await CheckLoginCookie();

            // ranked filter = s
            // mode filter = m
            string rParam;
            if (rankedFilter == SearchFilters.OsuRankStatus.Approved)
                rParam = "1";
            else if (rankedFilter == SearchFilters.OsuRankStatus.Loved)
                rParam = "8";
            else if (rankedFilter == SearchFilters.OsuRankStatus.Pending)
                rParam = "4";
            else if (rankedFilter == SearchFilters.OsuRankStatus.Graveyard)
                rParam = "5";
            else
                rParam = "7"; // all

            // Search time. Need to use cookies.
            // Same as bloodcat, construct QS
            var qs = HttpUtility.ParseQueryString(string.Empty);
            qs["q"] = query;
            if (rankedFilter != SearchFilters.OsuRankStatus.RankedAndApproved)
                qs["s"] = rParam;
            if (modeFilter != SearchFilters.OsuModes.All)
                qs["m"] = ((int)modeFilter).ToString();
            if (page > 1)
                qs["page"] = page.ToString();

            // p much same as bloodcat here, json apis ftw!
            // we cant use web.getjson as cookies.
            using (var handler = new HttpClientHandler() { CookieContainer = Cookies })
            using (var client = new HttpClient(handler))
            {
                var rawData = await client.GetStringAsync("https://osu.ppy.sh/beatmapsets/search?" + qs.ToString());
                var data = JsonConvert.DeserializeObject<JArray>(rawData);
                var standardized = data.Select(b => StandardizeToSetStruct((JObject)b));
                int count = standardized.Count();

                return new SearchResultSet(standardized, (count >= 50)); // 50 is the max load
            }
        }

        public class SearchNotSupportedException : Exception { }

        /// <summary>
        /// Standardizes Bloodcat JSON data to our central structure
        /// </summary>
        public static BeatmapSet StandardizeToSetStruct(JObject data)
        {
            var difficulties = new List<BeatmapSet.Difficulty>();
            foreach (var d in (JArray)data["beatmaps"]) // parse diffs
                difficulties.Add(new BeatmapSet.Difficulty(d["id"].ToString(), d["version"].ToString(), d["mode_int"].ToString()));
            
            return new BeatmapSet(
                typeof(Osu),
                data["id"].ToString(), data["artist"].ToString(),
                data["title"].ToString(), data["creator"].ToString(),
                ((SearchFilters.OsuRankStatus)int.Parse(data["ranked"].ToString())).ToString(),
                difficulties, data
            );
        }

        /// <summary>
        /// Checks headers of a beatmap set download to see if it has been taken down by DMCA.
        /// </summary>
        public static async Task CheckIllegal(BeatmapSet set)
        {
            // Get status code - 302 REDIRECT = redirected to the real download, 404 = illegal page!, 200 = found the please login dude!
            using (var handler = new HttpClientHandler() { CookieContainer = Cookies, AllowAutoRedirect = false })
            using (var client = new HttpClient(handler))
            {
                // HEAD request for the status code
                HttpResponseMessage response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, $"https://osu.ppy.sh/beatmapsets/{set.Id}/download"));

                if (response.StatusCode != HttpStatusCode.NotFound)
                    throw new IllegalDownloadException(); // 404 page not found == illegal.
                if (response.StatusCode == HttpStatusCode.OK)
                    throw new CookiesExpiredException(); // Redirected to login.
            }
        }

        /// <summary>
        /// Checks the DMCA of a map, then prepares a download object for it.
        /// </summary>
        public static async Task<BeatmapDownload> PrepareDownloadSet(BeatmapSet set, bool preferNoVid = false)
        {
            await CheckIllegal(set);
            var download = new BeatmapDownload(set, new Uri($"https://osu.ppy.sh/beatmapsets/{set.Id}/download" + (preferNoVid ? "?noVideo=1" : "")), Cookies);
            return download;
        }

        public class IllegalDownloadException : Exception { }
        public class CookiesExpiredException : Exception { }

        /// <summary>
        /// Tries to resolve a beatmap set's ID to an object.
        /// </summary>
        public static async Task<BeatmapSet> TryResolveSetId(string setId)
        {
            // to leverage the json "api", we have to be logged in vs scraping the page without logged in
            // i think the json api way is much preferrable. we have to be logged in to search anyway, which this uses.
            try
            {
                SearchResultSet data = await Search(setId);

                // throws if no exist so thats good
                return data.Results.First(b => b.Id == setId);
            }
            catch { return null; }
        }

        /// <summary>
        /// Tries to resolve a beatmap's ID to an object.
        /// </summary>
        public static async Task<BeatmapSet> TryResolveBeatmapId(string bmId)
        {
            try
            {
                SearchResultSet data = await Search(bmId);

                // look for the diff id within the set we want
                return data.Results.First(b => b.Difficulties.FirstOrDefault(d => d.Id == bmId) != null);
            }
            catch { return null; }
        }

        /// <summary>
        /// Gets raw data using preloaded cookies.
        /// </summary>
        private static async Task<string> GetRawWithCookies(string uri)
        {
            using (var handler = new HttpClientHandler() { CookieContainer = Cookies })
            using (var client = new HttpClient(handler))
            {
                var res = await client.GetStringAsync(uri);
                return res;
            }
        }
    }
}
