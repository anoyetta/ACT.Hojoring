using System;
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

        private Logger Logger => AppLog.DefaultLogger;

        #endregion Logger

        #region Replace TTS Method

        private FormActMain.PlayTtsDelegate originalTTSMethod;
        private FormActMain.PlaySoundDelegate originalSoundMethod;

        private System.Timers.Timer replaceTTSMethodTimer;

        private void StopReplaceTTSMethodTimer()
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

        private void StartReplaceTTSMethodTimer()
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
                    if (this.replaceTTSMethodTimer.Enabled)
                    {
                        this.ReplaceTTSMethod();
                    }
                };

                this.replaceTTSMethodTimer.Start();
            }
        }

        private void ReplaceTTSMethod()
        {
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
            @"/wait\s+(?<due>[\d\.]+)[,\s]+(?<tts>.+)$",
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
                this.Logger.Error(ex, "SpeakTTS で例外が発生しました。");
            }
        }

        #endregion Play Method

        public string PluginDirectory { get; private set; }

        private Label PluginStatusLabel;

        private bool isLoaded = false;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void InitPlugin(
            IActPluginV1 plugin,
            TabPage pluginScreenSpace,
            Label pluginStatusText)
        {
            // タイトルをセットする
            pluginScreenSpace.Text = "YUKKURI";

            EnvironmentMigrater.Migrate();
            MasterFilePublisher.Publish();
            WPFHelper.Start();
            WPFHelper.BeginInvoke(async () =>
            {
                AppLog.LoadConfiguration(AppLog.HojoringConfig);
                this.Logger?.Trace(Assembly.GetExecutingAssembly().GetName().ToString() + " start.");

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

                    this.Logger.Trace("[YUKKURI] Start InitPlugin");

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

                    // HojoringのSplashを表示する
                    UpdateChecker.ShowSplash();

                    // 外部リソースをダウンロードする
                    await ResourcesDownloader.Instance.DownloadAsync();

                    // 設定ファイルを読み込む
                    Settings.Default.Load();
                    Settings.Default.StatusAlertSettings.EnabledTPAlert = false;

                    // 設定ファイルをバックアップする
                    await EnvironmentHelper.BackupFilesAsync(
                        Settings.FilePath);

                    // 漢字変換を初期化する
                    KanjiTranslator.Default.Initialize();

                    // TTSキャッシュの移行とGarbageを行う
                    await Task.Run(() =>
                    {
                        this.MigrateTTSCache();
                        this.GarbageTTSCache();
                    });

                    EnvironmentHelper.WaitInitActDone();

                    await Task.Run(() =>
                    {
                        // TTSを初期化する
                        SpeechController.Default.Initialize();

                        // FF14監視スレッドを初期化する
                        FFXIVWatcher.Initialize();
                    });

                    // 設定Panelを追加する
                    pluginScreenSpace.Controls.Add(new ElementHost()
                    {
                        Child = new ConfigBaseView(),
                        Dock = DockStyle.Fill,
                    });

                    // TTSメソッドを置き換える
                    this.StartReplaceTTSMethodTimer();

                    await Task.Run(() =>
                    {
                        // DISCORD BOT クライアントを初期化する
                        DiscordClientModel.Model.Initialize();

                        // AutoJoinがONならば接続する
                        if (Settings.Default.DiscordSettings.AutoJoin)
                        {
                            DiscordClientModel.Model.Connect(true);
                        }
                    });

                    await Task.Run(() =>
                    {
                        // VOICEROIDを起動する
                        if (SpeechController.Default is VoiceroidSpeechController ctrl)
                        {
                            ctrl.Start();
                        }
                    });

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
                        CevioTrayManager.Start();
                        CevioTrayManager.ToIcon();
                    }

                    if (this.PluginStatusLabel != null)
                    {
                        this.PluginStatusLabel.Text = "Plugin Started";
                    }

                    this.Logger.Trace("[YUKKURI] End InitPlugin");

                    // 共通ビューを追加する
                    CommonViewHelper.Instance.AddCommonView(
                       pluginScreenSpace.Parent as TabControl);

                    this.isLoaded = true;

                    // アップデートを確認する
                    await Task.Run(() => this.Update());
                }
                catch (Exception ex)
                {
                    this.Logger.Error(ex, "InitPlugin error.");

                    ModernMessageBox.ShowDialog(
                        "Plugin init error !",
                        "ACT.TTSYukkuri",
                        System.Windows.MessageBoxButton.OK,
                        ex);

                    // TTSをゆっくりに戻す
                    Settings.Default.TTS = TTSType.Yukkuri;
                    Settings.Default.Save();
                }
            });
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
                CevioTrayManager.RestoreWindow();
                CevioTrayManager.End();

                // 設定を保存する
                Settings.Default.Save();
                FFXIV.Framework.Config.Save();
                FFXIV.Framework.Config.Free();

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
                this.Logger.Error(ex, "DeInitPlugin error.");
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
                this.Logger.Info("Playback buffers cleared.");
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
                    this.Logger.Error(message);
                }

                Settings.Default.LastUpdateDateTime = DateTime.Now;
                Settings.Default.Save();
            }
        }
    }
}
