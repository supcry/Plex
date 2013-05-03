using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Plex.Contracts;
using Plex.Contracts.Data;
using Plex.Helpers;
using Plex.Proxy;
using Plex.Storage;
using System.Collections.Concurrent;

namespace Plex.Slave
{
    internal class TaskProcess : IDisposable, IService
    {
        private readonly string _nodeName;
        private readonly string _taskName;
        private readonly Connection _masterConnect;
        private readonly FileStorage _storage;
        private readonly bool _haltOnException;
        private readonly int _maxThreadsCount;
        private bool _isStarted;

        private NodeTaskStatus _status;
        

        private readonly Dictionary<string, ulong> _mainFileList;

        private readonly Dictionary<string, SubTaskProcess> _subTasks = new Dictionary<string, SubTaskProcess>();
        private readonly ConcurrentQueue<string> _keys = new ConcurrentQueue<string>();
        private readonly ConcurrentQueue<SubTaskResult> _results = new ConcurrentQueue<SubTaskResult>();

        private Thread _schedulerThread;
        private readonly AutoResetEvent _schedulerLocker = new AutoResetEvent(true);
        private KeyValuePair<AppDomain, ExecProxy>? _execDomain;

        public class SubTaskProcess
        {
            public string Key;
            public DateTime StartDate;
            public Thread Thread;

            public bool ToDrop { get; private set; }
            public void Drop()
            {
                ToDrop = true;
                Thread.Abort();
            }

            public TaskProcessInfo.SubTaskProcessInfo GetInfo()
            {
                return new TaskProcessInfo.SubTaskProcessInfo
                           {
                               Key = Key,
                               StartDate = StartDate
                           };
            }
        }

        public TaskProcess(string nodeName, string taskName, Connection masterConnect,
                           string workDir, Dictionary<string, ulong> mainFileList,
                           bool haltOnException, int maxThreadsCount)
        {
            _nodeName = nodeName;
            _taskName = taskName;
            _masterConnect = masterConnect;
            _storage = new FileStorage(workDir, taskName, masterConnect);
            _mainFileList = mainFileList;
            _haltOnException = haltOnException;
            _maxThreadsCount = maxThreadsCount;
            _status = NodeTaskStatus.Unknown;
        }

        private void Init()
        {
            _status = NodeTaskStatus.Initing;
            _storage.Precache(_mainFileList);
            _execDomain = null;
            _status = NodeTaskStatus.Ready;
            _execDomain = ExecProxy.StartDomain(_storage.WorkDir, _taskName, null, _mainFileList.Keys);
            _mainFileList.Clear();
        }

        public TaskProcessInfo GetTaskInfo()
        {
            return new TaskProcessInfo
                       {
                           TaskName = _taskName,
                           SubTasksToProcess = _keys.Count,
                           SubTasksFinished = _results.Count,
                           SubTasksInProgress = _subTasks.Values.Select(p => p.GetInfo()).ToArray()
                       };
        }

        public NodeTaskStatus GetStatus()
        {
            return _status;
        }

        public void Dispose()
        {
            
            Stop();
            if (_execDomain.HasValue)
            {
                try
                {
                    _execDomain.Value.Value.CleanUp();
                }catch{}
                AppDomain.Unload(_execDomain.Value.Key);
                _execDomain = null;
            }
            _keys.Clear();
            _results.Clear();
            _subTasks.Clear();
            _storage.Clear();
            _schedulerLocker.Close();
            _status = NodeTaskStatus.Halt;
        }

        public void Enqueue(string[] keys)
        {
           
            Trace.TraceInformation("Рабочей ноде " + _nodeName + " добавили " + keys.Length + " подзадач для задачи " + _taskName);
            _keys.EnqueueRange(keys);
            _schedulerLocker.Set();
        }

        public void Dequeue(string[] keys)
        {
            lock (this)
            {
                var keysH = new HashSet<string>(keys);
                // Вычистим ключи необработанных задач
                _keys.Remove(keysH);
                
                // Пробежимся по текущим задачам. Прервём уже ненужные.
                foreach (var key in keys)
                {
                    SubTaskProcess st;
                    if (_subTasks.TryGetValue(key, out st))
                        st.Drop();
                }
                // Пробежимся по тем, которые уже обработаны
                _results.Remove(p => keysH.Contains(p.Key));
            }
        }

