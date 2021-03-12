using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Timers;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using Prism.Mvvm;

namespace FFXIV.Framework.Common
{
    /// <summary>
    /// WavePlayerTypes
    /// </summary>
    public enum WavePlayerTypes
    {
        WaveOut,
        DirectSound,
        WASAPI,
        WASAPIBuffered,
    }

    public static class WavePlayerTypesExtentions
    {
        public static IEnumerable<WavePlayerTypes> GetAvailablePlayers(
            this WavePlayerTypes t) =>
            Enum.GetValues(typeof(WavePlayerTypes))
            .Cast<WavePlayerTypes>()
            .Where(x =>
                x != WavePlayerTypes.WaveOut);

        public static string ToDisplay(
            this WavePlayerTypes v) => new[]
            {
                "WaveOut",
                "DirectSound",
                "WASAPI",
                "WASAPI (Buffered)"
            }[(int)v];
    }

    public class WavePlayer
    {
        #region Singleton

        private static WavePlayer instance;

        public static WavePlayer Instance =>
            instance ?? (instance = new WavePlayer());

        public static void Free()
        {
            DisposeTimer.Stop();
            DisposeSoundOuts();

            BufferedWavePlayer.Free();

            instance = null;
        }

        public WavePlayer()
        {
            DisposeTimer.Elapsed += (x, y) =>
            {
                DisposeSoundOuts();
            };

            DisposeTimer.AutoReset = true;
        }

        #endregion Singleton

        private static WavePlayerTypes? currentPlayerType = null;
        private static readonly List<PlayDevice> DeviceList = new List<PlayDevice>();
        private static readonly ConcurrentQueue<IWavePlayer> DisposeQueue = new ConcurrentQueue<IWavePlayer>();
        private static readonly Timer DisposeTimer = new Timer(5 * 1000);

        private static void DisposeSoundOuts()
        {
            if (DisposeQueue.IsEmpty)
            {
                return;
            }

            while (DisposeQueue.TryDequeue(out IWavePlayer player))
            {
                player.Stop();
                player.Dispose();
            }
        }

        /// <summary>
        /// 再生デバイスを列挙する
        /// </summary>
        /// <returns>再生デバイスのリスト</returns>
        public static List<PlayDevice> EnumerateDevices(
            WavePlayerTypes playerType)
        {
            if (currentPlayerType == playerType)
            {
                return DeviceList;
            }

            var list = default(List<PlayDevice>);

            switch (playerType)
            {
                case WavePlayerTypes.WaveOut:
                    list = EnumerateDevicesByWaveOut();
                    break;

                case WavePlayerTypes.DirectSound:
                    list = EnumerateDevicesByDirectSoundOut();
                    break;

                case WavePlayerTypes.WASAPI:
                    list = EnumerateDevicesByWasapiOut();
                    break;

                case WavePlayerTypes.WASAPIBuffered:
                    list = EnumerateDevicesByWasapiOut();
                    break;
            }

            if (list != null)
            {
                if (!list.Any(x => x.ID == PlayDevice.DiscordPlugin.ID))
                {
                    list.Add(PlayDevice.DiscordPlugin);
                }
            }

            DeviceList.Clear();
            DeviceList.AddRange(list);
            currentPlayerType = playerType;

            return DeviceList;
        }

        /// <summary>
        /// DirectSoundOutから再生デバイスを列挙する
        /// </summary>
        /// <returns>再生デバイスのリスト</returns>
        public static List<PlayDevice> EnumerateDevicesByDirectSoundOut()
        {
            var list = new List<PlayDevice>();

            foreach (var device in DirectSoundOut.Devices)
            {
                list.Add(new PlayDevice()
                {
                    ID = device.Guid.ToString(),
                    Name = device.Description,
                });
            }

            return list;
        }

        /// <summary>
        /// WasapiOutから再生デバイスを列挙する
        /// </summary>
        /// <returns>再生デバイスのリスト</returns>
        public static List<PlayDevice> EnumerateDevicesByWasapiOut()
        {
            var list = new List<PlayDevice>();

            var deviceEnumrator = new MMDeviceEnumerator();

            list.Add(PlayDevice.DefaultDevice);

            foreach (var device in deviceEnumrator.EnumerateAudioEndPoints(
                DataFlow.Render,
                DeviceState.Active))
            {
                list.Add(new PlayDevice()
                {
                    ID = device.ID,
                    Name = device.FriendlyName,
                });
            }

            return list;
        }

