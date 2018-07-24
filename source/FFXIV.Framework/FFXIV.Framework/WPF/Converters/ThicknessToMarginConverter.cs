using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FFXIV.Framework.WPF.Converters
{
    public class ThicknessToMarginConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double v)
            {
                return new Thickness(v / 2, v / 2, 0, 0);
            }

            return new Thickness();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
    }
}
