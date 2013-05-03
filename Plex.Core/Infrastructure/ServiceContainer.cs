using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Diagnostics;
using System.IO;
using System.ServiceModel;
using Plex.Contracts;
using Plex.Contracts.Data;
using Plex.Helpers;
using Plex.Infrastructure.Configuration;
using Plex.Infrastructure.Configuration.Services;
using Plex.Master;
using Plex.Network;
using Plex.Slave;
using ProtoBuf.ServiceModel;
using System.Linq;

namespace Plex.Infrastructure
{
    public sealed class ServiceContainer
    {
        private readonly List<ServiceHost> _hostList = new List<ServiceHost>();

        internal void Start()
        {
            try
            {
                var set = Settings.Get();
                _hostList.AddRange(GetConfiguredServices(set));
                if (set.Services.ElementInformation.IsPresent)
                    _hostList.ForEach(serviceHost => serviceHost.Open());

                _hostList.ForEach(serviceHost =>
                {
                    var service = serviceHost.SingletonInstance as IService;
                    if (service != null && !(service is IProxy))
                        if(!service.IsStarted())
                            service.Start();
                });
            }
            catch (Exception e)
            {
                Trace.TraceError("Старт сервисов не удался: {0}", e);
                throw;
            }
        }

        internal void Stop()
        {
            _hostList.Reverse();
            _hostList.ForEach(StopService);
            _hostList.Clear();
        }

        public static ServiceHost CreateServiceHost(Type serviceType, params Uri[] baseAddresses)
        {
            var host = new ServiceHost(serviceType, baseAddresses);
            SpecifyBehavior(host);
            return host;
        }

        public static ServiceHost CreateServiceHost(object singletonInstance, params Uri[] baseAddresses)
        {
            Debug.Assert(singletonInstance != null);
            var host = new ServiceHost(singletonInstance, baseAddresses);
            SpecifyBehavior(host);
            return host;
        }

        public static void SpecifyBehavior(ServiceHost host)
        {
            var behavior = (ServiceBehaviorAttribute)host.Description.Behaviors[typeof(ServiceBehaviorAttribute)];
            behavior.IncludeExceptionDetailInFaults = true;
            behavior.MaxItemsInObjectGraph = int.MaxValue;
        }

        public static void AddServiceEndpoints(ServicesElement srv, ServiceHost host, Type contract, string name)
        {
            // Добавляем TCP привязку
            var endpoint = host.AddServiceEndpoint(contract, ServiceReference.TcpBinding, ServiceReference.GetTcp(srv.Address, srv.Port, name));
            endpoint.Behaviors.Add(new ProtoEndpointBehavior());

            // Добавляем NamedPipe привязку
            endpoint = host.AddServiceEndpoint(contract, ServiceReference.NamedPipeBinding, ServiceReference.GetPipe(srv.Port, name));
            endpoint.Behaviors.Add(new ProtoEndpointBehavior());

            if (host.SingletonInstance != null)
                RegisterMefExport(contract, () => host.SingletonInstance, name);
            else
                RegisterMefExport(contract, () => CreateInstance(host.Description.ServiceType), name);
        }

        public static void AddServiceEndpoints(ServiceHost host, Type contract, string name, IConnection addr)
        {
            // Добавляем TCP привязку
            var endpoint = host.AddServiceEndpoint(contract, ServiceReference.TcpBinding, ServiceReference.GetTcp(addr.Address, addr.Port, name));
            endpoint.Behaviors.Add(new ProtoEndpointBehavior());

            // Добавляем NamedPipe привязку
            endpoint = host.AddServiceEndpoint(contract, ServiceReference.NamedPipeBinding, ServiceReference.GetPipe(addr.Port, name));
            endpoint.Behaviors.Add(new ProtoEndpointBehavior());

            if (host.SingletonInstance != null)
                RegisterMefExport(contract, () => host.SingletonInstance, name);
            else
                RegisterMefExport(contract, () => CreateInstance(host.Description.ServiceType), name);
        }

        public static void RegisterMefExport<T>(Func<T> exportedValueGetter, string name = null) where T : class
        {
            RegisterMefExport(typeof(T), exportedValueGetter, name);
        }

        public static void RegisterMefExport(Type contractType, Func<object> exportedValueGetter, string name = null)
        {
            var metadata = new Dictionary<string, object>
                               {
                                   {
                                       CompositionConstants.ExportTypeIdentityMetadataName,
                                       AttributedModelServices.GetTypeIdentity(contractType)
                                       }
                               };

            var batch = new CompositionBatch();
            batch.AddExport(new Export(new ExportDefinition(name ?? AttributedModelServices.GetContractName(contractType), metadata), exportedValueGetter));

            CompositionHost.Container.Compose(batch);
        }

        public static void StopService(ServiceHost host)
        {
            var instace = host.SingletonInstance as IDisposable;
            if (instace != null)
                instace.Dispose();
            host.Close(TimeSpan.FromSeconds(10));
        }

        public static ServiceHost InitializeSlave(SlaveElement cfg, ServicesElement srv)
        {
            var workDir = cfg.WorkDir == "" ? Path.Combine(Directory.GetCurrentDirectory(), srv.Name) : Path.GetFullPath(cfg.WorkDir);
            var n = new SlaveNode(srv.Name, workDir, cfg.MaxThreadsCount);
            var serviceHost = CreateServiceHost(n);
            const string serviceName = "slave";
            AddServiceEndpoints(srv, serviceHost, typeof(ISlaveNode), serviceName);
            Trace.TraceInformation(serviceName + " inited");
            return serviceHost;
        }

        public static ServiceHost InitializeMaster(MasterElement cfg, ServicesElement srv, IConnection[] slaves)
        {
            var n = new MasterNode(srv.Name, srv,
                srv.Slave.ElementInformation.IsPresent ? slaves.Union(new[] { (IConnection)srv }).ToArray() : slaves);
            var serviceHost = CreateServiceHost(n);
            const string serviceName = "master";
            AddServiceEndpoints(srv, serviceHost, typeof(IMasterNode), serviceName);
            Trace.TraceInformation(serviceName + " inited");

            const string serviceName1 = "plex";
            AddServiceEndpoints(srv, serviceHost, typeof(IPlex), serviceName1);
            Trace.TraceInformation(serviceName1 + " inited");

            const string serviceName2 = "storage";
            AddServiceEndpoints(srv, serviceHost, typeof(IStorageExt), serviceName2);
            Trace.TraceInformation(serviceName2 + " inited");

            return serviceHost;
        }

        private IEnumerable<ServiceHost> GetConfiguredServices(Settings cfg)
        {
            if (cfg.IsLocalSlaveEnabled())
                yield return InitializeSlave(cfg.Services.Slave, cfg.Services);
            if (cfg.IsLocalMasterEnabled())
            {
                var slaves = cfg.SlaveProxies.Select(p => p as IConnection).ToArray();
                yield return InitializeMaster(cfg.Services.Master, cfg.Services, slaves);
            }
        }

        public static object CreateInstance(Type type)
        {
            return Activator.CreateInstance(type);
        }
    }
}
