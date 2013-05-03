using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Plex.Contracts;
using Plex.Contracts.Data;
using Plex.Helpers;
using Plex.Proxy;

namespace Plex.Storage
{
    public class FileStorage : MarshalByRefObject, IStorage
    {
        public readonly string WorkDir;
        private readonly FileSet _fset;
        private readonly Connection _masterCon;
        private readonly string _taskName;

        

        private FileStorage(string rootDir)
        {
            WorkDir = Path.GetFullPath(rootDir);
            if (!Directory.Exists(WorkDir))
                Directory.CreateDirectory(WorkDir);

            var fn = Path.Combine(WorkDir, FileSet.FileSetFilename);
            _fset = File.Exists(fn) ? File.ReadAllBytes(fn).Deserialize<FileSet>() : new FileSet();
        }

        public FileStorage(string rootDir, string taskName, Connection con = null)
            : this(rootDir)
        {
            _masterCon = con;
            _taskName = taskName;
        }

        #region sys methods
        public long GetFreeSpace()
        {
            return DriveInfo.GetDrives()
                .First(p => p.RootDirectory.ToString().ToLower()[0] == WorkDir.ToLower()[0]).AvailableFreeSpace;
        }

        public long GetUsageSpace()
        {
            return GetUsageSpace(WorkDir);
        }

        private static long GetUsageSpace(string dir)
        {
            return Directory.GetFiles(dir).Sum(p => new FileInfo(p).Length) + Directory.GetDirectories(dir).Sum(p => GetUsageSpace(p));
        }


        public void Clear()
        {
            lock (this)
            {
                _fset.Tree.Clear();
                Clear(WorkDir);
            }
        }

        private static void Clear(string dir)
        {
            foreach (var d in Directory.GetDirectories(dir))
                Clear(d);

            foreach (var f in Directory.GetFiles(dir))
                File.Delete(f);

            Directory.Delete(dir);
        }
#endregion sys methods

        public void Precache(Dictionary<string, ulong> fileList)
        {
            foreach(var item in fileList)
            {
                var n = Download(item.Key);
                if(n.Hash != item.Value)
                    throw new Exception(_taskName + ": downloaded file '"+ item.Key +"' with wrong hash!");
            }
        }

        public Dictionary<string, ulong> Precache(string[] fileList)
        {
            var ret = new Dictionary<string, ulong>();
            foreach (var item in fileList)
            {
                var n = PreRead(item);
                if (n == null)
                    throw new Exception(_taskName + ": Требуемый файл '" + item + "' не найден");
                ret.Add(item, n.Hash);
            }
            return ret;
        }


        private FileNode Download(string path, bool check = false)
        {
            if (_masterCon == null)
                return null;

            var fullpath = Path.Combine(WorkDir, path);
            //Debug.Assert(!File.Exists(fullpath));
            var gw = new StorageClient(_taskName, _masterCon);
            using(var f = File.Create(fullpath, FileNode.MaxCachedSize))
            {
                long pos = 0;
                while (true)
                {
                    byte[] buf = gw.ReadData(path, pos, FileNode.MaxCachedSize);
                    f.Write(buf, 0, buf.Length);
                    pos += buf.Length;
                    if (buf.Length != FileNode.MaxCachedSize)
                        break;
                }
                f.Flush();
            }

            var ret = PreRead(path);
            if (check)
            {
                var len = gw.GetSize(path);
                var hs = gw.GetHash(path);
                Debug.Assert(len.HasValue && hs.HasValue);
                if (ret.Length != len.Value)
                    throw new Exception("File '" + path + "' corrupted: length");
                if (ret.Hash != hs.Value)
                    throw new Exception("File '" + path + "' corrupted: hash wrong");
            }
            
            return ret;
        }

        private FileNode PreRead(string path)
        {
            var fullpath = Path.Combine(WorkDir, path);
            var fi = new FileInfo(fullpath);
            if (fi.Exists)
            {
                var size = fi.Length;
                ulong hash;
                if (_masterCon != null)
                {
                    var gw = new StorageClient(_taskName, _masterCon);
                    var gwSize = gw.GetSize(path);
                    if (gwSize != size)
                    {
                        if (gwSize.HasValue)
                            fi.Delete();
                        return null;
                    }
                    hash = HashLib.HashFactory.Hash64.CreateMurmur2().ComputeFile(fullpath).GetULong();
                    var gwHash = gw.GetHash(path);
                    if (gwHash != hash)
                    {
                        if (gwHash.HasValue)
                            fi.Delete();
                        return null;
                    }
                }
                else
                    hash = HashLib.HashFactory.Hash64.CreateMurmur2().ComputeFile(fullpath).GetULong();
                

                var n = new FileNode
                {
                    Hash = hash,
                    Length = fi.Length,
                    ChangeDate = fi.LastWriteTime
                };
                _fset.Tree.Add(path, n);
                return n;
            }
            return null;
        }

        #region public methods

        #region IReadStorage
        public ulong? GetHash(string path)
        {
            lock (this)
            {
                path = path.ToLower();
                FileNode n;
                if (!_fset.Tree.TryGetValue(path, out n) && (n = PreRead(path)) == null && (n = Download(path)) == null)
                    return null;
                return n.Hash;
            }
        }

        public long? GetSize(string path)
        {
            lock (this)
            {
                path = path.ToLower();
                FileNode n;
                if (!_fset.Tree.TryGetValue(path, out n) && (n = PreRead(path)) == null && (n = Download(path)) == null)
                    return null;
                return n.Length;
            }
        }

        public byte[] ReadData(string path, long pos, int size)
        {
            lock (this)
            {
                path = path.ToLower();
                FileNode n;
                if (!_fset.Tree.TryGetValue(path, out n) && (n = PreRead(path)) == null && (n = Download(path)) == null)
                    return null;
                if (pos + size > n.Length)
                    size = (int) (n.Length - pos);
                if (n.Ref != null &&  n.Ref.IsAlive && pos + size <= FileNode.MaxCachedSize)
                {
                    var d = (byte[]) n.Ref.Target;
                    if (pos == 0 && size == d.Length)
                        return d;
                    var tmp = new byte[size];
                    Array.Copy(d, pos, tmp, 0, size);
                    return tmp;
                }

                using (var f = File.OpenRead(Path.Combine(WorkDir, path)))
                {
                    var buf = new byte[size];
                    f.Position = pos;
                    var len = f.Read(buf, 0, buf.Length);
                    if (len < size)
                    {
                        Trace.TraceWarning("ReadData at " + WorkDir + " for path " + path +
                                           " was wrong. Desynced metadata! Reloading.");
                        _fset.Tree.Remove(path);
                        PreRead(path);
                        Array.Resize(ref buf, len);
                    }

                    if (pos == 0 && len <= FileNode.MaxCachedSize && (len == FileNode.MaxCachedSize || len == n.Length))
                        n.Ref = new WeakReference(buf);

                    return buf;
                }
            }
        }
        #endregion IReadStorage

        #endregion public methods
    }
}
