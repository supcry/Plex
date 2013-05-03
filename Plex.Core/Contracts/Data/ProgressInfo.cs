using System.Runtime.Serialization;

namespace Plex.Contracts.Data
{
    [DataContract]
    public class ProgressInfo
    {
        /// <summary>
        /// В рутовой ноде ждёт отправки на рабочую ноду
        /// </summary>
        [DataMember(Order = 1)]
        public int ReadyToDistribute;

        /// <summary>
        /// В рабочих нодах
        /// </summary>
        [DataMember(Order = 2)]
        public int InProgress;

        /// <summary>
        /// Рутовая нода готова отдать
        /// </summary>
        [DataMember(Order = 3)]
        public int ReadyToReturn;

        [DataMember(Order = 4)]
        public bool Halted;
    }
}
