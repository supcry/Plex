using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Plex.Contracts;
using Plex.Contracts.Data;
using Plex.Helpers;
using Plex.Storage;

namespace Plex.Master
{
    internal class TaskScheduler : IPlexProxy, IService, IMasterNodeProxy, IDisposable
    {
        private readonly TimeSpan _optimalRequestDelay = TimeSpan.FromSeconds(10);

        private readonly string _taskName;
        private readonly FileStorage _storage;
        public FileStorage Storage { get { return _storage; } }
        private readonly Dictionary<string, ulong> _mainFileList;
        private bool _isStarted;

        private readonly ConcurrentQueue<string> _queue = new ConcurrentQueue<string>();
        private readonly Dictionary<string, NodeTaskData> _inProgress = new Dictionary<string, NodeTaskData>();
        private readonly ConcurrentQueue<SubTaskResult> _result = new ConcurrentQueue<SubTaskResult>();

        private readonly NodeDiscover _finder;
        private readonly Connection _connection;
        private Thread _schedulerThread;
        private readonly AutoResetEvent _schedulerLocker = new AutoResetEvent(true);

        public TaskScheduler(string taskName, string workDir, string[] mainFileList, NodeDiscover finder, Connection connection)
        {
            _taskName = taskName;
            _storage = new FileStorage(workDir, taskName);
            _mainFileList = _storage.Precache(mainFileList);
            _finder = finder;
            _connection = connection;
        }

        #region IMasterNodeProxy
        public void Event(string nodeName, NodeTaskStatus status)//oneway
        {
            _inProgress[nodeName].Event(status);
            _schedulerLocker.Set();
        }
        #endregion IMasterNodeProxy

        #region IPlexProxy
        public void InitTaskLocal(string workDir, string[] mainFileList, string[] externaFileList)
        {
            throw new Exception("Этот метод не должен вызываться");
        }

        public void InitTaskRemote(KeyValuePair<string, byte[]>[] mainFileList, KeyValuePair<string, byte[]>[] externaFileList)
        {
            throw new Exception("Этот метод не должен вызываться");
        }

        public void DropTask()//oneway
        {
            Stop();
            _queue.Clear();
            foreach (var t in _inProgress)
                t.Value.DropTask();
            _inProgress.Clear();
            _result.Clear();
        }

        public void EnqueueToTask(string[] subTaskKeys)//oneway
        {
            Trace.TraceInformation("Мастер ноде добавили " + subTaskKeys.Length + " подзадач для задачи " + _taskName);
            _queue.EnqueueRange(subTaskKeys);
        }

        public void DequeueFromTask(string[] subTaskKeys)//oneway
        {
            
            lock (this)
            {
                var set = new HashSet<string>(subTaskKeys);
                _queue.Remove(set);
                foreach (var t in _inProgress)
                    t.Value.DropSubTasks(set);
                _result.EnqueueRange(_result.Where(p => set.Contains(p.Key)));
            }
        }

        public SubTaskResult[] GetTaskResults()
        {
            var ret = _result.DequeueRange().ToArray();
#if DEBUG
            foreach (var r1 in ret)
            {
                if (r1 == null)
                    throw new Exception("logic error!");
            }
#endif
            return ret;
        }

        public ProgressInfo GetProgressInfo()
        {
            return new ProgressInfo
                       {
                           ReadyToDistribute = _queue.Count,
                           ReadyToReturn = _result.Count,
                           InProgress = -1,
                           Halted = false
                       };

        }

        public NodeTaskStatus GetStatus()
        {
            if (!_isStarted)
                return NodeTaskStatus.Stop;
            if(_inProgress.Any(p => p.Value.Status == NodeTaskStatus.Working))
                return NodeTaskStatus.Working;
            if (_queue.Count == 0 && _result.Count > 0)
                return NodeTaskStatus.Complete;
            if (_queue.Count == 0 && _result.Count == 0)
                return NodeTaskStatus.Ready;
            return NodeTaskStatus.Unknown;
        }
        #endregion IPlexProxy

        #region IService methods
        public void Start()
        {
            if (_isStarted)
                return;
            lock (this)
            {
                if (_isStarted)
                    return;
                _schedulerThread = new Thread(SchedulerThread)
                                       {Name = "RootScheduler." + _taskName, IsBackground = true};
                _schedulerThread.Start();
                _isStarted = true;
            }
        }

        public void Stop()
        {
            if (!_isStarted)
                return;
            lock (this)
            {
                if (!_isStarted)
                    return;
                _isStarted = false;
                _schedulerLocker.Set();
                Thread.Sleep(100);
                if (_schedulerThread.IsAlive)
                    _schedulerThread.Abort();
            }
        }

        public bool IsStarted()
        {
            return _isStarted;
        }

        public void Restart()
        {
            Stop();
            Start();
        }
        #endregion IService methods

        public void Dispose()
        {
            Stop();
            if(_storage != null)
                _storage.Clear();
        }

