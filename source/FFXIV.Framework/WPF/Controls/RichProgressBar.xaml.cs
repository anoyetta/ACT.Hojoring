using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FFXIV.Framework.Extensions;

namespace FFXIV.Framework.WPF.Controls
{
    /// <summary>
    /// ProgressBar.xaml の相互作用ロジック
    /// </summary>
    public partial class RichProgressBar :
        UserControl,
        INotifyPropertyChanged
    {
        public RichProgressBar()
        {
            this.InitializeComponent();
            this.Render();
        }

        #region Fill 依存関係プロパティ

        /// <summary>
        /// Fill 依存関係プロパティ
        /// </summary>
        public static readonly DependencyProperty FillProperty
            = DependencyProperty.Register(
            nameof(Fill),
            typeof(Brush),
            typeof(RichProgressBar),
            new PropertyMetadata(
                Brushes.White,
                (s, e) => (s as RichProgressBar)?.Render()));

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
            typeof(RichProgressBar),
            new PropertyMetadata(
                Brushes.Gray,
                (s, e) => (s as RichProgressBar)?.Render()));

        /// <summary>
        /// Stroke
        /// </summary>
        public Brush Stroke
        {
            get => (Brush)this.GetValue(StrokeProperty);
            set => this.SetValue(StrokeProperty, value);
        }

        #endregion Stroke 依存関係プロパティ

        #region Progress 依存関係プロパティ

        /// <summary>
        /// Progress 依存関係プロパティ
        /// </summary>
        public static readonly DependencyProperty ProgressProperty
            = DependencyProperty.Register(
            nameof(Progress),
            typeof(double),
            typeof(RichProgressBar),
            new PropertyMetadata(
                0d,
                (s, e) => (s as RichProgressBar)?.Render()));

        /// <summary>
        /// Progress
        /// </summary>
        public double Progress
        {
            get => (double)this.GetValue(ProgressProperty);
            set
            {
                var progress = value;
                if (progress < 0d)
                {
                    progress = 0d;
                }

                if (progress > 1.0d)
                {
                    progress = 1.0d;
                }

                this.SetValue(ProgressProperty, progress);
            }
        }

        #endregion Progress 依存関係プロパティ

        #region IsReverse 依存関係プロパティ

        /// <summary>
        /// IsReverse 依存関係プロパティ
        /// </summary>
        public static readonly DependencyProperty IsReverseProperty
            = DependencyProperty.Register(
            nameof(IsReverse),
            typeof(bool),
            typeof(RichProgressBar),
            new PropertyMetadata(
                false,
                (s, e) => (s as RichProgressBar)?.Render()));

        /// <summary>
        /// IsReverse バーの進行方向を逆にするか？
        /// </summary>
        public bool IsReverse
        {
            get => (bool)this.GetValue(IsReverseProperty);
            set => this.SetValue(IsReverseProperty, value);
        }

        #endregion IsReverse 依存関係プロパティ

        #region IsDarkBackground 依存関係プロパティ

        /// <summary>
        /// IsDarkBackground 依存関係プロパティ
        /// </summary>
        public static readonly DependencyProperty IsDarkBackgroundProperty
            = DependencyProperty.Register(
            nameof(IsDarkBackground),
            typeof(bool),
            typeof(RichProgressBar),
            new PropertyMetadata(
                true,
                (s, e) => (s as RichProgressBar)?.Render()));

        /// <summary>
        /// IsDarkBackground バーの背景を暗色にするか？
        /// </summary>
        public bool IsDarkBackground
        {
            get => (bool)this.GetValue(IsDarkBackgroundProperty);
            set => this.SetValue(IsDarkBackgroundProperty, value);
        }

        #endregion IsDarkBackground 依存関係プロパティ

        #region IsStrokeBackground 依存関係プロパティ

        /// <summary>
        /// IsStrokeBackground 依存関係プロパティ
        /// </summary>
        public static readonly DependencyProperty IsStrokeBackgroundProperty
            = DependencyProperty.Register(
            nameof(IsStrokeBackground),
            typeof(bool),
            typeof(RichProgressBar),
            new PropertyMetadata(
                true,
                (s, e) => (s as RichProgressBar)?.Render()));

        /// <summary>
        /// IsStrokeBackground Strokeを背景に対して設定するか？
        /// </summary>
        public bool IsStrokeBackground
        {
            get => (bool)this.GetValue(IsStrokeBackgroundProperty);
            set => this.SetValue(IsStrokeBackgroundProperty, value);
        }

