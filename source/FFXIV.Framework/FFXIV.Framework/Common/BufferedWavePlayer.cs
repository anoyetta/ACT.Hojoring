using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using NAudio.CoreAudioApi;
using NAudio.Wave;

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
        }

        public static void Free()
        {
            instance?.Dispose();
            instance = null;
        }

        #endregion Singleton

        /// <summary>
        /// Default WaveFormat 44.1khz 16bit 2ch
        /// </summary>
        private static readonly WaveFormat DefaultOutputFormat = new WaveFormat(44100, 16, 2);

        private MMDevice[] GetDevices() =>
            new MMDeviceEnumerator()
            .EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
            .ToArray();

        private MMDevice GetDefaultAudioDevice() =>
            (new MMDeviceEnumerator()).GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);

        private readonly Dictionary<string, PlayerSet> players = new Dictionary<string, PlayerSet>(2);

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
            var key = !string.IsNullOrEmpty(deviceID) ?
                deviceID :
                this.GetDefaultAudioDevice().ID;

            lock (this.players)
            {
                if (this.players.ContainsKey(key))
                {
                    ps = this.players[key];
                }
                else
                {
                    var device = this.GetDevices().FirstOrDefault(x => x.ID == deviceID);

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

        public class PlayerSet :
            IDisposable
        {
            private static int MultiplePlaybackCount => Config.Instance.WasapiMultiplePlaybackCount;

            private static TimeSpan BufferDurations => Config.Instance.WasapiLoopBufferDuration;

            private static readonly Dictionary<string, byte[]> WaveBuffer = new Dictionary<string, byte[]>(512);

            public string DeviceID { get; set; }

            public IWavePlayer Player { get; set; }

            public BufferedWaveProvider[] Buffers { get; private set; } = null;

            public int CurrentPlayerIndex { get; private set; } = 0;

            public WaveFormat OutputFormat { get; private set; } = DefaultOutputFormat;

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

                this.Player = new WasapiOut(
                    device,
                    AudioClientShareMode.Shared,
                    false,
                    Config.Instance.WasapiLatency);

                var list = new List<BufferedWaveProvider>();
                for (int i = 0; i < MultiplePlaybackCount; i++)
                {
                    var buffer = new BufferedWaveProvider(this.OutputFormat)
                    {
                        BufferDuration = BufferDurations,
                        DiscardOnBufferOverflow = true,
                    };

                    list.Add(buffer);
                }

                // シンクロ再生用のバッファを追加しておく
                list.Add(new BufferedWaveProvider(this.OutputFormat)
                {
                    BufferDuration = BufferDurations,
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

                if (!sync)
                {
                    lock (this)
                    {
                        buffer = this.Buffers[this.CurrentPlayerIndex];
                        this.CurrentPlayerIndex++;

                        if (this.CurrentPlayerIndex >= MultiplePlaybackCount)
                        {
                            this.CurrentPlayerIndex = 0;
                        }
                    }
                }
                else
                {
                    buffer = this.Buffers[MultiplePlaybackCount];
                }

                if (buffer == null)
                {
                    return;
                }

                var samples = default(byte[]);
                var key = this.GetBufferKey(file, volume);

                lock (WaveBuffer)
                {
                    if (WaveBuffer.ContainsKey(key))
                    {
                        samples = WaveBuffer[key];
                    }
                    else
                    {
                        samples = this.ReadWaveSamples(file, volume);
                        WaveBuffer[key] = samples;
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
                    var key = this.GetBufferKey(file, volume);
                    WaveBuffer[key] = this.ReadWaveSamples(file, volume);
                }
            }

            public void BufferWaves(
                IEnumerable<string> files,
                float volume)
            {
                lock (WaveBuffer)
                {
                    foreach (var file in files)
                    {
                        var key = this.GetBufferKey(file, volume);
                        WaveBuffer[key] = this.ReadWaveSamples(file, volume);
                        Thread.Sleep(10);
                    }
                }
            }

            private string GetBufferKey(string file, float volume) => $"{file}-{volume.ToString("N2")}".ToUpper();

            private byte[] ReadWaveSamples(
                string file,
                float volume)
            {
                var samples = default(byte[]);
                var vol = volume > 1.0f ? 1.0f : volume;

                using (var audio = new AudioFileReader(file) { Volume = vol })
                using (var resampler = new MediaFoundationResampler(audio, this.OutputFormat))
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
}