        private double RecalcAverageSpeed()
        {
            var tmp = new List<double>();
            foreach (var np in _inProgress)
            {
                var val = np.Value.SubPackPerCpuScorePerSecond;
                if(val.HasValue)
                    tmp.Add(val.Value);
            }
            return tmp.Count > 0 ? tmp.Average() : 0;
        }

        private void SchedulerThread()
        {
            const int minSubPack = 10;

            while(_isStarted)
            {
                try
                {
                    _schedulerLocker.WaitOne(_optimalRequestDelay);
                    if (!_isStarted)
                        return;
                    // 1. проверим, не остановились ли некоторые ноды
                    var tmp = new List<string>();
                    foreach (var n in _inProgress)
                    {
                        if(!n.Value.IsHalted)
                            continue;
                        Trace.TraceWarning(Thread.CurrentThread.Name + ": Рабочая нода" + n.Key + " упала. Передаём её подзадачи в общую очередь");
                        _queue.EnqueueRange(n.Value.ExportKeys());
                        tmp.Add(n.Key);
                    }
                    tmp.ForEach(p => _inProgress.Remove(p));
                    if (!_isStarted)
                        return;
                    // 2. проверим новые ноды.
                    var nodes = _finder.GetAvailableNodes().Where(p => !_inProgress.ContainsKey(p)).ToArray();
                    var curSubPackPerCpuScorePerSecond = RecalcAverageSpeed();
                    foreach(var n in nodes)
                    {
                        var ninfo = _finder.GetNodeInfo(n);
                        var ntd = new NodeTaskData(_taskName, _finder.GetNodeConnection(n), ninfo);

                        //[ToDo] Улучшить алгоритм вычисления размера пачки
                        var packSize = Math.Min(
                                         (int)Math.Round(Math.Max(
                                           curSubPackPerCpuScorePerSecond * ninfo.CpuScore * _optimalRequestDelay.TotalSeconds,
                                           minSubPack)),
                                         Constants.MaxWCFArrayLength);
                        
                        var pack = _queue.DequeueRange(packSize).ToArray();
                        try
                        {
                            Trace.TraceInformation(Thread.CurrentThread.Name + ": Добавляем ноду " + n);
                            _inProgress.Add(n, ntd);
                            ntd.Init(_connection, _mainFileList);
                            ntd.Enqueue(pack);
                        }
                        catch(Exception ex)
                        {
                            Trace.TraceError(Thread.CurrentThread.Name + ": Не удалось создать новую рабочую точку " + n + ", Ex=" + ex);
                            _queue.EnqueueRange(pack);
                        }
                    }

                    if (!_isStarted)
                        return;
                    // пройдёмся по тем нодам, о которых давно не вспоминали, либо которые имеют статус complete
                    var curDate = DateTime.Now;
                    foreach (var n in _inProgress.Where(p => p.Value.LastActiveDate + _optimalRequestDelay < curDate || p.Value.Status == NodeTaskStatus.Complete))
                    {
                        var r = n.Value.ReturnCompletedResults();

                        Trace.TraceInformation(Thread.CurrentThread.Name + ": Нода " + n.Key + " вернула " + r.Count + " результатов");

                        _result.EnqueueRange(r);
                    }

                    if (!_isStarted)
                        return;
                    // 3. заполним пустые рабочие ноды
                    if(_queue.Count > 0)
                    foreach (var n in _inProgress.Where(p => p.Value.IsEmpty))
                    {
                        //Debug.Assert(n.Value.SupPackPerSecond.HasValue);
                        //[ToDo] Улучшить алгоритм вычисления размера пачки
                        var packSize = n.Value.SupPackPerSecond.HasValue
                            ? (int)Math.Round(Math.Max(n.Value.SupPackPerSecond.Value * _optimalRequestDelay.TotalSeconds, minSubPack))
                            : minSubPack;
                        
                        var pack = _queue.DequeueRange(packSize).ToArray();
                        Trace.TraceInformation(Thread.CurrentThread.Name + ": Добавим дополнительно " + pack.Length +" элементов в ноду " + n.Key);
                        try
                        {
                            n.Value.Enqueue(pack);
                        }
                        catch (Exception ex)
                        {
                            Trace.TraceError(Thread.CurrentThread.Name + ": Не удалось добавить новые элементы в ноду " + n.Key + ", Ex=" + ex);
                            _queue.EnqueueRange(pack);
                            _queue.EnqueueRange(n.Value.ExportKeys());
                            n.Value.DropTask();
                        }
                    }

                    if (!_isStarted)
                        return;
                    // 4. удалим из списка все закрытые ноды:
                    foreach (var n in _inProgress.Where(p => p.Value.IsHalted).Select(p => p.Key).ToArray())
                        _inProgress.Remove(n);
                }
                catch(Exception ex)
                {
                    if (!_isStarted)
                        Trace.TraceInformation(Thread.CurrentThread.Name + ": Вышел по требования");
                    else
                        Trace.TraceInformation(Thread.CurrentThread.Name + ": Поток был прерван по невыясненной причине. Ex=" + ex);
                    return;
                }
            }
        }
    }
}
