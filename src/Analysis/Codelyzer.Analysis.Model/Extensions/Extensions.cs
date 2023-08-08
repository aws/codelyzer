using System;
using System.Collections.Generic;
using System.Text;

namespace Codelyzer.Analysis.Model.Extensions
{
    public static class EnumerableExtensions
    {
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> enumerable)
        {
            var set = new HashSet<T>();
            foreach (var item in enumerable)
            {
                set.Add(item);
            }
            return set;
        }
    }
}
