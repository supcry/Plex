using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Threading;
using System.Xml;
using ProtoBuf.ServiceModel;

namespace Plex.Network
{
    [DataContract]
    public sealed class ServiceReference : Contracts.Data.IConnection
    {
        public static bool UseReliableSessionForNewInstances
        {
            get
            {
                return TcpBinding.ReliableSession.Enabled;
            }
            set
            {
                TcpBinding.ReliableSession.Enabled = value;
            }
        }

        static ServiceReference()
        {
            var readerQuotas = new XmlDictionaryReaderQuotas { MaxStringContentLength = 50000000, MaxArrayLength = 50000000, MaxBytesPerRead = 50000000};

            TcpBinding = new NetTcpBinding(SecurityMode.None)
                             {
                                 MaxReceivedMessageSize = 50000000,
                                 ReaderQuotas = readerQuotas,
                                 ReliableSession = {Enabled = true}
                             };

            NamedPipeBinding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.None)
            {
                MaxReceivedMessageSize = 50000000,
                ReaderQuotas = readerQuotas
            };

            HttpBinding = new BasicHttpBinding
            {
                MaxReceivedMessageSize = 50000000,
                ReaderQuotas = readerQuotas
            };
        }

        [DataMember(Order = 1)]
        public string Address { get; set; }

        [DataMember(Order = 2)]
        public int Port { get; set; }

        [DataMember(Order = 3)]
        public string ServiceName;

        [DataMember(Order = 4)]
        public string UserName;

        [DataMember(Order = 5)]
        public string Password;

        public object Instance;

        public TimeSpan? OperationTimeout { get; set; }

        DateTime _openTime = DateTime.Now;

        public bool IsActive = true;

        int _callCount;
        /// <summary>
        /// Число вызовов которое идет через этот прокси в данный момент.
        /// </summary>
        public int CallCount { get { return _callCount; } }

        public bool NeedToPing
        {
            get { return DateTime.Now >= _openTime; }
        }

        public void SetResetDelay(TimeSpan dt, bool active)
        {
            _openTime = DateTime.Now + dt;
            IsActive = active;
        }

        public void Invoke<T>(Action<T> action, TimeSpan? timeOut = null) where T : class
        {
            Invoke<T, int>(channel => { action(channel); return 0; }, timeOut);
        }

        public TResult Invoke<T, TResult>(Func<T, TResult> action, TimeSpan? timeOut = null,
                                            bool throwOnCommunicationError = true) where T : class
        {
            if (Instance != null)
                return action((T)Instance);

            TResult result;

            T channel;
            try
            {
                channel = CreateChannel<T>();

                if (timeOut.HasValue)
                    ((IClientChannel)channel).OperationTimeout = timeOut.Value;
                else if (OperationTimeout.HasValue)
                    ((IClientChannel)channel).OperationTimeout = OperationTimeout.Value;

                ((IClientChannel)channel).Open();
            }
            catch (TimeoutException ex)
            {
                if (!throwOnCommunicationError)
                    return default(TResult);
                throw new CommunicationException("", ex);
            }
            catch (EndpointNotFoundException ex)
            {
                if (!throwOnCommunicationError)
                    return default(TResult);
                throw new CommunicationException("", ex);
            }
            Interlocked.Increment(ref _callCount);
            try
            {
                result = action(channel);

                ((IClientChannel)channel).Close();
            }
            catch
            {
                if (channel != null)
                    ((IClientChannel)channel).Abort();
                throw;
            }
            finally
            {
                Interlocked.Decrement(ref _callCount);
            }

            return result;
        }

        public bool IsAvailable<T>() where T : class
        {
            if (Instance != null)
                return true;

            IClientChannel channel;
            try
            {
                channel = (IClientChannel)CreateChannel<T>();
                channel.OperationTimeout = TimeSpan.FromMilliseconds(500);
                channel.Open();
            }
            catch (TimeoutException)
            {
                return false;
            }
            catch (CommunicationObjectFaultedException)
            {
                return false;
            }
            catch (EndpointNotFoundException)
            {
                return false;
            }

            channel.Close();
            return true;
        }

