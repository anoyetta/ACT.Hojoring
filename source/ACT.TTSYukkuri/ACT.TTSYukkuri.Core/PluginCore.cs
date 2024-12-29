using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using ACT.TTSYukkuri.Config;
using ACT.TTSYukkuri.Config.Views;
using ACT.TTSYukkuri.Discord.Models;
using ACT.TTSYukkuri.Voiceroid;
using Advanced_Combat_Tracker;
using FFXIV.Framework.Bridge;
using FFXIV.Framework.Common;
using FFXIV.Framework.Extensions;
using FFXIV.Framework.resources;
using FFXIV.Framework.WPF;
using FFXIV.Framework.WPF.Views;
using NLog;

namespace ACT.TTSYukkuri
{
    /// <summary>
    /// TTSゆっくりプラグイン
    /// </summary>
    public partial class PluginCore
    {
        #region Singleton

        private static PluginCore instance;

        public static PluginCore Instance =>
            instance ?? (instance = new PluginCore());

        private PluginCore()
        {
        }

        public static void Free()
        {
            instance = null;
        }

        #endregion Singleton

        #region Logger

        private Logger AppLogger => AppLog.DefaultLogger;

        #endregion Logger
        internal enum ERole : uint
        {
            eConsole = 0,
            eMultimedia = 1,
            eCommunications = 2,
            ERole_enum_count = 3
        }
        [Guid("F8679F50-850A-41CF-9C72-430F290290C8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IPolicyConfig
        {
            [PreserveSig]
            int GetMixFormat();
            [PreserveSig]
            int GetDeviceFormat();
            [PreserveSig]
            int ResetDeviceFormat();
            [PreserveSig]
            int SetDeviceFormat();
            [PreserveSig]
            int GetProcessingPeriod();
            [PreserveSig]
            int SetProcessingPeriod();
            [PreserveSig]
            int GetShareMode();
            [PreserveSig]
            int SetShareMode();
            [PreserveSig]
            int GetPropertyValue();
            [PreserveSig]
            int SetPropertyValue();
            [PreserveSig]
            int SetDefaultEndpoint(
                [In][MarshalAs(UnmanagedType.LPWStr)] string deviceId,
                [In][MarshalAs(UnmanagedType.U4)] ERole role);
            [PreserveSig]
            int SetEndpointVisibility();
        }
        [ComImport, Guid("870AF99C-171D-4F9E-AF0D-E63DF40C2BC9")]
        internal class _CPolicyConfigClient
        {
        }
        public class PolicyConfigClient
        {
            public static int SetDefaultDevice(string deviceID)
            {
                IPolicyConfig _policyConfigClient = (new _CPolicyConfigClient() as IPolicyConfig);
                try
                {
                    Marshal.ThrowExceptionForHR(_policyConfigClient.SetDefaultEndpoint(deviceID, ERole.eConsole));
                    Marshal.ThrowExceptionForHR(_policyConfigClient.SetDefaultEndpoint(deviceID, ERole.eMultimedia));
                    Marshal.ThrowExceptionForHR(_policyConfigClient.SetDefaultEndpoint(deviceID, ERole.eCommunications));
                    return 0;
                }
                catch
                {
                    return 1;
                }
            }
        }

        #region Replace TTS Method

        private FormActMain.PlayTtsDelegate originalTTSMethod;
        private FormActMain.PlaySoundDelegate originalSoundMethod;

        private System.Timers.Timer replaceTTSMethodTimer;

        private void StopReplaceTTSMethodTimer()
        {
            lock (this)
            {
                // タイマを止める
                if (this.replaceTTSMethodTimer != null &&
                    this.replaceTTSMethodTimer.Enabled)
                {
                    this.replaceTTSMethodTimer.Stop();
                    this.replaceTTSMethodTimer.Dispose();
                    this.replaceTTSMethodTimer = null;
                }
            }
        }

        private void StartReplaceTTSMethodTimer()
        {
            lock (this)
            {
                this.replaceTTSMethodTimer = new System.Timers.Timer()
                {
                    Interval = 3 * 1000,
                    AutoReset = true,
                };

                // 置き換え監視タイマを開始する
                if (!this.replaceTTSMethodTimer.Enabled)
                {
                    this.replaceTTSMethodTimer.Elapsed += (s, e) =>
                    {
                        lock (this)
                        {
                            if (this.replaceTTSMethodTimer.Enabled)
                            {
                                this.ReplaceTTSMethod();
                            }
                        }
                    };

                    this.replaceTTSMethodTimer.Start();
                }
            }
        }

        private void ReplaceTTSMethod()
        {
            if (ActGlobals.oFormActMain == null ||
                ActGlobals.oFormActMain.IsDisposed)
            {
                return;
            }

            // TTSメソッドを置き換える
            if (ActGlobals.oFormActMain.PlayTtsMethod != this.Speak)
            {
                this.originalTTSMethod = (FormActMain.PlayTtsDelegate)ActGlobals.oFormActMain.PlayTtsMethod.Clone();
                ActGlobals.oFormActMain.PlayTtsMethod = this.Speak;
            }

            // サウンド再生メソッドを置き換える
            if (ActGlobals.oFormActMain.PlaySoundMethod != this.PlaySound)
            {
                this.originalSoundMethod = (FormActMain.PlaySoundDelegate)ActGlobals.oFormActMain.PlaySoundMethod.Clone();
                ActGlobals.oFormActMain.PlaySoundMethod = this.PlaySound;
            }
        }

        private void RestoreTTSMethod()
        {
            if (ActGlobals.oFormActMain == null ||
                ActGlobals.oFormActMain.IsDisposed)
            {
                return;
            }

            // 置き換えたTTSメソッドを元に戻す
            if (this.originalTTSMethod != null)
            {
                ActGlobals.oFormActMain.PlayTtsMethod = this.originalTTSMethod;
            }

            // 置き換えたサウンド再生メソッドを元に戻す
            if (this.originalSoundMethod != null)
            {
                ActGlobals.oFormActMain.PlaySoundMethod = this.originalSoundMethod;
            }
        }

        #endregion Replace TTS Method

        #region Play Method

        private static readonly object WaveBlocker = new object();
        private static readonly object TTSBlocker = new object();

        public void PlaySound(string wave, int volume)
            => this.PlaySound(wave, PlayDevices.Both, false, null);

        public void PlaySound(
            string wave,
            PlayDevices playDevice = PlayDevices.Both,
            bool isSync = false,
            float? volume = null)
        {
            if (!File.Exists(wave))
            {
                return;
            }

            if (!isSync)
            {
                Task.Run(() => SoundPlayerWrapper.Play(wave, playDevice, isSync, volume));
            }
            else
            {
                Task.Run(() =>
                {
                    lock (WaveBlocker)
                    {
                        SoundPlayerWrapper.Play(wave, playDevice, isSync, volume);
                    }
                });
            }
        }

        public void Speak(string message) => this.Speak(message, PlayDevices.Both, false, null);

        public void Speak(
            string message,
            PlayDevices playDevice = PlayDevices.Both,
            bool isSync = false,
            float? volume = null)
            => Speak(message, playDevice, VoicePalettes.Default, isSync, volume);

        public void Speak(
            string message,
            PlayDevices playDevice = PlayDevices.Both,
            VoicePalettes voicePalette = VoicePalettes.Default,
            bool isSync = false,
            float? volume = null)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            // ファイルじゃない（TTS）？
            if (!message.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) &&
                !message.EndsWith(".wave", StringComparison.OrdinalIgnoreCase) &&
                !message.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
            {
                if (!isSync)
                {
                    Task.Run(() => this.SpeakTTS(message, playDevice, voicePalette, isSync, volume));
                }
                else
                {
                    Task.Run(() =>
                    {
                        lock (TTSBlocker)
                        {
                            this.SpeakTTS(message, playDevice, voicePalette, isSync, volume);
                        }
                    });
                }

                return;
            }

            // waveファイルとして再生する
            var wave = message;
            if (!File.Exists(wave))
            {
                var dirs = new string[]
                {
                    Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), @"resources\wav"),
                    Path.Combine(this.PluginDirectory, @"resources\wav"),
                };

                foreach (var dir in dirs)
                {
                    var f = Path.Combine(dir, wave);
                    if (File.Exists(f))
                    {
                        wave = f;
                        break;
                    }
                }
            }

