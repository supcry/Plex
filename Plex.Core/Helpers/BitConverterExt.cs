using System;

namespace Plex.Helpers
{
    public static class BitConverterExt
    {
        public static byte[] GetBytes(this int[] data)
        {
            var ret = new byte[data.Length * sizeof(int)];
            for(int i=0;i<data.Length;i++)
            {
                Array.Copy(BitConverter.GetBytes(data[i]), 0, ret, i * sizeof(int), sizeof(int));
            }
            return ret;
        }

        public static int GetBytes(this int[] data, ref byte[] buf)
        {
            var ret = data.Length*sizeof (int);
            if (ret > buf.Length)
                Array.Resize(ref buf, ret);
            for (int i = 0; i < data.Length; i++)
                Array.Copy(BitConverter.GetBytes(data[i]), 0, buf, i*sizeof (int), sizeof (int));
            return ret;
        }
    }
}
