using System;
using System.Runtime.Serialization;

namespace Test.NopDelay
{
    [DataContract]
    public class NopDelayResult
    {
        [DataMember(Order = 1)]
        public string Key;

        [DataMember(Order = 2)]
        public DateTime Started;

        [DataMember(Order = 3)]
        public DateTime Finished;
    }
}
