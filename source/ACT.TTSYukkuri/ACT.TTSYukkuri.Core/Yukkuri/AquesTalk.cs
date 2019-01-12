using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using FFXIV.Framework.Common;

namespace ACT.TTSYukkuri.Yukkuri
{
    public unsafe class AquesTalk
    {
        #region Singleton

        private static AquesTalk instance = new AquesTalk();
        public static AquesTalk Instance => instance;

        #endregion Singleton

        private const string YukkuriName = "AquesTalk";
        private const string YukkuriDriverLibName = "AquesTalkDriver";

        private static readonly string YukkuriDllName = Path.Combine(
            PluginCore.Instance.PluginDirectory,
            $@"Yukkuri\{YukkuriName}.dll");

        private static readonly string YukkuriDriverDllName = Path.Combine(
            PluginCore.Instance.PluginDirectory,
            $@"Yukkuri\{YukkuriDriverLibName}.dll");

        public static readonly string UserDictionaryEditor = Path.Combine(
            PluginCore.Instance.PluginDirectory,
            $@"Yukkuri\aq_dic\GenUserDic.exe");

        private UnmanagedLibrary yukkuri;
        private UnmanagedLibrary yukkuriDriver;
        private Synthe SyntheDelegate;
        private FreeWave FreeWaveDelegate;

        private delegate IntPtr Synthe(
            ref AQTK_VOICE voice,
            [MarshalAs(UnmanagedType.LPArray)] byte[] text,
            ref int waveSize);

        private delegate void FreeWave(IntPtr wave);

        public void Load()
        {
            if (this.yukkuri == null)
            {
                if (!File.Exists(YukkuriDllName))
                {
                    throw new FileNotFoundException(
                        $"{Path.GetFileName(YukkuriDllName)} が見つかりません。アプリケーションの配置を確認してください。",
                        YukkuriDllName);
                }

                this.yukkuri = new UnmanagedLibrary(YukkuriDllName);
            }

            if (this.yukkuriDriver == null)
            {
                if (!File.Exists(YukkuriDriverDllName))
                {
                    throw new FileNotFoundException(
                        $"{Path.GetFileName(YukkuriDriverDllName)} が見つかりません。アプリケーションの配置を確認してください。",
                        YukkuriDriverDllName);
                }

                this.yukkuriDriver = new UnmanagedLibrary(YukkuriDriverDllName);
            }

            if (this.yukkuriDriver == null)
            {
                return;
            }

            if (this.SyntheDelegate == null)
            {
                this.SyntheDelegate =
                    this.yukkuriDriver.GetUnmanagedFunction<Synthe>(nameof(Synthe));
            }

            if (this.FreeWaveDelegate == null)
            {
                this.FreeWaveDelegate =
                    this.yukkuriDriver.GetUnmanagedFunction<FreeWave>(nameof(FreeWave));
            }
        }

        public bool IsLoadedAppKey { get; private set; } = true;

        public void Free()
        {
            if (this.yukkuriDriver != null)
            {
                this.IsLoadedAppKey = false;

                this.SyntheDelegate = null;
                this.FreeWaveDelegate = null;

                this.yukkuriDriver.Dispose();
                this.yukkuriDriver = null;

                this.yukkuri.Dispose();
                this.yukkuri = null;
            }
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
}
