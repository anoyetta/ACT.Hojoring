using ACT.SpecialSpellTimer.Config;
using ACT.SpecialSpellTimer.Models;
using FFXIV.Framework.Extensions;
using FFXIV.Framework.WPF.Views;
using System;
using System.Windows;
using System.Windows.Media;

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
            this.ToNonActive();
            this.Opacity = 0;

            this.MouseLeftButtonDown += (x, y) => this.DragMove();
            this.Loaded += (x, y) => this.SubscribeZOrderCorrector();
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

            var countAsText = count.CeilingEx(1).ToString("N1");
            var displayTimeAsText = this.Ticker.DisplayTime.ToString("N1");
            countAsText = countAsText.PadLeft(displayTimeAsText.Length, '0');

            var count0AsText = count.CeilingEx().ToString("N0");
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
                this.TickerControl.BarHeight = 0;
            }
            else
            {
                this.TickerControl.BarVisible = true;
                this.TickerControl.BarHeight = 10;
                this.TickerControl.BarBlurRadius = this.Ticker.BarBlurRadius;
            }
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
    }
}
