using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Plex.Contracts.Data;
using System.Linq;

namespace Plex.Master
{
    /// <summary>
    /// Занимается агрегацией информации о существовании доступных нод.
    /// </summary>
    internal class NodeDiscover
    {

        private Thread _pinger;

        /// <summary>
        /// Список нод, которые пока безымянны
        /// </summary>
        private readonly List<NodePingInfo> _undiscovered = new List<NodePingInfo>();

        /// <summary>
        /// Список рабочих нод.
        /// </summary>
        private readonly Dictionary<string, NodePingInfo> _nodes = new Dictionary<string, NodePingInfo>(); 

        public NodeDiscover(IEnumerable<IConnection> cons)
        {
            foreach (var c in cons)
                _undiscovered.Add(new NodePingInfo(c));
        }

        public string[] GetAvailableNodes()
        {
            return _nodes.Keys.ToArray();
        }

        public Connection GetNodeConnection(string nodeName)
        {
            return _nodes[nodeName].Connection;
        }

        public void NodeStopped(string nodeName)
        {
            var n = _nodes[nodeName];
            _nodes.Remove(nodeName);
            n.SetHalted();
            _undiscovered.Add(n);
        }

        public NodeInfo GetNodeInfo(string nodeName)
        {
            return _nodes[nodeName].NodeInfo;
        }

        public void Start()
        {
            if(_pinger == null)
            {
                _pinger = new Thread(PingerThread) {IsBackground = true, Name = "PingerThread"};
                _pinger.Start();
            }
        }

        public void Stop()
        {
            if (_pinger == null)
                return;
            try
            {
                _pinger.Abort();
            }
            finally
            {
                _pinger = null;
            }
        }

        private void PingerThread()
        {
            //[ToDo] Распараллелить
            while(true)
            {
                var nextPingDate = DateTime.Now.AddSeconds(10);
                // пройдёмся по нормальным нодам
                foreach (var k in _nodes.Keys.ToArray())
                {
                    var n = _nodes[k];
                    if (n.NextPingDate < DateTime.Now && (!n.IsPinged || !n.Ping()))
                    {
                        _undiscovered.Add(n);
                        _nodes.Remove(k);
                    }
                    if (n.NextPingDate < nextPingDate)
                        nextPingDate = n.NextPingDate;
                }

                // а теперь по низвестным нодам
                bool changes = false;
                for(int i=0;i<_undiscovered.Count;i++)
                {
                    var n = _undiscovered[i];
                    if (n.NextPingDate < DateTime.Now && n.Ping())
                    {
                        _nodes.Add(n.NodeName, n);
                        _undiscovered[i] = null;
                        changes = true;
                    }
                    if (n.NextPingDate < nextPingDate)
                        nextPingDate = n.NextPingDate;
                }
                if (changes)
                    _undiscovered.RemoveAll(p => p == null);
                try
                {
                    var delay = nextPingDate - DateTime.Now;
                    if (delay>TimeSpan.Zero)
                        Thread.Sleep(delay);
                    else
                        Thread.Sleep(0);
                }catch(ThreadAbortException)
                {
                    break;
                }
                catch(Exception ex)
                {
                    Trace.TraceError("PingerThread: неизвестное исключение. Ex=" + ex);
                    throw;
                }
            }
        }
    }
}
