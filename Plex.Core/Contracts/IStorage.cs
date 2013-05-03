using System.ServiceModel;

namespace Plex.Contracts
{
    [ServiceContract(Namespace = "http://www.negentropy.ru/1.0")]
    public interface IStorage {
        /// <summary>
        /// Добыть хэш файла
        /// </summary>
        [OperationContract]
        ulong? GetHash(string path);

        /// <summary>
        /// Добыть размер файла
        /// </summary>
        [OperationContract]
        long? GetSize(string path);

        /// <summary>
        /// Добыть данные файла
        /// </summary>
        [OperationContract]
        byte[] ReadData(string path, long pos, int size);
    }

    [ServiceContract]
    public interface IStorageExt
    {
        /// <summary>
        /// Добыть хэш файла
        /// </summary>
        [OperationContract]
        ulong? GetHash(string taskName, string path);

        /// <summary>
        /// Добыть размер файла
        /// </summary>
        [OperationContract]
        long? GetSize(string taskName, string path);

        /// <summary>
        /// Добыть данные файла
        /// </summary>
        [OperationContract]
        byte[] ReadData(string taskName, string path, long pos, int size);
    }
}
