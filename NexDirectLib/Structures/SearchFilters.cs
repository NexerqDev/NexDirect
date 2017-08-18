using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NexDirectLib.Structures
{
    public class SearchFilters
    {
        public enum OsuRankStatus
        {
            Loved = 4,
            Qualified = 3,
            Approved = 2,
            Ranked = 1,
            Pending = 0,
            WIP = -1,
            Graveyard = -2,

            // custom ones
            All,
            RankedAndApproved,
            Unranked
        }

        public enum OsuModes
        {
            Osu = 0,
            Taiko = 1,
            CtB = 2,
            Mania = 3,

            // custom ones
            All = -1
        }

        public enum BloodcatIdFilter
        {
            BySetId,
            ByBeatmapId,
            ByMapperUserId,
            Normal
        }
    }
}
