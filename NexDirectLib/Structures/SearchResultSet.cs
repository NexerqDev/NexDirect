using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NexDirectLib.Structures
{
    /// <summary>
    /// Represents a search result.
    /// </summary>
    public class SearchResultSet
    {
        public IEnumerable<BeatmapSet> Results { get; set; }
        public bool CanLoadMore { get; set; }
        
        public SearchResultSet(IEnumerable<BeatmapSet> res, bool loadMore)
        {
            Results = res;
            CanLoadMore = loadMore;
        }
    }
}
