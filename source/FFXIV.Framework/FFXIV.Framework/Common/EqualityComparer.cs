using System;
using System.Collections.Generic;

namespace FFXIV.Framework.Common
{
    public class GenericEqualityComparer<T> : IEqualityComparer<T>
    {
        private Func<T, T, bool> equalsCallback;
        private Func<T, int> getHashCodeCallback;

        public GenericEqualityComparer(
            Func<T, T, bool> equalsCallback,
            Func<T, int> getHashCodeCallback = null)
        {
            this.equalsCallback = equalsCallback;
            this.getHashCodeCallback = getHashCodeCallback;
        }

        public bool Equals(T x, T y) =>
            this.equalsCallback?.Invoke(x, y) ?? object.Equals(x, y);

        public int GetHashCode(T obj) =>
            this.getHashCodeCallback?.Invoke(obj) ?? obj.GetHashCode();
    }
}
