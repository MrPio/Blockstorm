using System;

namespace ExtensionFunctions
{
    public static class ObjectExtensions
    {
        public static T Apply<T>(this T obj, Action<T> configure)
        {
            configure?.Invoke(obj);
            return obj;
        }

        // public static TR Select<T, TR>(this T obj, Func<T, TR> select)
            // => select.Invoke(obj);
    }
}