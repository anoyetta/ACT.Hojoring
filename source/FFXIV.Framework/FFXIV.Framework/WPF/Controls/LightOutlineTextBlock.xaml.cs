using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FFXIV.Framework.Common;
using FFXIV.Framework.WPF.Converters;

namespace FFXIV.Framework.WPF.Controls
{
    /// <summary>
    /// OutlineTextBlock.xaml の相互作用ロジック
    /// </summary>
    public partial class LightOutlineTextBlock :
        UserControl,
        INotifyPropertyChanged
    {
        public LightOutlineTextBlock()
        {
            this.InitializeComponent();
            this.Render();
        }

        #region Text 依存関係プロパティ

        /// <summary>
        /// Text 依存関係プロパティ
        /// </summary>
        public static readonly DependencyProperty TextProperty
            = DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(LightOutlineTextBlock),
            new FrameworkPropertyMetadata(
                WPFHelper.IsDesignMode ? "サンプルテキスト" : string.Empty,
                (s, e) => (s as LightOutlineTextBlock).Render()));

        /// <summary>
        /// Text
        /// </summary>
        public string Text
        {
            get => (string)this.GetValue(TextProperty);
            set => this.SetValue(TextProperty, value);
        }

        #endregion Text 依存関係プロパティ

        #region Fill 依存関係プロパティ

        /// <summary>
        /// Fill 依存関係プロパティ
        /// </summary>
        public static readonly DependencyProperty FillProperty
            = DependencyProperty.Register(
            nameof(Fill),
            typeof(Brush),
            typeof(LightOutlineTextBlock),
            new FrameworkPropertyMetadata(
                Brushes.White,
                (s, e) => (s as LightOutlineTextBlock).Render()));

        /// <summary>
        /// Fill
        /// </summary>
        public Brush Fill
        {
            get => (Brush)this.GetValue(FillProperty);
            set => this.SetValue(FillProperty, value);
        }

        #endregion Fill 依存関係プロパティ

        #region Stroke 依存関係プロパティ

        /// <summary>
        /// Stroke 依存関係プロパティ
        /// </summary>
        public static readonly DependencyProperty StrokeProperty
            = DependencyProperty.Register(
            nameof(Stroke),
            typeof(Brush),
            typeof(LightOutlineTextBlock),
            new FrameworkPropertyMetadata(
                Brushes.OrangeRed,
                (s, e) => (s as LightOutlineTextBlock).Render()));

        /// <summary>
        /// Stroke
        /// </summary>
        public Brush Stroke
        {
            get => (Brush)this.GetValue(StrokeProperty);
            set => this.SetValue(StrokeProperty, value);
        }

        #endregion Stroke 依存関係プロパティ

        #region StrokeThickness 依存関係プロパティ

        public static readonly DependencyProperty StrokeThicknessProperty
            = DependencyProperty.Register(
            nameof(StrokeThickness),
            typeof(double),
            typeof(LightOutlineTextBlock),
            new FrameworkPropertyMetadata(
                1d,
                (s, e) => (s as LightOutlineTextBlock).Render()));

        public double StrokeThickness
        {
            get => (double)GetValue(StrokeThicknessProperty);
            set => SetValue(StrokeThicknessProperty, value);
        }

        #endregion StrokeThickness 依存関係プロパティ

        #region StrokeOpacity 依存関係プロパティ

        public static readonly DependencyProperty StrokeOpacityProperty
            = DependencyProperty.Register(
            nameof(StrokeOpacity),
            typeof(double),
            typeof(LightOutlineTextBlock),
            new FrameworkPropertyMetadata(
                1d,
                (s, e) => (s as LightOutlineTextBlock).Render()));

        public double StrokeOpacity
        {
            get => (double)GetValue(StrokeOpacityProperty);
            set => SetValue(StrokeOpacityProperty, value);
        }

        #endregion StrokeOpacity 依存関係プロパティ

        #region BlurRadius 依存関係プロパティ

        public static readonly DependencyProperty BlurRadiusProperty
            = DependencyProperty.Register(
            nameof(BlurRadius),
            typeof(double),
            typeof(LightOutlineTextBlock),
            new FrameworkPropertyMetadata(
                1d,
                (s, e) => (s as LightOutlineTextBlock).Render()));

        public double BlurRadius
        {
            get => (double)GetValue(BlurRadiusProperty);
            set => SetValue(BlurRadiusProperty, value);
        }

        #endregion BlurRadius 依存関係プロパティ

        #region BlurOpacity 依存関係プロパティ

        public static readonly DependencyProperty BlurOpacityProperty
            = DependencyProperty.Register(
            nameof(BlurOpacity),
            typeof(double),
            typeof(LightOutlineTextBlock),
            new FrameworkPropertyMetadata(
                1d,
                (s, e) => (s as LightOutlineTextBlock).Render()));

        public double BlurOpacity
        {
            get => (double)GetValue(BlurOpacityProperty);
            set => SetValue(BlurOpacityProperty, value);
        }

