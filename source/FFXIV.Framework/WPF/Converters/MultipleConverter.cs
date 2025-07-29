using System;
using System.Globalization;
using System.Windows.Data;

namespace FFXIV.Framework.WPF.Converters
{
    public class MultipleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double v = 0, p = 1;
            double.TryParse(value?.ToString(), out v);
            double.TryParse(parameter?.ToString(), out p);
            return v * p;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
    }
}
