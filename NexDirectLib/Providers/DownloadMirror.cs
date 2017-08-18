using NexDirectLib.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NexDirectLib.Providers
{
    public static class DownloadMirror
    {
        public static BeatmapDownload PrepareDownloadSet(BeatmapSet set, string mirror)
        {
            BeatmapDownload download;
            download = new BeatmapDownload(set, new Uri(mirror.Replace("%s", set.Id)));
            return download;
        }
    }
}
