using NAudio.Wave;
using System.IO;

namespace NexDirectLib
{
    public static class AudioManager
    {
        public static bool DownloadCompleteSoundEnabled = true;
        public static WaveOut PreviewOut = new WaveOut(); // For playing beatmap previews and stuff
        public static WaveOut NotificationOut = new WaveOut(); // Specific interface for playing doong, so if previews are playing it doesnt take over

        public static void Init(UnmanagedMemoryStream notificationSound)
        {
            var reader = new WaveFileReader(notificationSound);
            NotificationOut.Init(reader);
            NotificationOut.PlaybackStopped += (o, e) => reader.Position = 0;
        }

        public static void OnDownloadComplete()
        {
            if (!DownloadCompleteSoundEnabled)
                return;
            NotificationOut.Play();
        }

        public static void ForceStopPreview() => PreviewOut.Stop();
    }
}
