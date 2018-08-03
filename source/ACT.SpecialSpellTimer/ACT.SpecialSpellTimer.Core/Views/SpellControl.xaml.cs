using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ACT.SpecialSpellTimer.Config;
using ACT.SpecialSpellTimer.Image;
using ACT.SpecialSpellTimer.Models;
using FFXIV.Framework.Extensions;
using FFXIV.Framework.WPF.Controls;

namespace ACT.SpecialSpellTimer.Views
{
    /// <summary>
    /// SpellTimerControl
    /// </summary>
    public partial class SpellControl :
        UserControl
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SpellControl()
        {
            this.InitializeComponent();
        }

        #region Colors

        /// <summary>バーのBrush</summary>
        private SolidColorBrush BarBrush { get; set; }

        /// <summary>バーのアウトラインのBrush</summary>
        private SolidColorBrush BarOutlineBrush { get; set; }

        /// <summary>フォントのBrush</summary>
        private SolidColorBrush FontBrush { get; set; }

        /// <summary>フォントのアウトラインBrush</summary>
        private SolidColorBrush FontOutlineBrush { get; set; }

        /// <summary>フォントのBrush</summary>
        private SolidColorBrush WarningFontBrush { get; set; }

        /// <summary>フォントのアウトラインBrush</summary>
        private SolidColorBrush WarningFontOutlineBrush { get; set; }

        #endregion Colors

        #region Sizes

        /// <summary>
        /// スペル表示領域の幅
        /// </summary>
        public int SpellWidth =>
            this.Spell.BarWidth > this.Spell.SpellIconSize ?
            this.Spell.BarWidth :
            this.Spell.SpellIconSize;

        #endregion Sizes

        public Spell Spell
        {
            get => this.DataContext as Spell;
            set => this.DataContext = value;
        }

        public double Progress { get; set; }

        public double RecastTime { get; set; }

        private static string RecastTimeFormat =>
            Settings.Default.EnabledSpellTimerNoDecimal ? "N0" : "N1";

        /// <summary>
        /// 描画を更新する
        /// </summary>
        public void Refresh()
        {
            // Titleを描画する
            var tb = this.SpellTitleTextBlock;
            var title =
                string.IsNullOrWhiteSpace(this.Spell.SpellTitleReplaced) ?
                this.Spell.SpellTitle :
                this.Spell.SpellTitleReplaced;
            title = string.IsNullOrWhiteSpace(title) ? "　" : title;
            title = title.Replace(",", Environment.NewLine);
            title = title.Replace("\\n", Environment.NewLine);

            tb.Text = title;

            tb.Visibility = this.Spell.HideSpellName ?
                Visibility.Collapsed :
                Visibility.Visible;

            // 点滅を判定する
            if (!this.StartBlink())
            {
                // アイコンの不透明度を設定する
                var opacity = 1.0;
                if (this.Spell.ReduceIconBrightness)
                {
                    if (this.RecastTime > 0)
                    {
                        opacity = this.Spell.IsReverse ?
                            1.0 :
                            ((double)Settings.Default.ReduceIconBrightness / 100d);
                    }
                    else
                    {
                        opacity = this.Spell.IsReverse ?
                            ((double)Settings.Default.ReduceIconBrightness / 100d) :
                            1.0;
                    }
                }

                if (this.SpellIconImage.Opacity != opacity)
                {
                    this.SpellIconImage.Opacity = opacity;
                }
            }

            // リキャスト時間を描画する
            if (!this.Spell.HideCounter)
            {
                var recastTimeToShow = 0d;
                recastTimeToShow = Settings.Default.EnabledSpellTimerNoDecimal ?
                    this.RecastTime.CeilingEx() :
                    this.RecastTime.CeilingEx(1);

                tb = this.RecastTimeTextBlock;
                var recast = this.RecastTime > 0 ?
                    recastTimeToShow.ToString(RecastTimeFormat) :
                    this.Spell.IsReverse ? Settings.Default.OverText : Settings.Default.ReadyText;

                if (tb.Text != recast) tb.Text = recast;
                tb.SetFontInfo(this.Spell.Font);
                tb.StrokeThickness = this.Spell.Font.OutlineThickness;
                tb.BlurRadius = this.Spell.Font.BlurRadius;

                var fill = this.FontBrush;
                var stroke = this.FontOutlineBrush;

                if (this.Spell.ChangeFontColorsWhenWarning &&
                    this.RecastTime < this.Spell.WarningTime)
                {
                    fill = this.WarningFontBrush;
                    stroke = this.WarningFontOutlineBrush;
                }

                if (tb.Fill != fill) tb.Fill = fill;
                if (tb.Stroke != stroke) tb.Stroke = stroke;
            }
        }

