using System;
using System.Collections.Generic;
using Random = UnityEngine.Random;

namespace ExtensionFunctions
{
    public static class DictionaryExtensions
    {
        public static void ForEach<TK, TV>(this Dictionary<TK, TV> dictionary, Action<TK, TV> action)
        {
            foreach (var item in dictionary)
                action(item.Key, item.Value);
        }
    }
}