            // Volume はダミーなので0で指定する
            this.PlaySound(wave, playDevice, isSync, volume);
        }

        private void SpeakTTS(
            string textToSpeak,
            PlayDevices playDevice = PlayDevices.Both,
            bool isSync = false,
            float? volume = null)
            => SpeakTTS(textToSpeak, playDevice, VoicePalettes.Default, isSync, volume);

        /// <summary>
        /// /wait付きTTS
        /// </summary>
        /// <remarks>
        /// /wait [duration] [tts]
        /// </remarks>
        /// <example>
        /// /wait 5.5 こんにちは
        /// コマンドの発行から5.5秒後に「こんにちは」という
        /// </example>
        private static readonly Regex WaitCommandRegex = new Regex(
            @"/wait\s+(?<due>[\d\.]+)(?<operator>[+-]|)(?<offset>\d|)[,\s]+(?<tts>.+)$",
            RegexOptions.Compiled);

        private void SpeakTTS(
            string textToSpeak,
            PlayDevices playDevice = PlayDevices.Both,
            VoicePalettes voicePalette = VoicePalettes.Default,
            bool isSync = false,
            float? volume = null)
        {
            try
            {
                if (!textToSpeak.ContainsIgnoreCase("/wait"))
                {
                    SpeechController.Default.Speak(textToSpeak, playDevice, voicePalette, isSync, volume);
                    return;
                }

                var match = WaitCommandRegex.Match(textToSpeak);
                if (!match.Success)
                {
                    SpeechController.Default.Speak(textToSpeak, playDevice, voicePalette, isSync, volume);
                    return;
                }

                var delayAsText = match.Groups["due"].Value;
                var operatorAsText = match.Groups["operator"].Value;
                var offsetAsText = match.Groups["offset"].Value;
                var message = match.Groups["tts"].Value?.Trim();

                if (!double.TryParse(delayAsText, out double delay))
                {
                    // 普通に読上げて終わる
                    SpeechController.Default.Speak(textToSpeak, playDevice, voicePalette, isSync, volume);
                    return;
                }

                if (string.IsNullOrEmpty(message))
                {
                    return;
                }

                // 必要ならdurationに対して簡易な足し算と引き算を行う
                if (operatorAsText != string.Empty && offsetAsText != string.Empty)
                {
                    double offset;
                    if (double.TryParse(offsetAsText, out offset))
                    {
                        if (operatorAsText == "+")
                        {
                            delay += offset;
                        }
                        else if (operatorAsText == "-")
                        {
                            delay -= offset;
                        }
                    }
                }

                // ディレイをかけて読上げる
                SpeechController.Default.SpeakWithDelay(
                    message,
                    delay,
                    playDevice,
                    isSync,
                    volume);
            }
            catch (Exception ex)
            {
                this.AppLogger.Error(ex, "SpeakTTS で例外が発生しました。");
            }
        }