        /// <summary>
        /// 描画設定を更新する
        /// </summary>
        public void Update()
        {
            this.Width = this.SpellWidth;

            // Brushを生成する
            var fontColor = string.IsNullOrWhiteSpace(this.Spell.FontColor) ?
                Colors.White :
                this.Spell.FontColor.FromHTMLWPF();
            var fontOutlineColor = string.IsNullOrWhiteSpace(this.Spell.FontOutlineColor) ?
                Colors.Navy :
                this.Spell.FontOutlineColor.FromHTMLWPF();
            var warningFontColor = string.IsNullOrWhiteSpace(this.Spell.WarningFontColor) ?
                Colors.White :
                this.Spell.WarningFontColor.FromHTMLWPF();
            var warningFontOutlineColor = string.IsNullOrWhiteSpace(this.Spell.WarningFontOutlineColor) ?
                Colors.OrangeRed :
                this.Spell.WarningFontOutlineColor.FromHTMLWPF();

            var barColor = string.IsNullOrWhiteSpace(this.Spell.BarColor) ?
                Colors.White :
                this.Spell.BarColor.FromHTMLWPF();
            var barOutlineColor = string.IsNullOrWhiteSpace(this.Spell.BarOutlineColor) ?
                Colors.Navy :
                this.Spell.BarOutlineColor.FromHTMLWPF();

            this.FontBrush = this.GetBrush(fontColor);
            this.FontOutlineBrush = this.GetBrush(fontOutlineColor);
            this.WarningFontBrush = this.GetBrush(warningFontColor);
            this.WarningFontOutlineBrush = this.GetBrush(warningFontOutlineColor);
            this.BarBrush = this.GetBrush(barColor);
            this.BarOutlineBrush = this.GetBrush(barOutlineColor);

            var tb = default(LightOutlineTextBlock);
            var font = this.Spell.Font;

            // アイコンを描画する
            var image = this.SpellIconImage;
            var iconFile = IconController.Instance.GetIconFile(this.Spell.SpellIcon);
            if (iconFile != null &&
                File.Exists(iconFile.FullPath))
            {
                if (image.Source == IconController.BlankBitmap ||
                    image.Height != this.Spell.SpellIconSize ||
                    image.Width != this.Spell.SpellIconSize ||
                    (image.Source as BitmapImage)?.UriSource?.LocalPath != iconFile.FullPath)
                {
                    var bitmap = iconFile.CreateBitmapImage();
                    image.Source = bitmap;
                    image.Height = this.Spell.SpellIconSize;
                    image.Width = this.Spell.SpellIconSize;

                    this.SpellIconPanel.OpacityMask = new ImageBrush(bitmap);
                }

                this.SpellIconPanel.Background = Brushes.Black;
            }
            else
            {
                image.Source = IconController.BlankBitmap;
                this.SpellIconPanel.Background = Brushes.Transparent;
            }

            // Titleを描画する
            tb = this.SpellTitleTextBlock;

            var title =
                string.IsNullOrWhiteSpace(this.Spell.SpellTitleReplaced) ?
                this.Spell.SpellTitle :
                this.Spell.SpellTitleReplaced;
            title = string.IsNullOrWhiteSpace(title) ? "　" : title;
            title = title.Replace(",", Environment.NewLine);
            title = title.Replace("\\n", Environment.NewLine);

            tb.Text = title;
            tb.Fill = this.FontBrush;
            tb.Stroke = this.FontOutlineBrush;
            tb.SetFontInfo(font);
            tb.StrokeThickness = font.OutlineThickness;
            tb.BlurRadius = font.BlurRadius;

            tb.Visibility = this.Spell.HideSpellName ?
                Visibility.Collapsed :
                Visibility.Visible;

            // スペルカウンタの表示及び表示位置を切り替える
            if (!this.Spell.HideCounter)
            {
                if (this.Spell.OverlapRecastTime)
                {
                    this.RecastTimePanelOnIcon.Visibility = image.Source != null ?
                        Visibility.Visible :
                        Visibility.Collapsed;
                    this.RecastTimePanel.Visibility = Visibility.Collapsed;
                }
                else
                {
                    this.RecastTimePanelOnIcon.Visibility = Visibility.Collapsed;
                    this.RecastTimePanel.Visibility = Visibility.Visible;
                }
            }
            else
            {
                this.RecastTimePanelOnIcon.Visibility = Visibility.Collapsed;
                this.RecastTimePanel.Visibility = Visibility.Collapsed;
            }

            // ProgressBarを描画する
            var foreRect = this.BarRectangle;
            foreRect.Fill = this.BarBrush;
            foreRect.Width = this.Spell.BarWidth;
            foreRect.Height = this.Spell.BarHeight;

            var backRect = this.BarBackRectangle;
            backRect.Width = this.Spell.BarWidth;

            var outlineRect = this.BarOutlineRectangle;
            outlineRect.Stroke = this.BarOutlineBrush;

            // バーのエフェクトカラーも手動で設定する
            // Bindingだとアニメーションでエラーが発生するため
            var effectColor = this.BarBrush.Color.ChangeBrightness(1.1);
            this.BarEffect.Color = effectColor;
        }

