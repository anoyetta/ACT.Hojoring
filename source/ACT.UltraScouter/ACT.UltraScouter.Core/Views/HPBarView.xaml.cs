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
    public partial class HPBarView :
        Window,
        IOverlay
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public HPBarView()
        {
            this.InitializeComponent();

            // アクティブにさせないようにする
            this.ToNonActive();
            this.Loaded += (s, e) => this.SubscribeZOrderCorrector();

            // ドラッグによる移動を設定する
            this.MouseLeftButtonDown += (s, e) => this.DragMove();

            // 初期状態は透明（非表示）にしておく
            this.Opacity = 0;

            // アニメーションのFPSを制限する
            Storyboard.SetDesiredFrameRate(
                this.Animation,
                Settings.Instance.AnimationMaxFPS > 0 ?
                (int?)Settings.Instance.AnimationMaxFPS :
                null);
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
        public HPBarViewModel ViewModel => (HPBarViewModel)this.DataContext;

        private readonly DoubleAnimation Animation = new DoubleAnimation();

        public void UpdateHPBar(
            double hpRate)
        {
            this.Animation.From = this.Bar.Progress;
            this.Animation.To = hpRate;
            this.Animation.Duration = TimeSpan.FromSeconds(0.08);

            this.Bar.BeginAnimation(
                RichProgressBar.ProgressProperty,
                this.Animation,
                HandoffBehavior.SnapshotAndReplace);
        }
    }
}
