using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.ServiceModel;
using Plex.Contracts;
using Plex.Contracts.Data;
using System.Linq;

namespace Plex.Master
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
    public class MasterNode : IMasterNode, IPlex, IService, IStorageExt, IDisposable
    {
        private bool _isStarted;
        private readonly Connection _connection;
        //private readonly string _nodeName;
        private readonly NodeDiscover _finder;

        private readonly Dictionary<string, TaskScheduler> _tasks = new Dictionary<string, TaskScheduler>();

        public MasterNode(string nodeName, IConnection connection, ICollection<IConnection> slaves)
        {
           // _nodeName = nodeName;
            _connection = new Connection(connection.Address, connection.Port);
            _finder = new NodeDiscover(slaves);
            
            Trace.TraceInformation("Создана мастер-нода " + nodeName + ", con=" + connection.Address + ":" + connection.Port + ", slavesCount=" + slaves.Count);
        }

        #region IMasterNode
        public void Event(string taskName, string nodeName, NodeTaskStatus status)
        {
            _tasks[taskName].Event(nodeName, status);
        }
        #endregion IMasterNode

        #region IStorageExt
        public ulong? GetHash(string taskName, string path)
        {
            return _tasks[taskName].Storage.GetHash(path);
        }

        public long? GetSize(string taskName, string path)
        {
            return _tasks[taskName].Storage.GetSize(path);
        }

        public byte[] ReadData(string taskName, string path, long pos, int size)
        {
            return _tasks[taskName].Storage.ReadData(path, pos, size);
        }
        #endregion IStorageExt

        #region IService methods
        public void Start()
        {
            foreach(var t in _tasks)
                t.Value.Start();
            _finder.Start();
            _isStarted = true;
        }

        public void Stop()
        {
            _isStarted = false;
            foreach (var t in _tasks)
                t.Value.Stop();
            _finder.Stop();
        }

        public bool IsStarted()
        {
            return _isStarted;
        }

        public void Restart()
        {
            if (IsStarted())
                Stop();
            Start();
        }
        #endregion IService methods

        #region IPlex methods
        public void InitTaskLocal(string taskName, string workDir, string[] mainFileList, string[] extraFileList)
        {
            if (_tasks.ContainsKey(taskName))
                throw new Exception("Такая задача уже была ранее проинициализирована");

            var t = new TaskScheduler(taskName, workDir, mainFileList, _finder, _connection);
            if(_isStarted)
                t.Start();
            _tasks.Add(taskName, t);
        }

        public void InitTaskRemote(string taskName, KeyValuePair<string, byte[]>[] mainFileList, KeyValuePair<string, byte[]>[] extraFileList)
        {
            if(_tasks.ContainsKey(taskName))
                throw new Exception("Такая задача уже была ранее проинициализирована");

            Directory.CreateDirectory(taskName);
            foreach (var f in mainFileList)
                File.WriteAllBytes(f.Key, f.Value);
            if(extraFileList != null)
                foreach (var f in extraFileList)
                    File.WriteAllBytes(f.Key, f.Value);

            var t = new TaskScheduler(taskName, taskName, mainFileList.Select(p => p.Key).ToArray(), _finder, _connection);
            if (_isStarted)
                t.Start();
            _tasks.Add(taskName, t);
        }

        public void DropTask(string taskName)
        {
            _tasks[taskName].DropTask();
            _tasks.Remove(taskName);
        }

        public void EnqueueToTask(string taskName, string[] subTaskKeys)
        {
            _tasks[taskName].EnqueueToTask(subTaskKeys);
        }

        public void DequeueFromTask(string taskName, string[] subTaskKeys)
        {
            _tasks[taskName].DequeueFromTask(subTaskKeys);
        }

        public SubTaskResult[] GetTaskResults(string taskName)
        {
            return _tasks[taskName].GetTaskResults();
        }

        public ProgressInfo GetProgressInfo(string taskName)
        {
            return _tasks[taskName].GetProgressInfo();
        }
        #endregion IPlex methods

        public void Dispose()
        {
            foreach(var t in _tasks)
                t.Value.Dispose();
            _tasks.Clear();
        }

        public NodeTaskStatus GetStatus(string taskName)
        {
            return _tasks[taskName].GetStatus();
        }

    }
}