        #region Bar Animations

        /// <summary>バーのアニメーション用DoubleAnimation</summary>
        private DoubleAnimation BarAnimation { get; set; }

        /// <summary>
        /// バーのアニメーションを開始する
        /// </summary>
        public void StartBarAnimation()
        {
            if (this.Spell.BarWidth == 0 ||
                this.Spell.BarHeight == 0)
            {
                return;
            }

            if (this.BarAnimation == null)
            {
                this.BarAnimation = new DoubleAnimation();
                this.BarAnimation.AutoReverse = false;
            }

            var fps = (int)Math.Ceiling(this.Spell.BarWidth / this.RecastTime);
            if (fps <= 0 || fps > Settings.Default.MaxFPS)
            {
                fps = Settings.Default.MaxFPS;
            }

            Timeline.SetDesiredFrameRate(this.BarAnimation, fps);

            var currentWidth = this.Spell.IsReverse ?
                (double)(this.Spell.BarWidth * (1.0d - this.Progress)) :
                (double)(this.Spell.BarWidth * this.Progress);
            if (this.Spell.IsReverse)
            {
                this.BarAnimation.From = currentWidth / this.Spell.BarWidth;
                this.BarAnimation.To = 0;
            }
            else
            {
                this.BarAnimation.From = currentWidth / this.Spell.BarWidth;
                this.BarAnimation.To = 1.0;
            }

            this.BarAnimation.Duration = new Duration(TimeSpan.FromSeconds(this.RecastTime));

            this.BarScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            this.BarScale.BeginAnimation(ScaleTransform.ScaleXProperty, this.BarAnimation);
        }

        #endregion Bar Animations

        #region Blink Animations

        /// <summary>
        /// アイコンの暗い状態の値
        /// </summary>
        /// <remarks>
        /// 暗さ設定の80%とする。点滅の際にはよりコントラストが必要なため</remarks>
        private static readonly double IconDarkValue =
            ((double)Settings.Default.ReduceIconBrightness / 100d) *
            Settings.Default.BlinkBrightnessDark;

        /// <summary>
        /// アイコンの明るい状態の値
        /// </summary>
        private static readonly double IconLightValue = 1.0;

        /// <summary>
        /// ブリンク状態か？
        /// </summary>
        private volatile bool isBlinking = false;

        #region Icon

        private DiscreteDoubleKeyFrame IconKeyframe1 => (DiscreteDoubleKeyFrame)this.iconBlinkAnimation.KeyFrames[0];
        private LinearDoubleKeyFrame IconKeyframe2 => (LinearDoubleKeyFrame)this.iconBlinkAnimation.KeyFrames[1];

