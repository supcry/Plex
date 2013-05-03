using System.ServiceModel;
using Plex.Contracts.Data;

namespace Plex.Contracts
{
	/// <summary>
	/// Контракт общения рабочих нод с мастер-нодой
	/// </summary>
    [ServiceContract(Namespace = "http://www.negentropy.ru/1.0")]
    public interface IMasterNode
	{
		/// <summary>
		/// Вернуть набор результатов (данные или исключения)
		/// </summary>
        [OperationContract(IsOneWay = true)]
        void Event(string taskName, string nodeName, NodeTaskStatus status);
	}

    [ServiceContract]
    public interface IMasterNodeProxy
    {
        /// <summary>
        /// Вернуть набор результатов (данные или исключения)
        /// </summary>
        [OperationContract(IsOneWay = true)]
        void Event(string nodeName, NodeTaskStatus status);
    }
}
