using System;
using System.Globalization;
using System.Windows.Data;

namespace ACT.UltraScouter.Views.Converters
{
    public class WidthToRadiusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var v = System.Convert.ToDouble(value);
            var p = 0d;
            if (parameter != null)
            {
                p = System.Convert.ToDouble(parameter);
            }

            return (v / 2) + (p * 2);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
    }
}
