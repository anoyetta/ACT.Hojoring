using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using FFXIV.Framework.Extensions;

namespace ACT.SpecialSpellTimer.Views
{
    public class ColorBrightnessConverter : IValueConverter
    {
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

            var color = (Color)value;
            if (double.TryParse((string)parameter, out double brightness))
            {
                return color.ChangeBrightness(brightness);
            }

            return color;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class BrushBrightnessConverter : IValueConverter
    {
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

            var brush = (SolidColorBrush)value;
            if (double.TryParse((string)parameter, out double brightness))
            {
                return new SolidColorBrush(brush.Color.ChangeBrightness(brightness));
            }

            return brush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
