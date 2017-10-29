using NAudio.Wave;
using System;
using System.IO;

namespace NexDirectLib.Management
{
    public static class AudioManager
    {
        public static WaveOut PlayWavBytes(byte[] wavBytes, float? volume = null, Action callback = null)
        {
            MemoryStream ms = new MemoryStream(wavBytes);
            WaveFileReader r = new WaveFileReader(ms);

            return PlayReader(r, volume, () =>
            {
                ms.Dispose();
                r.Dispose();
                callback?.Invoke();
            });
        }

        public static WaveOut PlayMp3Stream(Stream stream, float? volume = null, Action callback = null)
        {
            Mp3FileReader r = new Mp3FileReader(stream);

            return PlayReader(r, volume, () =>
            {
                r.Dispose();
                callback?.Invoke();
            });
        }

        public static WaveOut PlayReader(WaveStream ws, float? volume = null, Action callback = null)
        {
            WaveOut player = new WaveOut
            {
                Volume = (volume == null) ? 1.0f : (float)volume
            };

            player.Init(ws);
            player.Play();

            player.PlaybackStopped += (o, ex) =>
            {
                player.Dispose();
                callback?.Invoke();
            };

            return player;
        }
    }
}
