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

        public static async Task DownloadSet(Structures.BeatmapDownload download)
        {
            await download.DownloadClient.DownloadFileTaskAsync(download.DownloadUri, download.TempDownloadPath);
        }

        public static void CancelDownload(Structures.BeatmapDownload statusObj)
        {
            statusObj.DownloadCancelled = true;
            statusObj.DownloadClient.CancelAsync();
        }
    }
}
