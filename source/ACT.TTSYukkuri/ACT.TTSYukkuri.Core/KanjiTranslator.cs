using FFXIV.Framework.Common;
using NLog;
using System;
using System.Runtime.InteropServices;

namespace ACT.TTSYukkuri
{
    /// <summary>
    /// 漢字翻訳
    /// </summary>
    public class KanjiTranslator : IDisposable
    {
        #region Logger

        private Logger Logger => AppLog.DefaultLogger;

        #endregion Logger

        /// <summary>
        /// ロックオブジェクト
        /// </summary>
        private static object lockObject = new object();

        /// <summary>
        /// シングルトンinstance
        /// </summary>
        private static KanjiTranslator instance;

        /// <summary>
        /// シングルトンinstance
        /// </summary>
        public static KanjiTranslator Default
        {
            get
            {
                lock (lockObject)
                {
                    if (instance == null)
                    {
                        instance = new KanjiTranslator();
                    }
                }

                return instance;
            }
        }

        /// <summary>
        /// IFE言語オブジェクト
        /// </summary>
        public IFELanguage IFELang
        {
            get;
            private set;
        }

        /// <summary>
        /// 初期化する
        /// </summary>
        public void Initialize()
        {
            lock (lockObject)
            {
                try
                {
                    if (this.IFELang == null)
                    {
                        this.IFELang = Activator.CreateInstance(Type.GetTypeFromProgID("MSIME.Japan")) as IFELanguage;

                        if (this.IFELang == null)
                        {
                            this.Logger.Warn("IFELANG IME initialize failed. Disabled IME reverse translation.");
                        }
                        else
                        {
                            var hr = this.IFELang.Open();
                            if (hr != 0)
                            {
                                this.Logger.Warn("IFELANG IME connection failed. Disabled IME reverse translation.");
                                this.IFELang = null;
                            }

                            this.Logger.Trace("IFELANG IME Connected.");
                        }
                    }
                }
                catch (Exception)
                {
                    this.Logger.Warn("IFELANG IME initialize failed due to an unexpected exception. Disabled IME reverse translation.");
                    this.IFELang = null;
                }
            }
        }

        private volatile bool hasWarned;

        /// <summary>
        /// 読みがなを取得する
        /// </summary>
        /// <param name="text">変換対象のテキスト</param>
        /// <returns>読みがなに変換したテキスト</returns>
        public string GetPhonetic(
            string text)
        {
            var yomigana = text;

            var ifelang = this.IFELang;

            if (ifelang != null)
            {
                string t;
                var hr = ifelang.GetPhonetic(text, 1, -1, out t);
                if (hr != 0)
                {
                    this.Logger.Error($"IFELANG IME translate to phonetic faild. text={text}");
                    return yomigana;
                }

                yomigana = t;
            }
            else
            {
                if (!this.hasWarned)
                {
                    this.hasWarned = true;
                    this.Logger.Warn($"IFELANG IME has been disabled. text={text}");
                }
            }

            return yomigana;
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            if (this.IFELang != null)
            {
                this.IFELang.Close();
                this.IFELang = null;
            }
        }
    }

    /// <summary>
    /// IFELanguage Interface
    /// </summary>
    [ComImport]
    [Guid("019F7152-E6DB-11d0-83C3-00C04FDDB82E")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IFELanguage
    {
        int Open();

        int Close();

        int GetJMorphResult(uint dwRequest, uint dwCMode, int cwchInput, [MarshalAs(UnmanagedType.LPWStr)] string pwchInput, IntPtr pfCInfo, out object ppResult);

        int GetConversionModeCaps(ref uint pdwCaps);

        int GetPhonetic([MarshalAs(UnmanagedType.BStr)] string @string, int start, int length, [MarshalAs(UnmanagedType.BStr)] out string result);

        int GetConversion([MarshalAs(UnmanagedType.BStr)] string @string, int start, int length, [MarshalAs(UnmanagedType.BStr)] out string result);
    }
}
