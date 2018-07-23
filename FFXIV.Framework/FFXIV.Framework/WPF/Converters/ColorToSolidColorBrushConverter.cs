using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace FFXIV.Framework.WPF.Converters
{
    public class ColorToSolidColorBrushConverter :
        IValueConverter
    {
        private static Dictionary<Color, SolidColorBrush> brushDictionary =
            new Dictionary<Color, SolidColorBrush>();

        public static SolidColorBrush GetBrush(
            Color color)
        {
            if (!ColorToSolidColorBrushConverter.brushDictionary.ContainsKey(color))
            {
                var brush = new SolidColorBrush(color);
                ColorToSolidColorBrushConverter.brushDictionary.Add(
                    color,
                    brush);
            }

            return ColorToSolidColorBrushConverter.brushDictionary[color];
        }

        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            if (!(value is Color))
            {
                return null;
            }

            var color = (Color)value;

            return ColorToSolidColorBrushConverter.GetBrush(color);
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            return (value as SolidColorBrush)?.Color;
        }
    }
}
