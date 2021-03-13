using System;
using System.IO;
using ACT.TTSYukkuri.Config;
using FFXIV.Framework.Bridge;
using FFXIV.Framework.Common;
using NAudio.Wave;

namespace ACT.TTSYukkuri.Sasara
{
    /// <summary>
    /// CeVIO AIスピーチコントローラ
    /// </summary>
    public class CevioAISpeechController :
        ISpeechController
    {
        private CevioAIConfig config => Settings.Default.CevioAISettings;

        /// <summary>
        /// 初期化する
        /// </summary>
        public void Initialize()
        {
            this.config.LoadRemoteConfig();
            this.config.ApplyToCevio();
        }

        public void Free()
        {
            this.config.KillCevio();
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
                this.config.ToString());

            this.CreateWaveWrapper(wave, () =>
            {
                this.config.ApplyToCevio();

                this.TextToWave(
                    text,
                    wave,
                    this.config.Gain);
            });

            // 再生する
            SoundPlayerWrapper.Play(wave, playDevice, isSync, volume);
        }

        private void TextToWave(
            string tts,
            string waveFileName,
            float gain)
        {
            if (string.IsNullOrEmpty(tts))
            {
                return;
            }

            if (!this.config.IsCevioReady)
            {
                return;
            }

            if (string.IsNullOrEmpty(this.config.Cast))
            {
                return;
            }

            var tempWave = Path.GetTempFileName();

            try
            {
                var result = this.config.Talker.OutputWaveToFile(
                    tts,
                    tempWave);

                if (!result)
                {
                    return;
                }

                FileHelper.CreateDirectory(waveFileName);

                if (gain != 1.0f)
                {
                    using (var reader = new WaveFileReader(tempWave))
                    {
                        WaveFileWriter.CreateWaveFile(
                            waveFileName,
                            new VolumeWaveProvider16(reader)
                            {
                                Volume = gain
                            });
                    }
                }
                else
                {
                    File.Move(tempWave, waveFileName);
                }
            }
            finally
            {
                if (File.Exists(tempWave))
                {
                    File.Delete(tempWave);
                }
            }
        }
    }
}