        private T CreateChannel<T>()
        {
            ChannelFactory factory;

            var address = new ServiceReferenceAddress { ServiceName = ServiceName, Address = Address, Port = Port };

            if (!ChannelFactories.TryGetValue(address, out factory))
            {
                factory = new ChannelFactory<T>(GetBinding(), GetEndpoint());
                factory.Endpoint.Behaviors.Add(new ProtoEndpointBehavior());
                foreach (var operationDescription in factory.Endpoint.Contract.Operations)
                {
                    var dataContractBehavior = operationDescription.Behaviors[typeof(DataContractSerializerOperationBehavior)] as DataContractSerializerOperationBehavior;
                    if (dataContractBehavior != null)
                        dataContractBehavior.MaxItemsInObjectGraph = int.MaxValue;
                }
                try
                {//[ToDo] Почему-то повалилось...
                    ChannelFactories[address] = factory;
                }
                catch (Exception)
                {
                    ChannelFactories[address] = factory;
                }
            }

            return ((ChannelFactory<T>)factory).CreateChannel();
        }

        private Binding GetBinding()
        {
            return IsLocal(Address) ? (Binding)NamedPipeBinding : TcpBinding;
        }

        public const string LocalAddress = "127.0.0.1";

        private EndpointAddress GetEndpoint()
        {
            string uri = IsLocal(Address)
                             ? GetPipe(Port, ServiceName)
                             : GetTcp(Address, Port, ServiceName);

            return new EndpointAddress(uri);
        }

        public static string GetPipe(int port, string serviceName, string address = LocalAddress)
        {
            if (IsLocal(address))
                address = LocalAddress;
            return string.Format("net.pipe://{0}/{1}/{2}", address, port, serviceName);
        }

        public static string GetTcp(string address, int port, string serviceName)
        {
            if (IsLocal(address))
                address = LocalAddress;
            return string.Format("net.tcp://{0}:{1}/{2}", address, port, serviceName);
        }

        public static string GetHttp(string address, int httpPort, string serviceName)
        {
            if (IsLocal(address))
                address = LocalAddress;
            return string.Format("http://{0}:{1}/{2}", address, httpPort, serviceName);
        }

        private static bool IsLocal(string address)
        {
            return string.IsNullOrEmpty(address) || address == "127.0.0.1" || address == "localhost";
        }

        public override string ToString()
        {
            return Address + "/" + Port + "/" + ServiceName;
        }

        public bool Equals(Contracts.Data.IConnection r)
        {
            return r.Address == Address && r.Port == Port;
        }

        public int CompareTo(Contracts.Data.IConnection r)
        {
            var ret = String.CompareOrdinal(Address, r.Address);
            if (ret != 0)
                return ret;
            return Port.CompareTo(r.Port);
        }

        public static readonly NetTcpBinding TcpBinding;
        public static readonly NetNamedPipeBinding NamedPipeBinding;
        public static readonly BasicHttpBinding HttpBinding;

        private static readonly Dictionary<ServiceReferenceAddress, ChannelFactory> ChannelFactories = new Dictionary<ServiceReferenceAddress, ChannelFactory>();
    }

    internal class ServiceReferenceAddress
    {
        internal string ServiceName { get; set; }
        internal string Address { get; set; }
        internal int Port { get; set; }

        public override int GetHashCode()
        {
            return ServiceName.GetHashCode() + Address.GetHashCode() + Port;
        }

        public override bool Equals(object obj)
        {
            return ServiceName == ((ServiceReferenceAddress)obj).ServiceName && Address == ((ServiceReferenceAddress)obj).Address && Port == ((ServiceReferenceAddress)obj).Port;
        }
    }
}
