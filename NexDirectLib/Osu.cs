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

namespace NexDirectLib {
    using static Structures;

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
            await Task.Delay(150);
            if (DownloadManager.Downloads.Any(d => d.Set.Id == set.Id)) return; // check for if already d/l'ing overlaps

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

                if (str.Contains("You have specified an incorrect")) throw new InvalidPasswordException();

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
                else { Cookies = cookies; }
            }
        }

        public static async Task<string> SerializeCookies(CookieContainer cookies)
        {
            var cookieStore = new StringDictionary(); // make it serializable
            foreach (Cookie c in cookies.GetCookies(new Uri("http://osu.ppy.sh")))
            {
                if (!cookieStore.ContainsKey(c.Name)) // there are some duplicates
                {
                    cookieStore.Add(c.Name, c.Value);
                }
            }
            return await Task.Factory.StartNew(() => JsonConvert.SerializeObject(cookieStore));
        }

        public static async Task<CookieContainer> DeserializeCookies(string _dcookies)
        {
            var _cookies = new StringDictionary();
            var _dscookies = await Task.Factory.StartNew(() => JsonConvert.DeserializeObject(_dcookies));
            foreach (var kv in (JArray)(_dscookies))
            {
                _cookies.Add(kv["Key"].ToString(), kv["Value"].ToString());
            }

            var cookies = new CookieContainer();
            var osuUri = new Uri("http://osu.ppy.sh");
            foreach (DictionaryEntry c in _cookies)
            {
                cookies.Add(osuUri, new Cookie(c.Key.ToString(), c.Value.ToString()));
            }
            return cookies;
        }

        /// <summary>
        /// Searches the official beatmap listing for beatmaps.
        /// </summary>
        public static async Task<IEnumerable<BeatmapSet>> Search(string query, string sRankedParam, string mModeParam)
        {
            if (sRankedParam == "0,-1,-2")
            {
                throw new SearchNotSupportedException();
            }

            // Standardize the bloodcat stuff to osu! query param
            if (sRankedParam == "1,2") sRankedParam = "0";
            else if (sRankedParam == "3") sRankedParam = "11";
            else sRankedParam = "4";

            if (mModeParam == null) mModeParam = "-1"; // modes are all g except for "All"


            // Search time. Need to use cookies.
            // Same as bloodcat, construct QS
            var qs = HttpUtility.ParseQueryString(string.Empty);
            qs["q"] = query;
            qs["m"] = mModeParam;
            qs["r"] = sRankedParam;

            string rawData = await GetRawWithCookies("https://osu.ppy.sh/p/beatmaplist?" + qs.ToString());

            // Parse.
            var htmlDoc = new HtmlAgilityPack.HtmlDocument();
            htmlDoc.OptionUseIdAttribute = true;
            htmlDoc.LoadHtml(rawData);
            HtmlAgilityPack.HtmlNodeCollection beatmapNodes = htmlDoc.DocumentNode.SelectNodes("//div[@class='beatmapListing']/div[@class='beatmap']");
            if (beatmapNodes == null) return new List<BeatmapSet>(); // empty
            return beatmapNodes.Select(b => {
                var difficulties = new Dictionary<string, string>();
                try
                {
                    int i = 1;
                    foreach (var d in b.SelectNodes("div[@class='left-aligned']/div[starts-with(@class, 'difficulties')]/div"))
                    {
                        string _d = d.Attributes["class"].Value.Replace("diffIcon ", "");

                        if (_d.Contains("-t")) _d = "1"; // taiko
                        else if (_d.Contains("-f")) _d = "2"; // ctb
                        else if (_d.Contains("-m")) _d = "3"; // mania
                        else _d = "0"; // standard

                        difficulties.Add(i.ToString(), _d);
                        i++;
                    }
                } catch { } // rip

                // we can only base this off that green/red bar, lol
                string rankStatus;
                if (b.SelectSingleNode("div[@class='right-aligned']/div[@class='rating']") != null)
                {
                    rankStatus = "Ranked/Approved/Qualified";
                }
                else
                {
                    rankStatus = "Pending/Graveyard";
                }

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
            catch
            {
                return "<Unknown>";
            }
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
                if (response.StatusCode != HttpStatusCode.Redirect) throw new IllegalDownloadException(); // Check.
                if (response.Headers.Location.AbsoluteUri.Contains("/ucp.php?mode=login")) throw new CookiesExpiredException(); // Redirected to login.
            }
        }

        /// <summary>
        /// Checks the DMCA of a map, then prepares a download object for it.
        /// </summary>
        public static async Task<BeatmapDownload> PrepareDownloadSet(BeatmapSet set)
        {
            await CheckIllegal(set);
            var download = new BeatmapDownload(set, new Uri($"https://osu.ppy.sh/d/{set.Id}"));
            download.Client.Headers.Add(HttpRequestHeader.Cookie, Cookies.GetCookieHeader(new Uri("http://osu.ppy.sh"))); // use cookie auth
            return download;
        }

        public class IllegalDownloadException : Exception { }
        public class CookiesExpiredException : Exception { }

        /// <summary>
        /// Tries to resolve a beatmap set's ID to an object.
        /// </summary>
        public static async Task<BeatmapSet> TryResolveSetId(string setId)
        {
            try
            {
                string rawData = await GetRawWithCookies($"https://osu.ppy.sh/s/{setId}");
                if (rawData.Contains("looking for was not found")) return null;

                var htmlDoc = new HtmlAgilityPack.HtmlDocument();
                htmlDoc.OptionUseIdAttribute = true;
                htmlDoc.LoadHtml(rawData);
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
