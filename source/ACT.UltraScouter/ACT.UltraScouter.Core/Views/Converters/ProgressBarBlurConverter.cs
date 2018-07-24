using System;
using System.Globalization;
using System.Windows.Data;

namespace ACT.UltraScouter.Views.Converters
{
    public class ProgressBarBlurConverter :
        IValueConverter
    {
        private const double ProgressBarBlurDefault = 10;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                return b ? ProgressBarBlurDefault : 0;
            }

            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
    }
}
