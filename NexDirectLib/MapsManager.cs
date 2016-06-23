using System.IO;

namespace NexDirectLib
{
    public static class MapsManager
    {
        public static string[] Maps;

        public static void Reload(string songsFolder)
        {
            Maps = Directory.GetDirectories(songsFolder);
        }
    }
}
