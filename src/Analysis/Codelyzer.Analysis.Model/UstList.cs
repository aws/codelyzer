using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Codelyzer.Analysis.Model
{
    [JsonArray]
    public class UstList<T> : List<T>
    {
        public UstList()
        {
        }

        public UstList(IEnumerable<T> collection) : base(collection)
        {
        }

        public UstList(int capacity) : base(capacity)
        {
        }

        public override bool Equals(object obj)
        {
            if (obj is UstList<T>)
            {
                return Equals((UstList<T>)obj);
            }
            else return false;
        }

        public bool Equals(UstList<T> compareList)
        {
            if (compareList == null) return false;
            return compareList.SequenceEqual(this);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this);
        }
    }
}