        #endregion Play Method

        public string PluginDirectory { get; private set; }

        private Label PluginStatusLabel;

        private bool isLoaded = false;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public async void InitPlugin(
            IActPluginV1 plugin,
            TabPage pluginScreenSpace,
            Label pluginStatusText)
        {
            // タイトルをセットする
            pluginScreenSpace.Text = "YUKKURI";

            EnvironmentMigrater.Migrate();
            MasterFilePublisher.Publish();
            WPFHelper.Start();

            AppLog.LoadConfiguration(AppLog.HojoringConfig);
            this.AppLogger?.Trace(Assembly.GetExecutingAssembly().GetName().ToString() + " start.");

            try
            {
                this.PluginStatusLabel = pluginStatusText;

                if (!EnvironmentHelper.IsValidPluginLoadOrder())
                {
                    if (pluginStatusText != null)
                    {
                        pluginStatusText.Text = "Plugin Initialize Error";
                    }

                    return;
                }

                EnvironmentHelper.GarbageLogs();
                EnvironmentHelper.StartActivator(() =>
                {
                    ConfigBaseView.Instance.SetActivationStatus(false);
                    this.DeInitPlugin();
                });

                this.AppLogger.Trace("[YUKKURI] Start InitPlugin");

                var pluginInfo = ActGlobals.oFormActMain.PluginGetSelfData(plugin);
                if (pluginInfo != null)
                {
                    this.PluginDirectory = pluginInfo.pluginFile.DirectoryName;
                }

                // .NET FrameworkとOSのバージョンを確認する
                if (!UpdateChecker.IsAvailableDotNet() ||
                    !UpdateChecker.IsAvailableWindows())
                {
                    NotSupportedView.AddAndShow(pluginScreenSpace);
                    return;
                }

                // FFXIV.Framework.config を読み込ませる
                lock (FFXIV.Framework.Config.ConfigBlocker)
                {
                    _ = FFXIV.Framework.Config.Instance;
                }

                // HojoringのSplashを表示する
                UpdateChecker.ShowSplash();

                // 外部リソースをダウンロードする
                if (await ResourcesDownloader.Instance.DownloadAsync())
                {
                    await ResourcesDownloader.Instance.WaitDownloadingAsync();
                }

                // 設定ファイルを読み込む
                Settings.Default.Load();
                Settings.Default.StatusAlertSettings.EnabledTPAlert = false;

                // 設定ファイルをバックアップする
                await EnvironmentHelper.BackupFilesAsync(
                    Settings.FilePath);

                // 漢字変換を初期化する
                KanjiTranslator.Default.Initialize();

                // TTSキャッシュの移行とGarbageを行う
                this.MigrateTTSCache();
                this.GarbageTTSCache();

                await EnvironmentHelper.WaitInitActDoneAsync();

                // TTSを初期化する
                SpeechController.Default.Initialize();

                // FF14監視スレッドを初期化する
                FFXIVWatcher.Initialize();

                // 設定Panelを追加する
                pluginScreenSpace.Controls.Add(new ElementHost()
                {
                    Child = new ConfigBaseView(),
                    Dock = DockStyle.Fill,
                });

                // TTSメソッドを置き換える
                this.StartReplaceTTSMethodTimer();

                this.AppLogger.Trace("[YUKKURI] START Discord BOT INIT");
                // DISCORD BOT クライアントを初期化する
                DiscordClientModel.Model.Initialize();
                // AutoJoinがONならば接続する
                if (Settings.Default.DiscordSettings.AutoJoin)
                {
                    DiscordClientModel.Model.Connect(true);
                }
                this.AppLogger.Trace("[YUKKURI] END Discord BOT INIT");

                // VOICEROIDを起動する
                if (SpeechController.Default is VoiceroidSpeechController ctrl)
                {
                    ctrl.Start();
                }

                // Bridgeにメソッドを登録する
                PlayBridge.Instance.SetBothDelegate((message, voicePalette, isSync, volume) => this.Speak(message, PlayDevices.Both, voicePalette, isSync, volume));
                PlayBridge.Instance.SetMainDeviceDelegate((message, voicePalette, isSync, volume) => this.Speak(message, PlayDevices.Main, voicePalette, isSync, volume));
                PlayBridge.Instance.SetSubDeviceDelegate((message, voicePalette, isSync, volume) => this.Speak(message, PlayDevices.Sub, voicePalette, isSync, volume));
                PlayBridge.Instance.SetSyncStatusDelegate(() => Settings.Default.Player == WavePlayerTypes.WASAPIBuffered);

                // テキストコマンドの購読を登録する
                this.SubscribeTextCommands();

                // サウンドデバイスを初期化する
                SoundPlayerWrapper.Init();
                SoundPlayerWrapper.LoadTTSCache();

                // CeVIOをアイコン化する
                if (Settings.Default.TTS == TTSType.Sasara)
                {
                    if (Settings.Default.SasaraSettings.IsHideCevioWindow)
                    {
                        CevioTrayManager.Start();
                        CevioTrayManager.ToIcon();
                    }
                }
                if (Settings.Default.TTS == TTSType.CevioAI)
                {
                    if (Settings.Default.CevioAISettings.IsHideCevioWindow)
                    {
                        CevioTrayManager.Start();
                        CevioTrayManager.ToIcon();
                    }
                }

                if (this.PluginStatusLabel != null)
                {
                    this.PluginStatusLabel.Text = "Plugin Started";
                }

                this.AppLogger.Trace("[YUKKURI] End InitPlugin");

                // 共通ビューを追加する
                CommonViewHelper.Instance.AddCommonView(
                   pluginScreenSpace.Parent as TabControl);

                this.isLoaded = true;

                // アップデートを確認する
                await Task.Run(() => this.Update());
            }
            catch (Exception ex)
            {
                this.AppLogger.Error(ex, "InitPlugin error.");

                ModernMessageBox.ShowDialog(
                    "Plugin init error !",
                    "ACT.TTSYukkuri",
                    System.Windows.MessageBoxButton.OK,
                    ex);

                // TTSをゆっくりに戻す
                Settings.Default.TTS = TTSType.Yukkuri;
                Settings.Default.Save();
            }
        }