        #endregion IsStrokeBackground 依存関係プロパティ

        #region BeginProgress ルーティングイベント

        public static readonly RoutedEvent BeginProgressEvent = EventManager.RegisterRoutedEvent(
            nameof(RichProgressBar.BeginProgress),
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(RichProgressBar));

        public event RoutedEventHandler BeginProgress
        {
            add => AddHandler(BeginProgressEvent, value);
            remove => RemoveHandler(BeginProgressEvent, value);
        }

        public void RaiseBeginProgress(
            Duration duration)
        {
            if (this.ActualHeight == 0)
            {
                return;
            }

            var args = new BeginProgressEventArgs(
                RichProgressBar.BeginProgressEvent,
                duration);

            RaiseEvent(args);
        }

        #endregion BeginProgress ルーティングイベント

        #region BlurRadius 依存関係プロパティ

        /// <summary>
        /// BlurRadius 依存関係プロパティ
        /// </summary>
        public static readonly DependencyProperty BlurRadiusProperty
            = DependencyProperty.Register(
            nameof(BlurRadius),
            typeof(double),
            typeof(RichProgressBar),
            new PropertyMetadata(
                12d,
                (x, y) => (x as RichProgressBar)?.Render()));

        /// <summary>
        /// BlurRadius
        /// </summary>
        public double BlurRadius
        {
            get => (double)this.GetValue(BlurRadiusProperty);
            set => this.SetValue(BlurRadiusProperty, value);
        }

        #endregion BlurRadius 依存関係プロパティ

        #region BlurOpacity 依存関係プロパティ

        /// <summary>
        /// BlurOpacity 依存関係プロパティ
        /// </summary>
        public static readonly DependencyProperty BlurOpacityProperty
            = DependencyProperty.Register(
            nameof(BlurOpacity),
            typeof(double),
            typeof(RichProgressBar),
            new PropertyMetadata(
                0.95d,
                (x, y) => (x as RichProgressBar)?.Render()));

        /// <summary>
        /// BlurOpacity
        /// </summary>
        public double BlurOpacity
        {
            get => (double)this.GetValue(BlurOpacityProperty);
            set => this.SetValue(BlurOpacityProperty, value);
        }

        #endregion BlurOpacity 依存関係プロパティ

        /// <summary>
        /// 暗くする倍率
        /// </summary>
        public const double ToDarkRatio = 0.35d;

        /// <summary>
        /// 明るくする倍率
        /// </summary>
        public const double ToLightRatio = 10.00d;

        /// <summary>
        /// 描画する
        /// </summary>
        private void Render()
        {
            // エフェクトを設定する
            this.BarEffect.BlurRadius = this.BlurRadius;
            this.BarEffect.Opacity = this.BlurOpacity;

            // 背景色と前景色を設定する
            if (this.Fill is SolidColorBrush fill)
            {
                if (this.ForeBar.Fill != fill)
                {
                    this.BackBar.Fill =
                        this.IsDarkBackground ?
                        fill.Color.ChangeBrightness(ToDarkRatio).ToBrush() :
                        fill.Color.ChangeBrightness(ToLightRatio).ToBrush();

                    this.ForeBar.Fill = fill;
                }
            }
            else
            {
                this.BackBar.Fill = Brushes.Black;
                this.ForeBar.Fill = this.Fill;
            }

            var progress = this.Progress;

            if (this.Visibility == Visibility.Collapsed ||
                this.Visibility == Visibility.Hidden ||
                this.ActualHeight == 0)
            {
                progress = 0;
            }

            // バーの幅を設定する
            this.ForeBarScale.ScaleX = progress;

            // 反転？
            this.BaseRotate.Angle = this.IsReverse ? -180 : 0;

            // 枠の描画を決定する
            if (this.IsStrokeBackground)
            {
                this.ForeBar.StrokeThickness = 0;
                this.StrokeBar.Visibility = Visibility.Visible;
                this.StrokeBar.Width = this.ActualWidth;
                this.StrokeBar.Stroke = this.Stroke;
            }
            else
            {
                this.ForeBar.StrokeThickness = 1;
                this.StrokeBar.Visibility = Visibility.Collapsed;
                this.ForeBar.Stroke = this.Stroke;
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

    public class BeginProgressEventArgs : RoutedEventArgs
    {
        public BeginProgressEventArgs(
            RoutedEvent routedEvent,
            Duration duration) : base(routedEvent)
            => this.Duration = duration;

        public Duration Duration
        {
            get;
            private set;
        } = new Duration(TimeSpan.Zero);
    }
}
