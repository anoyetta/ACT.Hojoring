using System;
using System.IO;
using ACT.TTSYukkuri.Config;
using FFXIV.Framework.Common;
using VoiceTextWebAPI.Client;
using FFXIV.Framework.Bridge;

namespace ACT.TTSYukkuri.HOYA
{
    public class HOYASpeechController :
        ISpeechController
    {
        /// <summary>
        /// 初期化する
        /// </summary>
        public void Initialize()
        {
        }

        public void Free()
        {
        }

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

            HOYAConfig config;
            switch (voicePalette)
            {
                case VoicePalettes.Default:
                    config = Settings.Default.HOYASettings;
                    break;
                case VoicePalettes.Ext1:
                    config = Settings.Default.HOYASettingsExt1;
                    break;
                case VoicePalettes.Ext2:
                    config = Settings.Default.HOYASettingsExt2;
                    break;
                default:
                    config = Settings.Default.HOYASettings;
                    break;
            }

            // 現在の条件をハッシュ化してWAVEファイル名を作る
            var wave = this.GetCacheFileName(
                Settings.Default.TTS,
                text.Replace(Environment.NewLine, "+"),
                config.ToString());

            this.CreateWaveWrapper(wave, () =>
            {
                if (string.IsNullOrWhiteSpace(
                    Settings.Default.HOYASettings.APIKey))
                {
                    return;
                }

                this.CreateWave(
                    text,
                    config,
                    wave);
            });

            // 再生する
            SoundPlayerWrapper.Play(wave, playDevice, isSync, volume);
        }

        /// <summary>
        /// WAVEファイルを生成する
        /// </summary>
        /// <param name="textToSpeak">
        /// Text to Speak</param>
        /// <param name="wave">
        /// WAVEファイルのパス</param>
        private void CreateWave(
            string textToSpeak,
            HOYAConfig config,
            string wave)
        {
            var client = new VoiceTextClient()
            {
                APIKey = config.APIKey,
                Speaker = config.Speaker,
                Emotion = config.Emotion,
                EmotionLevel = config.EmotionLevel,
                Volume = config.Volume,
                Speed = config.Speed,
                Pitch = config.Pitch,
                Format = Format.WAV,
            };

            // TLSプロトコルを設定する
            EnvironmentHelper.SetTLSProtocol();

            var waveData = client.GetVoice(textToSpeak);

            File.WriteAllBytes(wave, waveData);
        }
    }
}
