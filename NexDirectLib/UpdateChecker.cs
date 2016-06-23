using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System;

namespace NexDirectLib
{
    public static class UpdateChecker
    {
        private const string API_URL = @"https://api.github.com/repos/nicholastay/NexDirect/releases/latest";

        /// <summary>
        /// Checks the GitHub API if there is a new release against the current version provided
        /// </summary>
        public static async Task<Update> Check(string currentVersion)
        {
            try
            {
                var release = await Web.GetJson<JObject>(API_URL, $"NexDirect/${currentVersion}");
                string onlineVersion = release["tag_name"].ToString();
                if (onlineVersion[0] == 'v') onlineVersion = onlineVersion.Substring(1);

                if (currentVersion == onlineVersion) return null;

                string releaseUrl = release["html_url"].ToString();
                DateTime publishedAt = release["published_at"].ToObject<DateTime>();
                return new Update(onlineVersion, releaseUrl, publishedAt);
            }
            catch { return null; } // no biggie on checking
        }

        public class Update
        {
            public string Version { get; set; }
            public string Url { get; set; }
            public DateTime PublishedAt { get; set; }

            public Update(string version, string url, DateTime published)
            {
                Version = version;
                Url = url;
                PublishedAt = published;
            }
        }
    }
}