        private void SchedulerThread()
        {
            try
            {
                while (_schedulerLocker.WaitOne())
                {
                    if (!_isStarted)
                        return;

                    if (_keys.Count != 0)
                    {
                        Trace.TraceInformation("Рабочая нода " + _nodeName + " начала  " + _keys.Count + " подзадач для задачи " + _taskName);
                        _status = NodeTaskStatus.Working;
                        // выполним все имеющиеся подзадачи
                        if(_maxThreadsCount>1)
                            Parallel.ForEach(_keys.DequeueRange(), new ParallelOptions{MaxDegreeOfParallelism = _maxThreadsCount}, SchedulerTaskExecute);
                        else
                            foreach (var key in _keys.DequeueRange())
                                SchedulerTaskExecute(key);
                        if (!_isStarted)
                            return;

                        Trace.TraceInformation("Рабочая нода " + _nodeName + " закончила обработку подзадач. Осталось " + _keys.Count + " штук");
                        // вернём результаты руту
                        if (_keys.Count == 0)
                        {
                            Trace.TraceInformation("Рабочая нода " + _nodeName + " посылает сообщение о завершении работы над всем переданными подзадачами");
                            _status = NodeTaskStatus.Complete;
                            SendStatus();
                        }
                    }
                }
            }
            catch (ThreadAbortException)
            {
                Trace.TraceError("SchedulerThread: shutting down by user");
            }
            catch (Exception ex)
            {
                Trace.TraceError("SchedulerThread.CriticalError: " + ex);
                Stop();
            }
        }

        private void SchedulerTaskExecute(string key)
        {
            if (!_isStarted)
                return;
            var task = new SubTaskProcess
            {
                Key = key,
                StartDate = DateTime.Now,
                Thread = Thread.CurrentThread
            };
            lock(this)
             _subTasks.Add(key, task);

            if (!_isStarted)
                return;
            try
            {

                Debug.Assert(_execDomain.HasValue);
                var t = DateTime.Now;
                var ret = _execDomain.Value.Value.Function(key, _storage);
                if (!_isStarted)
                    return;
                var res = new SubTaskResult(key, ret, DateTime.Now - t);
                lock (this)
                {
                    _subTasks.Remove(key);
                    if (!task.ToDrop)
                        _results.Enqueue(res);
                }
            }
            catch(OutOfMemoryException)
            {
                lock (this)
                {
                    _subTasks.Remove(key);
                    if (!task.ToDrop)
                        _results.Enqueue(new SubTaskResult(key, SubTaskResult.ResultType.OutOfMemory));
                }
            }
            catch (TimeoutException)
            {
                lock (this)
                {
                    _subTasks.Remove(key);
                    if (!task.ToDrop)
                        _results.Enqueue(new SubTaskResult(key, SubTaskResult.ResultType.TimeOut));
                }
            }
            catch (ThreadAbortException)
            {
                lock (this)
                {
                    _subTasks.Remove(key);
                    if (!task.ToDrop)
                        _results.Enqueue(new SubTaskResult(key, SubTaskResult.ResultType.Interrupted));
                }
            }
            catch (ThreadInterruptedException)
            {
                lock (this)
                {
                    _subTasks.Remove(key);
                    if (!task.ToDrop)
                        _results.Enqueue(new SubTaskResult(key, SubTaskResult.ResultType.Interrupted));
                }
            }
            catch (Exception ex)
            {
                lock (this)
                {
                    _subTasks.Remove(key);
                    if (!task.ToDrop)
                    {
                        if (_haltOnException)
                            lock (this)
                            {
                                _keys.Clear();
                                _results.Clear();
                                foreach (var tp in _subTasks)
                                    if (tp.Key != key)
                                        tp.Value.Drop();

                            }
                        _results.Enqueue(new SubTaskResult(key, SubTaskResult.ResultType.OtherException, ex.ToString()));
                    }
                }
            }
        }

        private void SendStatus()
        {
            var root = new MasterNodeClient(_taskName, _masterConnect);
            root.Event(_nodeName, _status);
        }

        public SubTaskResult[] ReturnCompletedResults()
        {
            var ret = _results.DequeueRange(Constants.MaxWCFArrayLength).ToArray();
            if(_status == NodeTaskStatus.Complete)
                _status = NodeTaskStatus.Ready;
#if DEBUG
            foreach (var r1 in ret)
            {
                if (r1 == null)
                    throw new Exception("logic error!");
            }
#endif
            return ret;
        }

        #region IService methods
        public void Start()
        {
            if (_isStarted)
                return;
            lock(this)
            {
                if (_isStarted)
                    return;
                if (_mainFileList.Count > 0)
                    Init();
                _schedulerThread = new Thread(SchedulerThread) {Name = "scheduletThread." + _taskName};
                _schedulerLocker.Set();
                _isStarted = true;
                _schedulerThread.Start();
                _status = NodeTaskStatus.Ready;
                SendStatus();
            }
        }

        public void Stop()
        {
            // Остановим текущие подзадачи. Вернём их в очередь.
            lock (this)
            {
                _status = NodeTaskStatus.Stop;
                _isStarted = false;
                if (Thread.CurrentThread != _schedulerThread)
                {
                    if (_schedulerThread != null && _schedulerThread.IsAlive)
                    {
                        _schedulerLocker.Set();
                        Thread.Sleep(100);
                        if (_schedulerThread.IsAlive)
                            _schedulerThread.Abort();
                    }
                        
                }
                //_schedulerThread = null;

                foreach (var kst in _subTasks)
                {
                    kst.Value.Thread.Abort();
                    _keys.Enqueue(kst.Key);
                }
                _subTasks.Clear();
                SendStatus();
            }
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
