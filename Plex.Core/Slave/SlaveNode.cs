using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel;
using Plex.Contracts;
using Plex.Contracts.Data;
using Plex.Helpers;

namespace Plex.Slave
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
    public class SlaveNode : ISlaveNode
    {
        /// <summary>
        ///Имя ноды
        /// </summary>
        private readonly string _name;

        private readonly string _workDir;

        private readonly int _maxThreadsCount;

        private readonly Dictionary<string, TaskProcess> _tasks = new Dictionary<string, TaskProcess>();

        private bool _isStarted;

        public SlaveNode(string name, string workDir, int maxThreadsCount)
        {
            _name = name;
            _workDir = workDir;
            if (!Directory.Exists(_workDir))
                Directory.CreateDirectory(_workDir);
            _maxThreadsCount = maxThreadsCount == -1 ? Environment.ProcessorCount : maxThreadsCount;
            Trace.TraceInformation("Создана рабочая-нода " + name + ", threadsCount=" + _maxThreadsCount + ", workDir=" + workDir);
        }


        #region ISlaveNode methods
        public NodeInfo GetNodeInfo()
        {
            return new NodeInfo
                       {
                           NodeName = _name,
                           MachineName = Environment.MachineName,
                           CpuFrequencyMhz = 2000,
                           CpuThreads = Environment.ProcessorCount,
                           OperationSystem = Environment.OSVersion.ToString(),
                           FreeOperationMemory = -1,
                           FreeStorageMemory = SysHelper.GetFreeSpace(_workDir),
                           GpuThreads = -1,
                           GpuFrequencyMhz = -1
                       };
        }

        public TaskProcessInfo[] GetTaskProcessesInfo()
        {
            return _tasks.Values.Select(p => p.GetTaskInfo()).ToArray();
        }

        public TaskProcessInfo GetTaskProcessInfo(string taskName)
        {
            TaskProcess tp;
            if (_tasks.TryGetValue(taskName, out tp))
                return tp.GetTaskInfo();
            throw new KeyNotFoundException();
        }

        public NodeTaskStatus GetStatus(string taskName)
        {
            TaskProcess tp;
            if (_tasks.TryGetValue(taskName, out tp))
                return tp.GetStatus();
            throw new KeyNotFoundException();
        }

        public void InitTask(string taskName, Connection masterConnect, Dictionary<string, ulong> mainFileList, bool haltOnException)
        {
            if(_tasks.ContainsKey(taskName))
                throw new DuplicateNameException();
            var tp = new TaskProcess(_name, taskName, masterConnect, Path.Combine(_workDir, taskName), mainFileList, haltOnException, _maxThreadsCount);
            lock (_tasks)
                _tasks.Add(taskName, tp);
            _tasks[taskName].Start();
        }

        public void DropTask(string taskName)
        {
            TaskProcess tp;
            if (!_tasks.TryGetValue(taskName, out tp))
                return;
            tp.Dispose();
            _tasks.Remove(taskName);
        }

        public void EnqueueToTask(string taskName, string[] subTaskKeys)
        {
            TaskProcess tp;
            if (!_tasks.TryGetValue(taskName, out tp))
                throw new KeyNotFoundException();
            tp.Enqueue(subTaskKeys);
        }

        public void DequeueFromTask(string taskName, string[] subTaskKeys)
        {
            TaskProcess tp;
            if (!_tasks.TryGetValue(taskName, out tp))
                throw new KeyNotFoundException();
            tp.Dequeue(subTaskKeys);
        }

        public SubTaskResult[] ReturnCompletedResults(string taskName)
        {
            TaskProcess tp;
            if (!_tasks.TryGetValue(taskName, out tp))
                throw new KeyNotFoundException();
            var ret =  tp.ReturnCompletedResults();
#if DEBUG
            foreach (var r1 in ret)
            {
                if (r1 == null)
                    throw new Exception("logic error!");
            }
#endif
            return ret;
        }

        #endregion ISlaveNode methods

        #region IService methods
        public void Start()
        {
            foreach (var tp in _tasks.Values)
                tp.Start();
            _isStarted = true;
        }

        public void Stop()
        {
            foreach (var tp in _tasks.Values)
                tp.Stop();
            _isStarted = false;
        }

        public bool IsStarted()
        {
            return _isStarted;
        }

        public void Restart()
        {
            if(IsStarted())
                Stop();
            Start();
        }
        #endregion IService methods
    }
}
