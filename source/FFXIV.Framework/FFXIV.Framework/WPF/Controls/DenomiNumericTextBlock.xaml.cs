using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using FFXIV.Framework.Common;

namespace FFXIV.Framework.WPF.Controls
{
    /// <summary>
    /// DenomiNumericTextBlock.xaml の相互作用ロジック
    /// </summary>
    public partial class DenomiNumericTextBlock :
        UserControl,
        INotifyPropertyChanged
    {
        public DenomiNumericTextBlock()
        {
            this.InitializeComponent();
            this.Render();
        }

        #region Value 依存関係プロパティ

        /// <summary>
        /// Value 依存関係プロパティ
        /// </summary>
        public static readonly DependencyProperty ValueProperty
            = DependencyProperty.Register(
            nameof(Value),
            typeof(double),
            typeof(DenomiNumericTextBlock),
            new FrameworkPropertyMetadata(
                WPFHelper.IsDesignMode ? 123456789d : 0d,
                (s, e) => (s as DenomiNumericTextBlock).Render()));

        /// <summary>
        /// Value
        /// </summary>
        public double Value
        {
            get => (double)this.GetValue(ValueProperty);
            set => this.SetValue(ValueProperty, value);
        }

        #endregion Value 依存関係プロパティ

        #region Fill 依存関係プロパティ

        /// <summary>
        /// Fill 依存関係プロパティ
        /// </summary>
        public static readonly DependencyProperty FillProperty
            = DependencyProperty.Register(
            nameof(Fill),
            typeof(Brush),
            typeof(DenomiNumericTextBlock),
            new FrameworkPropertyMetadata(
                Brushes.White,
                (s, e) => (s as DenomiNumericTextBlock).Render()));

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
            typeof(DenomiNumericTextBlock),
            new FrameworkPropertyMetadata(
                Brushes.OrangeRed,
                (s, e) => (s as DenomiNumericTextBlock).Render()));

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
            typeof(DenomiNumericTextBlock),
            new FrameworkPropertyMetadata(
                1d,
                (s, e) => (s as DenomiNumericTextBlock).Render()));

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
            typeof(DenomiNumericTextBlock),
            new FrameworkPropertyMetadata(
                1d,
                (s, e) => (s as DenomiNumericTextBlock).Render()));

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
            typeof(DenomiNumericTextBlock),
            new FrameworkPropertyMetadata(
                1d,
                (s, e) => (s as DenomiNumericTextBlock).Render()));

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
            typeof(DenomiNumericTextBlock),
            new FrameworkPropertyMetadata(
                1d,
                (s, e) => (s as DenomiNumericTextBlock).Render()));

        public double BlurOpacity
        {
            get => (double)GetValue(BlurOpacityProperty);
            set => SetValue(BlurOpacityProperty, value);
        }

        #endregion BlurOpacity 依存関係プロパティ

        #region TextDecorations 依存関係プロパティ

        public static readonly DependencyProperty TextDecorationsProperty
            = DependencyProperty.Register(
            nameof(TextDecorations),
            typeof(TextDecorationCollection),
            typeof(DenomiNumericTextBlock),
            new FrameworkPropertyMetadata(
                new TextDecorationCollection(),
                (s, e) => (s as DenomiNumericTextBlock).Render()));

        public TextDecorationCollection TextDecorations
        {
            get => (TextDecorationCollection)GetValue(TextDecorationsProperty);
            set => SetValue(TextDecorationsProperty, value);
        }

        #endregion TextDecorations 依存関係プロパティ

        private void Render()
        {
            var label = default(LightOutlineTextBlock);

            label = this.UpperPartLabel;
            label.Fill = this.Fill;
            label.Stroke = this.Stroke;
            label.StrokeThickness = this.StrokeThickness;
            label.StrokeOpacity = this.StrokeOpacity;
            label.BlurRadius = this.BlurRadius;
            label.BlurOpacity = this.BlurOpacity;
            label.TextDecorations = this.TextDecorations;

            label = this.BottomPartLabel;
            label.Fill = this.Fill;
            label.Stroke = this.Stroke;
            label.StrokeThickness = this.StrokeThickness;
            label.StrokeOpacity = this.StrokeOpacity;
            label.BlurRadius = this.BlurRadius;
            label.BlurOpacity = this.BlurOpacity;
            label.TextDecorations = this.TextDecorations;

            var text = DivideValueText(this.Value);

            this.UpperPartLabel.Text = text.UpperPart;
            this.BottomPartLabel.Text = text.BottomPart;

            this.UpperPartLabel.Visibility = string.IsNullOrEmpty(this.UpperPartLabel.Text) ?
                Visibility.Collapsed :
                Visibility.Visible;

            this.BottomPartLabel.Visibility = string.IsNullOrEmpty(this.BottomPartLabel.Text) ?
                Visibility.Collapsed :
                Visibility.Visible;
        }

        public static (string UpperPart, string BottomPart) DivideValueText(
            double value)
        {
            var result = default((string UpperPart, string BottomPart));

            var hp = (long)value;
            if (hp < 10000)
            {
                result.UpperPart = hp.ToString("N0");
                result.BottomPart = string.Empty;
            }
            else
            {
                result.UpperPart = (hp / 1000).ToString("N0");
                result.BottomPart = " ," + (hp % 1000).ToString("000");
            }

            return result;
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

    internal class TopMarginAdjuster : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var margin = new Thickness();

            if (value is double baseFontSize &&
                double.TryParse(parameter.ToString(), out double fontSizeRate))
            {
                var topMargin = baseFontSize - (baseFontSize * fontSizeRate);
                margin.Top = topMargin;
            }

            return margin;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => null;
    }
}
