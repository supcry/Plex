using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Plex.Contracts;
using Plex.Contracts.Data;

namespace Plex.Proxy
{
    public class StorageClient : Network.Proxy<IStorageExt>, IStorage
    {
        private readonly string _taskName;

        public StorageClient(string taskName, Connection con) :base(con, "storage")
        {
            _taskName = taskName;
        }
        public StorageClient(TaskConnection tcon):this(tcon.TaskName, tcon.AsConnection){}

        public ulong? GetHash(string path)
        {
            return Invoke(p => p.GetHash(_taskName, path));
        }

        public long? GetSize(string path)
        {
            return Invoke(p => p.GetSize(_taskName, path));
        }

        public byte[] ReadData(string path, long pos, int size)
        {
            return Invoke(p => p.ReadData(_taskName, path, pos, size));
        }
    }
}
