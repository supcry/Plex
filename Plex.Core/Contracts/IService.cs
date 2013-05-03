using System.ServiceModel;

namespace Plex.Contracts
{
    [ServiceContract(Namespace = "http://www.negentropy.ru/1.0")]
    public interface IService
    {
        [OperationContract]
        void Start();

        [OperationContract]
        void Stop();

        [OperationContract]
        bool IsStarted();
    }
}
