using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using Plex.Contracts.Data;
using Plex.Helpers;
using Plex.Proxy;
using System.Linq;

namespace Test.NopDelay
{
    class Program
    {
        static void Main(string[] args)
        {
            Thread.Sleep(TimeSpan.FromSeconds(10));
            var c = new PlexClient("nopDelay", new Connection("127.0.0.1", 1909));
            const int cont = 4;
            

            var keys = Enumerable.Range(0, cont).Select(p => p.ToString(CultureInfo.InvariantCulture)).ToArray();
            File.Copy("Test.NopDelay.exe", "Test.NopDelay.Clone.exe", true);
            c.InitTaskLocal(Directory.GetCurrentDirectory(), new[]{"Test.NopDelay.Clone.exe"});
            c.EnqueueToTask(keys.Take(cont/2).ToArray());
            c.EnqueueToTask(keys.Skip(cont / 2).ToArray());
            var rets = new List<SubTaskResult>();
            while(rets.Count < keys.Length)
            {
                //Trace.TraceInformation("Test: Пытаемся получить результат");
                var pack = c.GetTaskResults();
                if (pack.Length > 0)
                {
                    rets.AddRange(pack);
                    Trace.TraceInformation("Test: Получили пачку из " + pack.Length + " ответов. " + rets.Count + "/" + keys.Length);
                }
                Thread.Sleep(1000);
                
            }
            Console.WriteLine("finished. Result=" + string.Join(";", rets.Select(p => p.Result.Deserialize<NopDelayResult>().Key)));
            c.DropTask();
            Console.ReadKey();
        }
    }
}
