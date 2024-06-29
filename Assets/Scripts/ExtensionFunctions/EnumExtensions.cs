using System;

namespace ExtensionFunctions
{
    public static class EnumExtensions
    {
        private static readonly Random Random = new Random();

        public static T RandomItem<T>() where T : Enum
        {
            var values = Enum.GetValues(typeof(T));
            return (T)values.GetValue(Random.Next(values.Length));
        }
    }
}