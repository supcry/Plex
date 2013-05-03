using System;
using System.Collections.Generic;
using System.Linq;
using Plex.Contracts.Data;
using Plex.Helpers;
using Plex.Proxy;

namespace Plex.Master
{
    internal class NodeTaskData
    {
        private readonly TaskConnection _connection;

        private readonly NodeInfo _nodeInfo;

        private readonly HashSet<string> _inProgress = new HashSet<string>();

        private int _executedSuccessfullyCount;

        private TimeSpan _executedSuccessfullyTime = TimeSpan.Zero;

        public DateTime LastActiveDate { get; private set; }
        public bool IsHalted { get { return Status == NodeTaskStatus.Halt; } }

        public bool IsEmpty { get { return _inProgress.Count == 0; } }

        public NodeTaskStatus Status { get; private set; }

        public double? SupPackPerSecond
        {
            get { return _executedSuccessfullyCount == 0 ? null : (double?)_executedSuccessfullyCount / _executedSuccessfullyTime.TotalSeconds; }
        }

        public double? SubPackPerCpuScorePerSecond
        {
            get { return _executedSuccessfullyCount == 0 ? null : (double?) _executedSuccessfullyCount / (_executedSuccessfullyTime.TotalSeconds * _nodeInfo.CpuScore); }
        }

        public NodeTaskData(string task, Connection con, NodeInfo info)
        {
            _connection = new TaskConnection(con.Address, con.Port, task);
            _nodeInfo = info;
            LastActiveDate = DateTime.Now;
        }

        public void Returned(IEnumerable<SubTaskResult> result)
        {
            lock(this)
                foreach(var r in result)
                {
                    _inProgress.Remove(r.Key);
                    if (r.Type == SubTaskResult.ResultType.Success)
                    {
                        _executedSuccessfullyCount++;
                        _executedSuccessfullyTime += r.Time;
                    }
                }
        }

        public List<SubTaskResult> ReturnCompletedResults()
        {
            var ret = new List<SubTaskResult>();
            var n = new SlaveNodeClient(_connection);
            var b = n.IsStarted();
            //var s = n.IsStarted();
            //var i = n.GetStatus();
            SubTaskResult[] tmp;
            do
            {
                tmp = n.ReturnCompletedResults();
#if DEBUG
                foreach (var r1 in ret)
                {
                    if (r1 == null)
                        throw new Exception("logic error!");
                }
#endif
                Returned(tmp);
                ret.AddRange(tmp);
            } while (tmp.Length == Constants.MaxWCFArrayLength); 
            return ret;
        }

        public void Event(NodeTaskStatus status)
        {
            LastActiveDate = DateTime.Now;
            Status = status;
        }

        public void DropTask()
        {
            _inProgress.Clear();
            var c = new SlaveNodeClient(_connection);
            c.DropTask();
            Event(NodeTaskStatus.Halt);
        }

        public void DropSubTasks(HashSet<string> subTaskKeys)
        {
            var st = _inProgress.Where(subTaskKeys.Contains).ToArray();
            if(st.Length > 0)
            {
                var c = new SlaveNodeClient(_connection);
                c.DequeueFromTask(st);
            }
            LastActiveDate = DateTime.Now;
        }

        public void Init(Connection masterConnect, Dictionary<string, ulong> mainFileList)
        {
            var c = new SlaveNodeClient(_connection);
            c.InitTask(masterConnect, mainFileList);
            Event(NodeTaskStatus.Initing);
        }

        public void Enqueue(string[] subTaskKeys)
        {
            var c = new SlaveNodeClient(_connection);
            c.EnqueueToTask(subTaskKeys);
            LastActiveDate = DateTime.Now;
        }

        public IEnumerable<string> ExportKeys()
        {
            return _inProgress;
        } 


    }
}