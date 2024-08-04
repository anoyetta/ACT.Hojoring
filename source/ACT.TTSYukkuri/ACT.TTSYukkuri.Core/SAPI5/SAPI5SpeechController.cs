using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Speech.Synthesis;
using ACT.TTSYukkuri.Config;
using FFXIV.Framework.Bridge;

namespace ACT.TTSYukkuri.SAPI5
{
    public enum Volumes
    {
        Default = 0,
        Silent = 1,
        XSoft = 2,
        Soft = 3,
        Medium = 4,
        Loud = 5,
        XLoud = 6,
    }

    public enum Rates
    {
        XSlow = 1,
        Slow = 2,
        Medium = 3,
        Fast = 4,
        XFast = 5,
    }

    public enum Pitches
    {
        Default = 0,
        XLow = 1,
        Low = 2,
        Medium = 3,
        High = 4,
        XHigh = 5,
    }

    public static class ProsodyExtensions
    {
        public static string ToXML(
            this Volumes v)
            => new[]
            {
                "default",
                "silent",
                "x-soft",
                "soft",
                "medium",
                "loud",
                "x-loud",
            }[(int)v];

        public static string ToXML(
            this Rates r)
            => new[]
            {
                string.Empty,
                "x-slow",
                "slow",
                "medium",
                "fast",
                "x-fast",
            }[(int)r];

        public static string ToXML(
            this Pitches p)
            => new[]
            {
                "default",
                "x-low",
                "low",
                "medium",
                "high",
                "x-high",
            }[(int)p];
    }

    public class SAPI5SpeechController :
        ISpeechController
    {
        private static IReadOnlyList<InstalledVoice> synthesizers;

        public static IReadOnlyList<InstalledVoice> Synthesizers =>
            synthesizers ?? (synthesizers = (new SpeechSynthesizer()).GetInstalledVoices());

        private SAPI5Configs Config => Settings.Default.SAPI5Settings;

        /// <summary>
        /// 初期化する
        /// </summary>
        public void Initialize()
        {
        }

        /// <summary>
        /// 開放する
        /// </summary>
        public void Free()
        {
        }

        private InstalledVoice GetSynthesizer(
            string id)
            => Synthesizers.FirstOrDefault(x =>
                string.Equals(x.VoiceInfo.Id, id, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// テキストを読み上げる
        /// </summary>
        /// <param name="text">読み上げるテキスト</param>
        public void Speak(
            string text,
            PlayDevices playDevice = PlayDevices.Both,
            bool isSync = false,
            float? volume = null)
            => Speak(text, playDevice, VoicePalettes.Default, isSync, volume);

        /// <summary>
        /// テキストを読み上げる
        /// </summary>
        /// <param name="text">読み上げるテキスト</param>
        public void Speak(
            string text,
            PlayDevices playDevice = PlayDevices.Both,
            VoicePalettes voicePalette = VoicePalettes.Default,
            bool isSync = false,
            float? volume = null)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            SAPI5Configs config;
            switch (voicePalette)
            {
                case VoicePalettes.Default:
                    config = Settings.Default.SAPI5Settings;
                    break;
                case VoicePalettes.Ext1:
                    config = Settings.Default.SAPI5SettingsExt1;
                    break;
                case VoicePalettes.Ext2:
                    config = Settings.Default.SAPI5SettingsExt2;
                    break;
                case VoicePalettes.Ext3:
                    config = Settings.Default.SAPI5SettingsExt3;
                    break;
                default:
                    config = Settings.Default.SAPI5Settings;
                    break;
            }

            // 現在の条件をハッシュ化してWAVEファイル名を作る
            var wave = this.GetCacheFileName(
                Settings.Default.TTS,
                text,
                config.ToString());

            this.CreateWaveWrapper(wave, () =>
            {
                using (var fs = new FileStream(wave, FileMode.Create))
                using (var synth = new SpeechSynthesizer())
                {
                    // VOICEを設定する
                    var voice = this.GetSynthesizer(config.VoiceID);
                    if (voice == null)
                    {
                        return;
                    }

                    synth.SelectVoice(voice.VoiceInfo.Name);

                    synth.Rate = this.Config.Rate;
                    synth.Volume = this.Config.Volume;

                    // Promptを生成する
                    var pb = new PromptBuilder(voice.VoiceInfo.Culture);
                    pb.StartVoice(voice.VoiceInfo);
                    pb.AppendSsmlMarkup(
                        $"<prosody pitch=\"{this.Config.Pitch.ToXML()}\">{text}</prosody>");
                    pb.EndVoice();

                    synth.SetOutputToWaveStream(fs);
                    synth.Speak(pb);
                }
            });

            // 再生する
            SoundPlayerWrapper.Play(wave, playDevice, isSync, volume);
        }
    }
}
