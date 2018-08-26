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

            // ドラッグによる移動を設定する
            this.MouseLeftButtonDown += (s, e) => this.DragMove();

            // 初期状態は透明（非表示）にしておく
            this.Opacity = 0;

            // FPSを設定する
            Storyboard.SetDesiredFrameRate(this.animation, Settings.Instance.AnimationMaxFPS);
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

        private DoubleAnimation animation = new DoubleAnimation()
        {
            From = 0,
            To = 1,
        };

        public void BeginAnimation(
            double durationFromSecconds)
        {
            this.animation.Duration = TimeSpan.FromSeconds(durationFromSecconds);

            this.Bar.BeginAnimation(
                RichProgressBar.ProgressProperty,
                this.animation,
                HandoffBehavior.SnapshotAndReplace);
        }

        #endregion Animation
    }
}
