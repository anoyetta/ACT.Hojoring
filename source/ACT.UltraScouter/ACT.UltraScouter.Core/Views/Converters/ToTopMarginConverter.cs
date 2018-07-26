using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ACT.UltraScouter.Views.Converters
{
    internal class ToTopMarginConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var margin = new Thickness();

            if (value is double d)
            {
                margin.Top = d;

                if (parameter is double p ||
                    double.TryParse(parameter.ToString(), out p))
                {
                    margin.Top += p;
                }
            }

            return margin;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => null;
    }
}
