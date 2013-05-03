using System;
using System.Runtime.Serialization;

namespace Plex.Contracts.Data
{
    [DataContract]
	public class TaskProcessInfo
	{
        [DataMember(Order = 1)]
        public string TaskName;
        [DataMember(Order = 2)]
        public SubTaskProcessInfo[] SubTasksInProgress;
        [DataMember(Order = 3)]
        public int SubTasksToProcess;
        [DataMember(Order = 4)]
        public int SubTasksFinished;

        [DataContract]
		public class SubTaskProcessInfo
		{
            [DataMember(Order = 1)]
            public string Key;
            [DataMember(Order = 2)]
            public DateTime StartDate;
		}
	}
}
