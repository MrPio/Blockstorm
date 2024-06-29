using System.Collections.Generic;
using UnityEngine;

namespace ExtensionFunctions
{
    public static class BoolExtensions
    {
        static Dictionary<int, int> _frames = new();

        public static bool GetAsTrigger(this ref bool value)
        {
            var tmp = (_frames.ContainsKey(value.GetHashCode()) && _frames[value.GetHashCode()] == Time.frameCount) ||
                      value;
            if (tmp)
                _frames[value.GetHashCode()] = Time.frameCount;
            value = false;
            return tmp;
        }
        public static void Toggle(this ref bool value)
        {
            value = !value;
        }
    }
}