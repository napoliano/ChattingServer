using System;


namespace Server
{
    public static class EnumCache<T> where T : struct, Enum
    {
        public static T[] Values { get; } = Enum.GetValues<T>();
    }
}
