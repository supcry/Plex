using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Plex.Storage
{
    [DataContract]
    public class FileNode
    {
        [DataMember(Order = 1)]
        public ulong Hash;
        [DataMember(Order = 2)]
        public DateTime ChangeDate;
        [DataMember(Order = 3)]
        public long Length;

        public WeakReference Ref;

        public const int MaxCachedSize = 1024*1024;
    }

    [DataContract]
    public class FileSet
    {
        public const string FileSetFilename = "fileset.info";

        [DataMember(Order = 1)]
        public Dictionary<string, FileNode> Tree = new Dictionary<string, FileNode>();
    }
}
