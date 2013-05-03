using System;
using System.Collections.Concurrent;
using Plex.Contracts.Data;
using Plex.Helpers;

namespace Plex.Network
{
    /// <summary>
    /// Прокси с поддержкой зеркал.
    /// Если зеркал нет, то работает как обычно. Всегда коннектится и т.п.
    /// Если есть зеркала, то:
    /// Выбирает для каждого нового коннекта последующее активное зеркало.
    /// Если коннект разорвался, то помечает его как упавший на минуту, пробует через следующий, пока есть с активным статусом.
    /// Если все коннекты с упавшим статусом, а попыток выбора ещё не было, то пытается приконнектиться к случайному зеркалу.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Proxy<T> : IProxy where T : class
    {
        private readonly ServiceReference _endPoint;

//        /// <summary>
//        /// Кеш имеющихся проксей.
//        /// </summary>
//// ReSharper disable StaticFieldInGenericType
//        private static readonly ConcurrentDictionary<string, ServiceReference> EndpointsContainer;
//// ReSharper restore StaticFieldInGenericType

        #region constructors and helpers
        //static Proxy()
        //{
        //    var numProcs = Environment.ProcessorCount;
        //    var concurrencyLevel = numProcs * 2;
        //    EndpointsContainer = new ConcurrentDictionary<string, ServiceReference>(concurrencyLevel, 10);
        //}
        
        /// <summary>
        /// Конструктор для тестов
        /// </summary>
        public Proxy(string address, int port, string srvName)
        {
            _endPoint = new ServiceReference
            {
                Address = address,
                Port = port,
                ServiceName = srvName,
            };
        }

        public Proxy(IConnection con, string srvName) : this(con.Address, con.Port, srvName){}

        //public Proxy(string srvName)
        //{//
        //    if (EndpointsContainer.TryGetValue(srvName, out _endPoint))
        //        return;
        //    EndpointsContainer[srvName] = _endPoint = new ServiceReference
        //    {
        //        Address = address,
        //        Port = port,
        //        ServiceName = srvName,
        //    };
        //    //var inst = CompositionHost.Container.GetExportedValue<T>(srvName);
        //    //if (!EndpointsContainer.TryGetValue(srvName, out _endPoint))
        //    //EndpointsContainer[srvName] = _endPoint = new ServiceReference { Instance = inst };
        //}
        #endregion constructors and helpers

        #region invoke
        public virtual void Invoke(Action<T> action, TimeSpan? timeOut = null)
        {
            _endPoint.Invoke(action, timeOut);
        }

        public virtual TResult Invoke<TResult>(Func<T, TResult> action, TimeSpan? timeOut = null)
        {
            return _endPoint.Invoke(action, timeOut);
        }
        #endregion invoke
    }
}
