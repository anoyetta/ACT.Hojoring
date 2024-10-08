using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Timers;
using System.Xml.Serialization;
using ACT.SpecialSpellTimer.Sound;
using Advanced_Combat_Tracker;
using FFXIV.Framework.Bridge;
using FFXIV.Framework.Common;
using FFXIV.Framework.Globalization;
using Prism.Mvvm;

namespace ACT.SpecialSpellTimer.Config.Models
{
    [Serializable]
    public class AdvancedNoticeConfig :
        BindableBase,
        ICloneable
    {
        /// <summary>
        /// シンクロTTSの発声間隔 (ミリ秒)
        /// </summary>
        public const double SyncTTSInterval = 5;

        #region Available TTSYukkuri

        private static System.Timers.Timer timer = new System.Timers.Timer(5 * 1000);

        public static bool AvailableTTSYukkuri { get; private set; } = WPFHelper.IsDesignMode;

        static AdvancedNoticeConfig()
        {
            if (WPFHelper.IsDesignMode)
            {
                return;
            }

            timer.Elapsed += (x, y) =>
                AvailableTTSYukkuri = PlayBridge.Instance.IsAvailable;

            timer.Start();
        }

        #endregion Available TTSYukkuri

        [XmlIgnore]
        public bool Available => AdvancedNoticeConfig.AvailableTTSYukkuri;

        private bool isEnabled;

        public bool IsEnabled
        {
            get => this.isEnabled;
            set => this.SetProperty(ref this.isEnabled, value);
        }

        private bool toMainDevice = true;

        public bool ToMainDevice
        {
            get => this.toMainDevice;
            set => this.SetProperty(ref this.toMainDevice, value);
        }

        private bool toSubDevice;

        public bool ToSubDevice
        {
            get => this.toSubDevice;
            set => this.SetProperty(ref this.toSubDevice, value);
        }

        private bool toDicordTextChat;

        public bool ToDicordTextChat
        {
            get => this.toDicordTextChat;
            set => this.SetProperty(ref this.toDicordTextChat, value);
        }

        [XmlIgnore]
        public IEnumerable<VoicePalettes> Palettes => (IEnumerable<VoicePalettes>)Enum.GetValues(typeof(VoicePalettes));

        public VoicePalettes Palette { get; set; } = VoicePalettes.Default;

        public void PlayWave(string wave) => PlayWaveCore(wave, this);

        public void Speak(string tts, bool sync = false, int priority = 0) => SyncSpeak(tts, this, sync, priority);

        public const string SyncKeyword = "/sync";

        [XmlIgnore]
        private static readonly Regex SyncRegex = new Regex(
            $@"{SyncKeyword} (?<priority>\d+?):(?<text>.*)",
            RegexOptions.Compiled);

        [XmlIgnore]
        private static readonly List<SyncTTS> SyncList = new List<SyncTTS>(16);

        [XmlIgnore]
        private static volatile int SyncListCount = 0;

        [XmlIgnore]
        private static DateTime SyncListTimestamp = DateTime.MaxValue;

        [XmlIgnore]
        private static double SyncTimerIdleInterval => Settings.Default.WaitingTimeToSyncTTS < 100d ?
            100d :
            Settings.Default.WaitingTimeToSyncTTS;

        [XmlIgnore]
        private static readonly System.Timers.Timer SyncSpeakTimer = CreateSyncSpeakTimer();

        private static System.Timers.Timer CreateSyncSpeakTimer()
        {
            var timer = new System.Timers.Timer(SyncTimerIdleInterval)
            {
                AutoReset = true
            };

            timer.Elapsed += SyncSpeakTimerOnElapsed;

            return timer;
        }

        private static void PlayWaveCore(
            string wave,
            AdvancedNoticeConfig config,
            bool isSync = false)
        {
            if (string.IsNullOrEmpty(wave))
            {
                return;
            }

            if (!File.Exists(wave))
            {
                return;
            }

            if (!config.IsEnabled)
            {
                if (PlayBridge.Instance.IsAvailable)
                {
                    PlayBridge.Instance.Play(wave, isSync);
                }
                else
                {
                    ActGlobals.oFormActMain.PlaySound(wave);
                }

                return;
            }

            if (config.ToMainDevice)
            {
                PlayBridge.Instance.PlayMain(wave, config.Palette, isSync);
            }

            if (config.ToSubDevice)
            {
                PlayBridge.Instance.PlaySub(wave, config.Palette, isSync);
            }
        }

        private static void SpeakCore(
            string tts,
            AdvancedNoticeConfig config,
            bool isSync = false)
        {
            if (string.IsNullOrEmpty(tts))
            {
                return;
            }

            tts = TTSDictionary.Instance.ReplaceWordsTTS(tts);

            if (!config.IsEnabled)
            {
                if (PlayBridge.Instance.IsAvailable)
                {
                    if (Settings.Default.IsDefaultNoticeToOnlyMain)
                    {
                        PlayBridge.Instance.PlayMain(tts, isSync);
                    }
                    else
                    {
                        PlayBridge.Instance.Play(tts, isSync);
                    }
                }
                else
                {
                    ActGlobals.oFormActMain.TTS(tts);
                }

                return;
            }

            if (config.ToMainDevice)
            {
                PlayBridge.Instance.PlayMain(tts, config.Palette, isSync);
            }

            if (config.ToSubDevice)
            {
                PlayBridge.Instance.PlaySub(tts, config.Palette, isSync);
            }

            if (config.ToDicordTextChat)
            {
                DiscordBridge.Instance.SendMessageDelegate?.Invoke(tts);
            }
        }

        private static void SyncSpeak(
            string tts,
            AdvancedNoticeConfig config,
            bool sync = false,
            int priority = 0)
        {
            if (string.IsNullOrEmpty(tts))
            {
                return;
            }

            if (!sync)
            {
                SpeakCore(tts, config);
                return;
            }

            if (!PlayBridge.Instance.IsSyncAvailable)
            {
                var period = Settings.Default.UILocale == Locales.JA ? "、" : ",";
                if (tts.EndsWith(period))
                {
                    tts += period;
                }
            }

            lock (SyncList)
            {
                var interval = Settings.Default.WaitingTimeToSyncTTS / 4d;

                SyncList.Add(new SyncTTS(SyncList.Count, priority, tts, config));
                SyncListTimestamp = DateTime.UtcNow;
                SyncListCount = SyncList.Count;

                SyncSpeakTimer.Interval = interval;

                if (!SyncSpeakTimer.Enabled)
                {
                    SyncSpeakTimer.Start();
                }
            }
        }

        private static void SyncSpeakTimerOnElapsed(
            object sender,
            ElapsedEventArgs e)
        {
            if (SyncListCount < 1)
            {
                SyncSpeakTimer.Interval = SyncTimerIdleInterval;
                return;
            }

            var syncs = default(IEnumerable<SyncTTS>);
            lock (SyncList)
            {
                if ((DateTime.UtcNow - SyncListTimestamp).TotalMilliseconds < Settings.Default.WaitingTimeToSyncTTS)
                {
                    return;
                }

                syncs = (
                    from x in SyncList
                    where
                    !string.IsNullOrEmpty(x.Text)
                    orderby
                    x.Priority,
                    x.Seq
                    select
                    x).ToArray();

                SyncList.Clear();
                SyncListCount = 0;
            }

            var config = syncs.FirstOrDefault()?.Config;
            if (config == null)
            {
                return;
            }

            if (!PlayBridge.Instance.IsSyncAvailable)
            {
                var text = string.Join(Environment.NewLine, syncs.Select(x => x.Text));
                SpeakCore(text, config);
            }
            else
            {
                foreach (var sync in syncs)
                {
                    SpeakCore(sync.Text, config, true);
                    Thread.Sleep(TimeSpan.FromMilliseconds(SyncTTSInterval));
                }
            }
        }

        public object Clone() => this.MemberwiseClone();

        public class SyncTTS
        {
            public SyncTTS()
            {
            }

            public SyncTTS(
                int seq,
                double priority,
                string text,
                AdvancedNoticeConfig config)
            {
                this.Seq = seq;
                this.Priority = priority;
                this.Text = text;
                this.Config = config;
            }

            public int Seq { get; set; }

            public double Priority { get; set; }

            public string Text { get; set; }

            public AdvancedNoticeConfig Config { get; set; }
        }
    }
}
