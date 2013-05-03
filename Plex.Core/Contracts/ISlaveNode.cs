using System.Collections.Generic;
using System.ServiceModel;
using Plex.Contracts.Data;

namespace Plex.Contracts
{
	/// <summary>
	/// Контракт общения мастер-ноды с рабочей нодой.
	/// </summary>
    [ServiceContract(Namespace = "http://www.negentropy.ru/1.0")]
    public interface ISlaveNode : IService
	{
		/// <summary>
		/// Возвращает информацию о ноде
		/// </summary>
		[OperationContract]
        NodeInfo GetNodeInfo();

		/// <summary>
		/// Возвращает информацию о задаче и её запущенных подзадачах на ноде.
		/// Если задача не проинициализирована, то вернёт null.
		/// </summary>
        [OperationContract]
        TaskProcessInfo GetTaskProcessInfo(string taskName);

		/// <summary>
		/// Инициализирует задачу
		/// </summary>
        [OperationContract]
        void InitTask(string taskName, Connection masterConnect, Dictionary<string, ulong> mainFileList, bool haltOnException);

		/// <summary>
		/// Удаляет задачу и все её файлы. Останавливает её подзадачи, если те
		/// в процессе выполнения
		/// </summary>
		/// <param name="taskName"></param>
        [OperationContract(IsOneWay = true)]
        void DropTask(string taskName);

		/// <summary>
		/// Добавляет список подзадач на выполнение
		/// </summary>
        [OperationContract(IsOneWay = true)]
        void EnqueueToTask(string taskName, string[] subTaskKeys);

		/// <summary>
		/// Снимает список задач с выполнения
		/// </summary>
        [OperationContract(IsOneWay = true)]
        void DequeueFromTask(string taskName, string[] subTaskKeys);

	    [OperationContract]
	    SubTaskResult[] ReturnCompletedResults(string taskName);

        [OperationContract]
        NodeTaskStatus GetStatus(string taskName);
	}

    [ServiceContract]
    public interface ISlaveNodeProxy : IService
    {
        /// <summary>
        /// Возвращает информацию о ноде
        /// </summary>
        [OperationContract]
        NodeInfo GetNodeInfo();

        /// <summary>
        /// Возвращает информацию о задаче и её запущенных подзадачах на ноде.
        /// Если задача не проинициализирована, то вернёт null.
        /// </summary>
        [OperationContract]
        TaskProcessInfo GetTaskProcessInfo();

        /// <summary>
        /// Инициализирует задачу
        /// </summary>
        [OperationContract]
        void InitTask(Connection masterConnect, Dictionary<string, ulong> mainFileList);

        /// <summary>
        /// Удаляет задачу и все её файлы. Останавливает её подзадачи, если те
        /// в процессе выполнения
        /// </summary>
        [OperationContract(IsOneWay = true)]
        void DropTask();

        /// <summary>
        /// Добавляет список подзадач на выполнение
        /// </summary>
        [OperationContract(IsOneWay = true)]
        void EnqueueToTask(string[] subTaskKeys);

        /// <summary>
        /// Снимает список задач с выполнения
        /// </summary>
        [OperationContract(IsOneWay = true)]
        void DequeueFromTask(string[] subTaskKeys);

        [OperationContract]
        SubTaskResult[] ReturnCompletedResults();

        [OperationContract]
        NodeTaskStatus GetStatus();
    }
}
