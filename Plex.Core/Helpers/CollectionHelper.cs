using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Plex.Helpers
{
    public static class CollectionHelper
    {
        public static void Clear<T>(this ConcurrentQueue<T> q)
        {
            T tmp;
            while (q.TryDequeue(out tmp)){}
        }

        public static void Remove<T>(this Queue<T> q, ICollection<T> remove) where T : IEquatable<T>
        {
            var count = q.Count;
            for(int i=0;i<count;i++)
            {
                var item = q.Dequeue();
                if(!remove.Contains(item))
                    q.Enqueue(item);
            }
        }

        public static void Remove<T>(this Queue<T> q, Func<T, bool> func) where T : IEquatable<T>
        {
            var count = q.Count;
            for (int i = 0; i < count; i++)
            {
                var item = q.Dequeue();
                if (!func(item))
                    q.Enqueue(item);
            }
        }

        public static void Remove<T>(this ConcurrentQueue<T> q, ICollection<T> remove) where T : IEquatable<T>
        {
            var count = q.Count;
            for (int i = 0; i < count; i++)
            {
                T r;
                if(q.TryDequeue(out r))
                {
                    if (!remove.Contains(r))
                        q.Enqueue(r);
                }
                else
                    return;
            }
        }

        public static void Remove<T>(this ConcurrentQueue<T> q, Func<T, bool> func) where T : IEquatable<T>
        {
            var count = q.Count;
            for (int i = 0; i < count; i++)
            {
                T r;
                if (q.TryDequeue(out r))
                {
                    if (!func(r))
                        q.Enqueue(r);
                }
                else
                    return;
            }
        }

        public static void EnqueueRange<T>(this Queue<T> q, IEnumerable<T> data)
        {
            foreach(var d in data)
                q.Enqueue(d);
        }

        public static void EnqueueRange<T>(this ConcurrentQueue<T> q, IEnumerable<T> data)
        {
            foreach (var d in data)
                q.Enqueue(d);
        }

        public static IEnumerable<T> DequeueRange<T>(this Queue<T> q, int count = int.MaxValue)
        {
           while(count > 0 && q.Count > 0)
           {
               yield return q.Dequeue();
               count--;
           }
        }

        public static IEnumerable<T> DequeueRange<T>(this ConcurrentQueue<T> q, int count = int.MaxValue)
        {
            while (count > 0 && q.Count > 0)
            {
                T r;
                if (q.TryDequeue(out r))
                    yield return r;
                else
                    yield break;
                count--;
            }
        }
    }
}
