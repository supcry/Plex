using System;
using System.Linq;

namespace Plex.Helpers
{
    public static class ArrayHelper
    {
        public static bool ArrayEquals<T>(this T[] a, T[] b) where T:IEquatable<T>
        {
            if (a == null && b == null)
                return true;
            if (a == null || b == null)
                return false;
            if (a.Length != b.Length)
                return false;
            return !a.Where((t, i) => !t.Equals(b[i])).Any();
        }

        public static void SortAndDistinct<T>(ref T[] data) where T : IComparable
        {
            if (data.Length <= 1)
                return;

            Array.Sort(data);

            var j = 0;
            for (var i = 1; i < data.Length; i++)
            {
                if (data[i].CompareTo(data[j]) == 0)
                    continue;

                j++;
                data[j] = data[i];
            }
            j++;
            if (j != data.Length)
                Array.Resize(ref data, j);
        }


    }
}
