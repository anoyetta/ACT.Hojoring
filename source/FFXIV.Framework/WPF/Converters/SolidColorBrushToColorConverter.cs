using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace FFXIV.Framework.WPF.Converters
{
    public class SolidColorBrushToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is SolidColorBrush))
            {
                return null;
            }

            var brush = (SolidColorBrush)value;
            return brush.Color;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
    }
}
