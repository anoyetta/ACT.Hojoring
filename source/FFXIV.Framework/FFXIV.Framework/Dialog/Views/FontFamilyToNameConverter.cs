using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows.Data;
using System.Windows.Media;

namespace FFXIV.Framework.Dialog.Views
{
    public class FontFamilyToNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var v = value as FontFamily;
            var cul = Thread.CurrentThread.CurrentCulture;
            return v.FamilyNames.FirstOrDefault(o =>
                o.Key.IetfLanguageTag.ToLower() == cul.IetfLanguageTag.ToLower()).Value ?? v.Source;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
