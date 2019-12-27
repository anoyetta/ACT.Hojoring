using System;
using System.IO;
using ACT.TTSYukkuri.Config;
using FFXIV.Framework.Bridge;
using FFXIV.Framework.Common;
using NAudio.Wave;

namespace ACT.TTSYukkuri.Sasara
{
    /// <summary>
    /// さとうささらスピーチコントローラ
    /// </summary>
    public class SasaraSpeechController :
        ISpeechController
    {
        /// <summary>
        /// 初期化する
        /// </summary>
        public void Initialize()
        {
            Settings.Default.SasaraSettings.ApplyToCevio();
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
                Settings.Default.SasaraSettings.ToString());

            this.CreateWaveWrapper(wave, () =>
            {
                Settings.Default.SasaraSettings.ApplyToCevio();

                this.TextToWave(
                    text,
                    wave,
                    Settings.Default.SasaraSettings.Gain);
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

            if (!Settings.Default.SasaraSettings.IsCevioReady)
            {
                return;
            }

            if (string.IsNullOrEmpty(Settings.Default.SasaraSettings.Cast))
            {
                return;
            }

            var tempWave = Path.GetTempFileName();

            try
            {
                var result = Settings.Default.SasaraSettings.Talker.OutputWaveToFile(
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
