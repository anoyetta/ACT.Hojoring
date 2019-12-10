using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using FFXIV.Framework.WPF.Views;
using NLog;

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

                    // UnhandledException のイベントハンドラを設定する
                    Application.Current.DispatcherUnhandledException += OnDispatcherUnhandledException;

                    // WPFにおけるハードウェアアクセラレータを無効にする
                    RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
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

        /// <summary>
        /// ハンドルされない例外ハンドラ
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">イベント引数</param>
        private static void OnDispatcherUnhandledException(
            object sender,
            DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                AppLog.DefaultLogger.Fatal(
                    e.Exception,
                    "Unhandled Exception");
                LogManager.Flush();

                InvokeAsync(() => ModernMessageBox.ShowDialog(
                    "Fatal Error.\nUnhandled Exception.",
                    "Fatal Error",
                    MessageBoxButton.OK,
                    e.Exception));

                e.Handled = true;
            }
            catch (Exception)
            {
            }
        }

        public async static void DelayTask(int msec = 0) => await Task.Delay(msec);
    }
}
