using System.Collections.Generic;
using Plex.Contracts;
using Plex.Contracts.Data;
using Plex.Helpers;

namespace Plex.Proxy
{
    public class PlexClient : Network.Proxy<IPlex>, IPlexProxy
    {
        private readonly string _taskName;

        public PlexClient(string taskName, IConnection con) : base(con, "plex")
        {
            _taskName = taskName;
        }



        //public NodeInfo[] GetNodesInfo()
        //{
        //    return Invoke(p => p.)
        //}

        public void InitTaskLocal(string workDir, string[] mainFileList, string[] extraFileList = null)
        {
            Invoke(p => p.InitTaskLocal(_taskName, workDir, mainFileList,extraFileList));
        }

        public void InitTaskRemote(KeyValuePair<string, byte[]>[] mainFileList, KeyValuePair<string, byte[]>[] extraFileList = null)
        {
            Invoke(p => p.InitTaskRemote(_taskName, mainFileList, extraFileList));
        }

        public void DropTask()
        {
            Invoke(p => p.DropTask(_taskName));
        }

        public void EnqueueToTask(string[] subTaskKeys)
        {
            foreach (var s in subTaskKeys.EnumerateByPacks(Constants.MaxWCFArrayLength))
                Invoke(p => p.EnqueueToTask(_taskName, s));
        }

        public void DequeueFromTask(string[] subTaskKeys)
        {
            Invoke(p => p.DequeueFromTask(_taskName, subTaskKeys));
        }

        public SubTaskResult[] GetTaskResults()
        {
            return Invoke(p => p.GetTaskResults(_taskName));
        }

        //public TaskProcessInfo GetTaskProcessInfo()
        //{
        //    return Invoke(p => p.GetProgressInfo(_taskName));
        //}

        public ProgressInfo GetProgressInfo()
        {
            return Invoke(p => p.GetProgressInfo(_taskName));
        }

        public NodeTaskStatus GetStatus()
        {
            return Invoke(p => p.GetStatus(_taskName));
        }
    }
}
