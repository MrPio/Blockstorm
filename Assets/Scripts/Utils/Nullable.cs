using System;

namespace Utils
{
    [Serializable]
    public class Nullable<T>
    {
        public T value;
        public bool hasValue;

        public Nullable(T value, bool hasValue)
        {
            this.value = value;
            this.hasValue = hasValue;
        }
    }
}