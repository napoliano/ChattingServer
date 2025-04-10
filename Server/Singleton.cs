using System;


namespace Server
{
    public abstract class Singleton<T> where T : Singleton<T>, new()
    {
        public static T Instance => _instance.Value;
        private static readonly Lazy<T> _instance = new(() => new T());

        protected Singleton() 
        { }
    }
}
