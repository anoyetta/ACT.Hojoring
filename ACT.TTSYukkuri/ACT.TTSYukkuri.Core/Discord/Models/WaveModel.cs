using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using NAudio.Wave;

namespace ACT.TTSYukkuri.Discord.Models
{
    public class WaveModel
    {
        #region Singleton

        private static WaveModel instance = new WaveModel();

        public static WaveModel Instance => instance;

        #endregion Singleton

        private string FFMpeg =>
            Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "ffmpeg.exe");

        private Dictionary<string, byte[]> waves = new Dictionary<string, byte[]>();

        public byte[] GetWaveSound(
            string file)
        {
            if (!File.Exists(file))
            {
                return null;
            }

            var buffer = new byte[0];

            lock (this)
            {
                if (this.waves.TryGetValue(file, out buffer))
                {
                    return buffer;
                }

                buffer = File.ReadAllBytes(file);

                this.waves.Add(file, buffer);

                return buffer;
            }
        }

        public Task SendEncodeAsync(
            string wave,
            Func<byte[], int, Task> sendDelegate,
            bool useNAudio = true)
            => useNAudio ?
                this.EncodeByNAudio(wave, sendDelegate) :
                this.EncodeByFFMEPG(wave, sendDelegate);

        private async Task EncodeByFFMEPG(
            string wave,
            Func<byte[], int, Task> sendDelegate)
        {
            var pi = new ProcessStartInfo
            {
                FileName = this.FFMpeg,
                Arguments = $"-i \"{wave}\" -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            var ffmpeg = Process.Start(pi);
            var ffout = ffmpeg.StandardOutput.BaseStream;

            var bufferSize = 3840;

            // lets buffer ffmpeg output
            using (var ms = new MemoryStream())
            {
                await ffout.CopyToAsync(ms);
                ms.Position = 0;

                var buff = new byte[bufferSize];
                var br = 0;
                while ((br = ms.Read(buff, 0, buff.Length)) > 0)
                {
                    // it's possible we got less than expected, let's null the remaining part of the buffer
                    if (br < buff.Length)
                    {
                        for (var i = br; i < buff.Length; i++)
                        {
                            buff[i] = 0;
                        }
                    }

                    // we're sending 20ms of data
                    await sendDelegate(buff, 20);
                }
            }
        }

        /// <summary>
        /// DISCORDの規定オーディオフォーマット
        /// </summary>
        /// <remarks>
        /// 48kHz, 16bit, 2ch 固定である。
        /// </remarks>
        private static readonly WaveFormat DiscordOutputFormat = new WaveFormat(48000, 16, 2);

        private async Task EncodeByNAudio(
            string wave,
            Func<byte[], int, Task> sendDelegate)
        {
            using (var audio = new AudioFileReader(wave))
            using (var resampler = new MediaFoundationResampler(audio, DiscordOutputFormat))
            {
                resampler.ResamplerQuality = 60;

                var blockSize = DiscordOutputFormat.AverageBytesPerSecond / 50;
                var buffer = new byte[blockSize];
                var byteCount = 0;

                while ((byteCount = resampler.Read(buffer, 0, blockSize)) > 0)
                {
                    if (byteCount < blockSize)
                    {
                        for (int i = byteCount; i < blockSize; i++)
                        {
                            buffer[i] = 0;
                        }
                    }

                    await sendDelegate(buffer, blockSize);
                }
            }
        }

        public void WriteAudioStream(
            Stream outputStream,
            string audioFile)
        {
            using (var audio = new AudioFileReader(audioFile))
            using (var resampler = new MediaFoundationResampler(audio, DiscordOutputFormat))
            {
                resampler.ResamplerQuality = 60;

                var blockSize = DiscordOutputFormat.AverageBytesPerSecond / 50;
                var buffer = new byte[blockSize];
                var byteCount = 0;

                while ((byteCount = resampler.Read(buffer, 0, blockSize)) > 0)
                {
                    if (byteCount < blockSize)
                    {
                        for (int i = byteCount; i < blockSize; i++)
                        {
                            buffer[i] = 0;
                        }
                    }

                    outputStream.Write(buffer, 0, buffer.Length);
                }
            }
        }
    }
}
