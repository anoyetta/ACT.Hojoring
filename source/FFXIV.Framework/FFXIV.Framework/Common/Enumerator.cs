using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Threading;
using FFXIV.Framework.Extensions;

namespace FFXIV.Framework.Common
{
    public static class Enumerator
    {
        public static IEnumerable<DispatcherPriority> GetDispatcherPriorities()
            => (DispatcherPriority[])Enum.GetValues(typeof(DispatcherPriority));

        public static IEnumerable<ThreadPriority> GetThreadPriorities()
            => (ThreadPriority[])Enum.GetValues(typeof(ThreadPriority));
    }

    public class EnumContainer<T> where T : Enum
    {
        public EnumContainer()
        {
        }

        public EnumContainer(T value)
        {
            this.Value = value;
        }

        public T Value { get; set; }

        public string Display => this.Value.DisplayName();

        public override string ToString() => this.Display;
    }
}
