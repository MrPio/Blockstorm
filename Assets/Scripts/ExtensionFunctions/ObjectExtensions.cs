using System;
using System.Collections.Generic;
using Model;
using UnityEngine;

namespace ExtensionFunctions
{
    public static class ObjectExtensions
    {
        public static T Apply<T>(this T obj, Action<T> configure)
        {
            configure.Invoke(obj);
            return obj;
        }

        // public static TR Select<T, TR>(this T obj, Func<T, TR> select)
            // => select.Invoke(obj);
            
                    static Dictionary<int, Tuple<int, WeaponType>> _frames = new();

        public static WeaponType? GetAsTrigger(this ref WeaponType? value)
        {
            var tmp = _frames.ContainsKey(value.GetHashCode()) && _frames[value.GetHashCode()].Item1 == Time.frameCount
                ? _frames[value.GetHashCode()].Item2
                : value;
            if (value!=null)
                _frames[value.GetHashCode()] = new Tuple<int, WeaponType>(Time.frameCount, value.Value);
            value = null;
            return tmp;
        }
    }
}