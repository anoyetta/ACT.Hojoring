using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ACT.SpecialSpellTimer.Config;
using FFXIV.Framework.Common;
using FFXIV.Framework.Extensions;
using FFXIV.Framework.WPF.Controls;

namespace ACT.SpecialSpellTimer.Views
{
    /// <summary>
    /// TickerControl.xaml の相互作用ロジック
    /// </summary>
    public partial class TickerControl : UserControl
    {
        public TickerControl()
        {
            this.InitializeComponent();
#if DEBUG
            if (WPFHelper.IsDesignMode)
            {
                this.Message = "戦士サンプルテロップ";
                this.Font = new FontInfo(
                    "メイリオ",
                    30.0,
                    "Normal",
                    "Bold",
                    "Normal");

                this.BarHeight = 11;
                this.FontBrush = new SolidColorBrush(Colors.Red);
                this.FontOutlineBrush = new SolidColorBrush(Colors.White);
                this.BarVisible = true;
            }
#endif
        }

        public string Message
        {
            get => this.MessageTextBlock.Text;
            set
            {
                if (this.MessageTextBlock.Text != value)
                {
                    this.MessageTextBlock.Text = value;

                    if (string.IsNullOrEmpty(this.MessageTextBlock.Text))
                    {
                        this.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        this.Visibility = Visibility.Visible;
                    }
                }
            }
        }

        public FontInfo Font
        {
            get => this.MessageTextBlock.GetFontInfo();
            set
            {
                if (this.MessageTextBlock.SetFontInfo(value))
                {
                    this.MessageTextBlock.StrokeThickness = value.OutlineThickness;
                    this.MessageTextBlock.BlurRadius = value.BlurRadius;
                }
            }
        }

        /// <summary>フォントのBrush</summary>
        public SolidColorBrush FontBrush
        {
            get => this.MessageTextBlock.Fill as SolidColorBrush;
            set
            {
                if (this.MessageTextBlock.Fill != value)
                {
                    this.MessageTextBlock.Fill = value;
                }
            }
        }

        /// <summary>フォントのアウトラインBrush</summary>
        public SolidColorBrush FontOutlineBrush
        {
            get => this.MessageTextBlock.Stroke as SolidColorBrush;
            set
            {
                if (this.MessageTextBlock.Stroke != value)
                {
                    this.MessageTextBlock.Stroke = value;
                }
            }
        }

        public double BarHeight
        {
            get => this.Bar.Height;
            set
            {
                if (this.Bar.Height != value)
                {
                    this.Bar.Height = value;
                }
            }
        }

        public double BarBlurRadius
        {
            get => this.Bar.BlurRadius;
            set
            {
                if (this.Bar.BlurRadius != value)
                {
                    this.Bar.BlurRadius = value;
                }
            }
        }

        public bool BarVisible
        {
            get => this.Bar.Visibility == Visibility.Visible;
            set
            {
                var visibility = value ? Visibility.Visible : Visibility.Collapsed;
                if (this.Bar.Visibility != visibility)
                {
                    this.Bar.Visibility = visibility;
                }
            }
        }

        #region Animation

        private DoubleAnimation animation = new DoubleAnimation()
        {
            From = 1.0,
            To = 0,
        };

        public void StartProgressBar(
            double timeToCount)
        {
            this.Bar.BeginAnimation(
                RichProgressBar.ProgressProperty,
                null);

            if (!this.BarVisible)
            {
                return;
            }

            if (timeToCount >= 0)
            {
                this.animation.Duration = TimeSpan.FromMilliseconds(timeToCount);

                Timeline.SetDesiredFrameRate(this.animation, Settings.Default.MaxFPS);

                this.Bar.BeginAnimation(
                    RichProgressBar.ProgressProperty,
                    this.animation);
            }
        }

        #endregion Animation
    }
}
