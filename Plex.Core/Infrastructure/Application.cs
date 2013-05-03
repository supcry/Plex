using System;
using System.ComponentModel.Composition.Hosting;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Plex.Helpers;
using Plex.Infrastructure.Configuration;

namespace Plex.Infrastructure
{
    public sealed class Application : MarshalByRefObject
    {
        public Application() : this(null)
        {

        }

        public override object InitializeLifetimeService()
        {
            return null;
        }

        static Application()
        {
            var dir = Assembly.GetExecutingAssembly().GetDirectoryName();
            Directory.SetCurrentDirectory(dir);
        }

        public Application(string appConfigFile)
        {
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
            AppDomain.CurrentDomain.TypeResolve += OnTypeResolve;
            AppDomain.CurrentDomain.DomainUnload += Stop;

            CompositionHost.Initialize(new CompositionContainer(new AggregateCatalog()));
                                                                    //new DirectoryCatalog(dir, "Antiplagiat.exe"),
                                                                    //new DirectoryCatalog(dir, "Antiplagiat.*.dll"))));

            if (appConfigFile != null)
            {
                if (appConfigFile == "" || !File.Exists(appConfigFile))
                    throw new Exception("Couldn't find config: " + Path.GetFullPath(appConfigFile));
                Settings.Load(appConfigFile);
            }
            //var settings = (Settings) ConfigurationManager.GetSection("plex");
            //if (settings == null)
            //    throw new Exception("Couldn't find section 'plex' in config: " + appConfigFile);

            SysHelper.CheckArchitectureLibPath();
        }



        public void Start()
        {
            _serviceContainer = new ServiceContainer();
            _serviceContainer.Start();
        }

        //public static KeyValuePair<AppDomain,Application> StartDomain(string domainName, string configPath)
        //{
        //    var dir = Assembly.GetExecutingAssembly().GetDirectoryName();
        //    var domain = AppDomain.CreateDomain(domainName, new Evidence(),
        //        new AppDomainSetup { ConfigurationFile = configPath, ApplicationBase = dir});
        //    var app = (Application)domain.CreateInstanceAndUnwrap(
        //        typeof(Application).Assembly.FullName, typeof(Application).FullName);
        //    app.Start();
        //    Trace.TraceInformation("Domain started: " + domainName + ", config: " + configPath);
        //    return new KeyValuePair<AppDomain, Application>(domain, app);
        //}

        public void Stop()
        {
            if (_serviceContainer != null)
            {
                _serviceContainer.Stop();
                _serviceContainer = null;
            }
        }

        public void Stop(object obj, EventArgs ev)
        {
            Stop();
        }



        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            Trace.TraceError("Unhandled exception: " + args.ExceptionObject);
        }

        private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (!args.Name.Contains(".resources"))// Начиная с .Net4.0 идёт попытка подгрузить и ресурсы.
                Trace.TraceError("Не удалось загрузить сборку " + args.Name);
            return null;
        }

        private static Assembly OnTypeResolve(object sender, ResolveEventArgs args)
        {
            Trace.TraceError("Не удалось загрузить тип " + args.Name);
            return null;
        }

        private ServiceContainer _serviceContainer;
    }
}
