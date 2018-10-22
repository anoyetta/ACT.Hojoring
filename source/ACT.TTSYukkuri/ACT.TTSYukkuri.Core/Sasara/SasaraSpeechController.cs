using System;
using System.IO;
using ACT.TTSYukkuri.Config;
using FFXIV.Framework.TTS.Common;

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
            Settings.Default.SasaraSettings.SetToRemote();
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
                Settings.Default.SasaraSettings.ToString());

            lock (this)
            {
                if (!File.Exists(wave))
                {
                    // 音声waveファイルを生成する
                    RemoteTTSClient.Instance.TTSModel.TextToWave(
                        TTSTypes.CeVIO,
                        text,
                        wave,
                        0,
                        Settings.Default.SasaraSettings.Gain);
                }
            }

            // 再生する
            SoundPlayerWrapper.Play(wave, playDevice, isSync, volume);
        }
    }
}
