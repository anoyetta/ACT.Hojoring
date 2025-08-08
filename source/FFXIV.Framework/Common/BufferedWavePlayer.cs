using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Windows.Media.Devices;

namespace FFXIV.Framework.Common
{
    public class BufferedWavePlayer :
        IDisposable
    {
        #region Singleton

        private static BufferedWavePlayer instance;

        public static BufferedWavePlayer Instance
            => instance ?? (instance = new BufferedWavePlayer());

        private BufferedWavePlayer()
        {
            this.Init();
        }

        public static void Free()
        {
            if (instance != null)
            {
                MediaDevice.DefaultAudioRenderDeviceChanged -= instance.MediaDevice_DefaultAudioRenderDeviceChanged;
                instance.Dispose();
                instance = null;
            }
        }

        #endregion Singleton

        /// <summary>
        /// Default WaveFormat 44.1khz 16bit 2ch
        /// </summary>
        private static readonly WaveFormat DefaultOutputFormat = new WaveFormat(44100, 16, 2);

        private static readonly MMDeviceEnumerator DeviceEnuerator = new MMDeviceEnumerator();

        private MMDevice[] GetDevices() => DeviceEnuerator
            .EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
            .ToArray();

        private MMDevice GetDefaultAudioDevice() => DeviceEnuerator
            .GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);

        private MMDevice GetDefaultComAudioDevice() => DeviceEnuerator
            .GetDefaultAudioEndpoint(DataFlow.Render, Role.Communications);

        private readonly Dictionary<string, PlayerSet> players = new Dictionary<string, PlayerSet>(2);

        private static readonly string[] TTSCacheDirectories = new[]
        {
            System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                @"anoyetta\ACT\tts cache"),
            DirectoryHelper.FindSubDirectory(@"resources\wav"),
        };

        private static readonly string TTSHistoryFileName =
            System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                @"anoyetta\ACT\tts cache",
                @"history.txt");

        private static readonly TimeSpan CacheToloadScope = TimeSpan.FromDays(10);

        private void Init()
        {
            MediaDevice.DefaultAudioRenderDeviceChanged += this.MediaDevice_DefaultAudioRenderDeviceChanged;
        }

        public void Dispose()
        {
            if (this.players.Count < 1)
            {
                return;
            }

            lock (this.players)
            {
                foreach (var entry in this.players)
                {
                    entry.Value?.Dispose();
                }

                this.players.Clear();
            }
        }

        public void Play(
            string file,
            float volume = 1.0f,
            string deviceID = null,
            bool sync = false)
        {
            try
            {
                if (!File.Exists(file))
                {
                    return;
                }

                var ps = this.GetPlayerSet(deviceID);
                if (ps == null)
                {
                    return;
                }

                ps.Play(file, volume, sync);
            }
#if DEBUG
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                throw;
            }
