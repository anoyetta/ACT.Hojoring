using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using FFXIV.Framework.Common;
using FFXIV.Framework.XIVHelper;

namespace FFXIV.Framework.WPF.Views
{
    public static class OverlayExtensions
    {
        #region Win32 API

        public const int GWL_EXSTYLE = (-20);

        public const int WS_EX_NOACTIVATE = 0x08000000;
        public const int WS_EX_TRANSPARENT = 0x00000020;

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        #endregion Win32 API

        public static void ChangeTopMost(
            this Window window,
            bool value)
        {
            if (window.Topmost != value)
            {
                window.Topmost = value;
            }
        }

        /// <summary>
        /// オーバーレイの表示を切り替える
        /// </summary>
        /// <param name="overlay">overlay</param>
        /// <param name="overlayVisible"></param>
        /// <param name="newValue"></param>
        /// <param name="opacity"></param>
        /// <returns>
        /// 切り替わったか否か？</returns>
        public static bool SetOverlayVisible(
            this IOverlay overlay,
            ref bool overlayVisible,
            bool newValue,
            double opacity = 1.0d)
        {
            if (overlayVisible != newValue)
            {
                overlayVisible = newValue;
                if (overlayVisible)
                {
                    overlay.ShowOverlay(opacity);
                }
                else
                {
                    overlay.HideOverlay();
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// オーバーレイとして表示させる
        /// </summary>
        /// <param name="overlay">overlay</param>
        public static bool ShowOverlay(
            this IOverlay overlay,
            double opacity = 1.0d)
        {
            var r = false;

            if (overlay is Window w)
            {
                if (w.Opacity <= 0)
                {
                    w.Opacity = opacity;
                    r = true;
                }
            }

            return r;
        }

        /// <summary>
        /// オーバーレイとして隠す
        /// </summary>
        /// <param name="overlay">overlay</param>
        public static void HideOverlay(
            this IOverlay overlay)
        {
            if (overlay is Window w)
            {
                w.Opacity = 0;
            }
        }

        /// <summary>
        /// アクティブにさせない
        /// </summary>
        /// <param name="x">Window</param>
        public static void ToNonActive(
            this Window window)
        {
            window.SourceInitialized += (s, e) =>
            {
                // Get this window's handle
                var hwnd = new WindowInteropHelper(window).Handle;

                // Change the extended window style to include WS_EX_TRANSPARENT
                var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);

                SetWindowLong(
                    hwnd,
                    GWL_EXSTYLE,
                    extendedStyle | WS_EX_NOACTIVATE);
            };
        }

        /// <summary>
        /// Clickの透過を解除する
        /// </summary>
        /// <param name="x">対象のWindow</param>
        public static void ToNotTransparent(
            this Window window)
        {
            // Get this window's handle
            IntPtr hwnd = new WindowInteropHelper(window).Handle;

            // Change the extended window style to include WS_EX_TRANSPARENT
            int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);

            SetWindowLong(
                hwnd,
                GWL_EXSTYLE,
                extendedStyle & ~WS_EX_TRANSPARENT);
        }

        /// <summary>
        /// Click透過させる
        /// </summary>
        /// <param name="x">対象のWindow</param>
        public static void ToTransparent(
            this Window window)
        {
            // Get this window's handle
            var hwnd = new WindowInteropHelper(window).Handle;

            // Change the extended window style to include WS_EX_TRANSPARENT
            var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);

            SetWindowLong(
                hwnd,
                GWL_EXSTYLE,
                extendedStyle | WS_EX_TRANSPARENT);
        }

        #region ZOrder Corrector

        private static readonly object ZOrderLocker = new object();

        private static readonly Lazy<DispatcherTimer> LazyZOrderCorrector = new Lazy<DispatcherTimer>(() =>
        {
            var timer = new DispatcherTimer(DispatcherPriority.ContextIdle)
            {
                Interval = TimeSpan.FromSeconds(1.5),
            };

            timer.Tick += ZOrderCorrectorOnTick;

            return timer;
        });

        private static readonly List<IOverlay> ToCorrectOverlays = new List<IOverlay>(64);

