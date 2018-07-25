using System.ComponentModel;
using System.Windows.Media;
using ACT.UltraScouter.Config;
using ACT.UltraScouter.Models;
using ACT.UltraScouter.ViewModels.Bases;
using FFXIV.Framework.Common;
using FFXIV.Framework.Extensions;

namespace ACT.UltraScouter.ViewModels
{
    public class HPViewModel :
        OverlayViewModelBase,
        IOverlayViewModel
    {
        public HPViewModel() : this(null, null)
        {
        }

        public HPViewModel(
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

        public bool OverlayVisible =>
            this.Config.Visible && !this.Config.IsHPValueOnHPBar;

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
                        break;
                }
            }
        }

        private void UpdateCurrentHPRateText()
        {
            this.CurrentHPRateText = $"({(this.Model.CurrentHPRate * 100).CeilingEx(1):N1}%)";

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
    }
}
