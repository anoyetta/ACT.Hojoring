using ACT.TTSYukkuri.Config;
using Google.Cloud.TextToSpeech.V1;
using System;
using System.IO;

namespace ACT.TTSYukkuri.GoogleCloudTextToSpeech
{
    /// <summary>
    /// Google Cloud Text-to-Speechコントローラ
    /// </summary>
    public class GoogleCloudTextToSpeechSpeechController :
        ISpeechController
    {
        private TextToSpeechClient client;

        /// <summary>
        /// 初期化する
        /// </summary>
        public void Initialize()
        {
            this.client = TextToSpeechClient.Create();
        }

        public void Free()
        {
            this.client = null;
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
                Settings.Default.GoogleCloudTextToSpeechSettings.ToString());

            // Double-checked locking
            if (!File.Exists(wave))
            {
                lock (this)
                {
                    if (!File.Exists(wave))
                    {
                        // 合成する音声のパラメーターを設定する
                        SynthesisInput input = new SynthesisInput
                        {
                            Text = text
                        };

                        VoiceSelectionParams voice = new VoiceSelectionParams
                        {
                            LanguageCode = Settings.Default.GoogleCloudTextToSpeechSettings.LanguageCode,
                            Name = Settings.Default.GoogleCloudTextToSpeechSettings.Name,
                        };

                        AudioConfig config = new AudioConfig
                        {
                            AudioEncoding = AudioEncoding.Linear16,
                            VolumeGainDb = Settings.Default.GoogleCloudTextToSpeechSettings.VolumeGainDb,
                            Pitch = Settings.Default.GoogleCloudTextToSpeechSettings.Pitch,
                            SpeakingRate = Settings.Default.GoogleCloudTextToSpeechSettings.SpeakingRate,
                            SampleRateHertz = Settings.Default.GoogleCloudTextToSpeechSettings.SampleRateHertz,
                        };

                        // 音声合成リクエストを送信する
                        var response = client.SynthesizeSpeech(new SynthesizeSpeechRequest
                        {
                            Input = input,
                            Voice = voice,
                            AudioConfig = config
                        });

                        // 合成した音声をファイルに書き出す
                        using (Stream output = File.Create(wave))
                        {
                            response.AudioContent.WriteTo(output);
                        }
                    }
                }
            }

            // 再生する
            SoundPlayerWrapper.Play(wave, playDevice, isSync, volume);
        }
    }
}