        private DoubleAnimationUsingKeyFrames iconBlinkAnimation = new DoubleAnimationUsingKeyFrames()
        {
            KeyFrames = new DoubleKeyFrameCollection()
            {
                new DiscreteDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0))),
                new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.3)))
            }
        };

        #endregion Icon

        #region Bar

        private DiscreteDoubleKeyFrame BarKeyframe1 => (DiscreteDoubleKeyFrame)this.barBlinkAnimation.KeyFrames[0];
        private LinearDoubleKeyFrame BarKeyframe2 => (LinearDoubleKeyFrame)this.barBlinkAnimation.KeyFrames[1];

        private DoubleAnimationUsingKeyFrames barBlinkAnimation = new DoubleAnimationUsingKeyFrames()
        {
            KeyFrames = new DoubleKeyFrameCollection()
            {
                new DiscreteDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0))),
                new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.3)))
            }
        };

        #endregion Bar

        public bool StartBlink()
        {
            this.InitializeBlinkAnimation();

            if (this.Spell.BlinkTime == 0 ||
                this.RecastTime == 0 ||
                this.RecastTime > this.Spell.BlinkTime)
            {
                if (this.isBlinking)
                {
                    this.isBlinking = false;

                    this.SpellIconImage.BeginAnimation(
                        System.Windows.Controls.Image.OpacityProperty,
                        null);
                    this.BarRectangle.BeginAnimation(
                        Rectangle.OpacityProperty,
                        null);
                    this.BarEffect.BeginAnimation(
                        DropShadowEffect.OpacityProperty,
                        null);
                }

                return false;
            }

            if (!this.isBlinking)
            {
                this.isBlinking = true;

                if (this.Spell.BlinkIcon)
                {
                    Timeline.SetDesiredFrameRate(this.iconBlinkAnimation, Settings.Default.MaxFPS);

                    this.SpellIconImage.BeginAnimation(
                        System.Windows.Controls.Image.OpacityProperty,
                        this.iconBlinkAnimation);
                }

                if (this.Spell.BlinkBar)
                {
                    Timeline.SetDesiredFrameRate(this.barBlinkAnimation, Settings.Default.MaxFPS);

                    this.BarRectangle.BeginAnimation(
                        Rectangle.OpacityProperty,
                        this.barBlinkAnimation);

                    this.BarEffect.BeginAnimation(
                        DropShadowEffect.OpacityProperty,
                        this.barBlinkAnimation);
                }
            }

            return true;
        }

        private void InitializeBlinkAnimation()
        {
            // アイコンのアニメを設定する
            if (this.Spell.SpellIconSize > 0 &&
                this.Spell.BlinkIcon)
            {
                var value1 = !this.Spell.IsReverse ? SpellControl.IconDarkValue : SpellControl.IconLightValue;
                var value2 = !this.Spell.IsReverse ? SpellControl.IconLightValue : SpellControl.IconDarkValue;

                this.IconKeyframe1.Value = value2;
                this.IconKeyframe2.Value = value1;

                this.iconBlinkAnimation.AutoReverse = true;
                this.iconBlinkAnimation.RepeatBehavior = new RepeatBehavior(TimeSpan.FromSeconds(this.Spell.BlinkTime));
            }

            // バーのアニメを設定する
            if ((this.Spell.BarWidth > 0 || this.Spell.BarHeight > 0) &&
                this.Spell.BlinkBar)
            {
                // バーのエフェクト強度を設定する
                var weekEffect = 0.0;
                var strongEffect = 1.0;

                var effect1 = !this.Spell.IsReverse ? weekEffect : strongEffect;
                var effect2 = !this.Spell.IsReverse ? strongEffect : weekEffect;

                this.BarKeyframe1.Value = effect2;
                this.BarKeyframe2.Value = effect1;

                this.barBlinkAnimation.AutoReverse = true;
                this.barBlinkAnimation.RepeatBehavior = new RepeatBehavior(TimeSpan.FromSeconds(this.Spell.BlinkTime));
            }
        }

        #endregion Blink Animations

        private void TestMenuItem_Click(
            object sender,
            RoutedEventArgs e)
            => this.Spell?.SimulateMatch();
    }
}