        #endregion BlurOpacity 依存関係プロパティ

        #region TextAlignment 依存関係プロパティ

        public static readonly DependencyProperty TextAlignmentProperty
            = DependencyProperty.Register(
            nameof(TextAlignment),
            typeof(TextAlignment),
            typeof(LightOutlineTextBlock),
            new FrameworkPropertyMetadata(
                TextAlignment.Left,
                (s, e) => (s as LightOutlineTextBlock).Render()));

        public TextAlignment TextAlignment
        {
            get => (TextAlignment)GetValue(TextAlignmentProperty);
            set => SetValue(TextAlignmentProperty, value);
        }

        #endregion TextAlignment 依存関係プロパティ

        #region TextDecorations 依存関係プロパティ

        public static readonly DependencyProperty TextDecorationsProperty
            = DependencyProperty.Register(
            nameof(TextDecorations),
            typeof(TextDecorationCollection),
            typeof(LightOutlineTextBlock),
            new FrameworkPropertyMetadata(
                new TextDecorationCollection(),
                (s, e) => (s as LightOutlineTextBlock).Render()));

        public TextDecorationCollection TextDecorations
        {
            get => (TextDecorationCollection)GetValue(TextDecorationsProperty);
            set => SetValue(TextDecorationsProperty, value);
        }

        #endregion TextDecorations 依存関係プロパティ

        #region TextTrimming 依存関係プロパティ

        public static readonly DependencyProperty TextTrimmingProperty
            = DependencyProperty.Register(
            nameof(TextTrimming),
            typeof(TextTrimming),
            typeof(LightOutlineTextBlock),
            new FrameworkPropertyMetadata(
                TextTrimming.None,
                (s, e) => (s as LightOutlineTextBlock).Render()));

        public TextTrimming TextTrimming
        {
            get => (TextTrimming)GetValue(TextTrimmingProperty);
            set => SetValue(TextTrimmingProperty, value);
        }

        #endregion TextTrimming 依存関係プロパティ

        #region TextWrapping 依存関係プロパティ

        public static readonly DependencyProperty TextWrappingProperty
            = DependencyProperty.Register(
            nameof(TextWrapping),
            typeof(TextWrapping),
            typeof(LightOutlineTextBlock),
            new FrameworkPropertyMetadata(
                TextWrapping.NoWrap,
                (s, e) => (s as LightOutlineTextBlock).Render()));

        public TextWrapping TextWrapping
        {
            get => (TextWrapping)GetValue(TextWrappingProperty);
            set => SetValue(TextWrappingProperty, value);
        }

        #endregion TextWrapping 依存関係プロパティ

        private static readonly SolidColorBrushToColorConverter BrushToColorConverter = new SolidColorBrushToColorConverter();

        private static Color BrushToColor(
            Brush brush)
            => (Color)BrushToColorConverter.Convert(brush, null, null, null);

        private void Render()
        {
            this.CoreTextBlock.Text = this.Text;

            this.CoreTextBlock.Foreground = this.Fill;
            this.CoreTextBlock.TextAlignment = this.TextAlignment;
            this.CoreTextBlock.TextWrapping = this.TextWrapping;
            this.CoreTextBlock.TextDecorations = this.TextDecorations;
            this.CoreTextBlock.TextTrimming = this.TextTrimming;

            this.InnerEffect.BlurRadius = this.StrokeThickness;
            this.InnerEffect.Opacity = this.StrokeOpacity;
            this.InnerEffect.Color = BrushToColor(this.Stroke);

            this.OuterEffect.BlurRadius = this.BlurRadius;
            this.OuterEffect.Opacity = this.BlurOpacity;
            this.OuterEffect.Color = BrushToColor(this.Stroke);

            var fillColor = BrushToColor(this.Fill);
            var strokeColor = BrushToColor(this.Stroke);

            if (this.StrokeThickness <= 0 ||
                fillColor == strokeColor ||
                strokeColor.A <= 0)
            {
                this.Outline1TextBlock.Visibility = Visibility.Collapsed;
                this.Outline2TextBlock.Visibility = Visibility.Collapsed;
                this.Outline3TextBlock.Visibility = Visibility.Collapsed;
                this.Outline4TextBlock.Visibility = Visibility.Collapsed;
                this.Outline5TextBlock.Visibility = Visibility.Collapsed;
                this.Outline6TextBlock.Visibility = Visibility.Collapsed;
            }
            else
            {
                this.Outline1TextBlock.Visibility = Visibility.Visible;
                this.Outline2TextBlock.Visibility = Visibility.Visible;
                this.Outline3TextBlock.Visibility = Visibility.Visible;
                this.Outline4TextBlock.Visibility = Visibility.Visible;
                this.Outline5TextBlock.Visibility = Visibility.Visible;
                this.Outline6TextBlock.Visibility = Visibility.Visible;
            }
        }

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
