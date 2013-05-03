using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Plex.Helpers
{
    public static class LinqHelper
    {
        public static void ForEach<T>(this IEnumerable<T> items, Action<T> func)
        {
            foreach (var i in items)
                func(i);
        }

        public static IEnumerable<T> Prepare<T>(this IEnumerable<T> c, Action<T> action)
        {
            if (c == null)
                yield break;

            foreach (T obj in c)
            {
                if (action != null)
                    action(obj);

                yield return obj;
            }
        }

        public static TValue TryGetValue<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        {
            TValue value;
            return dictionary.TryGetValue(key, out value) ? value : default(TValue);
        }

        public static IEnumerable<T[]> EnumerateByPacks<T>(this IEnumerable<T> seq, int packSize)
        {
            var tmp = new List<T>(packSize);
            foreach(var item in seq)
            {
                tmp.Add(item);
                if(tmp.Count >= packSize)
                {
                    yield return tmp.ToArray();
                    tmp.Clear();
                }
            }
            if (tmp.Count > 0)
                yield return tmp.ToArray();
        } 
    }
}
