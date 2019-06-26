using System;
using System.Windows;
using System.Windows.Media.Animation;
using ACT.UltraScouter.Config;
using ACT.UltraScouter.ViewModels;
using FFXIV.Framework.WPF.Controls;
using FFXIV.Framework.WPF.Views;

namespace ACT.UltraScouter.Views
{
    /// <summary>
    /// TargetActionView.xaml の相互作用ロジック
    /// </summary>
    public partial class ActionView :
        Window,
        IOverlay
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public ActionView()
        {
            this.InitializeComponent();

            // アクティブにさせないようにする
            this.ToNonActive();

            this.Loaded += (s, e) => this.SubscribeZOrderCorrector();

            // ドラッグによる移動を設定する
            this.MouseLeftButtonDown += (s, e) => this.DragMove();

            // 初期状態は透明（非表示）にしておく
            this.Opacity = 0;

            // FPSを設定する
            this.SetFPS();
        }

        private int? previousFPS;

        private void SetFPS()
        {
            var currentFPS = Settings.Instance.AnimationMaxFPS > 0 ?
                (int?)Settings.Instance.AnimationMaxFPS :
                null;

            if (this.previousFPS != currentFPS)
            {
                this.previousFPS = currentFPS;
                Storyboard.SetDesiredFrameRate(this.Animation, currentFPS);
                Storyboard.SetDesiredFrameRate(this.LinerAnimation, currentFPS);
            }
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
        public ActionViewModel ViewModel => (ActionViewModel)this.DataContext;

        #region Animation

        private readonly DoubleAnimation Animation = new DoubleAnimation()
        {
            From = 0,
            To = 1,
        };

        private readonly DoubleAnimationUsingKeyFrames LinerAnimation = new DoubleAnimationUsingKeyFrames()
        {
            KeyFrames = new DoubleKeyFrameCollection()
            {
                new LinearDoubleKeyFrame(0.0, KeyTime.FromPercent(0.0)),
                new LinearDoubleKeyFrame(0.1, KeyTime.FromPercent(0.1)),
                new LinearDoubleKeyFrame(0.2, KeyTime.FromPercent(0.2)),
                new LinearDoubleKeyFrame(0.3, KeyTime.FromPercent(0.3)),
                new LinearDoubleKeyFrame(0.4, KeyTime.FromPercent(0.4)),
                new LinearDoubleKeyFrame(0.5, KeyTime.FromPercent(0.5)),
                new LinearDoubleKeyFrame(0.6, KeyTime.FromPercent(0.6)),
                new LinearDoubleKeyFrame(0.7, KeyTime.FromPercent(0.7)),
                new LinearDoubleKeyFrame(0.8, KeyTime.FromPercent(0.8)),
                new LinearDoubleKeyFrame(0.9, KeyTime.FromPercent(0.9)),
                new LinearDoubleKeyFrame(1.0, KeyTime.FromPercent(1.0)),
            }
        };

        public void BeginAnimation(
            double durationFromSecconds)
        {
            this.SetFPS();

            this.LinerAnimation.Duration = TimeSpan.FromSeconds(durationFromSecconds);

            this.Bar.BeginAnimation(
                RichProgressBar.ProgressProperty,
                this.LinerAnimation,
                HandoffBehavior.SnapshotAndReplace);
        }

        #endregion Animation
    }
}
