using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;

namespace NexDirectLib
{
    public static class Web
    {
        /// <summary>
        /// Downloads content from a webpage and parses it as JSON
        /// </summary>
        public static async Task<T> GetJson<T>(string url, string userAgent)
        {
            using (var client = new HttpClient())
            {
                if (!string.IsNullOrEmpty(userAgent)) client.DefaultRequestHeaders.Add("User-Agent", userAgent);
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string body = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(body);
            }
        }

        public static Task<T> GetJson<T>(string url)
        {
            return GetJson<T>(url, null);
        }
    }
}
