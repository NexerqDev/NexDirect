using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace NexDirectLib
{
    using Structures;

    public static class Bloodcat
    {
        public const int MAX_LOAD_RESULTS = 61; // the max one result will have. if have at least this much then there is more.

        /// <summary>
        /// Searches Bloodcat for a string with some params
        /// </summary>
        public static async Task<SearchResultSet> Search(string query, string sRankedParam, string mModeParam, string cNumbersParam, int page = 1)
        {
            // build query string -- https://stackoverflow.com/questions/17096201/build-query-string-for-system-net-httpclient-get
            var qs = HttpUtility.ParseQueryString(string.Empty);
            qs["mod"] = "json";
            qs["q"] = query;
            if (sRankedParam != null)
                qs["s"] = sRankedParam;
            if (mModeParam != null)
                qs["m"] = mModeParam;
            if (cNumbersParam != null)
                qs["c"] = cNumbersParam;
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
            var difficulties = new Dictionary<string, string>();
            foreach (var d in (JArray)bloodcatData["beatmaps"])
                difficulties.Add(d["name"].ToString(), d["mode"].ToString());

            return new BeatmapSet(
                bloodcatData["id"].ToString(), bloodcatData["artist"].ToString(),
                bloodcatData["title"].ToString(), bloodcatData["creator"].ToString(),
                ((Osu.RankingStatus)int.Parse(bloodcatData["status"].ToString())).ToString(),
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
                IEnumerable<BeatmapSet> results = (await Search(beatmapId, null, null, "b")).Results;
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
                IEnumerable<BeatmapSet> results = (await Search(id, null, null, "s")).Results;
                BeatmapSet map = results.FirstOrDefault(r => r.Id == id);
                return map;
            }
            catch { return null; }
        }

        /// <summary>
        /// Prepares a download object for a set from bloodcat or mirror if defined
        /// </summary>
        public static BeatmapDownload PrepareDownloadSet(BeatmapSet set, string mirror)
        {
            Uri downloadUri;
            if (string.IsNullOrEmpty(mirror))
                downloadUri = new Uri("http://bloodcat.com/osu/s/" + set.Id);
            else
                downloadUri = new Uri(mirror.Replace("%s", set.Id));

            return new BeatmapDownload(set, downloadUri);
        }
    }
}
