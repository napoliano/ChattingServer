using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.ObjectPool;


namespace Server
{
    public interface IResettable
    {
        void Reset();
    }

    public static class ObjectPool<T> where T : class, IResettable, new()
    {
        private class ObjectPoolPolicy : IPooledObjectPolicy<T>
        {
            public T Create()
            {
                return new();
            }

            public bool Return(T obj)
            {
                obj.Reset();
                return true;
            }
        }

        private static readonly Microsoft.Extensions.ObjectPool.ObjectPool<T> _pool = new DefaultObjectPool<T>(new ObjectPoolPolicy(), GlobalConstants.Network.MaxObjectPoolSize);

        public static T Rent()
        {
            return _pool.Get();
        }

        public static void Return(T item)
        {
            _pool.Return(item);
        }
    }
}
