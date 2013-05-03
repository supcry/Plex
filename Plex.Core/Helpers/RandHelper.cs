using System;

namespace Plex.Helpers
{
    public static class RandHelper
    {
        public static ulong NextULong(this Random rnd)
        {
            var tmp = new byte[sizeof(ulong)];
            rnd.NextBytes(tmp);
            return BitConverter.ToUInt64(tmp, 0);
        }
    }
}
