using System;
using System.Diagnostics;
using Plex.Contracts.Data;
using Plex.Proxy;

namespace Plex.Master
{
    internal class NodePingInfo
    {
        private readonly TimeSpan _normalDelay = TimeSpan.FromSeconds(15);
        private readonly TimeSpan _haltedDelay = TimeSpan.FromMinutes(1);

        public readonly Connection Connection;

        public string NodeName { get; private set; }

        public NodeInfo NodeInfo { get; private set; }

        public bool IsPinged { get; private set; }

        public DateTime NextPingDate { get; private set; }

        public NodePingInfo(IConnection con, string nodeName = null)
        {
            Connection = new Connection(con);
            NextPingDate = DateTime.Now;
            IsPinged = false;
            NodeName = nodeName;
        }

        public bool Ping()
        {
            try
            {
                var c = new SlaveNodeClient(null, Connection);
                IsPinged = c.IsStarted();
                if (NodeName == null || IsPinged == false)
                {
                    NodeInfo = c.GetNodeInfo();
                    NodeName = NodeInfo.NodeName;
                }
                NextPingDate = DateTime.Now + _normalDelay;
                Trace.TraceInformation("node " + NodeName + " succesfully pinged");
                return true;
            }
            catch
            {
                SetHalted();
                return false;
            }
        }

        public void SetHalted()
        {
            IsPinged = false;
            NextPingDate = DateTime.Now + _haltedDelay;
        }
    }

}
