using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace FFXIV.Framework.WPF.Controls
{
    /// <summary>
    /// Arc.xaml の相互作用ロジック
    /// </summary>
    public partial class Arc : UserControl
    {
        private static readonly Shape ShapeDefaultValue = new Ellipse();

        #region StartAngle 依存関係プロパティ

        /// <summary>
        /// StartAngle 依存関係プロパティ
        /// </summary>
        public static readonly DependencyProperty StartAngleProperty
            = DependencyProperty.Register(
            nameof(StartAngle),
            typeof(double),
            typeof(Arc),
            new PropertyMetadata(
                0d,
                (s, e) => (s as Arc)?.OnStartAngleChanged()));

        /// <summary>
        /// StartAngle 変更イベントハンドラ
        /// </summary>
        private void OnStartAngleChanged() => this.Render();

        /// <summary>
        /// 始点角度
        /// </summary>
        public double StartAngle
        {
            get => (double)this.GetValue(StartAngleProperty);
            set => this.SetValue(StartAngleProperty, value);
        }

        #endregion StartAngle 依存関係プロパティ

        #region EndAngle 依存関係プロパティ

        /// <summary>
        /// EndAngle 依存関係プロパティ
        /// </summary>
        public static readonly DependencyProperty EndAngleProperty
            = DependencyProperty.Register(
            nameof(EndAngle),
            typeof(double),
            typeof(Arc),
            new PropertyMetadata(
                360d,
                (s, e) => (s as Arc)?.OnEndAngleChanged()));

        /// <summary>
        /// EndAngle 変更イベントハンドラ
        /// </summary>
        private void OnEndAngleChanged() => this.Render();

        /// <summary>
        /// 終点角度
        /// </summary>
        public double EndAngle
        {
            get => (double)this.GetValue(EndAngleProperty);
            set => this.SetValue(EndAngleProperty, value);
        }

        #endregion EndAngle 依存関係プロパティ

        #region Radius 依存関係プロパティ

        /// <summary>
        /// Radius 依存関係プロパティ
        /// </summary>
        public static readonly DependencyProperty RadiusProperty
            = DependencyProperty.Register(
            "Radius",
            typeof(double),
            typeof(Arc),
            new PropertyMetadata(
                100d,
                (s, e) => (s as Arc)?.OnRadiusChanged()));

        /// <summary>
        /// Radius 変更イベントハンドラ
        /// </summary>
        private void OnRadiusChanged() => this.Render();

        /// <summary>
        /// 半径
        /// </summary>
        public double Radius
        {
            get => (double)this.GetValue(RadiusProperty);
            set => this.SetValue(RadiusProperty, value);
        }

        #endregion Radius 依存関係プロパティ

        #region StrokeThickness 依存関係プロパティ

        /// <summary>
        /// StrokeThickness 依存関係プロパティ
        /// </summary>
        public static readonly DependencyProperty StrokeThicknessProperty
            = DependencyProperty.Register(
            nameof(StrokeThickness),
            typeof(double),
            typeof(Arc),
            new PropertyMetadata(
                1d,
                (s, e) => (s as Arc)?.OnStrokeThicknessChanged()));

        /// <summary>
        /// StrokeThickness 変更イベントハンドラ
        /// </summary>
        private void OnStrokeThicknessChanged() =>
            this.Shape.StrokeThickness = this.StrokeThickness;

        /// <summary>
        /// 枠線の太さ
        /// </summary>
        public double StrokeThickness
        {
            get => (double)this.GetValue(StrokeThicknessProperty);
            set => this.SetValue(StrokeThicknessProperty, value);
        }

        #endregion StrokeThickness 依存関係プロパティ

        #region StrokeStartLineCap 依存関係プロパティ

        /// <summary>
        /// StrokeStartLineCap 依存関係プロパティ
        /// </summary>
        public static readonly DependencyProperty StrokeStartLineCapProperty
            = DependencyProperty.Register(
                nameof(StrokeStartLineCap),
                typeof(PenLineCap),
                typeof(Arc),
                new PropertyMetadata(
                    ShapeDefaultValue.StrokeStartLineCap,
                    (s, e) => (s as Arc)?.OnStrokeStartLineCapChanged()));

        /// <summary>
        /// StrokeStartLineCap 変更イベントハンドラ
        /// </summary>
        private void OnStrokeStartLineCapChanged() =>
            this.Shape.StrokeStartLineCap = this.StrokeStartLineCap;

        /// <summary>
        /// StrokeStartLineCap
        /// </summary>
        public PenLineCap StrokeStartLineCap
        {
            get => (PenLineCap)this.GetValue(StrokeStartLineCapProperty);
            set => this.SetValue(StrokeStartLineCapProperty, value);
        }

        #endregion StrokeStartLineCap 依存関係プロパティ

        #region StrokeMiterLimit 依存関係プロパティ

        /// <summary>
        /// StrokeMiterLimit 依存関係プロパティ
        /// </summary>
        public static readonly DependencyProperty StrokeMiterLimitProperty
            = DependencyProperty.Register(
                nameof(StrokeMiterLimit),
                typeof(double),
                typeof(Arc),
                new PropertyMetadata(
                    ShapeDefaultValue.StrokeMiterLimit,
                    (s, e) => (s as Arc)?.OnStrokeMiterLimitChanged()));

        /// <summary>
        /// StrokeMiterLimit 変更イベントハンドラ
        /// </summary>
        private void OnStrokeMiterLimitChanged() =>
            this.Shape.StrokeMiterLimit = this.StrokeMiterLimit;

        /// <summary>
        /// StrokeMiterLimit
        /// </summary>
        public double StrokeMiterLimit
        {
            get => (double)this.GetValue(StrokeMiterLimitProperty);
            set => this.SetValue(StrokeMiterLimitProperty, value);
        }

        #endregion StrokeMiterLimit 依存関係プロパティ

        #region StrokeLineJoin 依存関係プロパティ

        /// <summary>
        /// StrokeLineJoin 依存関係プロパティ
        /// </summary>
        public static readonly DependencyProperty StrokeLineJoinProperty
            = DependencyProperty.Register(
                nameof(StrokeLineJoin),
                typeof(PenLineJoin),
                typeof(Arc),
                new PropertyMetadata(
                    ShapeDefaultValue.StrokeLineJoin,
                    (s, e) => (s as Arc)?.OnStrokeLineJoinChanged()));

        /// <summary>
        /// StrokeLineJoin 変更イベントハンドラ
        /// </summary>
        private void OnStrokeLineJoinChanged() =>
            this.Shape.StrokeLineJoin = this.StrokeLineJoin;

        /// <summary>
        /// StrokeLineJoin
        /// </summary>
        public PenLineJoin StrokeLineJoin
        {
            get => (PenLineJoin)this.GetValue(StrokeLineJoinProperty);
            set => this.SetValue(StrokeLineJoinProperty, value);
        }

        #endregion StrokeLineJoin 依存関係プロパティ

        #region StrokeEndLineCap 依存関係プロパティ

        /// <summary>
        /// StrokeEndLineCap 依存関係プロパティ
        /// </summary>
        public static readonly DependencyProperty StrokeEndLineCapProperty
            = DependencyProperty.Register(
                nameof(StrokeEndLineCap),
                typeof(PenLineCap),
                typeof(Arc),
                new PropertyMetadata(
                    ShapeDefaultValue.StrokeEndLineCap,
                    (s, e) => (s as Arc)?.OnStrokeEndLineCapChanged()));

        /// <summary>
        /// StrokeEndLineCap 変更イベントハンドラ
        /// </summary>
        private void OnStrokeEndLineCapChanged() =>
            this.Shape.StrokeEndLineCap = this.StrokeEndLineCap;

        /// <summary>
        /// StrokeEndLineCap
        /// </summary>
        public PenLineCap StrokeEndLineCap
        {
            get => (PenLineCap)this.GetValue(StrokeEndLineCapProperty);
            set => this.SetValue(StrokeEndLineCapProperty, value);
        }

        #endregion StrokeEndLineCap 依存関係プロパティ

        #region StrokeDashOffset 依存関係プロパティ

        /// <summary>
        /// StrokeDashOffset 依存関係プロパティ
        /// </summary>
        public static readonly DependencyProperty StrokeDashOffsetProperty
            = DependencyProperty.Register(
                nameof(StrokeDashOffset),
                typeof(double),
                typeof(Arc),
                new PropertyMetadata(
                    ShapeDefaultValue.StrokeDashOffset,
                    (s, e) => (s as Arc)?.OnStrokeDashOffsetChanged()));

        /// <summary>
        /// StrokeDashOffset 変更イベントハンドラ
        /// </summary>
        private void OnStrokeDashOffsetChanged() =>
            this.Shape.StrokeDashOffset = this.StrokeDashOffset;

        /// <summary>
        /// StrokeDashOffset
        /// </summary>
        public double StrokeDashOffset
        {
            get => (double)this.GetValue(StrokeDashOffsetProperty);
            set => this.SetValue(StrokeDashOffsetProperty, value);
        }

        #endregion StrokeDashOffset 依存関係プロパティ

        #region StrokeDashCap 依存関係プロパティ

        /// <summary>
        /// StrokeDashCap 依存関係プロパティ
        /// </summary>
        public static readonly DependencyProperty StrokeDashCapProperty
            = DependencyProperty.Register(
                nameof(StrokeDashCap),
                typeof(PenLineCap),
                typeof(Arc),
                new PropertyMetadata(
                    ShapeDefaultValue.StrokeDashCap,
                    (s, e) => (s as Arc)?.OnStrokeDashCapChanged()));

        /// <summary>
        /// StrokeDashCap 変更イベントハンドラ
        /// </summary>
        private void OnStrokeDashCapChanged() =>
            this.Shape.StrokeDashCap = this.StrokeDashCap;

        /// <summary>
        /// StrokeDashCap
        /// </summary>
        public PenLineCap StrokeDashCap
        {
            get => (PenLineCap)this.GetValue(StrokeDashCapProperty);
            set => this.SetValue(StrokeDashCapProperty, value);
        }

        #endregion StrokeDashCap 依存関係プロパティ

        #region StrokeDashArray 依存関係プロパティ

        /// <summary>
        /// StrokeDashArray 依存関係プロパティ
        /// </summary>
        public static readonly DependencyProperty StrokeDashArrayProperty
            = DependencyProperty.Register(
                nameof(StrokeDashArray),
                typeof(DoubleCollection),
                typeof(Arc),
                new PropertyMetadata(
                    ShapeDefaultValue.StrokeDashArray,
                    (s, e) => (s as Arc)?.OnStrokeDashArrayChanged()));

        /// <summary>
        /// StrokeDashArray 変更イベントハンドラ
        /// </summary>
        private void OnStrokeDashArrayChanged() =>
            this.Shape.StrokeDashArray = this.StrokeDashArray;

        /// <summary>
        /// StrokeDashArray
        /// </summary>
        public DoubleCollection StrokeDashArray
        {
            get => (DoubleCollection)this.GetValue(StrokeDashArrayProperty);
            set => this.SetValue(StrokeDashArrayProperty, value);
        }

        #endregion StrokeDashArray 依存関係プロパティ

        #region Stroke 依存関係プロパティ

        /// <summary>
        /// Stroke 依存関係プロパティ
        /// </summary>
        public static readonly DependencyProperty StrokeProperty
            = DependencyProperty.Register(
                nameof(Stroke),
                typeof(Brush),
                typeof(Arc),
                new PropertyMetadata(
                    ShapeDefaultValue.Stroke,
                    (s, e) => (s as Arc)?.OnStrokeChanged()));

        /// <summary>
        /// Stroke 変更イベントハンドラ
        /// </summary>
        private void OnStrokeChanged() =>
            this.Shape.Stroke = this.Stroke;

        /// <summary>
        /// Stroke
        /// </summary>
        public Brush Stroke
        {
            get => (Brush)this.GetValue(StrokeProperty);
            set => this.SetValue(StrokeProperty, value);
        }

        #endregion Stroke 依存関係プロパティ

        #region Fill 依存関係プロパティ

        /// <summary>
        /// Fill 依存関係プロパティ
        /// </summary>
        public static readonly DependencyProperty FillProperty
            = DependencyProperty.Register(
                nameof(Fill),
                typeof(Brush),
                typeof(Arc),
                new PropertyMetadata(
                    ShapeDefaultValue.Fill,
                    (s, e) => (s as Arc)?.OnFillChanged()));

        /// <summary>
        /// Fill 変更イベントハンドラ
        /// </summary>
        private void OnFillChanged() =>
            this.Shape.Fill = this.Fill;

        /// <summary>
        /// Fill
        /// </summary>
        public Brush Fill
        {
            get => (Brush)this.GetValue(FillProperty);
            set => this.SetValue(FillProperty, value);
        }

        #endregion Fill 依存関係プロパティ

        #region GeometryTransform 依存関係プロパティ

        /// <summary>
        /// GeometryTransform 依存関係プロパティ
        /// </summary>
        public static readonly DependencyProperty GeometryTransformProperty
            = DependencyProperty.Register(
                nameof(GeometryTransform),
                typeof(Transform),
                typeof(Arc),
                new PropertyMetadata(
                    ShapeDefaultValue.GeometryTransform,
                    (s, e) => (s as Arc)?.OnGeometryTransformChanged()));

        /// <summary>
        /// GeometryTransform 変更イベントハンドラ
        /// </summary>
        private void OnGeometryTransformChanged() { }

        /// <summary>
        /// GeometryTransform
        /// </summary>
        public Transform GeometryTransform
        {
            get => (Transform)this.GetValue(GeometryTransformProperty);
        }

        #endregion GeometryTransform 依存関係プロパティ

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public Arc()
        {
            this.InitializeComponent();
            this.Shape.Data = this.Geometory;
        }

        private readonly PathGeometry Geometory = new PathGeometry();

        /// <summary>
        /// 描画する
        /// </summary>
        public void Render()
        {
            if (this.Visibility == Visibility.Collapsed)
            {
                return;
            }

            var start = this.StartAngle;
            var end = this.EndAngle;

            if (this.StartAngle > this.EndAngle)
            {
                start = this.EndAngle;
                end = this.StartAngle;
            }

            var figure = new PathFigure();
            figure.StartPoint = this.ComputeAngle(start);

            var size = new Size(this.Radius, this.Radius);

            var diff = end - start;
            if (diff < 0)
            {
                diff += 360;
            }

            // 4分割で描画する
            var step = diff / 4d;
            var degree = start;

            do
            {
                degree += step;
                if (degree > end)
                {
                    degree = end;
                }

                figure.Segments.Add(new ArcSegment()
                {
                    Point = this.ComputeAngle(degree),
                    Size = size,
                    IsLargeArc = false,
                    RotationAngle = 0,
                    SweepDirection = SweepDirection.Clockwise,
                });
            } while (degree < end);

            this.Geometory.Figures.Add(figure);

            if (this.Geometory.Figures.Count > 1)
            {
                this.Geometory.Figures.RemoveAt(0);
            }
        }

        /// <summary>
        /// 角度を XY 座標に変換する
        /// </summary>
        /// <param name="angle">角度</param>
        /// <returns>XY 座標</returns>
        private Point ComputeAngle(double angle)
            => new Point(
                this.Radius + (this.Radius * Math.Cos(angle * Math.PI / 180d)),
                this.Radius + (this.Radius * Math.Sin(angle * Math.PI / 180d)));
    }
}
