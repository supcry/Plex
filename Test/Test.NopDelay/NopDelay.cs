using System;
using System.Diagnostics;
using System.Linq;
using Plex.Contracts;
using Plex.Helpers;

namespace Test.NopDelay
{
    public class NopDelay : IExec
    {
        public void Prepare(string taskName)
        {
            Trace.TraceInformation("Preparing...");
        }

        public void CleanUp()
        {
            Trace.TraceInformation("CleaningUp...");
        }

        public byte[] Function(string key, IStorage store)
        {
            var s = DateTime.Now;
            Trace.TraceInformation("Начался обрабатываться элемент " + key);
            var rnd = new Random();
            var count = 100000000;
            var rs = Enumerable.Range(0, count).Sum(p => rnd.NextDouble());
            if(rs == 0.0)
                Trace.TraceError("QQQQQQQQQQ");
            //Thread.Sleep(TimeSpan.FromMilliseconds(10));
            var ret = new NopDelayResult
                          {
                              Key = key,
                              Started = s,
                              Finished = DateTime.Now
                          };
            Trace.TraceInformation("Закончил обрабатываться элемент " + key);
            return ret.Serialize();
        }
    }


}
