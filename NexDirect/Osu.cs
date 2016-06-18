using System.IO;
using System.Net.Http;
using NAudio.Wave;
using System.Net;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows;
using System.Collections.Specialized;
using System.Collections;
using System;
using Newtonsoft.Json;
using System.Web;
using System.Collections.ObjectModel;

namespace NexDirect
{
    public static class Osu
    {
        /// <summary>
        /// Plays preview audio of a specific beatmap set to the waveout interface
        /// </summary>
        public static async void PlayPreviewAudio(Structures.BeatmapSet set, WaveOut waveOut)
        {
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
        /// Logs in to the official osu! website with a given username/password and returns cookies in a serializable format.
        /// </summary>
        public static async Task<StringDictionary> LoginAndGetCookie(string username, string password)
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

                if (str.Contains("You have specified an incorrect")) throw new Exception("Invalid username/password");

                var cookieStore = new StringDictionary(); // "serialized" format
                foreach (Cookie c in handler.CookieContainer.GetCookies(new Uri("http://osu.ppy.sh")))
                {
                    if (!cookieStore.ContainsKey(c.Name)) // there are some duplicates
                    {
                        cookieStore.Add(c.Name, c.Value);
                    }
                }
                return cookieStore;
            }
        }

        /// <summary>
        /// Checks if cookies are still working.
        /// </summary>
        public static async Task<CookieContainer> CheckLoginCookie(MainWindow _mw, StringDictionary _cookies)
        {
            var cookies = new CookieContainer();
            var osuUri = new Uri("http://osu.ppy.sh");
            foreach (DictionaryEntry c in _cookies)
            {
                cookies.Add(osuUri, new Cookie(c.Key.ToString(), c.Value.ToString()));
            }

            using (var handler = new HttpClientHandler() { CookieContainer = cookies })
            using (var client = new HttpClient(handler))
            {
                var response = await client.GetAsync("https://osu.ppy.sh/forum/ucp.php");
                response.EnsureSuccessStatusCode();
                string str = await response.Content.ReadAsStringAsync();

                if (str.Contains("Please login in order to access"))
                {
                    // try with creds to renew login
                    try
                    {
                        StringDictionary newCookies = await LoginAndGetCookie(_mw.officialOsuUsername, _mw.officialOsuPassword);
                        // new cookies!
                        _mw.officialOsuCookies = await Task.Factory.StartNew(() => JsonConvert.SerializeObject(newCookies));
                        Properties.Settings.Default.officialOsuCookies = _mw.officialOsuCookies;
                        Properties.Settings.Default.Save();
                        return cookies;
                    }
                    catch
                    {
                        return null; // ok creds wrong
                    }
                }
                return cookies;
            }
        }

