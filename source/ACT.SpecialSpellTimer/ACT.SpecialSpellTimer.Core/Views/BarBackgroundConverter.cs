using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ACT.SpecialSpellTimer.Config;
using FFXIV.Framework.Extensions;

namespace ACT.SpecialSpellTimer.Views
{
    public class BarBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is SolidColorBrush))
            {
                return null;
            }

            var baseBrush = value as SolidColorBrush;
            var color = default(Color);

            if (Settings.Default.BarBackgroundFixed)
            {
                color = Settings.Default.BarDefaultBackgroundColor;
            }
            else
            {
                color = baseBrush.Color.ChangeBrightness(Settings.Default.BarBackgroundBrightness);
            }

            return new SolidColorBrush(color);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
    }
}
