using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Threading;
using ACT.TTSYukkuri.Config;
using ACT.TTSYukkuri.Discord.Models;
using FFXIV.Framework.Common;

namespace ACT.TTSYukkuri
{
    public enum PlayDevices
    {
        Both = 0,
        Main = 1,
        Sub = 2
    }

    /// <summary>
    /// DirectXでサウンドを再生する
    /// </summary>
    public static class SoundPlayerWrapper
    {
        #region Logger

        private static NLog.Logger Logger => AppLog.DefaultLogger;

        #endregion Logger

        /// <summary>
        /// 無声音を発声してデバイスを初期化する
        /// </summary>
        public static async void Init()
        {
            try
            {
                var wave = Path.Combine(
                    DirectoryHelper.FindSubDirectory(@"resources\wav"),
                    "mute.wav");

                if (!File.Exists(wave))
                {
                    return;
                }

                BufferedWavePlayer.Instance?.Dispose();

                var volume = Settings.Default.WaveVolume / 100f;

                if (Settings.Default.EnabledSubDevice &&
                    !string.IsNullOrEmpty(Settings.Default.SubDeviceID) &&
                    Settings.Default.SubDeviceID != PlayDevice.DiscordDeviceID)
                {
                    await Task.Run(() => SoundPlayerWrapper.PlayCore(
                        wave,
                        volume,
                        Settings.Default.Player,
                        Settings.Default.SubDeviceID,
                        false));
                }

                if (!string.IsNullOrEmpty(Settings.Default.MainDeviceID) &&
                    Settings.Default.MainDeviceID != PlayDevice.DiscordDeviceID)
                {
                    await Task.Run(() => SoundPlayerWrapper.PlayCore(
                        wave,
                        volume,
                        Settings.Default.Player,
                        Settings.Default.MainDeviceID,
                        false));
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Playback stream initialize faild. Play mute.wav.");
            }
        }

        public static void Play(
            string waveFile,
            PlayDevices playDevice = PlayDevices.Both,
            bool isSync = false,
            float? volume = null)
        {
            if (!volume.HasValue)
            {
                volume = Settings.Default.WaveVolume / 100.0f;
            }

            switch (playDevice)
            {
                case PlayDevices.Both:
                    if (Settings.Default.EnabledSubDevice &&
                        !string.IsNullOrEmpty(Settings.Default.SubDeviceID))
                    {
                        SoundPlayerWrapper.PlayCore(
                            waveFile,
                            volume.Value,
                            Settings.Default.Player,
                            Settings.Default.SubDeviceID,
                            isSync);
                    }

                    SoundPlayerWrapper.PlayCore(
                        waveFile,
                        volume.Value,
                        Settings.Default.Player,
                        Settings.Default.MainDeviceID,
                        isSync);
                    break;

                case PlayDevices.Main:
                    SoundPlayerWrapper.PlayCore(
                        waveFile,
                        volume.Value,
                        Settings.Default.Player,
                        Settings.Default.MainDeviceID,
                        isSync);
                    break;

                case PlayDevices.Sub:
                    if (Settings.Default.EnabledSubDevice &&
                        !string.IsNullOrEmpty(Settings.Default.SubDeviceID))
                    {
                        SoundPlayerWrapper.PlayCore(
                            waveFile,
                            volume.Value,
                            Settings.Default.Player,
                            Settings.Default.SubDeviceID,
                            isSync);
                    }
                    break;
            }
        }

        private static readonly Dictionary<string, DateTime> LastPlayTimestamp
            = new Dictionary<string, DateTime>();

        private static void PlayCore(
            string file,
            float volume = 1.0f,
            WavePlayerTypes playerType = WavePlayerTypes.WASAPI,
            string deviceID = null,
            bool isSync = false)
        {
            if (!File.Exists(file))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(deviceID))
            {
                return;
            }

            if (deviceID == PlayDevice.DiscordDeviceID)
            {
                Task.Run(() => DiscordClientModel.Model.Play(file));
                return;
            }

            var key = $"{deviceID}-{file}";
            var timestamp = DateTime.MinValue;
            if (LastPlayTimestamp.ContainsKey(key))
            {
                timestamp = LastPlayTimestamp[key];
            }

            if ((DateTime.Now - timestamp).TotalSeconds
                <= Settings.Default.GlobalSoundInterval)
            {
                return;
            }

            LastPlayTimestamp[key] = DateTime.Now;

            isSync |= Settings.Default.IsSyncPlayback;

            WavePlayer.Instance.Play(
                file,
                volume,
                Settings.Default.Player,
                deviceID,
                isSync);
        }

        public static void LoadTTSCache(
            bool isNow = false)
        {
            if (Settings.Default.Player != WavePlayerTypes.WASAPIBuffered)
            {
                return;
            }

            var volume = Settings.Default.WaveVolume / 100f;

            WPFHelper.BeginInvoke(() =>
            { },
            DispatcherPriority.ContextIdle).Task.ContinueWith(async (_) =>
            {
                if (!isNow)
                {
                    await Task.Delay(TimeSpan.FromSeconds(120));
                }
                else
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(100));
                }

                var dirs = new[]
                {
                    Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        @"anoyetta\ACT\tts cache"),
                    DirectoryHelper.FindSubDirectory(@"resources\wav"),
                };

                var files = new List<string>();

                foreach (var dir in dirs)
                {
                    if (!Directory.Exists(dir))
                    {
                        continue;
                    }

                    files.AddRange(Directory.GetFiles(dir, "*.wav"));
                    files.AddRange(Directory.GetFiles(dir, "*.mp3"));
                }

                if (files.Count > 0)
                {
                    try
                    {
                        Logger.Info("Load TTS caches.");

                        BufferedWavePlayer.Instance.BufferWaves(
                            files,
                            volume);
                    }
                    finally
                    {
                        Logger.Info("Load TTS caches, done.");
                    }
                }
            });
        }
    }
}
