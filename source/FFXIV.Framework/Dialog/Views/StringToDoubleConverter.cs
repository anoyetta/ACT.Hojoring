using System;
using System.Globalization;
using System.Windows.Data;

namespace FFXIV.Framework.Dialog.Views
{
    public class StringToDoubleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double d;
            double.TryParse((string)value, out d);
            return d;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter == null)
            {
                return ((double)value).ToString();
            }
            else
            {
                return ((double)value).ToString((string)parameter);
            }
        }
    }
}
