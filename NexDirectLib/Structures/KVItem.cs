using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NexDirectLib.Structures
{
    // Key/Value pair items, for utility
    public class KVItem<T>
    {
        public string Key { get; set; }
        public T Value { get; set; }
        public KVItem(string k, T v)
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
