using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace NexDirectLib.Providers
{
    using Structures;
    using System.Net;
    using System.Net.Http;
    using Util;

    public static class Bloodcat
    {
        public const int MAX_LOAD_RESULTS = 61; // the max one result will have. if have at least this much then there is more.

        public static CookieContainer Cookies = new CookieContainer();

        /// <summary>
        /// Searches Bloodcat for a string with some params
        /// </summary>
        public static async Task<SearchResultSet> Search(string query, SearchFilters.OsuRankStatus rankedFilter, SearchFilters.OsuModes modeFilter, SearchFilters.BloodcatIdFilter? bloodcatNumbersFilter, int page = 1)
        {
            // rank status filter = s
            // mode filter = m
            // when there are numebrs only special filter = c

            string sParam;
            if (rankedFilter == SearchFilters.OsuRankStatus.RankedAndApproved)
                sParam = "1,2";
            else if (rankedFilter == SearchFilters.OsuRankStatus.All)
                sParam = ""; // none needed for all
            else if (rankedFilter == SearchFilters.OsuRankStatus.Unranked)
                sParam = "0";
            else
                sParam = ((int)rankedFilter).ToString();

            // build query string -- https://stackoverflow.com/questions/17096201/build-query-string-for-system-net-httpclient-get
            var qs = HttpUtility.ParseQueryString(string.Empty);
            qs["mod"] = "json";
            qs["q"] = query;
            qs["s"] = sParam;
            qs["m"] = modeFilter == SearchFilters.OsuModes.All ? "" : ((int)modeFilter).ToString();
            if (bloodcatNumbersFilter != null)
                qs["c"] = ((int)bloodcatNumbersFilter).ToString();
            qs["p"] = page.ToString();

            var data = await Web.GetJson<JArray>("http://bloodcat.com/osu/?" + qs.ToString());
            var standardized = data.Select(b => StandardizeToSetStruct((JObject)b));
            int count = standardized.Count();

            return new SearchResultSet(standardized, (count >= MAX_LOAD_RESULTS));
        }

        /// <summary>
        /// Retrieves front page popular data from Bloodcat
        /// </summary>
        public static async Task<IEnumerable<BeatmapSet>> Popular()
        {
            var data = await Web.GetJson<JArray>("http://bloodcat.com/osu/popular.php?mod=json");
            return data.Select(b => StandardizeToSetStruct((JObject)b));
        }

        /// <summary>
        /// Standardizes Bloodcat JSON data to our central structure
        /// </summary>
        public static BeatmapSet StandardizeToSetStruct(JObject bloodcatData)
        {
            var difficulties = new List<BeatmapSet.Difficulty>();
            foreach (var d in (JArray)bloodcatData["beatmaps"])
                difficulties.Add(new BeatmapSet.Difficulty(d["id"].ToString(), d["name"].ToString(), d["mode"].ToString()));

            return new BeatmapSet(
                typeof(Bloodcat),
                bloodcatData["id"].ToString(), bloodcatData["artist"].ToString(),
                bloodcatData["title"].ToString(), bloodcatData["creator"].ToString(),
                ((SearchFilters.OsuRankStatus)int.Parse(bloodcatData["status"].ToString())).ToString(),
                difficulties, bloodcatData
            );
        }

        /// <summary>
        /// Shorthand to try and resolve a beatmap ID to a BeatmapSet object
        /// </summary>
        public static async Task<BeatmapSet> TryBeatmapId(string beatmapId)
        {
            try
            {
                IEnumerable<BeatmapSet> results = (await Search(beatmapId, SearchFilters.OsuRankStatus.All, SearchFilters.OsuModes.All, SearchFilters.BloodcatIdFilter.ByBeatmapId)).Results;
                if (results.Count() == 0)
                    return null;
                return results.First();
            }
            catch { return null; }
        }

        /// <summary>
        /// Shorthand to try and resolve a set ID to a BeatmapSet object
        /// </summary>
        public static async Task<BeatmapSet> TryResolveSetId(string id)
        {
            try
            {
                IEnumerable<BeatmapSet> results = (await Search(id, SearchFilters.OsuRankStatus.All, SearchFilters.OsuModes.All, SearchFilters.BloodcatIdFilter.BySetId)).Results;
                BeatmapSet map = results.FirstOrDefault(r => r.Id == id);
                return map;
            }
            catch { return null; }
        }

        /// <summary>
        /// Checks for CAPTCHA, and throws if so
        /// </summary>
        public static async Task CheckCaptcha(BeatmapSet set)
        {
            // 401 UNAUTHORIZED = captcha
            using (var handler = new HttpClientHandler() { CookieContainer = Cookies, AllowAutoRedirect = false })
            using (var client = new HttpClient(handler))
            {
                // HEAD request for the status code
                HttpResponseMessage response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, "http://bloodcat.com/osu/s/" + set.Id));
                
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    throw new BloodcatCaptchaException();
            }
        }

        /// <summary>
        /// Prepares a download object for a set from bloodcat
        /// </summary>
        public static async Task<BeatmapDownload> PrepareDownloadSet(BeatmapSet set)
        {
            BeatmapDownload download;

            Uri downloadUri;

            await CheckCaptcha(set);
            downloadUri = new Uri("http://bloodcat.com/osu/s/" + set.Id);
            download = new BeatmapDownload(set, downloadUri, Cookies);

            return download;
        }

        public class BloodcatCaptchaException : Exception { }
    }
}
