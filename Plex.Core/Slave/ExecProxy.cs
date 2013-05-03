using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using Plex.Contracts;
using Plex.Helpers;

namespace Plex.Slave
{
    public class ExecProxy : MarshalByRefObject, IExec
    {
        private IExec _exec;

        public void Prepare(string taskName)
        {
            _exec.Prepare(taskName);
        }

        public void CleanUp()
        {
            _exec.CleanUp();
        }

        public byte[] Function(string key, IStorage storage)
        {
            return _exec.Function(key, storage);
        }

        private void InitExec(string taskName, string workDir, IEnumerable<string> files)
        {
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new TextWriterTraceListener("Trace." + taskName + ".log"));
            Trace.AutoFlush = true;
            Trace.Indent();

            foreach (var f in files.Where(p => p.EndsWith(".dll") || p.EndsWith(".exe") | p.EndsWith(".msi")))
            {
                var asm = Assembly.LoadFile(Path.Combine(workDir, f));
                foreach (var type in asm.GetTypes())
                {
                    Trace.TraceInformation("Seek: " + type.FullName + ", class=" + type.IsClass + ",a=" +
                                           type.IsAssignableFrom(typeof (IExec)));
                    if (type.GetInterfaces().Any(p => p == typeof (IExec)) && type.IsClass)
                    {
                        Trace.TraceInformation("ExecProxy.InitExec: обнаружен IExec класс " + type.FullName +
                                               " в файле " + f);
                        var ci = type.GetConstructor(new Type[] {});
                        Debug.Assert(ci != null);
                        _exec = (IExec) ci.Invoke(new Object[] {});
                        _exec.Prepare(taskName);
                        return;
                    }
                }
            }
            const string msg = "ExecProxy.InitExec: Не удалось найти IExec класс в файлах";
            Trace.TraceError(msg);
            throw new BadImageFormatException(msg);
        }

        public static KeyValuePair<AppDomain, ExecProxy> StartDomain(string workDir, string taskName, string configPath, ICollection<string> execFiles)
        {
            var dir = Assembly.GetExecutingAssembly().GetDirectoryName();
            var domain = AppDomain.CreateDomain(taskName, new Evidence(),
                new AppDomainSetup { ConfigurationFile = configPath, ApplicationBase = dir });
            var app = (ExecProxy)domain.CreateInstanceAndUnwrap(typeof(ExecProxy).Assembly.FullName, typeof(ExecProxy).FullName);
            app.InitExec(taskName, workDir, execFiles);
            Trace.TraceInformation("Domain started: " + taskName + ", config: " + configPath);
            return new KeyValuePair<AppDomain, ExecProxy>(domain, app);
        }
    }
}