#endif
            finally
            {
            }
        }

        public void ClearBuffers()
        {
            lock (this.players)
            {
                if (this.players.Count > 0)
                {
                    var player = this.players.FirstOrDefault().Value;
                    player.ClearBuffers();
                }
            }
        }

        public int BufferWaves(
            float volume)
        {
            var count = 0;
            var files = new List<string>();

            foreach (var dir in TTSCacheDirectories)
            {
                if (!Directory.Exists(dir))
                {
                    continue;
                }

                files.AddRange(Directory.GetFiles(dir, "*.wav"));
                files.AddRange(Directory.GetFiles(dir, "*.mp3"));
            }

            if (files.Any())
            {
                lock (this.players)
                {
                    if (this.players.Count > 0)
                    {
                        var player = this.players.FirstOrDefault().Value;
                        count = player.BufferWaves(files, volume);
                    }
                }
            }

            return count;
        }

        public void BufferWaves(
            IEnumerable<string> files,
            float volume)
        {
            lock (this.players)
            {
                if (this.players.Count > 0)
                {
                    var player = this.players.FirstOrDefault().Value;
                    player.BufferWaves(files, volume);
                }
            }
        }

        private PlayerSet GetPlayerSet(
            string deviceID)
        {
            var ps = default(PlayerSet);

            lock (this.players)
            {
                if (this.defaultAudioDevice == null ||
                    this.defaultComAudioDevice == null)
                {
                    this.UpdateDefaultAudioDevices();
                }

                var key = !string.IsNullOrEmpty(deviceID) ?
                    deviceID :
                    this.defaultAudioDevice.ID;

                var isNew = false;

                if (!this.players.ContainsKey(key))
                {
                    isNew = true;
                }
                else
                {
                    ps = this.players[key];

                    switch (deviceID)
                    {
                        case PlayDevice.DefaultDeviceID:
                            isNew = ps.DeviceID != this.defaultAudioDevice.ID;
                            break;

                        case PlayDevice.DefaultComDeviceID:
                            isNew = ps.DeviceID != this.defaultComAudioDevice.ID;
                            break;
                    }

                    if (isNew)
                    {
                        ps.Dispose();
                        this.players.Remove(key);
                    }
                }

                if (isNew)
                {
                    var device = deviceID switch
                    {
                        PlayDevice.DefaultDeviceID => this.GetDevices().FirstOrDefault(x => x.ID == this.defaultAudioDevice.ID),
                        PlayDevice.DefaultComDeviceID => this.GetDevices().FirstOrDefault(x => x.ID == this.defaultComAudioDevice.ID),
                        _ => this.GetDevices().FirstOrDefault(x => x.ID == deviceID)
                    };

                    if (device != null)
                    {
                        ps = new PlayerSet();
                        ps.Init(device);

                        this.players[key] = ps;
                    }
                }
            }

            return ps;
        }

        private MMDevice defaultAudioDevice;
        private MMDevice defaultComAudioDevice;

        private void MediaDevice_DefaultAudioRenderDeviceChanged(
            object sender,
            DefaultAudioRenderDeviceChangedEventArgs args)
        {
            this.UpdateDefaultAudioDevices();
        }

        private void UpdateDefaultAudioDevices()
        {
            lock (this.players)
            {
                this.defaultAudioDevice = this.GetDefaultAudioDevice();
                this.defaultComAudioDevice = this.GetDefaultComAudioDevice();
            }
        }

        public class PlayerSet :
            IDisposable
        {
            private static readonly Dictionary<string, WaveDataContainer> WaveBuffer = new Dictionary<string, WaveDataContainer>(512);

            public string DeviceID { get; set; }

            public IWavePlayer Player { get; set; }

            public BufferedWaveProvider[] Buffers { get; private set; } = null;

            public int CurrentPlayerIndex { get; private set; } = 0;

            public WaveFormat OutputFormat => DefaultOutputFormat;

            public void Dispose()
            {
                this.Player?.Dispose();
            }

            public void Init(
                MMDevice device)
            {
                if (device == null)
                {
                    return;
                }

                var config = Config.Instance;

                this.DeviceID = device.ID;

                this.Player = new WasapiOut(
                    device,
                    AudioClientShareMode.Shared,
                    false,
                    config.WasapiLatency);

                var list = new List<BufferedWaveProvider>();
                for (int i = 0; i < config.WasapiMultiplePlaybackCount; i++)
                {
                    var buffer = new BufferedWaveProvider(this.OutputFormat)
                    {
                        BufferDuration = config.WasapiLoopBufferDuration,
                        DiscardOnBufferOverflow = true,
                    };

                    list.Add(buffer);
                }

                // シンクロ再生用のバッファを追加しておく
                list.Add(new BufferedWaveProvider(this.OutputFormat)
                {
                    BufferDuration = config.WasapiLoopBufferDuration,
                    DiscardOnBufferOverflow = true,
                });

                this.Buffers = list.ToArray();

                // ミキサを生成する
                var mixer = new MixingWaveProvider32(
                    this.Buffers.Select(x => new Wave16ToFloatProvider(x)));

                this.Player.Init(mixer);
                this.Player.Play();
                this.Player.SetBackground();
            }

            public void ClearBuffers()
            {
                lock (this)
                {
                    foreach (var buffer in this.Buffers)
                    {
                        buffer.ClearBuffer();
                    }
                }
            }

            public void Play(
                string file,
                float volume = 1.0f,
                bool sync = false)
            {
                if (!File.Exists(file))
                {
                    return;
                }

                var buffer = default(BufferedWaveProvider);
                var config = Config.Instance;

                if (!sync)
                {
                    lock (this)
                    {
                        buffer = this.Buffers[this.CurrentPlayerIndex];
                        this.CurrentPlayerIndex++;

                        if (this.CurrentPlayerIndex >= config.WasapiMultiplePlaybackCount)
                        {
                            this.CurrentPlayerIndex = 0;
                        }
                    }
                }
                else
                {
                    buffer = this.Buffers[config.WasapiMultiplePlaybackCount];
                }

                if (buffer == null)
                {
                    return;
                }

                var samples = default(byte[]);
                var key = GetBufferKey(file, volume);

                lock (WaveBuffer)
                {
                    if (WaveBuffer.ContainsKey(key))
                    {
                        var wave = WaveBuffer[key];

                        if (wave.Samples.Length > 0)
                        {
                            samples = wave.Samples;
                            wave.LastAccessTimestamp = DateTime.Now;
                        }
                    }

                    if (samples == null || samples.Length <= 0)
                    {
                        samples = ReadWaveSamples(file, volume);
                        WaveBuffer[key] = new WaveDataContainer(ReadWaveSamples(file, volume));
                    }
                }

                buffer.AddSamples(
                    samples,
                    0,
                    samples.Length);

#if DEBUG
                System.Diagnostics.Debug.WriteLine($"WASAPI(Buffered) Play: {file}");
#endif
            }

            public void BufferWave(
                string file,
                float volume)
            {
                lock (WaveBuffer)
                {
                    var key = GetBufferKey(file, volume);
                    WaveBuffer[key] = new WaveDataContainer(ReadWaveSamples(file, volume));
                }
            }

            public int BufferWaves(
                IEnumerable<string> files,
                float volume)
            {
                var count = 0;

                lock (WaveBuffer)
                {
                    foreach (var file in files)
                    {
                        var key = GetBufferKey(file, volume);

                        if (WaveBuffer.ContainsKey(key))
                        {
                            var container = WaveBuffer[key];
                            if ((DateTime.Now - container.LastAccessTimestamp) <= CacheToloadScope)
                            {
                                container.Samples = ReadWaveSamples(file, volume);
                                count++;
                                Thread.Yield();
                            }
                        }
                        else
                        {
                            WaveBuffer[key] = new WaveDataContainer(ReadWaveSamples(file, volume))
                            {
                                LastAccessTimestamp = DateTime.MinValue
                            };

                            count++;
                            Thread.Yield();
                        }
                    }
                }

                return count;
            }

            public static string GetBufferKey(string file, float volume) => $"{file}-{volume.ToString("N2")}".ToUpper();

            private static byte[] ReadWaveSamples(
                string file,
                float volume)
            {
                var samples = default(byte[]);
                var vol = volume > 1.0f ? 1.0f : volume;

                using (var audio = new AudioFileReader(file) { Volume = vol })
                using (var resampler = new MediaFoundationResampler(audio, DefaultOutputFormat))
                using (var output = new MemoryStream(51200))
                using (var wrap = new WrappingStream(output))
                {
                    WaveFileWriter.WriteWavFileToStream(wrap, resampler);
                    wrap.Flush();
                    wrap.Position = 0;

                    // ヘッダをカットする
                    var raw = wrap.ToArray();
                    var headerLength = 0;
                    using (var wave = new WaveFileReader(wrap))
                    {
                        headerLength = (int)(raw.Length - wave.Length);
                    }

                    // ヘッダをスキップした波形データを取得する
                    samples = raw.Skip(headerLength).ToArray();
                }

                return samples;
            }

            public static void LoadTTSHistory()
            {
                var file = TTSHistoryFileName;

                if (!File.Exists(file))
                {
                    return;
                }

                lock (WaveBuffer)
                {
                    var lines = File.ReadAllLines(file, new UTF8Encoding(false));

                    foreach (var line in lines)
                    {
                        var values = line.Split('\t');
                        if (values.Length < 2)
                        {
                            continue;
                        }

                        var key = values[0];

                        if (!DateTime.TryParse(values[1], out DateTime timestamp))
                        {
                            continue;
                        }

                        var keySplits = key.Split('-');
                        if (keySplits.Length < 2)
                        {
                            continue;
                        }

                        var waveFile = keySplits[0];
                        if (!File.Exists(waveFile))
                        {
                            continue;
                        }

                        WaveBuffer[key] = new WaveDataContainer()
                        {
                            LastAccessTimestamp = timestamp
                        };
                    }
                }
            }

            public static void SaveTTSHistory()
            {
                var sb = new StringBuilder();

                lock (WaveBuffer)
                {
                    foreach (var item in WaveBuffer)
                    {
                        sb.AppendLine($"{item.Key}\t{item.Value.LastAccessTimestamp}");
                    }

                    File.WriteAllText(
                        TTSHistoryFileName,
                        sb.ToString(),
                        new UTF8Encoding(false));
                }
            }
        }
    }

    public static class WavePlayerExtensions
    {
        public static void SetBackground(
            this IWavePlayer player)
        {
            var fi = player.GetType().GetField(
                "playThread",
                BindingFlags.Instance |
                BindingFlags.NonPublic);

            if (fi != null)
            {
                var thread = fi.GetValue(player) as Thread;
                if (thread != null)
                {
                    thread.IsBackground = true;
                }
            }
        }
    }

    public class WaveDataContainer
    {
        public WaveDataContainer()
        {
        }

        public WaveDataContainer(byte[] samples)
        {
            this.Samples = samples;
        }

        public byte[] Samples { get; set; } = new byte[0];

        public DateTime LastAccessTimestamp { get; set; } = DateTime.Now;
    }
}