        /// <summary>
        /// Zオーダー修正キューに登録する
        /// </summary>
        /// <param name="overlay"></param>
        public static void SubscribeZOrderCorrector(
            this IOverlay overlay)
        {
            lock (ZOrderLocker)
            {
                if (overlay is Window window)
                {
                    window.Closing += (x, y) =>
                    {
                        if (x is IOverlay o)
                        {
                            o.UnsubscribeZOrderCorrector();
                        }
                    };
                }

                if (!ToCorrectOverlays.Contains(overlay))
                {
                    ToCorrectOverlays.Add(overlay);
                }

                LazyZOrderCorrector.Value.Start();
            }
        }

        /// <summary>
        /// Zオーダー修正キューに登録する
        /// </summary>
        /// <param name="overlay"></param>
        public static void UnsubscribeZOrderCorrector(
            this IOverlay overlay)
        {
            lock (ZOrderLocker)
            {
                ToCorrectOverlays.Remove(overlay);

                if (!ToCorrectOverlays.Any())
                {
                    LazyZOrderCorrector.Value.Stop();
                }
            }
        }

        private static void ZOrderCorrectorOnTick(
            object sender,
            EventArgs e)
        {
            lock (ZOrderLocker)
            {
                foreach (var overlay in ToCorrectOverlays)
                {
                    Thread.Yield();

                    if (overlay == null)
                    {
                        continue;
                    }

                    if (overlay is Window window &&
                        window.IsLoaded)
                    {
                        if (!XIVPluginHelper.Instance.IsAvailable)
                        {
                            overlay.EnsureTopMost();
                            continue;
                        }

                        if (!overlay.IsOverlaysGameWindow())
                        {
                            overlay.EnsureTopMost();
                        }
                    }
                }
            }
        }

        public static IntPtr GetHandle(
            this IOverlay overlay) =>
            new WindowInteropHelper(overlay as Window).Handle;

        /// <summary>
        /// FFXIVより前面にいるか？
        /// </summary>
        /// <param name="overlay"></param>
        /// <returns></returns>
        public static bool IsOverlaysGameWindow(
            this IOverlay overlay)
        {
            var xivHandle = GetGameWindowHandle();
            var handle = overlay.GetHandle();

            while (handle != IntPtr.Zero)
            {
                // Overlayウィンドウよりも前面側にFF14のウィンドウがあった
                if (handle == xivHandle)
                {
                    return false;
                }

                handle = NativeMethods.GetWindow(handle, NativeMethods.GW_HWNDPREV);
            }

            // 前面側にOverlayが存在する、もしくはFF14が起動していない
            return true;
        }

        /// <summary>
        /// Windowを最前面に持ってくる
        /// </summary>
        /// <param name="overlay"></param>
        public static void EnsureTopMost(
            this IOverlay overlay)
        {
            NativeMethods.SetWindowPos(
                overlay.GetHandle(),
                NativeMethods.HWND_TOPMOST,
                0, 0, 0, 0,
                NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOACTIVATE);
        }

        private static readonly object xivProcLocker = new object();
        private static readonly TimeSpan TryInterval = TimeSpan.FromSeconds(15);
        private static Process xivProc;
        private static DateTime lastTry;

        private static IntPtr GetGameWindowHandle()
        {
            lock (xivProcLocker)
            {
                try
                {
                    // プロセスがすでに終了してるならプロセス情報をクリア
                    if (xivProc != null && xivProc.HasExited)
                    {
                        xivProc = null;
                    }

                    // プロセス情報がなく、tryIntervalよりも時間が経っているときは新たに取得を試みる
                    if (xivProc == null && (DateTime.Now - lastTry) > TryInterval)
                    {
                        xivProc = XIVPluginHelper.Instance.CurrentFFXIVProcess;

                        if (xivProc == null)
                        {
                            xivProc = Process.GetProcessesByName("ffxiv_dx11").FirstOrDefault();

                            if (xivProc == null)
                            {
                                xivProc = Process.GetProcessesByName("ffxiv").FirstOrDefault();
                            }
                        }

                        lastTry = DateTime.Now;
                    }

                    if (xivProc != null)
                    {
                        return xivProc.MainWindowHandle;
                    }
                }
                catch (System.ComponentModel.Win32Exception) { }

                return IntPtr.Zero;
            }
        }

        #endregion ZOrder Corrector
    }
}
