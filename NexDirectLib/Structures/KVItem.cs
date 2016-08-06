using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NexDirectLib.Structures
{
    // Key/Value pair items, for utility
    public class KVItem
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public KVItem(string k, string v)
        {
            Key = k;
            Value = v;
        }

        public override string ToString()
        {
            return Key;
        }
    }
}