        public void DeInitPlugin()
        {
            if (!this.isLoaded)
            {
                return;
            }

            try
            {
                // CeVIO のアイコン化を解除する
                if (Settings.Default.SasaraSettings.IsHideCevioWindow || Settings.Default.CevioAISettings.IsHideCevioWindow)
                {
                    CevioTrayManager.RestoreWindow();
                    CevioTrayManager.End();
                }

                // 設定を保存する
                Settings.Default.Save();
                FFXIV.Framework.Config.Save();

                // TTSアクションを元に戻す
                this.StopReplaceTTSMethodTimer();
                this.RestoreTTSMethod();

                // TTSコントローラを開放する
                SpeechController.Default.Free();

                // Bridgeにメソッドを解除する
                PlayBridge.Instance.SetBothDelegate(null);
                PlayBridge.Instance.SetMainDeviceDelegate(null);
                PlayBridge.Instance.SetSubDeviceDelegate(null);

                // Discordを終了する
                DiscordClientModel.Model.Dispose();

                // FF14監視スレッドを開放する
                FFXIVWatcher.Deinitialize();

                // 漢字変換オブジェクトを開放する
                KanjiTranslator.Default.Dispose();

                // TTS用waveファイルを削除する？
                if (Settings.Default.WaveCacheClearEnable)
                {
                    var appdir = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        @"anoyetta\ACT\tts cache");

                    if (Directory.Exists(appdir))
                    {
                        var files = new List<string>();
                        files.AddRange(Directory.GetFiles(appdir, "*.wav"));
                        files.AddRange(Directory.GetFiles(appdir, "*.mp3"));
                        foreach (var file in files)
                        {
                            try
                            {
                                File.Delete(file);
                            }
                            catch
                            {
                            }
                        }
                    }
                }

                // TTSヒストリを保存する
                SoundPlayerWrapper.SaveTTSHistory();

                if (this.PluginStatusLabel != null)
                {
                    this.PluginStatusLabel.Text = "Plugin Exited";
                }
            }
            catch (Exception ex)
            {
                this.AppLogger.Error(ex, "DeInitPlugin error.");
            }
        }

        private void SubscribeTextCommands()
        {
            // wipeout に対するコマンドを登録する
            TextCommandBridge.Instance.Subscribe(new TextCommand(
            (string logLine, out Match match) =>
            {
                var result = false;
                match = null;

                if (!string.IsNullOrEmpty(logLine))
                {
                    if (logLine.Contains(WipeoutKeywords.WipeoutLog) ||
                        logLine.Contains(WipeoutKeywords.WipeoutLogEcho) ||
                        logLine.Contains("01:Changed Zone to"))
                    {
                        result = true;
                    }
                }

                return result;
            },
            (string logLine, Match match) =>
            {
                BufferedWavePlayer.Instance?.ClearBuffers();
                this.AppLogger.Info("Playback buffers cleared.");
            })
            {
                IsSilent = true,
            });

            // ミュートを解除されたときにサウンドデバイスを初期化する
            TextCommandBridge.Instance.Subscribe(new TextCommand(
            (string logLine, out Match match) =>
            {
                var result = false;
                match = null;

                if (!string.IsNullOrEmpty(logLine))
                {
                    if (logLine.Contains(UnMuteKeywords.UnMuteJP) ||
                    logLine.Contains(UnMuteKeywords.UnMuteEN) ||
                    logLine.Contains(UnMuteKeywords.UnMuteDE) ||
                    logLine.Contains(UnMuteKeywords.UnMuteFR)
                    )
                    {
                        result = true;
                    }
                }

                return result;
            },
            (string logLine, Match match) =>
            {
                List<PlayDevice> list = FFXIV.Framework.Common.WavePlayer.EnumerateDevicesByWasapiOut();
                string temp_device_name = Settings.Default.TemporarySwitchSoundDevice;
                if (!string.IsNullOrEmpty(temp_device_name))
                {
                    string current_device_id = Settings.Default.MainDeviceID;
                    
                    string deviceid = list.Find(x => x.Name.Contains(temp_device_name)).ID;
                    if (PolicyConfigClient.SetDefaultDevice(deviceid) == 0)
                    {
                        this.AppLogger.Info("Change Default Sound Device to " + temp_device_name + ", success.");

                        deviceid = list.Find(x => x.ID == current_device_id).ID;
                        string devicename = list.Find(x => x.ID == current_device_id).Name;
                        if (PolicyConfigClient.SetDefaultDevice(deviceid) == 0)
                        {
                            this.AppLogger.Info("Restore Default Sound Device to " + devicename + ", success.");

                        }
                    }
                }
                SoundPlayerWrapper.Init();
                this.AppLogger.Info("Playback stream initialized.");
            })
            {
                IsSilent = true,
            });
        }

        /// <summary>
        /// TTSキャッシュをガーベージする
        /// </summary>
        private void GarbageTTSCache()
        {
            var cacheFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                @"anoyetta\ACT\tts cache");

            if (!Directory.Exists(cacheFolder))
            {
                return;
            }

            Directory.EnumerateFiles(
                cacheFolder,
                "*.*",
                SearchOption.TopDirectoryOnly).Walk((file) =>
                {
                    var timestamp = File.GetLastAccessTime(file);
                    if ((DateTime.Now - timestamp).TotalDays > 30)
                    {
                        File.Delete(file);
                    }
                });

            Directory.EnumerateFiles(
                cacheFolder,
                "*本日は晴天なり*",
                SearchOption.TopDirectoryOnly).Walk((file) =>
                {
                    File.Delete(file);
                });
        }

        /// <summary>
        /// TTSのキャッシュ(waveファイル)をマイグレーションする
        /// </summary>
        private void MigrateTTSCache()
        {
            var oldCacheDir = Path.Combine(
                 Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                 @"anoyetta\ACT");

            var newCacheDir = Path.Combine(
                oldCacheDir,
                "tts cache");

            if (!Directory.Exists(newCacheDir))
            {
                Directory.CreateDirectory(newCacheDir);
            }

            foreach (var file in Directory.EnumerateFiles(
                oldCacheDir, "*.wav", SearchOption.TopDirectoryOnly))
            {
                var dest = Path.Combine(
                    newCacheDir,
                    Path.GetFileName(file));

                if (File.Exists(dest))
                {
                    File.Delete(dest);
                }

                File.Move(file, dest);
            }
        }

        /// <summary>
        /// アップデートを行う
        /// </summary>
        private void Update()
        {
            if ((DateTime.Now - Settings.Default.LastUpdateDateTime).TotalHours
                >= Settings.UpdateCheckInterval)
            {
                var message = UpdateChecker.Update(
                    "ACT.TTSYukkuri",
                    Assembly.GetExecutingAssembly());
                if (!string.IsNullOrWhiteSpace(message))
                {
                    this.AppLogger.Error(message);
                }

                Settings.Default.LastUpdateDateTime = DateTime.Now;
                Settings.Default.Save();
            }
        }
    }
}
