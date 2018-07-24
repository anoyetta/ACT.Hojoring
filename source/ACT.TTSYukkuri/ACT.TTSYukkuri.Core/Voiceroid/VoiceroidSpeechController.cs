using System;
using System.IO;
using System.Reactive.Disposables;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ACT.TTSYukkuri.Config;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using RucheHome.Voiceroid;

namespace ACT.TTSYukkuri.Voiceroid
{
    /// <summary>
    /// VOICEROIDスピーチコントローラ
    /// </summary>
    public class VoiceroidSpeechController :
        ISpeechController
    {
        private VoiceroidConfig Config => Settings.Default.VoiceroidSettings;

        public ProcessFactory ProcessFactory { get; private set; }

        private CompositeDisposable CompositeDisposable { get; set; }

        /// <summary>
        /// 初期化する
        /// </summary>
        public void Initialize()
        {
            lock (this)
            {
                if (this.CompositeDisposable != null)
                {
                    return;
                }

                this.CompositeDisposable = new CompositeDisposable();
            }

            this.ProcessFactory = new ProcessFactory()
                .AddTo(this.CompositeDisposable);

            // プロセス更新タイマ設定＆開始
            var updateTimer = new ReactiveTimer(TimeSpan.FromMilliseconds(100))
                .AddTo(this.CompositeDisposable);
            updateTimer?.Subscribe(async x =>
                {
                    try
                    {
                        if (this.ProcessFactory == null)
                        {
                            return;
                        }

                        await this.ProcessFactory?.Update();
                        foreach (var innerProcess in this.ProcessFactory?.Processes)
                        {
                            if (innerProcess != null)
                            {
                                var process = this.Config.Get(innerProcess.Id);
                                if (process != null)
                                {
                                    if (!string.IsNullOrEmpty(innerProcess?.ExecutablePath) &&
                                        File.Exists(innerProcess?.ExecutablePath))
                                    {
                                        process.Path = innerProcess?.ExecutablePath;
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        this.GetLogger().Error(ex, "error on VOICEROID SpeachController.");
                    }
                }).AddTo(this.CompositeDisposable);

            updateTimer.Start();
        }

        /// <summary>
        /// 開放する
        /// </summary>
        public async void Free()
        {
            if (this.Config.ExitVoiceroidWhenExit)
            {
                var process = this.Config.GetSelected()?.InnerProcess;
                await process?.Exit();
            }

            lock (this)
            {
                this.CompositeDisposable?.Dispose();
                this.CompositeDisposable = null;
                this.ProcessFactory = null;
            }
        }

        /// <summary>
        /// 指定されたVOICEROIDを起動する
        /// </summary>
        /// <returns>
        /// エラーメッセージ</returns>
        public async void Start()
        {
            var err = await this.StartAsync();
            if (!string.IsNullOrEmpty(err))
            {
                this.GetLogger().Error($"VOICEROID start error. error={err}");
            }
        }

        /// <summary>
        /// 指定されたVOICEROIDを起動する
        /// </summary>
        /// <returns>
        /// エラーメッセージ</returns>
        public async Task<string> StartAsync()
        {
            this.Initialize();

            var process = this.Config.GetSelected();
            if (process == null)
            {
                return "VOICEROID not found.";
            }

            if (process.InnerProcess == null)
            {
                this.Config.Load();
            }

            if (!process.InnerProcess.IsRunning)
            {
                if (!string.IsNullOrEmpty(process.Path) &&
                    File.Exists(process.Path))
                {
                    await process?.InnerProcess?.Run(process.Path);
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// テキストを読み上げる
        /// </summary>
        /// <param name="text">読み上げるテキスト</param>
        public async void Speak(
            string text,
            PlayDevices playDevice = PlayDevices.Both,
            bool isSync = false)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            // 起動していなければ起動させる
            var err = await this.StartAsync();
            if (!string.IsNullOrEmpty(err))
            {
                this.GetLogger().Error($"VOICEROID Speeak error text={text}, err={err}");
                return;
            }

            var process = this.Config.GetSelected()?.InnerProcess;
            if (process == null)
            {
                return;
            }

            // アクティブにさせないようにする
            this.SetNotActiveWindow(process.MainWindowHandle);

            if (this.Config.DirectSpeak)
            {
                // 直接再生する
                if (await process.SetTalkText(text))
                {
                    if (!await process.Play())
                    {
                        this.GetLogger().Error($"VOICEROID Speeak error text={text}");
                        return;
                    }
                }

                return;
            }

            // 現在の条件をハッシュ化してWAVEファイル名を作る
            var wave = this.GetCacheFileName(
                Settings.Default.TTS,
                text,
                this.Config.ToString());

            if (!File.Exists(wave))
            {
                // 音声waveファイルを生成する
                if (await process.SetTalkText(text))
                {
                    var result = await process.Save(wave);
                    if (!result.IsSucceeded)
                    {
                        this.GetLogger().Error($"VOICEROID Speeak error text={text}, err={result.Error}, extra={result.ExtraMessage}");
                        return;
                    }
                }
            }

            // 再生する
            SoundPlayerWrapper.Play(wave);
        }

        /// <summary>
        /// 対象のウィンドウをアクティブにさせないようにする
        /// </summary>
        /// <param name="hWnd">
        /// 対象のWindowハンドル</param>
        private void SetNotActiveWindow(
            IntPtr hWnd)
        {
            var exStyle = NativeMethods.GetWindowLong(hWnd, NativeMethods.GWL.GWL_EXSTYLE);

            if ((exStyle & (uint)NativeMethods.EX_WS.WS_EX_NOACTIVATE) == 0)
            {
                // 拡張スタイルを設定する
                exStyle |= (uint)NativeMethods.EX_WS.WS_EX_NOACTIVATE;
                NativeMethods.SetWindowLong(hWnd, NativeMethods.GWL.GWL_EXSTYLE, exStyle);

                NativeMethods.SetWindowPos(
                    hWnd,
                    IntPtr.Zero,
                    0, 0, 0, 0,
                    NativeMethods.SWP.SWP_NOMOVE |
                    NativeMethods.SWP.SWP_NOSIZE |
                    NativeMethods.SWP.SWP_NOZORDER |
                    NativeMethods.SWP.SWP_FRAMECHANGED);
            }
        }
    }

    public static class NativeMethods
    {
        public enum ShowWindowCommands : uint
        {
            /// <summary>
            /// Hides the window and activates another window.
            /// </summary>
            SW_HIDE = 0,

            /// <summary>
            /// Activates and displays a window. If the window is minimized or maximized, the system restores it to its original size and position. An application should specify this flag when displaying the window for the first time.
            /// </summary>
            SW_SHOWNORMAL = 1,

            /// <summary>
            /// Activates and displays a window. If the window is minimized or maximized, the system restores it to its original size and position. An application should specify this flag when displaying the window for the first time.
            /// </summary>
            SW_NORMAL = 1,

            /// <summary>
            /// Activates the window and displays it as a minimized window.
            /// </summary>
            SW_SHOWMINIMIZED = 2,

            /// <summary>
            /// Activates the window and displays it as a maximized window.
            /// </summary>
            SW_SHOWMAXIMIZED = 3,

            /// <summary>
            /// Maximizes the specified window.
            /// </summary>
            SW_MAXIMIZE = 3,

            /// <summary>
            /// Displays a window in its most recent size and position. This value is similar to <see cref="ShowWindowCommands.SW_SHOWNORMAL"/>, except the window is not activated.
            /// </summary>
            SW_SHOWNOACTIVATE = 4,

            /// <summary>
            /// Activates the window and displays it in its current size and position.
            /// </summary>
            SW_SHOW = 5,

            /// <summary>
            /// Minimizes the specified window and activates the next top-level window in the z-order.
            /// </summary>
            SW_MINIMIZE = 6,

            /// <summary>
            /// Displays the window as a minimized window. This value is similar to <see cref="ShowWindowCommands.SW_SHOWMINIMIZED"/>, except the window is not activated.
            /// </summary>
            SW_SHOWMINNOACTIVE = 7,

            /// <summary>
            /// Displays the window in its current size and position. This value is similar to <see cref="ShowWindowCommands.SW_SHOW"/>, except the window is not activated.
            /// </summary>
            SW_SHOWNA = 8,

            /// <summary>
            /// Activates and displays the window. If the window is minimized or maximized, the system restores it to its original size and position. An application should specify this flag when restoring a minimized window.
            /// </summary>
            SW_RESTORE = 9
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool ShowWindow(IntPtr hWnd, ShowWindowCommands nCmdShow);

        public enum GWL : int
        {
            GWL_EXSTYLE = -20,
            GWL_HINSTANCE = -6,
            GWL_HWNDPARENT = -8,
            GWL_ID = -12,
            GWL_STYLE = -16,
            GWL_USERDATA = -21,
            GWL_WNDPROC = -4,
        }

        public enum WS : uint
        {
            WS_DISABLED = 0x08000000,
            WS_VISIBLE = 0x10000000,
        }

        public enum EX_WS : uint
        {
            WS_EX_NOACTIVATE = 0x08000000,
            WS_EX_LAYERED = 0x00080000,
        }

        public enum SWP : int
        {
            SWP_NOSIZE = 0x0001,
            SWP_NOMOVE = 0x0002,
            SWP_NOZORDER = 0x0004,
            SWP_NOREDRAW = 0x0008,
            SWP_NOACTIVATE = 0x0010,
            SWP_FRAMECHANGED = 0x0020,
            SWP_SHOWWINDOW = 0x0040,
            SWP_HIDEWINDOW = 0x0080,
            SWP_NOCOPYBITS = 0x0100,
            SWP_NOOWNERZORDER = 0x0200,
            SWP_NOSENDCHANGING = 0x400
        }

        [DllImport("user32.dll")]
        public static extern uint GetWindowLong(IntPtr hWnd, GWL nIndex);

        [DllImport("user32.dll")]
        public static extern uint SetWindowLong(IntPtr hWnd, GWL nIndex, uint dwNewLong);

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, SWP uFlags);
    }
}
