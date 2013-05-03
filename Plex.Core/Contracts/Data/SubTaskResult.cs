using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Plex.Contracts.Data
{
    [DataContract]
    public class SubTaskResult : IEquatable<SubTaskResult>
    {
        [DataMember(Order = 1)]
        public readonly string Key;

        [DataMember(Order = 2)]
        public ResultType Type;

        [DataMember(Order = 3)]
        public byte[] Result;

        [DataMember(Order = 4)]
        public TimeSpan Time;

        public SubTaskResult(string key, byte[] result, TimeSpan time)
        {
            Key = key;
            Result = result;
            Type = ResultType.Success;
            Time = time;
        }

        public SubTaskResult(string key, ResultType type, string message = null)
        {
            Key = key;
            Type = type;
            if(message != null)
                Result = Encoding.Unicode.GetBytes(message);
        }

        public enum ResultType
        {
            Success = 0,
            OutOfMemory = 1,
            TimeOut = 2,
            Interrupted = 3,

            OtherException = 128
        }

        public bool Equals(SubTaskResult item)
        {
            if (Type == item.Type && Type == ResultType.Success && !ArrayEquals(item.Result, Result))
                return false;
            return item.Key == Key;
        }

        public override bool Equals(Object obj)
        {
            var item = obj as SubTaskResult;
            if (item == null)
                return false;
            return Equals(item);
        }

        public override int GetHashCode()
        {
            return Key.GetHashCode();
        }

        public static bool ArrayEquals<T>(T[] a, T[] b) where T : IEquatable<T>
        {
            if (a == null && b == null)
                return true;
            if (a == null || b == null)
                return false;
            if (a.Length != b.Length)
                return false;
            return !a.Where((t, i) => !t.Equals(b[i])).Any();
        }
    }
}
