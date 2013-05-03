using System.Collections.Generic;
using System.ServiceModel;
using Plex.Contracts.Data;

namespace Plex.Contracts
{
    /// <summary>
    /// Основной контракт пользователя для общения с мастер нодой
    /// </summary>
    [ServiceContract(Namespace = "http://www.negentropy.ru/1.0")]
    public interface IPlex
    {
        /// <summary>
        /// Инициализирует задачу
        /// </summary>
        /// <param name="taskName">Имя задачи</param>
        /// <param name="workDir">Путь к рабочим файлам задачи</param>
        /// <param name="mainFileList">Список файлов задачи, необходимых для подгрузки</param>
        /// <param name="extraFileList">Список файлов задачи, не обязательный для подгрузки на ноды</param>
        [OperationContract]
        void InitTaskLocal(string taskName, string workDir, string[] mainFileList, string[] extraFileList);

        /// <summary>
        /// Инициализирует задачу
        /// </summary>
        /// <param name="taskName">Имя задачи</param>
        /// <param name="mainFileList">Список файлов задачи, необходимых для подгрузки</param>
        /// <param name="extraFileList">Список файлов задачи, не обязательный для подгрузки на ноды</param>
        [OperationContract]
        void InitTaskRemote(string taskName, KeyValuePair<string, byte[]>[] mainFileList, KeyValuePair<string, byte[]>[] extraFileList);

        /// <summary>
        /// Остановить задачу, вычистить все её результаты и файлы
        /// </summary>
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

        /// <summary>
        /// Получает обработанные результаты вычислений
        /// </summary>
        [OperationContract]
        SubTaskResult[] GetTaskResults(string taskName);

        /// <summary>
        /// Возвращает информацию о текущем ходе выполнения задачи
        /// </summary>
        /// <returns></returns>
        [OperationContract]
        ProgressInfo GetProgressInfo(string taskName);

        [OperationContract]
        NodeTaskStatus GetStatus(string taskName);
    }

    [ServiceContract]
    public interface IPlexProxy
    {
        /// <summary>
        /// Инициализирует задачу
        /// </summary>
        /// <param name="workDir">Путь к рабочим файлам задачи</param>
        /// <param name="mainFileList">Список файлов задачи, необходимых для подгрузки</param>
        /// <param name="extraFileList">Список файлов задачи, не обязательный для подгрузки на ноды</param>
        [OperationContract]
        void InitTaskLocal(string workDir, string[] mainFileList, string[] extraFileList);

        [OperationContract]
        void InitTaskRemote(KeyValuePair<string, byte[]>[] mainFileList, KeyValuePair<string, byte[]>[] extraFileList);

        /// <summary>
        /// Остановить задачу, вычистить все её результаты и файлы
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

        /// <summary>
        /// Получает обработанные результаты вычислений
        /// </summary>
        [OperationContract]
        SubTaskResult[] GetTaskResults();

        /// <summary>
        /// Возвращает информацию о текущем ходе выполнения задачи
        /// </summary>
        /// <returns></returns>
        [OperationContract]
        ProgressInfo GetProgressInfo();

        [OperationContract]
        NodeTaskStatus GetStatus();
    }
}
