using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Plex.Contracts.Data;
using Plex.Proxy;

namespace Test.Files
{
    class Program
    {
        static void Main(string[] args)
        {
            Thread.Sleep(TimeSpan.FromSeconds(10));
            var c = new PlexClient("testFiles", new Connection("127.0.0.1", 1909));
            const int count = 125500;
            var rnd = new byte[count];
            (new Random()).NextBytes(rnd);
            File.Delete(TestFilesExec.FileName);
            File.WriteAllBytes(TestFilesExec.FileName, rnd);
            var keys = Enumerable.Range(0, count).Select(p => p.ToString(CultureInfo.InvariantCulture)).ToArray();
            File.Copy("Test.Files.exe", "Test.Files.Clone.exe", true);
            try
            {
                c.InitTaskLocal(Directory.GetCurrentDirectory(), new[] { "Test.Files.Clone.exe" }, new [] { TestFilesExec.FileName });
            
                c.EnqueueToTask(keys);
                //c.EnqueueToTask(keys.Take(count / 2).ToArray());
                //c.EnqueueToTask(keys.Skip(count / 2).ToArray());
                var rets = new List<SubTaskResult>();
                while (rets.Count < keys.Length)
                {
                    //Trace.TraceInformation("Test: Пытаемся получить результат");
                    var pack = c.GetTaskResults();

                    if (pack.Length > 0)
                    {
                        var ex1 = pack.Where(p => p == null || p.Type != SubTaskResult.ResultType.Success).ToArray();
                        if (ex1.Length != 0)
                            throw new Exception();
                        rets.AddRange(pack);
                        Trace.TraceInformation("Test: Получили пачку из " + pack.Length + " ответов. " + rets.Count +
                                               "/" + keys.Length);
                    }
                }
                var ex = rets.Where(p => p == null || p.Type != SubTaskResult.ResultType.Success).ToArray();
                if(ex.Length != 0)
                    throw new Exception();
                var nl = rets.Where(p => p.Result == null || p.Result.Length == 0).ToArray();
                if (nl.Length != 0)
                    throw new Exception();
                var data = rets.OrderBy(p => int.Parse(p.Key)).SelectMany(p => p.Result).ToArray();
                if (data.Length != rnd.Length)
                    throw new Exception("test error len");
                for (int i = 0; i < data.Length; i++)
                {
                    if (data[i] != rnd[i])
                        throw new Exception("test error data");
                }
                Console.WriteLine("finished.");
            }
            finally
            {
                c.DropTask();
            }
            Console.ReadKey();
        }
    }
}
