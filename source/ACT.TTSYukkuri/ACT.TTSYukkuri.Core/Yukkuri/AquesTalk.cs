using FFXIV.Framework.Common;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace ACT.TTSYukkuri.Yukkuri
{
    public unsafe class AquesTalk
    {
        #region Singleton

        private static AquesTalk instance = new AquesTalk();
        public static AquesTalk Instance => instance;

        #endregion Singleton

        public static string YukkuriDirectory => new[]
        {
            DirectoryHelper.FindSubDirectory("bin", "Yukkuri"),
            Path.Combine(PluginCore.Instance.PluginDirectory, "bin", "Yukkuri"),
            Path.Combine(PluginCore.Instance.PluginDirectory, "Yukkuri"),
        }.FirstOrDefault(x => Directory.Exists(x));

        public static string UserDictionaryEditor => Path.Combine(
            YukkuriDirectory,
            $@"aq_dic\GenUserDic.exe");

        private Synthe SyntheDelegate;
        private FreeWave FreeWaveDelegate;

        private delegate IntPtr Synthe(
            ref AQTK_VOICE voice,
            [MarshalAs(UnmanagedType.LPArray)] byte[] text,
            ref int waveSize);

        private delegate void FreeWave(IntPtr wave);

        public void Load()
        {
            this.SyntheDelegate ??= YukkuriDriver.Synthe;
            this.FreeWaveDelegate ??= YukkuriDriver.FreeWave;
        }

        public bool IsLoadedAppKey { get; private set; } = true;

        public void Free()
        {
            this.IsLoadedAppKey = false;

            this.SyntheDelegate = null;
            this.FreeWaveDelegate = null;
        }

        /// <summary>
        /// アプリケーションキーをセットする
        /// </summary>
        /// <returns>
        /// status</returns>
        public bool SetAppKey() => true;

        private readonly Encoding UTF16Encoding = Encoding.GetEncoding("UTF-16");

        public void TextToWave(
            string textToSpeak,
            string waveFileName,
            AQTK_VOICE voice)
        {
            if (string.IsNullOrWhiteSpace(textToSpeak))
            {
                return;
            }

            if (this.SyntheDelegate == null ||
                this.FreeWaveDelegate == null)
            {
                return;
            }

            var wavePtr = IntPtr.Zero;
            var waveSize = 0;

            try
            {
                var chars = this.UTF16Encoding.GetBytes(textToSpeak);

                // テキストを音声データに変換する
                wavePtr = this.SyntheDelegate?.Invoke(
                    ref voice,
                    chars,
                    ref waveSize) ?? IntPtr.Zero;

                if (wavePtr == IntPtr.Zero ||
                    waveSize <= 0)
                {
                    return;
                }

                FileHelper.CreateDirectory(waveFileName);

                // 生成したwaveデータを読み出す
                var buff = new byte[waveSize];
                Marshal.Copy(wavePtr, buff, 0, (int)waveSize);
                using (var fs = new FileStream(waveFileName, FileMode.Create, FileAccess.Write))
                {
                    fs.Write(buff, 0, buff.Length);
                }
            }
            finally
            {
                if (wavePtr != IntPtr.Zero &&
                    waveSize != 0)
                {
                    this.FreeWaveDelegate?.Invoke(wavePtr);
                }
            }
        }
    }

    public static class YukkuriDriver
    {
        [DllImport("AquesTalkDriver.dll")]
        public extern static IntPtr Synthe(
            ref AQTK_VOICE voice,
            [MarshalAs(UnmanagedType.LPArray)] byte[] text,
            ref int waveSize);

        [DllImport("AquesTalkDriver.dll")]
        public extern static void FreeWave(IntPtr wave);
    }
}
