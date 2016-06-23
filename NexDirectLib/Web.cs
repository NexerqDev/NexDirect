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
        public static async Task<T> GetJson<T>(string url)
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string body = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(body);
            }
        }
    }
}
