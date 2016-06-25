using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace NexDirectLib
{
    public static class Bloodcat
    {
        /// <summary>
        /// Searches Bloodcat for a string with some params
        /// </summary>
        public static async Task<IEnumerable<Structures.BeatmapSet>> Search(string query, string sRankedParam, string mModeParam, string cNumbersParam)
        {
            // build query string -- https://stackoverflow.com/questions/17096201/build-query-string-for-system-net-httpclient-get
            var qs = HttpUtility.ParseQueryString(string.Empty);
            qs["mod"] = "json";
            qs["q"] = query;
            if (sRankedParam != null) qs["s"] = sRankedParam;
            if (mModeParam != null) qs["m"] = mModeParam;
            if (cNumbersParam != null) qs["c"] = cNumbersParam;

            var data = await Web.GetJson<JArray>("http://bloodcat.com/osu/?" + qs.ToString());
            return data.Select(b => StandardizeToSetStruct((JObject)b));
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
        public static Structures.BeatmapSet StandardizeToSetStruct(JObject bloodcatData)
        {
            var difficulties = new Dictionary<string, string>();
            foreach (var d in (JArray)bloodcatData["beatmaps"])
            {
                difficulties.Add(d["name"].ToString(), d["mode"].ToString());
            }

            return new Structures.BeatmapSet(
                bloodcatData["id"].ToString(), bloodcatData["artist"].ToString(),
                bloodcatData["title"].ToString(), bloodcatData["creator"].ToString(),
                ((Osu.RankingStatus)int.Parse(bloodcatData["status"].ToString())).ToString(),
                difficulties, bloodcatData
            );
        }

        /// <summary>
        /// Shorthand to resolve a set ID to a BeatmapSet object
        /// </summary>
        public static async Task<Structures.BeatmapSet> ResolveSetId(string beatmapSetId)
        {
            IEnumerable<Structures.BeatmapSet> results = await Search(beatmapSetId, null, null, "s");
            Structures.BeatmapSet map = results.FirstOrDefault(r => r.Id == beatmapSetId);
            return map;
        }

        /// <summary>
        /// Prepares a download object for a set from bloodcat or mirror if defined
        /// </summary>
        public static Structures.BeatmapDownload PrepareDownloadSet(Structures.BeatmapSet set, string mirror)
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

            return new Structures.BeatmapDownload(set, downloadUri);
        }
    }
}