        /// <summary>
        /// WaveOutから再生デバイスを列挙する
        /// </summary>
        /// <returns>再生デバイスのリスト</returns>
        public static List<PlayDevice> EnumerateDevicesByWaveOut()
        {
            var list = new List<PlayDevice>();

            for (int i = 0; i < WaveOut.DeviceCount; i++)
            {
                var capabilities = WaveOut.GetCapabilities(i);
                list.Add(new PlayDevice()
                {
                    ID = i.ToString(),
                    Name = capabilities.ProductName,
                });
            }

            return list;
        }

        /// <summary>
        /// 再生する
        /// </summary>
        /// <param name="file">対象のサウンドファイル</param>
        /// <param name="volume">再生ボリューム</param>
        /// <param name="playerType">再生方式</param>
        /// <param name="deviceID">再生デバイス</param>
        /// <param name="sync">同期再生か？</param>
        public void Play(
            string file,
            float volume = 1.0f,
            WavePlayerTypes playerType = WavePlayerTypes.WASAPI,
            string deviceID = null,
            bool sync = false)
        {
#if DEBUG
            var sw = System.Diagnostics.Stopwatch.StartNew();
#endif
            try
            {
                if (!File.Exists(file))
                {
                    return;
                }

                if (playerType == WavePlayerTypes.WASAPIBuffered)
                {
                    BufferedWavePlayer.Instance.Play(file, volume, deviceID, sync);

                    if (DisposeTimer.Enabled)
                    {
                        DisposeTimer.Stop();
                    }

                    DisposeSoundOuts();
                    return;
                }

                var audio = new AudioFileReader(file)
                {
                    Volume = volume
                };

                var player = this.CreatePlayer(playerType, deviceID);
                player.Init(audio);

                player.PlaybackStopped += (x, y)
                    => DisposeQueue.Enqueue(x as IWavePlayer);

                player.Play();

                if (!DisposeTimer.Enabled)
                {
                    DisposeTimer.Start();
                }

                BufferedWavePlayer.Instance.Dispose();
            }
            finally
            {
#if DEBUG
                sw.Stop();
                AppLog.DefaultLogger.Info($"play wave duration_ticks={sw.ElapsedTicks:N0}, duration_ms={sw.ElapsedMilliseconds:N0} type={playerType}");
#endif
            }
        }

        private const int PlaybackLatency = 100;

        /// <summary>
        /// プレイヤーを生成する
        /// </summary>
        /// <param name="playerType">プレイヤーの種類</param>
        /// <param name="deviceID">再生デバイス</param>
        /// <returns>
        /// プレイヤー</returns>
        public IWavePlayer CreatePlayer(
            WavePlayerTypes playerType = WavePlayerTypes.WASAPI,
            string deviceID = null)
        {
            var deviceEnumrator = new MMDeviceEnumerator();

            var player = default(IWavePlayer);
            switch (playerType)
            {
                case WavePlayerTypes.WaveOut:
                    player = deviceID == null ?
                        new WaveOut() :
                        new WaveOut()
                        {
                            DeviceNumber = int.Parse(deviceID),
                            DesiredLatency = PlaybackLatency,
                        };
                    break;

                case WavePlayerTypes.DirectSound:
                    player = deviceID == null ?
                        new DirectSoundOut() :
                        new DirectSoundOut(Guid.Parse(deviceID), PlaybackLatency);
                    break;

                case WavePlayerTypes.WASAPI:
                    var device = deviceID switch
                    {
                        PlayDevice.DefaultDeviceID => deviceEnumrator
                            .GetDefaultAudioEndpoint(DataFlow.Render, Role.Console),
                        _ => deviceEnumrator
                            .EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
                            .FirstOrDefault(x => x.ID == deviceID)
                    };

                    player = device == null ?
                        new WasapiOut() :
                        new WasapiOut(
                            device,
                            AudioClientShareMode.Shared,
                            false,
                            PlaybackLatency);
                    break;
            }

            return player;
        }
    }

    /// <summary>
    /// 再生デバイス
    /// </summary>
    public class PlayDevice :
        BindableBase
    {
        public const string DiscordDeviceID = "DISCORD";

        public readonly static PlayDevice DiscordPlugin = new PlayDevice()
        {
            ID = DiscordDeviceID,
            Name = "Use DISCORD BOT",
        };

        public const string DefaultDeviceID = "DEFAULT";

        public readonly static PlayDevice DefaultDevice = new PlayDevice()
        {
            ID = DefaultDeviceID,
            Name = "Default",
        };

        private string id = null;
        private string name = null;

        /// <summary>
        /// デバイスのID
        /// </summary>
        public string ID
        {
            get => this.id;
            set => this.SetProperty(ref this.id, value);
        }

        /// <summary>
        /// デバイス名
        /// </summary>
        public string Name
        {
            get => this.name;
            set => this.SetProperty(ref this.name, value);
        }

        public override string ToString()
            => this.Name;
    }
}
