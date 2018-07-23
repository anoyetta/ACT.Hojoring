using System;
using System.Globalization;
using System.Windows.Data;
using ACT.UltraScouter.Config;

namespace ACT.UltraScouter.Views.Converters
{
    public class StrokeThicknessToBlurRadiusConverter :
        IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
            {
                // アウトラインの太さを基準にして増幅する
                // 増幅率は一応設定可能とする
                return d * Settings.Instance.TextBlurGain;
            }

            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
    }
}
