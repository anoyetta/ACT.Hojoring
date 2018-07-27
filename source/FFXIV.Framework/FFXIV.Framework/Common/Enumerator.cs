using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Threading;

namespace FFXIV.Framework.Common
{
    public static class Enumerator
    {
        public static IEnumerable<DispatcherPriority> GetDispatcherPriorities()
            => (DispatcherPriority[])Enum.GetValues(typeof(DispatcherPriority));

        public static IEnumerable<ThreadPriority> GetThreadPriorities()
            => (ThreadPriority[])Enum.GetValues(typeof(ThreadPriority));
    }
}
