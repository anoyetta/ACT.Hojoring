using System;
using System.ComponentModel;
using System.Windows.Media;
using ACT.UltraScouter.Config;
using ACT.UltraScouter.Models;
using ACT.UltraScouter.ViewModels.Bases;
using ACT.UltraScouter.Views;
using FFXIV.Framework.Common;
using FFXIV.Framework.Extensions;

namespace ACT.UltraScouter.ViewModels
{
    public class HPBarViewModel :
        OverlayViewModelBase,
        IOverlayViewModel
    {
        public HPBarViewModel() : this(null, null)
        {
        }

        public HPBarViewModel(
            TargetHP config,
            TargetInfoModel model)
        {
            this.config = config ?? Settings.Instance.TargetHP;
            this.model = model ?? TargetInfoModel.Instance;

            this.Initialize();
        }

        public override void Initialize()
        {
            this.Model.PropertyChanged -= this.Model_PropertyChanged;
            this.Model.PropertyChanged += this.Model_PropertyChanged;
        }

        public override void Dispose()
        {
            this.Model.PropertyChanged -= this.Model_PropertyChanged;
            base.Dispose();
        }

        private TargetHP config;
        private TargetInfoModel model;

        public virtual TargetHP Config => this.config;

        public virtual TargetInfoModel Model => this.model;

        private Color progressBarBackColor;
        private Color progressBarEffectColor;
        private Color progressBarForeColor;
        private Color progressBarOutlineColor;
        private double progress;

        public double CanvasHeight =>
            this.Config.HPBarVisible ?
            this.Config.ProgressBar.Height + (11 * 2) :
            0;

        public double CanvasWidth =>
            this.Config.HPBarVisible ?
            this.Config.ProgressBar.Width + (11 * 2) :
            0;

        public bool OverlayVisible => this.Config.Visible;

        public Color ProgressBarBackColor
        {
            get => this.progressBarBackColor;
            set => this.SetProperty(ref this.progressBarBackColor, value);
        }

        public Color ProgressBarEffectColor
        {
            get => this.progressBarEffectColor;
            set => this.SetProperty(ref this.progressBarEffectColor, value);
        }

        public Color ProgressBarForeColor
        {
            get => this.progressBarForeColor;
            set => this.SetProperty(ref this.progressBarForeColor, value);
        }

        public Color ProgressBarOutlineColor
        {
            get => this.progressBarOutlineColor;
            set => this.SetProperty(ref this.progressBarOutlineColor, value);
        }

        public double Progress
        {
            get => this.progress;
            private set => this.SetProperty(ref this.progress, value);
        }

        private void Model_PropertyChanged(
            object sender,
            PropertyChangedEventArgs e)
        {
            if (this.View != null)
            {
                // HPの変化？
                switch (e.PropertyName)
                {
                    case nameof(this.Model.MaxHP):
                    case nameof(this.Model.CurrentHP):
                        WPFHelper.BeginInvoke(this.UpdateCurrentHPText);
                        break;

                    case nameof(this.Model.CurrentHPRate):
                        WPFHelper.BeginInvoke(this.UpdateCurrentHPRateText);
                        WPFHelper.BeginInvoke(this.UpdateHPBar);
                        break;
                }
            }
        }

        private DateTime lastHPBarUpdateDateTime;

        private void UpdateHPBar()
        {
            // プログレスバーのカラーを取得する
            var color = this.Config.ProgressBar.AvailableColor(
                this.Model.CurrentHPRate * 100.0d);

            this.ProgressBarForeColor = color;
            this.ProgressBarBackColor = color.ChangeBrightness(Settings.Instance.ProgressBarDarkRatio);
            this.ProgressBarEffectColor = color.ChangeBrightness(Settings.Instance.ProgressBarEffectRatio);

            // プログレスバーのアウトラインも連動するか？
            this.ProgressBarOutlineColor = this.Config.ProgressBar.LinkOutlineColor ?
                color :
                this.Config.ProgressBar.OutlineColor;

            // HPバーの進捗率を更新する
            if ((DateTime.Now - this.lastHPBarUpdateDateTime).TotalSeconds >= 0.1d)
            {
                var view = this.View as HPBarView;
                if (view != null)
                {
                    this.lastHPBarUpdateDateTime = DateTime.Now;

                    // HPバーを描画する
                    view.UpdateHPBar(this.Model.CurrentHPRate);

                    // Topmostを設定し直す
                    view.Topmost = false;
                    view.Topmost = true;
                }
            }
        }

        #region HP Text

        private string currentHPRateText;
        private string currentHPText;
        private Color fontColor;
        private Color fontStrokeColor;

        public bool HPVisible => Settings.Instance.HPVisible;
        public bool HPRateVisible => Settings.Instance.HPRateVisible;

        public string CurrentHPRateText
        {
            get => this.currentHPRateText;
            set => this.SetProperty(ref this.currentHPRateText, value);
        }

        public string CurrentHPText
        {
            get => this.currentHPText;
            set => this.SetProperty(ref this.currentHPText, value);
        }

        public Color FontColor
        {
            get => this.fontColor;
            set => this.SetProperty(ref this.fontColor, value);
        }

        public Color FontStrokeColor
        {
            get => this.fontStrokeColor;
            set => this.SetProperty(ref this.fontStrokeColor, value);
        }

        private void UpdateCurrentHPRateText()
        {
            var rate = this.Model.CurrentHPRate * 100;
            rate = Math.Ceiling(rate * 10) / 10;
            if (rate > 100.0)
            {
                rate = 100.0;
            }

            this.CurrentHPRateText = $"({rate:N1}%)";

            // プログレスバーのカラーを取得する
            var color = this.Config.ProgressBar.AvailableColor(
                this.Model.CurrentHPRate * 100.0d);

            // フォントカラーも連動させるか？
            this.FontColor = this.Config.LinkFontColorToBarColor ?
                color :
                this.Config.DisplayText.Color;

            // フォントのアウトラインカラーも連動させるか？
            this.FontStrokeColor = this.Config.LinkFontOutlineColorToBarColor ?
                color :
                this.Config.DisplayText.OutlineColor;
        }

        private void UpdateCurrentHPText()
        {
            this.CurrentHPText = $"{this.Model.CurrentHP:N0} / {this.Model.MaxHP:N0}";

            // プログレスバーのカラーを取得する
            var color = this.Config.ProgressBar.AvailableColor(
                this.Model.CurrentHPRate * 100.0d);

            // フォントカラーも連動させるか？
            this.FontColor = this.Config.LinkFontColorToBarColor ?
                color :
                this.Config.DisplayText.Color;

            // フォントのアウトラインカラーも連動させるか？
            this.FontStrokeColor = this.Config.LinkFontOutlineColorToBarColor ?
                color :
                this.Config.DisplayText.OutlineColor;
        }

        #endregion HP Text
    }
}
