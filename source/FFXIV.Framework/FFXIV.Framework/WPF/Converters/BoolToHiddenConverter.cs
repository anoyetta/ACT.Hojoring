using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FFXIV.Framework.WPF.Converters
{
    public class BoolToHiddenConverter :
        IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                return b ? Visibility.Visible : Visibility.Hidden;
            }

            return Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
    }
}
