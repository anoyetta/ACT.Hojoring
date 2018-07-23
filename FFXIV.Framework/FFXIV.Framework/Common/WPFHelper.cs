using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;

namespace FFXIV.Framework.Common
{
    public class WPFHelper
    {
        private static readonly object Locker = new object();

        public static DispatcherOperation BeginInvoke(
            Action action,
            DispatcherPriority priority = DispatcherPriority.Background)
        {
            return Application.Current?.Dispatcher.BeginInvoke(
                action,
                priority);
        }

        public static void Invoke(
            Action action,
            DispatcherPriority priority = DispatcherPriority.Background)
        {
            Application.Current?.Dispatcher.Invoke(
                action,
                priority);
        }

        public static DispatcherOperation InvokeAsync(
            Action action,
            DispatcherPriority priority = DispatcherPriority.Background)
        {
            return Application.Current?.Dispatcher.InvokeAsync(
                action,
                priority);
        }

        public static void Start()
        {
            lock (Locker)
            {
                if (Application.Current == null)
                {
                    new Application().ShutdownMode = ShutdownMode.OnExplicitShutdown;
                }
            }
        }

        public static void Shutdown()
        {
            if (Application.Current == null)
            {
                Application.Current.Shutdown();
            }
        }

#if DEBUG
        private static int _IsDebugMode = -1;
#endif

        /// <summary>
        /// 現在のプロセスがデザインモードかどうか返します。
        /// </summary>
        public static bool IsDesignMode
        {
            get
            {
#if DEBUG
                if (_IsDebugMode == -1)
                {
                    if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                    {
                        _IsDebugMode = 1;
                    }
                    else
                    {
                        using (var p = Process.GetCurrentProcess())
                        {
                            _IsDebugMode = (
                                p.ProcessName.Equals("DEVENV", StringComparison.OrdinalIgnoreCase) ||
                                p.ProcessName.Equals("XDesProc", StringComparison.OrdinalIgnoreCase)
                            ) ? 1 : 0;
                        }
                    }
                }

                return _IsDebugMode == 1;
#else
                return false;
#endif
            }
        }
    }
}
