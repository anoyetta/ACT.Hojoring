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

        public static bool AvailableTTSYukkuri { get; private set; } = false;

        static AdvancedNoticeConfig()
        {
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

        public void PlayWave(string wave) => PlayWaveCore(wave, this);

        public void Speak(string tts) => SyncSpeak(tts, this);

        public const string SyncKeyword = "/sync";

        [XmlIgnore]
        private static readonly Regex SyncRegex = new Regex(
            $@"{SyncKeyword} (?<priority>\d+?) (?<text>.*)",
            RegexOptions.Compiled |
            RegexOptions.Multiline);

        [XmlIgnore]
        private static readonly List<SyncTTS> SyncList = new List<SyncTTS>();

        [XmlIgnore]
        private static System.Timers.Timer syncSpeakTimer;

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
                PlayBridge.Instance.PlayMain(wave, isSync);
            }

            if (config.ToSubDevice)
            {
                PlayBridge.Instance.PlaySub(wave, isSync);
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
                    PlayBridge.Instance.Play(tts, isSync);
                }
                else
                {
                    ActGlobals.oFormActMain.TTS(tts);
                }

                return;
            }

            if (config.ToMainDevice)
            {
                PlayBridge.Instance.PlayMain(tts, isSync);
            }

            if (config.ToSubDevice)
            {
                PlayBridge.Instance.PlaySub(tts, isSync);
            }

            if (config.ToDicordTextChat)
            {
                DiscordBridge.Instance.SendMessageDelegate?.Invoke(tts);
            }
        }

        private static void SyncSpeak(
            string tts,
            AdvancedNoticeConfig config)
        {
            if (string.IsNullOrEmpty(tts))
            {
                return;
            }

            // シンクロTTSじゃない？
            if (!tts.Contains(SyncKeyword))
            {
                SpeakCore(tts, config);
                return;
            }

            var match = SyncRegex.Match(tts);
            if (!match.Success)
            {
                // シンクロTTSじゃない
                SpeakCore(tts, config);
                return;
            }

            var value = string.Empty;
            value = match.Groups["priority"].Value;

            double priority;
            if (!double.TryParse(value, out priority))
            {
                SpeakCore(tts, config);
                return;
            }

            var speakText = match.Groups["text"].Value;
            if (string.IsNullOrEmpty(speakText))
            {
                return;
            }

            var period = Settings.Default.UILocale == Locales.JA ? "、" : ",";
            if (speakText.EndsWith(period))
            {
                speakText += period;
            }

            lock (SyncList)
            {
                var interval = Settings.Default.WaitingTimeToSyncTTS;

                if (syncSpeakTimer == null)
                {
                    // シンクロTTSの受付タイマを生成する
                    syncSpeakTimer = new System.Timers.Timer(interval)
                    {
                        AutoReset = false
                    };

                    syncSpeakTimer.Elapsed += SyncSpeakTimerOnElapsed;
                }

                syncSpeakTimer.Stop();
                syncSpeakTimer.Interval = interval;

                SyncList.Add(new SyncTTS(SyncList.Count, priority, speakText, config));

                syncSpeakTimer.Start();
            }
        }

        private static void SyncSpeakTimerOnElapsed(
            object sender,
            ElapsedEventArgs e)
        {
            lock (SyncList)
            {
                try
                {
                    var syncs =
                        from x in SyncList
                        where
                        !string.IsNullOrEmpty(x.Text)
                        orderby
                        x.Priority,
                        x.Seq
                        select
                        x;

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
                finally
                {
                    SyncList.Clear();
                    (sender as System.Timers.Timer)?.Stop();
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
