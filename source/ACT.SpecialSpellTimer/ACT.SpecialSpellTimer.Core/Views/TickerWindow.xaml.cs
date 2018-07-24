using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using ACT.SpecialSpellTimer.Config;
using ACT.SpecialSpellTimer.Models;
using FFXIV.Framework.Extensions;
using FFXIV.Framework.WPF.Views;

namespace ACT.SpecialSpellTimer.Views
{
    /// <summary>
    /// ワンポイントテロップWindow
    /// </summary>
    public partial class TickerWindow :
        Window,
        IOverlay
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public TickerWindow(
            Ticker ticker)
        {
            this.Ticker = ticker;

            this.InitializeComponent();

            this.MouseLeftButtonDown += (x, y) => this.DragMove();

            this.Opacity = 0;
            this.Topmost = false;
        }

        private bool overlayVisible;

        public bool OverlayVisible
        {
            get => this.overlayVisible;
            set => this.SetOverlayVisible(ref this.overlayVisible, value, Settings.Default.OpacityToView);
        }

        private bool? isClickthrough = null;

        public bool IsClickthrough
        {
            get => this.isClickthrough ?? false;
            set
            {
                if (this.isClickthrough != value)
                {
                    this.isClickthrough = value;

                    if (this.isClickthrough.Value)
                    {
                        this.ToTransparent();
                    }
                    else
                    {
                        this.ToNotTransparent();
                    }
                }
            }
        }

        /// <summary>
        /// 表示するデータソース
        /// </summary>
        public Ticker Ticker { get; set; }

        private SolidColorBrush BackgroundBrush { get; set; }
        private SolidColorBrush BarBrush { get; set; }
        private SolidColorBrush BarOutlineBrush { get; set; }
        private SolidColorBrush FontBrush { get; set; }
        private SolidColorBrush FontOutlineBrush { get; set; }

        /// <summary>
        /// 描画を更新する
        /// </summary>
        public void Refresh()
        {
            if (this.Ticker == null)
            {
                this.HideOverlay();
                return;
            }

            // Brushを生成する
            var fontColor = this.Ticker.FontColor.FromHTML().ToWPF();
            var fontOutlineColor = string.IsNullOrWhiteSpace(this.Ticker.FontOutlineColor) ?
                Colors.Navy :
                this.Ticker.FontOutlineColor.FromHTMLWPF();
            var barColor = fontColor;
            var barOutlineColor = fontOutlineColor;
            var c = this.Ticker.BackgroundColor.FromHTML().ToWPF();
            var backGroundColor = Color.FromArgb(
                (byte)this.Ticker.BackgroundAlpha,
                c.R,
                c.G,
                c.B);

            this.FontBrush = this.GetBrush(fontColor);
            this.FontOutlineBrush = this.GetBrush(fontOutlineColor);
            this.BarBrush = this.GetBrush(barColor);
            this.BarOutlineBrush = this.GetBrush(barOutlineColor);
            this.BackgroundBrush = this.GetBrush(backGroundColor);

            // 背景色を設定する
            var nowbackground = this.BaseColorRectangle.Fill as SolidColorBrush;
            if (nowbackground == null ||
                nowbackground.Color != this.BackgroundBrush.Color)
            {
                this.BaseColorRectangle.Fill = this.BackgroundBrush;
            }

            var forceVisible =
                this.Ticker.IsDesignMode ||
                this.Ticker.IsTest;

            var message = forceVisible ?
                this.Ticker.Message
                    .Replace(",", Environment.NewLine)
                    .Replace("\\n", Environment.NewLine) :
                this.Ticker.MessageReplaced
                    .Replace(",", Environment.NewLine)
                    .Replace("\\n", Environment.NewLine);

            // カウントダウンプレースホルダを置換する
            var count = (
                this.Ticker.MatchDateTime.AddSeconds(Ticker.Delay + Ticker.DisplayTime) -
                DateTime.Now).TotalSeconds;

            if (count < 0.0d)
            {
                count = 0.0d;
            }

            var countAsText = count.ToString("N1");
            var displayTimeAsText = this.Ticker.DisplayTime.ToString("N1");
            countAsText = countAsText.PadLeft(displayTimeAsText.Length, '0');

            var count0AsText = count.ToString("N0");
            var displayTime0AsText = this.Ticker.DisplayTime.ToString("N0");
            count0AsText = count0AsText.PadLeft(displayTime0AsText.Length, '0');

            message = message.Replace("{COUNT}", countAsText);
            message = message.Replace("{COUNT0}", count0AsText);

            // テキストブロックにセットする
            this.TickerControl.Message = message;
            this.TickerControl.Font = this.Ticker.Font;
            this.TickerControl.FontBrush = this.FontBrush;
            this.TickerControl.FontOutlineBrush = this.FontOutlineBrush;

            // プログレスバーを表示しない？
            if (!this.Ticker.ProgressBarEnabled ||
                this.Ticker.DisplayTime <= 0)
            {
                this.TickerControl.BarVisible = false;
            }
            else
            {
                this.TickerControl.BarVisible = true;
            }

            // プログレスバーを初期化する
            this.TickerControl.BarHeight = 10;
        }

        #region Animation

        private DateTime previousMatchDateTime = DateTime.MinValue;

        public void StartProgressBar(
            bool force = false)
        {
            if (this.Ticker == null ||
                !this.Ticker.ProgressBarEnabled)
            {
                this.TickerControl.BarVisible = false;
                return;
            }

            var matchDateTime = this.Ticker.MatchDateTime;

            // 強制アニメーションならば強制マッチ状態にする
            if (force)
            {
                if (matchDateTime <= DateTime.MinValue)
                {
                    matchDateTime = DateTime.Now;
                }
            }

            var timeToHide = matchDateTime.AddSeconds(
                this.Ticker.Delay + this.Ticker.DisplayTime);
            var timeToLive = (timeToHide - DateTime.Now).TotalMilliseconds;

            if (this.previousMatchDateTime != matchDateTime)
            {
                this.TickerControl.StartProgressBar(timeToLive);
            }

            this.previousMatchDateTime = matchDateTime;
        }

        #endregion Animation

        #region フォーカスを奪わない対策

        private const int GWL_EXSTYLE = -20;

        private const int WS_EX_NOACTIVATE = 0x08000000;

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            WindowInteropHelper helper = new WindowInteropHelper(this);
            SetWindowLong(helper.Handle, GWL_EXSTYLE, GetWindowLong(helper.Handle, GWL_EXSTYLE) | WS_EX_NOACTIVATE);
        }

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        #endregion フォーカスを奪わない対策
    }
}
