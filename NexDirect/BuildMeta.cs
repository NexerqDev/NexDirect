using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NexDirect
{
    public static class BuildMeta
    {
        public static DateTime BuildDateTime;
        public static string BuildBranch;
        public static string BuildCommit;
        public static string BuildProfile;

        public static bool IsDebug => BuildProfile == "Debug";

        static BuildMeta()
        {
            string data = Properties.Resources.BuildData;
            string[] lines = data.Split('\n').Select(l => l.Trim()).ToArray();
            if (lines.Length < 4)
                return; // corrupted

            for (int i = 0; i < 4; i++)
            {
                // line 0: date yyyy-mm-dd time hh:mm:ss
                BuildDateTime = DateTime.Parse(lines[0]);

                // line 1: branch
                BuildBranch = lines[1];

                // line 2: full commit hash (lets make it short 7)
                BuildCommit = lines[2].Substring(0, 7);

                // line 3: debug/release/etc
                BuildProfile = lines[3];
            }
        }
    }
}
