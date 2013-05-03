using Plex.Contracts;
using Plex.Contracts.Data;

namespace Plex.Proxy
{
    public class MasterNodeClient :  Network.Proxy<IMasterNode>, IMasterNodeProxy
    {
        private readonly string _taskName;

        public MasterNodeClient(string taskName, IConnection con) : base(con.Address, con.Port, "master")
        {
            _taskName = taskName;
        }

        public MasterNodeClient(TaskConnection tcon) : this(tcon.TaskName, tcon.AsConnection){}

        public void Event(string nodeName, NodeTaskStatus status)
        {
            Invoke(p => p.Event(_taskName, nodeName, status));
        }
    }
}
