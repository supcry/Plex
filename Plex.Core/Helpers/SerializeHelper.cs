using System.IO;
using System.Text;
using ProtoBuf;

namespace Plex.Helpers
{
    public static class SerializeHelper
    {
        public static byte[] Serialize<T>(this T obj)
        {
            using (var m = new MemoryStream())
            {
                Serializer.Serialize(m, obj);
                return m.ToArray();
            }
        }

        public static T Deserialize<T>(this byte[] data)
        {
            using (var m = new MemoryStream(data))
                return Serializer.Deserialize<T>(m);
        }

        //public static string SerializeJson<T>(this T obj)
        //{
        //    var s = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(T));
        //    using (var m = new MemoryStream())
        //    {
        //        s.WriteObject(m, obj);
        //        return Encoding.UTF8.GetString(m.ToArray());
        //    }
        //}

        //public static T DeserializeJson<T>(this string stoke)
        //{
        //    var s = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(T));
        //    using (var m = new MemoryStream(Encoding.UTF8.GetBytes(stoke)))
        //        return (T)s.ReadObject(m);
        //}
    }
}
