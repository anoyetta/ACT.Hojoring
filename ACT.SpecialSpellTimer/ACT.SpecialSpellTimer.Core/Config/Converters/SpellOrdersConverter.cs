using System;
using System.Globalization;
using System.Windows.Data;
using ACT.SpecialSpellTimer.Models;

namespace ACT.SpecialSpellTimer.Config.Converters
{
    public class SpellOrdersConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SpellOrders s)
            {
                return (int)s;
            }
            else
            {
                return value;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int i)
            {
                return (SpellOrders)i;
            }
            else
            {
                return value;
            }
        }
    }
}
