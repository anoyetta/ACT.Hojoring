using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;

namespace FFXIV.Framework.WPF.Controls
{
    [ContentProperty(nameof(Text))]
    public class OutlineTextBlock :
        FrameworkElement
    {
        public static readonly DependencyProperty FontFamilyProperty = TextElement.FontFamilyProperty.AddOwner(
            typeof(OutlineTextBlock),
            new FrameworkPropertyMetadata(OnFormattedTextUpdated));

        public static readonly DependencyProperty FontSizeProperty = TextElement.FontSizeProperty.AddOwner(
            typeof(OutlineTextBlock),
            new FrameworkPropertyMetadata(OnFormattedTextUpdated));

        public static readonly DependencyProperty FontStretchProperty = TextElement.FontStretchProperty.AddOwner(
            typeof(OutlineTextBlock),
            new FrameworkPropertyMetadata(OnFormattedTextUpdated));

        public static readonly DependencyProperty FontStyleProperty = TextElement.FontStyleProperty.AddOwner(
            typeof(OutlineTextBlock),
            new FrameworkPropertyMetadata(OnFormattedTextUpdated));

        public static readonly DependencyProperty FontWeightProperty = TextElement.FontWeightProperty.AddOwner(
            typeof(OutlineTextBlock),
            new FrameworkPropertyMetadata(OnFormattedTextUpdated));

        public static readonly DependencyProperty FillProperty = DependencyProperty.Register(
            nameof(Fill),
            typeof(Brush),
            typeof(OutlineTextBlock),
            new FrameworkPropertyMetadata(
                Brushes.White,
                FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register(
            nameof(Stroke),
            typeof(Brush),
            typeof(OutlineTextBlock),
            new FrameworkPropertyMetadata(
                Brushes.OrangeRed,
                FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register(
            nameof(StrokeThickness),
            typeof(double),
            typeof(OutlineTextBlock),
            new FrameworkPropertyMetadata(
                1d,
                FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(OutlineTextBlock),
            new FrameworkPropertyMetadata(
                OnFormattedTextInvalidated));

        public static readonly DependencyProperty TextAlignmentProperty = DependencyProperty.Register(
            nameof(TextAlignment),
            typeof(TextAlignment),
            typeof(OutlineTextBlock),
            new FrameworkPropertyMetadata(
                OnFormattedTextUpdated));

        public static readonly DependencyProperty TextDecorationsProperty = DependencyProperty.Register(
            nameof(TextDecorations),
            typeof(TextDecorationCollection),
            typeof(OutlineTextBlock),
            new FrameworkPropertyMetadata(
                new TextDecorationCollection(),
                OnFormattedTextUpdated));

        public static readonly DependencyProperty TextTrimmingProperty = DependencyProperty.Register(
            nameof(TextTrimming),
            typeof(TextTrimming),
            typeof(OutlineTextBlock),
            new FrameworkPropertyMetadata(
                OnFormattedTextUpdated));

        public static readonly DependencyProperty TextWrappingProperty = DependencyProperty.Register(
            nameof(TextWrapping),
            typeof(TextWrapping),
            typeof(OutlineTextBlock),
            new FrameworkPropertyMetadata(
                TextWrapping.NoWrap,
                OnFormattedTextUpdated));

        private FormattedText formattedText;
        private Geometry textGeometry;

        public OutlineTextBlock()
        {
        }

        public Brush Fill
        {
            get => (Brush)GetValue(FillProperty);
            set => SetValue(FillProperty, value);
        }

        public FontFamily FontFamily
        {
            get => (FontFamily)GetValue(FontFamilyProperty);
            set => SetValue(FontFamilyProperty, value);
        }

        [TypeConverter(typeof(FontSizeConverter))]
        public double FontSize
        {
            get => (double)GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        public FontStretch FontStretch
        {
            get => (FontStretch)GetValue(FontStretchProperty);
            set => SetValue(FontStretchProperty, value);
        }

        public FontStyle FontStyle
        {
            get => (FontStyle)GetValue(FontStyleProperty);
            set => SetValue(FontStyleProperty, value);
        }

        public FontWeight FontWeight
        {
            get => (FontWeight)GetValue(FontWeightProperty);
            set => SetValue(FontWeightProperty, value);
        }

        public Brush Stroke
        {
            get => (Brush)GetValue(StrokeProperty);
            set => SetValue(StrokeProperty, value);
        }

        public double StrokeThickness
        {
            get => (double)GetValue(StrokeThicknessProperty);
            set => SetValue(StrokeThicknessProperty, value);
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public TextAlignment TextAlignment
        {
            get => (TextAlignment)GetValue(TextAlignmentProperty);
            set => SetValue(TextAlignmentProperty, value);
        }

        public TextDecorationCollection TextDecorations
        {
            get => (TextDecorationCollection)this.GetValue(TextDecorationsProperty);
            set => this.SetValue(TextDecorationsProperty, value);
        }

        public TextTrimming TextTrimming
        {
            get => (TextTrimming)GetValue(TextTrimmingProperty);
            set => SetValue(TextTrimmingProperty, value);
        }

        public TextWrapping TextWrapping
        {
            get => (TextWrapping)GetValue(TextWrappingProperty);
            set => SetValue(TextWrappingProperty, value);
        }

        private double PixelPerDip => VisualTreeHelper.GetDpi(this).PixelsPerDip;

        protected override Size ArrangeOverride(
            Size finalSize)
        {
            this.EnsureFormattedText();

            if (this.formattedText != null)
            {
                this.formattedText.MaxTextWidth = finalSize.Width > 0.0d ? finalSize.Width : 1.0d;
                this.formattedText.MaxTextHeight = finalSize.Height > 0.0d ? finalSize.Height : 1.0d;
            }

            this.textGeometry = null;

            return finalSize;
        }

        protected override Size MeasureOverride(
            Size availableSize)
        {
            this.EnsureFormattedText();

            if (this.formattedText != null)
            {
                this.formattedText.MaxTextWidth = Math.Min(3579139, availableSize.Width);
                this.formattedText.MaxTextHeight = availableSize.Height > 0.0d ? availableSize.Height : 1.0d;
                return new Size(this.formattedText.Width + 1.0d, this.formattedText.Height);
            }
            else
            {
                return new Size();
            }
        }

        protected override void OnRender(
            DrawingContext drawingContext)
        {
            this.EnsureGeometry();

            var thickness = Math.Round(this.StrokeThickness, 1);
            var strokePen = new Pen(this.Stroke, thickness);

            // アウトラインを描画する
            drawingContext.DrawGeometry(
                null,
                strokePen,
                this.textGeometry);

            // テキスト本体を上書きする
            drawingContext.DrawGeometry(
                this.Fill,
                null,
                this.textGeometry);
        }

        private static void OnFormattedTextInvalidated(
            DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs e)
        {
            var sender = (OutlineTextBlock)dependencyObject;
            sender.formattedText = null;
            sender.textGeometry = null;

            sender.InvalidateMeasure();
            sender.InvalidateVisual();
        }

        private static void OnFormattedTextUpdated(
            DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs e)
        {
            var sender = (OutlineTextBlock)dependencyObject;
            sender.UpdateFormattedText();
            sender.textGeometry = null;

            sender.InvalidateMeasure();
            sender.InvalidateVisual();
        }

        private void EnsureGeometry()
        {
            if (this.textGeometry != null)
            {
                return;
            }

            this.EnsureFormattedText();

            if (this.formattedText != null)
            {
                this.textGeometry = this.formattedText.BuildGeometry(new Point(0, 0));
            }
        }

        private void EnsureFormattedText()
        {
            if (this.formattedText != null ||
                this.Text == null)
            {
                return;
            }

            this.formattedText = new FormattedText(
                this.Text,
                CultureInfo.CurrentUICulture,
                this.FlowDirection,
                new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch),
                this.FontSize,
                Brushes.Black,
                new NumberSubstitution(),
                TextFormattingMode.Ideal,
                this.PixelPerDip);

            this.UpdateFormattedText();
        }

        private void UpdateFormattedText()
        {
            if (this.formattedText == null)
            {
                return;
            }

            this.formattedText.MaxLineCount = this.TextWrapping == TextWrapping.NoWrap ? 1 : int.MaxValue;
            this.formattedText.TextAlignment = this.TextAlignment;
            this.formattedText.Trimming = this.TextTrimming;

            this.formattedText.SetFontSize(this.FontSize);
            this.formattedText.SetFontStyle(this.FontStyle);
            this.formattedText.SetFontWeight(this.FontWeight);
            this.formattedText.SetFontFamily(this.FontFamily);
            this.formattedText.SetFontStretch(this.FontStretch);
            this.formattedText.SetTextDecorations(this.TextDecorations);
        }
    }
}
