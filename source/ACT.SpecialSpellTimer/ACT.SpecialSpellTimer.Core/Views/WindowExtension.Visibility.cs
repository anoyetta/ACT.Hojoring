using System.Collections.Concurrent;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ACT.SpecialSpellTimer.Views
{
    /// <summary>
    /// Windowの拡張メソッド
    /// </summary>
    public static partial class WindowExtension
    {
        /// <summary>
        /// Brush辞書
        /// </summary>
        private readonly static ConcurrentDictionary<string, SolidColorBrush> brushDictionary = new ConcurrentDictionary<string, SolidColorBrush>();

        /// <summary>
        /// Brushを取得する
        /// </summary>
        /// <param name="x">Window</param>
        /// <param name="color">Brushの色</param>
        /// <returns>Brush</returns>
        public static SolidColorBrush GetBrush(
            this Window x,
            Color color)
            => GetBrush(color);

        /// <summary>
        /// Brushを取得する
        /// </summary>
        /// <param name="x">UserControl</param>
        /// <param name="color">Brushの色</param>
        /// <returns>Brush</returns>
        public static SolidColorBrush GetBrush(
            this UserControl x,
            Color color)
            => GetBrush(color);

        /// <summary>
        /// Brushを取得する
        /// </summary>
        /// <param name="color">Brushの色</param>
        /// <returns>Brush</returns>
        private static SolidColorBrush GetBrush(
            Color color)
        {
            if (!brushDictionary.ContainsKey(color.ToString()))
            {
                var brush = new SolidColorBrush(color);
                brush.Freeze();
                brushDictionary[color.ToString()] = brush;
                return brush;
            }

            return brushDictionary[color.ToString()];
        }
    }
}
