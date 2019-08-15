using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using ACT.UltraScouter.Config;
using ACT.UltraScouter.Models;
using ACT.UltraScouter.ViewModels.Bases;
using ACT.UltraScouter.Views;
using FFXIV.Framework.Common;
using FFXIV.Framework.Extensions;
using FFXIV.Framework.XIVHelper;

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
            this.Model.RestartTickerCallback = () => WPFHelper.InvokeAsync(
                this.BeginAnimation,
                DispatcherPriority.Normal);

            this.Config.PropertyChanged += (_, e) =>
            {
                switch (e.PropertyName)
                {
                    case nameof(this.Config.Visible):
                        if (this.Config.Visible)
                        {
                            this.Model.StartSync();
                        }
                        else
                        {
                            this.Model.StopSync();
                        }
                        break;

                    case nameof(this.Config.TestMode):
                        if (this.Config.TestMode)
                        {
                            this.BeginAnimation();
                        }
                        break;
                }
            };

            if (this.Config.Visible)
            {
                this.Model.StartSync();
            }
        }

        private async void BeginAnimation()
        {
            await Task.Delay(TimeSpan.FromSeconds(Settings.Instance.MPTicker.Offset));

            this.IsVisibleSyncIndicator = true;

            (this.View as MPTickerView)?.BeginAnimation();

            await Task.Run(async () =>
            {
                await Task.Delay(1200);
                await WPFHelper.InvokeAsync(() => this.IsVisibleSyncIndicator = false);
            });
        }

        public virtual Settings RootConfig => Settings.Instance;

        public virtual MPTicker Config => Settings.Instance.MPTicker;

        public virtual TickerModel Model => TickerModel.Instance;

        private volatile bool previousCombatStat = false;
        private DateTime endCombatTimestamp = DateTime.MinValue;

        public bool OverlayVisible
        {
            get
            {
                if (!this.Config.Visible)
                {
                    return false;
                }

                if (this.Config.TestMode)
                {
                    return true;
                }

                var inCombat = XIVPluginHelper.Instance.InCombat;
                if (inCombat)
                {
                    this.previousCombatStat = inCombat;
                    return true;
                }

                if (this.Config.ExplationTimeForDisplay > 0)
                {
                    if (this.previousCombatStat != inCombat)
                    {
                        this.previousCombatStat = inCombat;
                        this.endCombatTimestamp = DateTime.Now;
                    }

                    if ((DateTime.Now - this.endCombatTimestamp).TotalSeconds >=
                        this.Config.ExplationTimeForDisplay)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        private bool isVisibleSyncIndicator = WPFHelper.IsDesignMode;

        public bool IsVisibleSyncIndicator
        {
            get => this.isVisibleSyncIndicator;
            set => this.SetProperty(ref this.isVisibleSyncIndicator, value);
        }
    }

    namespace MPTickerConverters
    {
        public static class BrushContainer
        {
            private static readonly Dictionary<Color, SolidColorBrush> BrushDictionary = new Dictionary<Color, SolidColorBrush>(8);

            public static SolidColorBrush GetBrush(
                Color color)
            {
                var brush = default(SolidColorBrush);

                if (BrushDictionary.ContainsKey(color))
                {
                    brush = BrushDictionary[color];
                }
                else
                {
                    brush = new SolidColorBrush(color);
                    brush.Freeze();
                    BrushDictionary[color] = brush;
                }

                return brush;
            }
        }

        public class BarForeColorConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (!(value is double))
                {
                    return null;
                }

                var counter = (double)value;
                return BrushContainer.GetBrush(
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
                return BrushContainer.GetBrush(
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
                return BrushContainer.GetBrush(
                    Settings.Instance.MPTicker.ProgressBar.LinkOutlineColor ?
                    baseColor :
                    Settings.Instance.MPTicker.ProgressBar.OutlineColor);
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
        }
    }
}
