using System;
using System.Globalization;
using System.Windows.Data;

namespace ACT.TTSYukkuri.Discord.Converters
{
    public class BoolConverter :
        IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                return !b;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                return !b;
            }

            return null;
        }
    }
}
