using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using ACT.UltraScouter.Config;
using ACT.UltraScouter.Models;
using ACT.UltraScouter.ViewModels;
using FFXIV.Framework.WPF.Controls;
using FFXIV.Framework.WPF.Views;

namespace ACT.UltraScouter.Views
{
    /// <summary>
    /// MPTickerView.xaml の相互作用ロジック
    /// </summary>
    public partial class MPTickerView :
        Window,
        IOverlay
    {
        public MPTickerView()
        {
            this.InitializeComponent();

            // アクティブにさせないようにする
            this.ToNonActive();
            this.Loaded += (s, e) => this.SubscribeZOrderCorrector();

            // ドラッグによる移動を設定する
            this.MouseLeftButtonDown += (s, e) => this.DragMove();

            // 初期状態は透明（非表示）にしておく
            this.Opacity = 0;

            // FPSを制限する
            var fps = Settings.Instance.AnimationMaxFPS > 0 ?
                (int?)Settings.Instance.AnimationMaxFPS :
                null;
            Timeline.SetDesiredFrameRate(this.CountdownAnimation, fps);
            Timeline.SetDesiredFrameRate(this.BarAnimation, fps);
        }

        /// <summary>オーバーレイとして表示状態</summary>
        private bool overlayVisible;

        /// <summary>
        /// オーバーレイとして表示状態
        /// </summary>
        public bool OverlayVisible
        {
            get => this.overlayVisible;
            set => this.SetOverlayVisible(ref this.overlayVisible, value, Settings.Instance.Opacity);
        }

        /// <summary>ViewModel</summary>
        public MPTickerViewModel ViewModel => (MPTickerViewModel)this.DataContext;

        /// <summary>
        /// アニメーション用のMP回復間隔（TimeSpan型）
        /// </summary>
        private static readonly TimeSpan MPRecoverTimeSpan =
            TimeSpan.FromSeconds(MeInfoModel.Constants.MPRecoverySpan);

        /// <summary>
        /// アニメーション用のMP回復間隔（KeyTime型）
        /// </summary>
        private static readonly KeyTime MPRecoverKeyTime =
            KeyTime.FromTimeSpan(TimeSpan.FromSeconds(MeInfoModel.Constants.MPRecoverySpan));

        public void BeginAnimation()
        {
            this.BeginBarAnimation();
            this.BeginCountdownAnimation();
        }

        #region Countdown Animation

        private readonly DoubleAnimation CountdownAnimation = new DoubleAnimation()
        {
            From = 3,
            To = 0,
            Duration = MPRecoverTimeSpan,
            AutoReverse = false,
            RepeatBehavior = RepeatBehavior.Forever,
        };

        private void BeginCountdownAnimation()
        {
            this.CounterLabel.BeginAnimation(
                Label.WidthProperty,
                this.CountdownAnimation,
                HandoffBehavior.SnapshotAndReplace);
        }

        #endregion Countdown Animation

        #region Bar Animation

        private readonly DoubleAnimation BarAnimation = new DoubleAnimation()
        {
            From = 0,
            To = 1,
            Duration = MPRecoverTimeSpan,
            AutoReverse = false,
            RepeatBehavior = RepeatBehavior.Forever,
        };

        private void BeginBarAnimation()
        {
            this.Bar.BeginAnimation(
                RichProgressBar.ProgressProperty,
                this.BarAnimation,
                HandoffBehavior.SnapshotAndReplace);
        }

        #endregion Bar Animation
    }
}
