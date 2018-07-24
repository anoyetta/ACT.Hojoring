using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ACT.UltraScouter.Config;
using ACT.UltraScouter.Models;
using ACT.UltraScouter.ViewModels.Bases;
using ACT.UltraScouter.Views;
using FFXIV.Framework.Extensions;

namespace ACT.UltraScouter.ViewModels
{
    public class MPTickerViewModel :
        OverlayViewModelBase,
        IOverlayViewModel
    {
        public MPTickerViewModel()
        {
            this.Initialize();
        }

        public override void Initialize()
        {
            this.Model.MPRecovered -= this.Model_MPRecovered;
            this.Model.MPRecovered += this.Model_MPRecovered;
        }

        public override void Dispose()
        {
            this.Model.MPRecovered -= this.Model_MPRecovered;

            base.Dispose();
        }

        public virtual Settings RootConfig => Settings.Instance;
        public virtual MPTicker Config => Settings.Instance.MPTicker;
        public virtual MeInfoModel Model => MeInfoModel.Instance;

        public bool OverlayVisible => this.Config.Visible;

        /// <summary>
        /// 規定のMP回復を検知した
        /// </summary>
        /// <param name="sender">モデル</param>
        /// <param name="e">イベント引数</param>
        private void Model_MPRecovered(
            object sender,
            EventArgs e)
        {
            var view = this.View as MPTickerView;
            if (view != null)
            {
                view.BeginAnimation();
            }
        }
    }

    namespace MPTickerConverters
    {
        public class BarForeColorConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (!(value is double))
                {
                    return null;
                }

                var counter = (double)value;
                return new SolidColorBrush(
                    Settings.Instance.MPTicker.ProgressBar.AvailableColor(counter));
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
        }

        public class BarBackColorConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (!(value is SolidColorBrush))
                {
                    return null;
                }

                var baseColor = ((SolidColorBrush)value).Color;
                return new SolidColorBrush(
                    Settings.Instance.MPTicker.UseCircle ?
                    baseColor.ChangeBrightness(Settings.Instance.ProgressBarDarkRatio) :
                    baseColor.ChangeBrightness(Settings.Instance.CircleBackBrightnessRate));
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
        }

        public class BarEffectColorConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (!(value is SolidColorBrush))
                {
                    return null;
                }

                var baseColor = ((SolidColorBrush)value).Color;
                return
                    baseColor.ChangeBrightness(Settings.Instance.ProgressBarEffectRatio);
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
        }

        public class BarStrokeColorConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (!(value is SolidColorBrush))
                {
                    return null;
                }

                var baseColor = ((SolidColorBrush)value).Color;
                return new SolidColorBrush(
                    Settings.Instance.MPTicker.ProgressBar.LinkOutlineColor ?
                    baseColor :
                    Settings.Instance.MPTicker.ProgressBar.OutlineColor);
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
        }
    }
}
