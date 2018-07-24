using System;

namespace ACT.SpecialSpellTimer.Utility
{
    /// <summary>
    /// ログ
    /// </summary>
    public static class Logger
    {
        private static NLog.Logger AppLogger => FFXIV.Framework.Common.AppLog.DefaultLogger;

        public static void Init()
        {
        }

        public static void DeInit()
        {
        }

        /// <summary>
        /// ログを書き込む
        /// </summary>
        /// <param name="text">書き込む内容</param>
        public static void Write(string text)
        {
            AppLogger.Trace(text);
        }

        /// <summary>
        /// ログを書き込む
        /// </summary>
        /// <param name="text">書き込む内容</param>
        /// <param name="ex">例外情報</param>
        public static void Write(string text, Exception ex)
        {
            AppLogger.Error(ex, text);
        }
    }
}
