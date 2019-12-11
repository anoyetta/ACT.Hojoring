using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using ACT.SpecialSpellTimer.Config;
using ACT.SpecialSpellTimer.Image;
using ACT.SpecialSpellTimer.Models;
using FFXIV.Framework.Common;
using FFXIV.Framework.Extensions;
using FFXIV.Framework.WPF.Controls;
using FFXIV.Framework.XIVHelper;

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
            if (WPFHelper.IsDesignMode)
            {
                this.DataContext = new Spell();
            }

            this.InitializeComponent();
            this.BaseGrid.Visibility = Visibility.Collapsed;
        }

        public bool IsActive => this.BaseGrid.Visibility == Visibility.Visible;

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
            this.RefreshHide();

            if (this.Spell.IsStandardStyle)
            {
                this.RefreshCommon(
                    this.SpellTitleTextBlock,
                    this.SpellIconImage,
                    this.RecastTimeTextBlock);
            }
            else
            {
                this.RefreshCommon(
                    this.GetSpellTitleTextBlock(),
                    this.CircleIcon,
                    this.CircleRecastTime);
            }
        }

        private void RefreshHide()
        {
            if (this.Spell.IsDesignMode)
            {
                this.BaseGrid.Visibility = Visibility.Visible;
                return;
            }

            var visibility =
                !this.Spell.IsHideInNotCombat || XIVPluginHelper.Instance.InCombat ?
                Visibility.Visible :
                Visibility.Collapsed;

            if (this.Spell.DelayToShow > 0)
            {
                if (visibility == Visibility.Visible)
                {
                    var e = DateTime.Now - this.Spell.MatchDateTime;
                    if (e.TotalSeconds <= this.Spell.DelayToShow)
                    {
                        visibility = Visibility.Collapsed;
                    }
                }
            }

            this.BaseGrid.Visibility = visibility;
        }

        private void RefreshCommon(
            LightOutlineTextBlock titleTextBlock,
            FantImage iconImage,
            LightOutlineTextBlock remainTimeTextBlock)
        {
            var tb = default(LightOutlineTextBlock);

            // Titleを描画する
            if (titleTextBlock != null)
            {
                tb = titleTextBlock;
                var title =
                    string.IsNullOrWhiteSpace(this.Spell.SpellTitleReplaced) ?
                    this.Spell.SpellTitle :
                    this.Spell.SpellTitleReplaced;
                title = string.IsNullOrWhiteSpace(title) ? "　" : title;
                title = title.Replace(",", Environment.NewLine);
                title = title.Replace("\\n", Environment.NewLine);

                var fill = this.FontBrush;
                var stroke = this.FontOutlineBrush;

                if (this.Spell.ChangeFontColorWhenContainsMe)
                {
                    var player = CombatantsManager.Instance.Player;
                    if (player != null)
                    {
                        if (player.ContainsName(title) ||
                            player.ContainsName(this.Spell.MatchedLog))
                        {
                            fill = this.WarningFontBrush;
                            stroke = this.WarningFontOutlineBrush;
                        }
                    }
                }

                if (tb.Fill != fill) tb.Fill = fill;
                if (tb.Stroke != stroke) tb.Stroke = stroke;

                tb.Text = title;

                tb.Visibility = this.Spell.HideSpellName ?
                    Visibility.Collapsed :
                    Visibility.Visible;
            }

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

                if (iconImage.Opacity != opacity)
                {
                    iconImage.Opacity = opacity;
                }
            }

            // リキャスト時間を描画する
            if (!this.Spell.HideCounter)
            {
                var recastTimeToShow = 0d;
                recastTimeToShow = Settings.Default.EnabledSpellTimerNoDecimal ?
                    this.RecastTime.CeilingEx() :
                    this.RecastTime.CeilingEx(1);

                tb = remainTimeTextBlock;
                var recast = this.RecastTime > 0 ?
                    recastTimeToShow.ToString(RecastTimeFormat) :
                    this.Spell.IsReverse ? Settings.Default.OverText : Settings.Default.ReadyText;

                if (tb.Text != recast) tb.Text = recast;
                tb.SetFontInfo(this.Spell.Font);
                tb.StrokeThickness = this.Spell.Font.OutlineThickness;
                tb.BlurRadius = this.Spell.Font.BlurRadius;

                var fill = this.FontBrush;
                var stroke = this.FontOutlineBrush;

                if (this.Spell.ChangeFontColorsWhenWarning)
                {
                    if (this.RecastTime < this.Spell.WarningTime)
                    {
                        fill = this.WarningFontBrush;
                        stroke = this.WarningFontOutlineBrush;
                    }
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
            this.RefreshHide();
            this.UpdateBrushes();

            if (this.Spell.IsStandardStyle)
            {
                this.UpdateStandardStyle();
            }
            else
            {
                this.UpdateCircleStyle();
            }
        }

        private void UpdateCommon(
            LightOutlineTextBlock titleTextBlock,
            FantImage iconImage)
        {
            var font = this.Spell.Font;

            // アイコンを描画する
            var image = iconImage;
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

                image.Visibility = Visibility.Visible;
                this.SpellIconPanel.Visibility = Visibility.Visible;
            }
            else
            {
                image.Source = IconController.BlankBitmap;
                image.Height = 0;
                image.Width = 0;
                this.SpellIconPanel.Background = Brushes.Transparent;

                image.Visibility = Visibility.Collapsed;
                this.SpellIconPanel.Visibility = Visibility.Collapsed;
            }

            // Titleを描画する
            if (titleTextBlock != null)
            {
                var tb = titleTextBlock;

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
            }
        }

        private void UpdateStandardStyle()
        {
            this.Width = this.SpellWidth;

            this.UpdateCommon(
                this.SpellTitleTextBlock,
                this.SpellIconImage);

            // スペルカウンタの表示及び表示位置を切り替える
            if (!this.Spell.HideCounter)
            {
                if (this.Spell.OverlapRecastTime)
                {
                    this.RecastTimePanelOnIcon.Visibility = this.SpellIconImage.Source != null ?
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

        private void UpdateCircleStyle()
        {
            this.Width = double.NaN;

            this.UpdateCommon(
                this.GetSpellTitleTextBlock(),
                this.CircleIcon);

            this.CircleProgress.Fill = this.BarBrush;
            this.CircleProgress.Stroke = this.BarOutlineBrush;
            this.CircleProgress.Radius = this.Spell.BarWidth / 2;
            this.CircleProgress.Thickness = this.Spell.BarHeight;
            this.CircleProgress.IsCCW = true;
        }

        private LightOutlineTextBlock GetSpellTitleTextBlock()
        {
            var title = default(LightOutlineTextBlock);
            if (this.Spell.HideSpellName)
            {
                this.CircleSpellTitle.Visibility = Visibility.Collapsed;
                this.CircleAlterSpellTitle.Visibility = Visibility.Collapsed;
            }
            else
            {
                switch (this.Spell.TitleVerticalAlignmentInCircle)
                {
                    case VerticalAlignment.Top:
                        title = this.CircleAlterSpellTitle;
                        this.CircleSpellTitle.Visibility = Visibility.Collapsed;
                        Grid.SetRow(title, 0);
                        title.Margin = new Thickness(0, 0, 0, 2);
                        break;

                    case VerticalAlignment.Bottom:
                        title = this.CircleAlterSpellTitle;
                        this.CircleSpellTitle.Visibility = Visibility.Collapsed;
                        Grid.SetRow(title, 2);
                        title.Margin = new Thickness(0, 2, 0, 0);
                        break;

                    default:
                        title = this.CircleSpellTitle;
                        this.CircleAlterSpellTitle.Visibility = Visibility.Collapsed;
                        break;
                }
            }

            return title;
        }

        private void UpdateBrushes()
        {
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
        }

        #region Bar Animations

        /// <summary>バーのアニメーション用DoubleAnimation</summary>
        private DoubleAnimation BarAnimation { get; set; } = new DoubleAnimation()
        {
            AutoReverse = false
        };

        private static readonly int FPSLowerLimit = 15;

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

            var fps = (int)Math.Ceiling(this.Spell.BarWidth / this.RecastTime);
            if (fps <= 0 || fps > Settings.Default.MaxFPS)
            {
                fps = Settings.Default.MaxFPS;
            }

            if (fps < FPSLowerLimit)
            {
                fps = FPSLowerLimit;
            }

            Timeline.SetDesiredFrameRate(this.BarAnimation, fps);

            if (this.Spell.IsStandardStyle)
            {
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
            else
            {
                if (this.Spell.IsReverse)
                {
                    this.BarAnimation.From = 1.0d - this.Progress;
                    this.BarAnimation.To = 0;
                }
                else
                {
                    this.BarAnimation.From = this.Progress;
                    this.BarAnimation.To = 1;
                }

                this.BarAnimation.Duration = new Duration(TimeSpan.FromSeconds(this.RecastTime));

                this.CircleProgress.BeginAnimation(ProgressCircle.ProgressProperty, null);
                this.CircleProgress.BeginAnimation(ProgressCircle.ProgressProperty, this.BarAnimation);
            }
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

            var targetIcon = this.Spell.IsStandardStyle ?
                this.SpellIconImage :
                this.CircleIcon;

            var targetBar = this.Spell.IsStandardStyle ?
                this.BarRectangle as UIElement :
                this.CircleProgress as UIElement;

            var targetEffect = this.Spell.IsStandardStyle ?
                this.BarEffect :
                this.CircleProgress.BlurEffect;

            if (this.Spell.BlinkTime == 0 ||
                this.RecastTime == 0 ||
                this.RecastTime > this.Spell.BlinkTime)
            {
                if (this.isBlinking)
                {
                    this.isBlinking = false;

                    targetIcon.BeginAnimation(
                        System.Windows.Controls.Image.OpacityProperty,
                        null);
                    targetBar.BeginAnimation(
                        UIElement.OpacityProperty,
                        null);
                    targetEffect.BeginAnimation(
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

                    targetIcon.BeginAnimation(
                        System.Windows.Controls.Image.OpacityProperty,
                        this.iconBlinkAnimation);
                }

                if (this.Spell.BlinkBar)
                {
                    Timeline.SetDesiredFrameRate(this.barBlinkAnimation, Settings.Default.MaxFPS);

                    targetBar.BeginAnimation(
                        UIElement.OpacityProperty,
                        this.barBlinkAnimation);

                    targetEffect.BeginAnimation(
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

        #region INotifyPropertyChanged

        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(
            [CallerMemberName]string propertyName = null)
        {
            this.PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }

        protected virtual bool SetProperty<T>(
            ref T field,
            T value,
            [CallerMemberName]string propertyName = null)
        {
            if (Equals(field, value))
            {
                return false;
            }

            field = value;
            this.PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));

            return true;
        }

        #endregion INotifyPropertyChanged
    }
}
