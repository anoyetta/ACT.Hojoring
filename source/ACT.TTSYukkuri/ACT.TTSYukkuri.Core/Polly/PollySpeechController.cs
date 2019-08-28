using System;
using System.IO;
using ACT.TTSYukkuri.Config;
using ACT.TTSYukkuri.SAPI5;
using Amazon.Polly;
using Amazon.Polly.Model;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using FFXIV.Framework.Bridge;

namespace ACT.TTSYukkuri.Polly
{
    public class PollySpeechController :
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

            // 現在の条件をハッシュ化してWAVEファイル名を作る
            var wave = this.GetCacheFileName(
                Settings.Default.TTS,
                text.Replace(Environment.NewLine, "+"),
                Settings.Default.PollySettings.ToString(),
                true);

            this.CreateWaveWrapper(wave, () =>
            {
                this.CreateWave(
                    text,
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
            string wave)
        {
            var config = Settings.Default.PollySettings;
            var endpoint = config.Endpoint;
            var chain = new CredentialProfileStoreChain();

            var hash = (config.Region + config.AccessKey + config.SecretKey).GetHashCode().ToString("X4");
            var profileName = $"polly_profile_{hash}";

            AWSCredentials awsCredentials;
            if (!chain.TryGetAWSCredentials(
                profileName,
                out awsCredentials))
            {
                var options = new CredentialProfileOptions
                {
                    AccessKey = config.AccessKey,
                    SecretKey = config.SecretKey,
                };

                var profile = new CredentialProfile(profileName, options);
                profile.Region = endpoint;

                chain.RegisterProfile(profile);

                chain.TryGetAWSCredentials(
                    profileName,
                    out awsCredentials);
            }

            if (awsCredentials == null)
            {
                return;
            }

            using (var pc = new AmazonPollyClient(
                awsCredentials,
                endpoint))
            {
                var ssml =
                    $@"<speak><prosody volume=""{config.Volume.ToXML()}"" rate=""{config.Rate.ToXML()}"" pitch=""{config.Pitch.ToXML()}"">{textToSpeak}</prosody></speak>";

                var req = new SynthesizeSpeechRequest();
                req.TextType = TextType.Ssml;
                req.Text = ssml;
                req.OutputFormat = OutputFormat.Mp3;
                req.VoiceId = config.Voice;

                var res = pc.SynthesizeSpeech(req);

                using (var fs = new FileStream(wave, FileMode.Create, FileAccess.Write))
                {
                    res.AudioStream.CopyTo(fs);
                    fs.Flush();
                    fs.Close();
                }
            }
        }
    }
}
