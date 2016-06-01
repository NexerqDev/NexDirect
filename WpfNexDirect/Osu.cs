using System.IO;
using System.Net.Http;
using NAudio.Wave;

namespace NexDirect
{
    public static class Osu
    {
        public static async void PlayPreviewAudio(Bloodcat.BeatmapSet set, WaveOut waveOut)
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
    }
}
