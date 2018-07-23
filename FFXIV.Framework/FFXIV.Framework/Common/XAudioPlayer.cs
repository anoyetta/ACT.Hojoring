#if false
using System.Collections.Generic;
using System.Linq;
using CSCore;
using CSCore.Codecs;
using CSCore.CoreAudioAPI;
using CSCore.XAudio2;

namespace FFXIV.Framework.Common
{
    public class XAudioPlayer
    {
#region Singleton

        private static XAudioPlayer instance;

        public static XAudioPlayer Instance =>
            instance ?? (instance = new XAudioPlayer());

        private XAudioPlayer()
        {
            this.DisposeTimer.Elapsed += (x, y) => this.DisposeVoices();
            this.DisposeTimer.AutoReset = true;
            this.DisposeTimer.Start();
        }

        public static void Free()
        {
            if (instance != null)
            {
                instance.DisposeTimer.Stop();
                instance.DisposeVoices();

                foreach (var entry in instance.masterVoiceDictionary)
                {
                    entry.Value.Dispose();
                }

                instance.masterVoiceDictionary.Clear();
                instance.xaudio?.Dispose();

                instance = null;
            }
        }

#endregion Singleton

        private readonly static List<PlayDevice> DeviceList = new List<PlayDevice>();

        public static List<PlayDevice> EnumerateDevices()
        {
            if (!DeviceList.Any())
            {
                var devices = WavePlayer.EnumerateDevicesByWasapiOut();
                DeviceList.AddRange(devices);
            }

            return DeviceList;
        }

        private XAudio2 xaudio = XAudio2.CreateXAudio2();
        private readonly Dictionary<string, XAudio2MasteringVoice> masterVoiceDictionary = new Dictionary<string, XAudio2MasteringVoice>();
        private readonly List<(IWaveSource Source, XAudio2SourceVoice SourceVoice)> DisposeQueue = new List<(IWaveSource Source, XAudio2SourceVoice SourceVoice)>();
        private readonly System.Timers.Timer DisposeTimer = new System.Timers.Timer(5 * 1000);

        private void DisposeVoices()
        {
            if (this.DisposeQueue.Count <= 0)
            {
                return;
            }

            lock (this.DisposeQueue)
            {
                var queues = this.DisposeQueue.ToArray();
                var targets = queues.Where(z => z.SourceVoice.State.BuffersQueued <= 0);

                foreach (var entry in targets)
                {
                    entry.SourceVoice?.Stop();
                    entry.SourceVoice?.Dispose();
                    entry.Source?.Dispose();

                    this.DisposeQueue.Remove(entry);
                }
            }
        }

        private XAudio2MasteringVoice GetMasteringVoice(
            string devicePath)
        {
            var voice = default(XAudio2MasteringVoice);

            lock (masterVoiceDictionary)
            {
                var key = devicePath ?? "Default";

                if (masterVoiceDictionary.ContainsKey(key))
                {
                    voice = masterVoiceDictionary[key];
                }
                else
                {
                    voice = this.xaudio.CreateMasteringVoice(
                        XAudio2.DefaultChannels,
                        XAudio2.DefaultSampleRate,
                        devicePath);

                    masterVoiceDictionary[key] = voice;
                }
            }

            return voice;
        }

        public void Play(
            string wave,
            float volume = 1.0f,
            string deviceID = null)
        {
            var device = DeviceList?
                .FirstOrDefault(x => x.ID == deviceID)?
                .DeviceObject as MMDevice;

            this.GetMasteringVoice(device?.DevicePath);

            var source = CodecFactory.Instance.GetCodec(wave);
            var sourceVoice = this.xaudio.CreateSourceVoice(
                source.WaveFormat,
                VoiceFlags.NoPitch);

            sourceVoice.SetVolume(volume, 0);

            using (var buffer = new XAudio2Buffer((int)source.Length))
            using (var stream = buffer.GetStream())
            {
                source.WriteToStream(stream);
                stream.Flush();

                sourceVoice.SubmitSourceBuffer(buffer);
            }

            sourceVoice.Start();

            lock (this.DisposeQueue)
            {
                this.DisposeQueue.Add((source, sourceVoice));
            }
        }
    }
}
#endif
