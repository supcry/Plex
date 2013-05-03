using System.Runtime.Serialization;

namespace Plex.Contracts.Data
{
    [DataContract]
	public class NodeInfo
	{
        [DataMember(Order = 1)]
		public string NodeName;

        [DataMember(Order = 2)]
		public string OperationSystem;
        [DataMember(Order = 3)]
		public string MachineName;

        [DataMember(Order = 4)]
		public long FreeOperationMemory;
        [DataMember(Order = 5)]
        public long FreeStorageMemory;

        [DataMember(Order = 6)]
        public int CpuThreads;
        [DataMember(Order = 7)]
        public double CpuFrequencyMhz;

        [DataMember(Order = 8)]
        public int GpuThreads;
        [DataMember(Order = 9)]
        public double GpuFrequencyMhz;

        public double CpuScore { get { return CpuFrequencyMhz*CpuThreads; } }
        public double GpuScore { get { return GpuFrequencyMhz * GpuThreads; } }
	}
}