        /// <summary>
        /// Searches the official beatmap listing for beatmaps.
        /// </summary>
        public static async Task<IEnumerable<Structures.BeatmapSet>> Search(MainWindow _mw, string query, string sRankedParam, string mModeParam)
        {
            if (sRankedParam == "0,-1,-2")
            {
                MessageBox.Show("Sorry, this mode of Ranking search is currently not supported via the official osu! servers.");
                return null;
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

            string rawData = await GetRawWithCookies(_mw, "https://osu.ppy.sh/p/beatmaplist?" + qs.ToString());

            // Parse.
            var htmlDoc = new HtmlAgilityPack.HtmlDocument();
            htmlDoc.OptionUseIdAttribute = true;
            htmlDoc.LoadHtml(rawData);
            HtmlAgilityPack.HtmlNodeCollection beatmapNodes = htmlDoc.DocumentNode.SelectNodes("//div[@class='beatmapListing']/div[@class='beatmap']");
            if (beatmapNodes == null) return new List<Structures.BeatmapSet>(); // empty
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

                return new Structures.BeatmapSet(_mw,
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

        /// <summary>
        /// Tries to get text from node.
        /// </summary>
        private static string TryGetNodeText(HtmlAgilityPack.HtmlNode node, string xpath)
        {
            try
            {
                return node.SelectSingleNode(xpath).InnerText;
            }
            catch
            {
                return "<Unknown>";
            }
        }

        /// <summary>
        /// Checks headers of a beatmap set download to see if it has been taken down by DMCA.
        /// </summary>
        public static async Task<bool> CheckIllegal(MainWindow _mw, Structures.BeatmapSet set)
        {
            // Get status code - 302 REDIRECT = redirected to the real download, 200 OK = illegal page!
            using (var handler = new HttpClientHandler() { CookieContainer = _mw.officialCookieJar, AllowAutoRedirect = false })
            using (var client = new HttpClient(handler))
            {
                // HEAD request for the status code
                try
                {
                    HttpResponseMessage response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, $"https://osu.ppy.sh/d/{set.Id}"));
                    return response.StatusCode != HttpStatusCode.Redirect; // Check.
                }
                catch
                {
                    return true;
                }
            }
        }

        /// <summary>
        /// Checks the DMCA of a map, then downloads it.
        /// </summary>
        public static async Task DownloadSet(MainWindow _mw, Structures.BeatmapSet set, ObservableCollection<Structures.BeatmapDownload> downloadProgress, string osuFolder, WaveOut doongPlayer, bool launchOsu)
        {
            if (await CheckIllegal(_mw, set))
            {
                MessageBoxResult bloodcatAsk = MessageBox.Show("Sorry, this map seems like it has been taken down from the official osu! servers due to a DMCA request to them. Would you like to check if a copy off Bloodcat is available, and if so download it?", "NexDirect - Mirror?", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (bloodcatAsk == MessageBoxResult.No) return;

                _mw.DownloadBeatmapSet(set, true);
                return;
            }

            using (var client = new WebClient())
            {
                var download = new Structures.BeatmapDownload(set, client, osuFolder, doongPlayer, launchOsu);
                downloadProgress.Add(download);

                client.Headers.Add(HttpRequestHeader.Cookie, _mw.officialCookieJar.GetCookieHeader(new Uri("http://osu.ppy.sh"))); // use cookie auth
                try { await client.DownloadFileTaskAsync($"https://osu.ppy.sh/d/{set.Id}", download.TempDownloadPath); } // appdomain.etc is a WPF way of getting startup dir... stupid :(
                catch (Exception ex)
                {
                    if (download.DownloadCancelled == true) return;
                    MessageBox.Show($"An error has occured whilst downloading {set.Title} ({set.Mapper}).\n\n{ex.ToString()}");
                }
                finally
                {
                    downloadProgress.Remove(download);
                }
            }
        }

        /// <summary>
        /// Resolves a beatmap set's ID to an object.
        /// </summary>
        public static async Task<Structures.BeatmapSet> ResolveSetId(MainWindow _mw, string setId)
        {
            string rawData = await GetRawWithCookies(_mw, $"https://osu.ppy.sh/s/{setId}");
            if (rawData.Contains("looking for was not found")) return null;

            var htmlDoc = new HtmlAgilityPack.HtmlDocument();
            htmlDoc.OptionUseIdAttribute = true;
            htmlDoc.LoadHtml(rawData);
            HtmlAgilityPack.HtmlNode infoNode = htmlDoc.DocumentNode.SelectSingleNode("//table[@id='songinfo']");
            return new Structures.BeatmapSet(_mw,
                setId,
                infoNode.SelectSingleNode("tr[1]/td[2]/a").InnerText, // artist
                infoNode.SelectSingleNode("tr[2]/td[2]/a").InnerText, // title
                infoNode.SelectSingleNode("tr[3]/td[2]/a").InnerText, // mapper
                null,
                new Dictionary<string, string>(),
                null
            );
        }

        /// <summary>
        /// Gets raw data using preloaded cookies.
        /// </summary>
        private static async Task<string> GetRawWithCookies(MainWindow _mw, string uri)
        {
            using (var handler = new HttpClientHandler() { CookieContainer = _mw.officialCookieJar })
            using (var client = new HttpClient(handler))
            {
                var res = await client.GetStringAsync(uri);
                return res;
            }
        }
    }
}
