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

namespace NexDirectLib
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
            _formData.Add("redirect", "index.php");
            _formData.Add("sid", "");
            _formData.Add("login", "Login");
            var formData = new FormUrlEncodedContent(_formData);

            using (var handler = new HttpClientHandler() { UseCookies = true, CookieContainer = new CookieContainer() })
            using (var client = new HttpClient(handler))
            {
                var response = await client.PostAsync("https://osu.ppy.sh/forum/ucp.php?mode=login", formData);
                response.EnsureSuccessStatusCode();
                string str = await response.Content.ReadAsStringAsync();

                if (str.Contains("You have specified an incorrect"))
                    throw new InvalidPasswordException();

                return handler.CookieContainer;
            }
        }

        public class InvalidPasswordException : Exception { }

        /// <summary>
        /// Checks if persisted cookies are still working and if so, continue to use them in here.
        /// </summary>
        public static async Task CheckLoginCookie(CookieContainer cookies, string username, string password)
        {
            using (var handler = new HttpClientHandler() { CookieContainer = cookies })
            using (var client = new HttpClient(handler))
            {
                var response = await client.GetAsync("https://osu.ppy.sh/forum/ucp.php");
                response.EnsureSuccessStatusCode();
                string str = await response.Content.ReadAsStringAsync();

                if (str.Contains("Please login in order to access"))
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

        public static async Task<string> SerializeCookies(CookieContainer cookies)
        {
            var cookieStore = new StringDictionary(); // make it serializable
            foreach (Cookie c in cookies.GetCookies(new Uri("http://osu.ppy.sh")))
            {
                if (!cookieStore.ContainsKey(c.Name)) // there are some duplicates
                    cookieStore.Add(c.Name, c.Value);
            }
            return await Task.Factory.StartNew(() => JsonConvert.SerializeObject(cookieStore));
        }

        public static async Task<CookieContainer> DeserializeCookies(string _dcookies)
        {
            var _cookies = new StringDictionary();
            var _dscookies = await Task.Factory.StartNew(() => JsonConvert.DeserializeObject(_dcookies));
            foreach (var kv in (JArray)(_dscookies))
                _cookies.Add(kv["Key"].ToString(), kv["Value"].ToString());

            var cookies = new CookieContainer();
            var osuUri = new Uri("http://osu.ppy.sh");
            foreach (DictionaryEntry c in _cookies)
                cookies.Add(osuUri, new Cookie(c.Key.ToString(), c.Value.ToString()));

            return cookies;
        }

        public static Task<SearchResultSet> Search(bool useNewSite, string query, SearchFilters.OsuRankStatus rankedFilter, SearchFilters.OsuModes modeFilter, int page = 1)
            => useNewSite ? Search_New(query, rankedFilter, modeFilter, page) : Search_Old(query, rankedFilter, modeFilter, page);

        /// <summary>
        /// Currently actually doesn't require login. Who knows, this may change in the future. - needs auth tho.
        /// </summary>
        public static async Task<SearchResultSet> Search_New(string query, SearchFilters.OsuRankStatus rankedFilter, SearchFilters.OsuModes modeFilter, int page = 1)
        {
            // ranked filter = s
            // mode filter = m

            string sParam;
            // Ranked filter into website param
            if (rankedFilter == SearchFilters.OsuRankStatus.RankedAndApproved)
                sParam = "0";
            else if (rankedFilter == SearchFilters.OsuRankStatus.Approved)
                sParam = "1";
            else if (rankedFilter == SearchFilters.OsuRankStatus.Qualified)
                throw new SearchNotSupportedException(); // uhh the site doesnt have it??/ sParam = "11";
            else if (rankedFilter == SearchFilters.OsuRankStatus.Loved)
                sParam = "8";
            else
                sParam = "7"; // "Any"

            var qs = HttpUtility.ParseQueryString(string.Empty);
            qs["q"] = query;
            qs["s"] = sParam;
            if (modeFilter != SearchFilters.OsuModes.All)
                qs["m"] = ((int)modeFilter).ToString();
            if (page > 1)
                qs["page"] = page.ToString();

            var data = await Web.GetJson<JArray>("https://new.ppy.sh/beatmapsets/search?" + qs.ToString());
            var standardized = data.Select(b => StandardizeToSetStruct((JObject)b));

            return new SearchResultSet(standardized, (standardized.Count() > 0));
        }

        /// <summary>
        /// Standard set data to a set (new data)
        /// </summary>
        public static BeatmapSet StandardizeToSetStruct(JObject jsonData)
        {
            // (similar to bloodcat now)
            var difficulties = new Dictionary<string, string>();
            foreach (var d in (JArray)jsonData["beatmaps"])
            {
                string mode = d["mode"].ToString();
                if (mode == "taiko")
                    mode = "1";
                else if (mode == "fruits")
                    mode = "2";
                else if (mode == "mania")
                    mode = "3";
                else
                    mode = "0";

                difficulties.Add(d["version"].ToString(), mode); // diffname: diffmode_id
            }

            return new BeatmapSet(
                jsonData["id"].ToString(), jsonData["artist"].ToString(),
                jsonData["title"].ToString(), jsonData["creator"].ToString(),
                ((RankingStatus)int.Parse(jsonData["ranked"].ToString())).ToString(),
                difficulties, jsonData, false);
        }

        /// <summary>
        /// Searches the official beatmap listing for beatmaps. - current (old) website
        /// </summary>
        public static async Task<SearchResultSet> Search_Old(string query, SearchFilters.OsuRankStatus rankedFilter, SearchFilters.OsuModes modeFilter, int page = 1)
        {
            // ranked filter = r
            // mode filter = m

            string rParam;
            // Ranked filter into website param
            if (rankedFilter == SearchFilters.OsuRankStatus.RankedAndApproved)
                rParam = "0";
            else if (rankedFilter == SearchFilters.OsuRankStatus.Approved)
                rParam = "6";
            else if (rankedFilter == SearchFilters.OsuRankStatus.Qualified)
                rParam = "11";
            else if (rankedFilter == SearchFilters.OsuRankStatus.Loved)
                rParam = "12";
            else
                rParam = "4";

            // Search time. Need to use cookies.
            // Same as bloodcat, construct QS
            var qs = HttpUtility.ParseQueryString(string.Empty);
            qs["q"] = query;
            qs["m"] = ((int)modeFilter).ToString();
            qs["r"] = rParam;
            if (page > 1)
                qs["page"] = page.ToString();

            string rawData = await GetRawWithCookies("https://osu.ppy.sh/p/beatmaplist?" + qs.ToString());

            // Check if still logged in
            if (rawData.Contains("Please enter your credentials"))
                throw new CookiesExpiredException();

            // Parse.
            var htmlDoc = new HtmlAgilityPack.HtmlDocument();
            htmlDoc.OptionUseIdAttribute = true;
            htmlDoc.LoadHtml(rawData);
            HtmlAgilityPack.HtmlNodeCollection beatmapNodes = htmlDoc.DocumentNode.SelectNodes("//div[@class='beatmapListing']/div[@class='beatmap']");
            if (beatmapNodes == null)
                return new SearchResultSet(new List<BeatmapSet>(), false); // empty

            var sets = beatmapNodes.Select(b => {
                var difficulties = new Dictionary<string, string>();
                try
                {
                    int i = 1;
                    foreach (var d in b.SelectNodes("div[@class='left-aligned']/div[starts-with(@class, 'difficulties')]/div"))
                    {
                        string _d = d.Attributes["class"].Value.Replace("diffIcon ", "");

                        if (_d.Contains("-t"))
                            _d = "1"; // taiko
                        else if (_d.Contains("-f"))
                            _d = "2"; // ctb
                        else if (_d.Contains("-m"))
                            _d = "3"; // mania
                        else
                            _d = "0"; // standard

                        difficulties.Add(i.ToString(), _d);
                        i++;
                    }
                }
                catch { } // rip

                // we can only base this off that green/red bar, lol -- or the search filter
                string rankStatus;
                if (rankedFilter == SearchFilters.OsuRankStatus.Loved)
                    rankStatus = "Loved";
                else if (rankedFilter == SearchFilters.OsuRankStatus.Approved)
                    rankStatus = "Approved";
                else if (rankedFilter == SearchFilters.OsuRankStatus.Qualified)
                    rankStatus = "Qualified";
                else if (rankedFilter == SearchFilters.OsuRankStatus.RankedAndApproved)
                    rankStatus = "Ranked/Approved";
                else if (b.SelectSingleNode("div[@class='right-aligned']/div[@class='rating']") != null)
                    rankStatus = "Ranked/Apprv./Quali./Loved";
                else
                    rankStatus = "Pending/Graveyard";

                return new BeatmapSet(
                    b.Id,
                    TryGetNodeText(b, "div[@class='maintext']/span[@class='artist']"),
                    TryGetNodeText(b, "div[@class='maintext']/a[@class='title']"),
                    TryGetNodeText(b, "div[@class='left-aligned']/div[1]/a"),
                    rankStatus,
                    difficulties,
                    null
                );
            });

            bool canLoadMore = htmlDoc.DocumentNode.SelectNodes("//div[@class='pagination']")
                                       .Descendants("a")
                                       .Any(d => d.InnerText == "Next");

            return new SearchResultSet(sets, canLoadMore);
        }

        public class SearchNotSupportedException : Exception { }

        /// <summary>
        /// Tries to get text from node and escapes it from HTML.
        /// </summary>
        private static string TryGetNodeText(HtmlAgilityPack.HtmlNode node, string xpath)
        {
            try
            {
                string txt = node.SelectSingleNode(xpath).InnerText;
                return HttpUtility.HtmlDecode(txt);
            }
            catch { return "<Unknown>"; }
        }

        /// <summary>
        /// Checks headers of a beatmap set download to see if it has been taken down by DMCA.
        /// </summary>
        public static async Task CheckIllegal(BeatmapSet set)
        {
            // Get status code - 302 REDIRECT = redirected to the real download OR redirected to login, 200 OK = illegal page!
            using (var handler = new HttpClientHandler() { CookieContainer = Cookies, AllowAutoRedirect = false })
            using (var client = new HttpClient(handler))
            {
                // HEAD request for the status code
                HttpResponseMessage response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, $"https://osu.ppy.sh/d/{set.Id}"));

                if (response.StatusCode != HttpStatusCode.Redirect)
                    throw new IllegalDownloadException(); // Check.
                if (response.Headers.Location.AbsoluteUri.Contains("/ucp.php?mode=login"))
                    throw new CookiesExpiredException(); // Redirected to login.
            }
        }

        /// <summary>
        /// Checks the DMCA of a map, then prepares a download object for it.
        /// </summary>
        public static async Task<BeatmapDownload> PrepareDownloadSet(BeatmapSet set, bool preferNoVid = false)
        {
            await CheckIllegal(set);
            var download = new BeatmapDownload(set, new Uri($"https://osu.ppy.sh/d/{set.Id}" + (preferNoVid ? "n" : "")));
            download.Client.Headers.Add(HttpRequestHeader.Cookie, Cookies.GetCookieHeader(new Uri("http://osu.ppy.sh"))); // use cookie auth
            return download;
        }

        public class IllegalDownloadException : Exception { }
        public class CookiesExpiredException : Exception { }

        /// <summary>
        /// Tries to resolve a beatmap set's ID to an object.
        /// </summary>
        public static Task<BeatmapSet> TryResolveSetId(string setId)
            => resolveThing($"https://osu.ppy.sh/s/{setId}");

        /// <summary>
        /// Tries to resolve a beatmap's ID to an object.
        /// </summary>
        public static Task<BeatmapSet> TryResolveBeatmapId(string bmId)
            => resolveThing($"https://osu.ppy.sh/b/{bmId}");

        private static Regex setIdRegex = new Regex(@"thumb\/(\d+)l\.jpg");
        /// <summary>
        /// Resolve... thing. (beatmap page.)
        /// </summary>
        public static async Task<BeatmapSet> resolveThing(string url)
        {
            try
            {
                string rawData = await Web.GetContent(url); // no cookies needed for this in fact
                if (rawData.Contains("looking for was not found"))
                    return null;

                var htmlDoc = new HtmlAgilityPack.HtmlDocument();
                htmlDoc.OptionUseIdAttribute = true;
                htmlDoc.LoadHtml(rawData);

                // get the set id
                string setIdInfo = HttpUtility.HtmlDecode(htmlDoc.DocumentNode.SelectSingleNode("//div[@class='posttext']/img[@class='bmt']").Attributes["src"].Value);
                string setId = setIdRegex.Match(setIdInfo).Groups[1].ToString();

                HtmlAgilityPack.HtmlNode infoNode = htmlDoc.DocumentNode.SelectSingleNode("//table[@id='songinfo']");
                return new BeatmapSet(
                    setId,
                    HttpUtility.HtmlDecode(infoNode.SelectSingleNode("tr[1]/td[2]/a").InnerText), // artist
                    HttpUtility.HtmlDecode(infoNode.SelectSingleNode("tr[2]/td[2]/a").InnerText), // title
                    HttpUtility.HtmlDecode(infoNode.SelectSingleNode("tr[3]/td[2]/a").InnerText), // mapper
                    null,
                    new Dictionary<string, string>(),
                    null
                );
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

        /// <summary>
        /// Ranking Status codes (API-conformant)
        /// </summary>
        public enum RankingStatus
        {
            Qualified = 3,
            Approved = 2,
            Ranked = 1,
            Pending = 0,
            WIP = -1,
            Graveyard = -2
        }
    }
}
