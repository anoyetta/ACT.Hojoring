using System;
using System.IO;
using System.Runtime.InteropServices;
using FFXIV.Framework.Common;
using NLog;

namespace FFXIV.Framework.TTS.Server.Models
{
    public class YukkuriModel
    {
        #region Singleton

        private static YukkuriModel instance = new YukkuriModel();
        public static YukkuriModel Instance => instance;

        #endregion Singleton

        #region Logger

        private Logger logger = AppLog.DefaultLogger;

        #endregion Logger

        private const string YukkuriLibName = "AquesTalk";
        private static readonly string YukkuriDllName = $"{YukkuriLibName}.dll";

        private AquesTalk_FreeWave FreeWaveDelegate;
        private AquesTalk_Synthe SynthesizeDelegate;
        private UnmanagedLibrary yukkuriLib;

        private delegate void AquesTalk_FreeWave(IntPtr wave);

        private delegate IntPtr AquesTalk_Synthe(string koe, ushort iSpeed, ref uint size);

        public void Free()
        {
            if (this.yukkuriLib != null)
            {
                this.yukkuriLib.Dispose();
                this.yukkuriLib = null;
            }
        }

        public void Load()
        {
            if (!NetiveMethods.IsModuleLoaded(YukkuriLibName))
            {
                this.yukkuriLib = new UnmanagedLibrary(YukkuriDllName);
            }

            if (this.yukkuriLib != null)
            {
                if (this.SynthesizeDelegate == null)
                {
                    this.SynthesizeDelegate =
                        this.yukkuriLib.GetUnmanagedFunction<AquesTalk_Synthe>(nameof(AquesTalk_Synthe));
                }

                if (this.FreeWaveDelegate == null)
                {
                    this.FreeWaveDelegate =
                        this.yukkuriLib.GetUnmanagedFunction<AquesTalk_FreeWave>(nameof(AquesTalk_FreeWave));
                }
            }
        }

        public void TextToWave(
            string textToSpeak,
            string waveFileName,
            int speed)
        {
            if (string.IsNullOrWhiteSpace(textToSpeak))
            {
                return;
            }

            if (this.SynthesizeDelegate == null ||
                this.FreeWaveDelegate == null)
            {
                return;
            }

            var wavePtr = IntPtr.Zero;

            try
            {
                // テキストを音声データに変換する
                uint size = 0;
                wavePtr = this.SynthesizeDelegate.Invoke(
                    textToSpeak,
                    (ushort)speed,
                    ref size);

                if (wavePtr == IntPtr.Zero ||
                    size <= 0)
                {
                    return;
                }

                FileHelper.CreateDirectory(waveFileName);

                // 生成したwaveデータを読み出す
                var buff = new byte[size];
                Marshal.Copy(wavePtr, buff, 0, (int)size);
                using (var fs = new FileStream(waveFileName, FileMode.Create, FileAccess.Write))
                {
                    fs.Write(buff, 0, buff.Length);
                }
            }
            finally
            {
                if (wavePtr != IntPtr.Zero)
                {
                    this.FreeWaveDelegate.Invoke(wavePtr);
                }
            }
        }

        #region Native Methods

        private static class NetiveMethods
        {
            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern IntPtr GetModuleHandle(string moduleName);

            /// <summary>
            /// Check whether or not the specified module is loaded in the
            /// current process.
            /// </summary>
            /// <param name="moduleName">the module name</param>
            /// <returns>
            /// The function returns true if the specified module is loaded in
            /// the current process. If the module is not loaded, the function
            /// returns false.
            /// </returns>
            public static bool IsModuleLoaded(string moduleName)
            {
                // Get the module in the process according to the module name.
                IntPtr hMod = GetModuleHandle(moduleName);
                return (hMod != IntPtr.Zero);
            }
        }

        #endregion Native Methods
    }
}
