using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;

namespace NexDirect
{
    static class Web
    {
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
