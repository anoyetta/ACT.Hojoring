using System;
using System.IO;
using ACT.TTSYukkuri.Config;
using FFXIV.Framework.Common;
using VoiceTextWebAPI.Client;

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
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            // 現在の条件をハッシュ化してWAVEファイル名を作る
            var wave = this.GetCacheFileName(
                Settings.Default.TTS,
                text.Replace(Environment.NewLine, "+"),
                Settings.Default.HOYASettings.ToString());

            lock (this)
            {
                if (!File.Exists(wave))
                {
                    if (string.IsNullOrWhiteSpace(
                        Settings.Default.HOYASettings.APIKey))
                    {
                        return;
                    }

                    this.CreateWave(
                        text,
                        wave);
                }
            }

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
            string wave)
        {
            var client = new VoiceTextClient()
            {
                APIKey = Settings.Default.HOYASettings.APIKey,
                Speaker = Settings.Default.HOYASettings.Speaker,
                Emotion = Settings.Default.HOYASettings.Emotion,
                EmotionLevel = Settings.Default.HOYASettings.EmotionLevel,
                Volume = Settings.Default.HOYASettings.Volume,
                Speed = Settings.Default.HOYASettings.Speed,
                Pitch = Settings.Default.HOYASettings.Pitch,
                Format = Format.WAV,
            };

            // TLSプロトコルを設定する
            EnvironmentHelper.SetTLSProtocol();

            var waveData = client.GetVoice(textToSpeak);

            File.WriteAllBytes(wave, waveData);
        }
    }
}
