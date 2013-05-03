using System;
using System.Collections.Generic;
using Plex.Contracts;
using Plex.Contracts.Data;

namespace Plex.Proxy
{
    public class SlaveNodeClient : Network.Proxy<ISlaveNode>, ISlaveNodeProxy
    {
        private readonly string _taskName;

        public SlaveNodeClient(string taskName, Connection con) :base(con, "slave")
        {
            _taskName = taskName;
        }

        public SlaveNodeClient(TaskConnection tcon) : this(tcon.TaskName, tcon.AsConnection){}

        public NodeInfo GetNodeInfo()
        {
            return Invoke(p => p.GetNodeInfo());
        }

        public TaskProcessInfo GetTaskProcessInfo()
        {
            return Invoke(p => p.GetTaskProcessInfo(_taskName));
        }

        public NodeTaskStatus GetStatus()
        {
            return Invoke(p => p.GetStatus(_taskName));
        }

        public void InitTask(Connection masterConnect, Dictionary<string, ulong> mainFileList)
        {
            Invoke(p => p.InitTask(_taskName, masterConnect, mainFileList, true));
        }

        public void DropTask()
        {
            Invoke(p => p.DropTask(_taskName));
        }

        public void EnqueueToTask(string[] subTaskKeys)
        {
            Invoke(p => p.EnqueueToTask(_taskName, subTaskKeys));
        }

        public void DequeueFromTask(string[] subTaskKeys)
        {
            Invoke(p => p.DequeueFromTask(_taskName, subTaskKeys));
        }

        public SubTaskResult[] ReturnCompletedResults()
        {
            return Invoke(p => p.ReturnCompletedResults(_taskName));
        }


        public void Start()
        {
            Invoke(p => p.Start());
        }

        public void Stop()
        {
            Invoke(p => p.Stop());
        }

        public bool IsStarted()
        {
            return Invoke(p => p.IsStarted(), TimeSpan.FromSeconds(5));
        }
    }
}
