using System;
using System.Collections.Generic;
using System.Linq;

namespace ExtensionFunctions
{
    public static class DictionaryExtensions
    {
        public static void ForEach<TK, TV>(this Dictionary<TK, TV> dictionary, Action<TK, TV> action)
        {
            foreach (var item in dictionary)
                action(item.Key, item.Value);
        }

        public static TK DrawRandom<TK>(this Dictionary<TK, float> dictionary)
        {
            var p = new Random().NextDouble() * dictionary.Values.Sum();
            var acc = 0f;
            foreach (var (key, value) in dictionary)
            {
                acc += value;
                if (acc >= p)
                    return key;
            }

            return dictionary.Keys.First();
        }
    }
}