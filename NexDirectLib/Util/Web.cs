using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace NexDirectLib.Util
{
    public static class Web
    {
        /// <summary>
        /// Just get content
        /// </summary>
        /// <returns></returns>
        public static async Task<string> GetContent(string url, string userAgent = null)
        {
            using (var client = new HttpClient())
            {
                if (string.IsNullOrEmpty(userAgent))
                    userAgent = "NexDirect-Lib/0.1.0";
                client.DefaultRequestHeaders.Add("User-Agent", userAgent);

                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string body = await response.Content.ReadAsStringAsync();
                return body;
            }
        }

        /// <summary>
        /// Downloads content from a webpage and parses it as JSON
        /// </summary>
        public static async Task<T> GetJson<T>(string url, string userAgent = null)
        {
            string content = await GetContent(url, userAgent);
            return JsonConvert.DeserializeObject<T>(content);
        }
    }
}
