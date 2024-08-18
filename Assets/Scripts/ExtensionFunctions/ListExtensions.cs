using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ExtensionFunctions
{
    public static class ListExtensions
    {
        public static T RandomItem<T>(this List<T> list) =>
            list[new System.Random().Next(0, list.Count)];

        public static List<T> Shuffle<T>(this List<T> list)
        {
            var n = list.Count;
            while (n > 1)
            {
                var randomIndex = Random.Range(0, --n + 1);
                (list[randomIndex], list[n]) = (list[n], list[randomIndex]);
            }

            return list;
        }

        public static void ForEach<T>(this List<T> list, Action<T, int> action)
        {
            for (var i = 0; i < list.Count; i++)
                action(list[i], i);
        }

        public static List<T> RandomSublist<T>(this List<T> list, int length)
            => list.ToList().Shuffle().Take(length).ToList();

        public static void Print<T>(this List<T> list)
        {
            foreach (var item in list)
                Debug.Log(item.ToString());
        }
    }
}