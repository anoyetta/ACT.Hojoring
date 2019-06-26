using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ACT.UltraScouter.Views.Converters
{
    internal class ToRightMarginConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var margin = new Thickness();

            if (value is double d)
            {
                margin.Right = d;

                if (parameter != null)
                {
                    if (parameter is double p ||
                        double.TryParse(parameter.ToString(), out p))
                    {
                        margin.Right += p;
                    }
                }
            }

            return margin;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => null;
    }
}
