using System;
using System.Diagnostics;
using Plex.Contracts;

namespace Test.Files
{
    public class TestFilesExec : IExec
    {
        public const string FileName = "testfilename.bin";

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
            //Trace.TraceInformation("Начался обрабатываться элемент " + key);
            int pos = int.Parse(key);
            var size = store.GetSize(FileName);
            if(!size.HasValue || size.Value < pos)
                throw new Exception();
            var ret = store.ReadData(FileName, pos, 1);
            //Trace.TraceInformation("Элемент " + key + " обработан. Получен результат.");
            return ret;
        }
    }


}
