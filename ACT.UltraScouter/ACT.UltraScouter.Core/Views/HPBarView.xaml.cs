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

            // ドラッグによる移動を設定する
            this.MouseLeftButtonDown += (s, e) => this.DragMove();

            // 初期状態は透明（非表示）にしておく
            this.Opacity = 0;
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

        private DoubleAnimation animation = new DoubleAnimation();

        public void UpdateHPBar(
            double hpRate)
        {
            this.animation.From = this.Bar.Progress;
            this.animation.To = hpRate;
            this.animation.Duration = TimeSpan.FromSeconds(0.08);

            Timeline.SetDesiredFrameRate(this.animation, Settings.Instance.AnimationMaxFPS);

            this.Bar.BeginAnimation(
                RichProgressBar.ProgressProperty,
                this.animation);
        }
    }
}
