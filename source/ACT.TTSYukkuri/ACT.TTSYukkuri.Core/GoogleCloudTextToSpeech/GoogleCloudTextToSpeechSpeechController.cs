using ACT.TTSYukkuri.Config;
using FFXIV.Framework.Bridge;
using FFXIV.Framework.Common;
using Google.Cloud.TextToSpeech.V1;
using System;
using System.IO;
using System.Reflection;

namespace ACT.TTSYukkuri.GoogleCloudTextToSpeech
{
    /// <summary>
    /// Google Cloud Text-to-Speechコントローラ
    /// </summary>
    public class GoogleCloudTextToSpeechSpeechController :
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
            GoogleCloudTextToSpeechConfig config;
            switch (voicePalette)
            {
                case VoicePalettes.Default:
                    config = Settings.Default.GoogleCloudTextToSpeechSettings;
                    break;
                case VoicePalettes.Ext1:
                    config = Settings.Default.GoogleCloudTextToSpeechSettingsExt1;
                    break;
                case VoicePalettes.Ext2:
                    config = Settings.Default.GoogleCloudTextToSpeechSettingsExt2;
                    break;
                case VoicePalettes.Ext3:
                    config = Settings.Default.GoogleCloudTextToSpeechSettingsExt3;
                    break;
                default:
                    config = Settings.Default.GoogleCloudTextToSpeechSettings;
                    break;
            }

            var client = GoogleCloudTextToSpeechConfig.TTSClient;
            if (client == null)
            {
                return;
            }

            // 現在の条件をハッシュ化してWAVEファイル名を作る
            var wave = this.GetCacheFileName(
                    Settings.Default.TTS,
                    text.Replace(Environment.NewLine, "+"),
                    config.ToString());

            this.CreateWaveWrapper(wave, () =>
            {
                // 合成する音声のパラメーターを設定する
                SynthesisInput input = new SynthesisInput
                {
                    Text = text
                };

                VoiceSelectionParams voice = new VoiceSelectionParams
                {
                    LanguageCode = config.LanguageCode,
                    Name = config.Name,
                };

                AudioConfig audioconfig = new AudioConfig
                {
                    AudioEncoding = AudioEncoding.Linear16,
                    VolumeGainDb = config.VolumeGainDb,
                    Pitch = config.Pitch,
                    SpeakingRate = config.SpeakingRate,
                    SampleRateHertz = config.SampleRateHertz,
                };

                // 音声合成リクエストを送信する
                var response = client.SynthesizeSpeech(new SynthesizeSpeechRequest
                {
                    Input = input,
                    Voice = voice,
                    AudioConfig = audioconfig
                });

                // 合成した音声をファイルに書き出す
                using (Stream output = File.Create(wave))
                {
                    response.AudioContent.WriteTo(output);
                }
            });

            // 再生する
            SoundPlayerWrapper.Play(wave, playDevice, isSync, volume);
        }

        public static void SetupLibrary()
        {
            /*
            if (string.IsNullOrEmpty(PluginCore.Instance?.PluginDirectory))
            {
                return;
            }

            var entryDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var libDirectory = DirectoryHelper.FindSubDirectory("bin", "lib");

            var libs = new (string dst, string src)[]
            {
                (Path.Combine(entryDirectory, "grpc_csharp_ext.x64.dll"), Path.Combine(libDirectory, "grpc_csharp_ext.x64.dll")),
                (Path.Combine(entryDirectory, "grpc_csharp_ext.x86.dll"), Path.Combine(libDirectory, "grpc_csharp_ext.x86.dll")),
            };

            foreach (var lib in libs)
            {
                if (!File.Exists(lib.src))
                {
                    continue;
                }

                if (!File.Exists(lib.dst) ||
                    !Crypter.IsMatchHash(lib.src, lib.dst))
                {
                    File.Copy(lib.src, lib.dst, true);
                }
            }
            */
        }
    }
}
