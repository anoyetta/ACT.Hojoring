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

            if (WPFHelper.IsDesignMode)
            {
                this.config = new TargetHP();

                this.FontColor = Colors.White;
                this.FontStrokeColor = Colors.Red;
                this.CurrentHPText = "123,456,789 / 123,456,789";
                this.CurrentHPRateText = "(100.0%)";

                this.CurrentHPUpperText = "123,456";
                this.CurrentHPBottomText = " ,789";
                this.MaxHPUpperText = "123,456";
                this.MaxHPBottomText = " ,789";
            }

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

        private string currentHPUpperText;
        private string currentHPBottomText;
        private string maxHPUpperText;
        private string maxHPBottomText;

        private Color fontColor;
        private Color fontStrokeColor;

        public bool HPVisible =>
            WPFHelper.IsDesignMode ?
            true :
            Settings.Instance.HPVisible;

        public bool HPRateVisible =>
            WPFHelper.IsDesignMode ?
            true :
            Settings.Instance.HPRateVisible;

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

        public string CurrentHPUpperText
        {
            get => this.currentHPUpperText;
            set => this.SetProperty(ref this.currentHPUpperText, value);
        }

        public string CurrentHPBottomText
        {
            get => this.currentHPBottomText;
            set => this.SetProperty(ref this.currentHPBottomText, value);
        }

        public string MaxHPUpperText
        {
            get => this.maxHPUpperText;
            set => this.SetProperty(ref this.maxHPUpperText, value);
        }

        public string MaxHPBottomText
        {
            get => this.maxHPBottomText;
            set => this.SetProperty(ref this.maxHPBottomText, value);
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
            if (this.Config.IsHPValueNotCompact)
            {
                this.CurrentHPRateText = $"({(this.Model.CurrentHPRate * 100).CeilingEx(1):N1}%)";
            }

            if (this.Config.IsHPValueCompact)
            {
                this.CurrentHPRateText = $"{(this.Model.CurrentHPRate * 100).CeilingEx(1):N1}";
            }

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
            if (this.Config.IsHPValueNotCompact)
            {
                this.CurrentHPText = $"{this.Model.CurrentHP:N0} / {this.Model.MaxHP:N0}";
            }

            if (this.Config.IsHPValueCompact)
            {
                var hp = HPViewModel.FormatHPText(this.Model.CurrentHP);
                this.CurrentHPUpperText = hp.UpperPart;
                this.CurrentHPBottomText = hp.BottomPart;

                hp = HPViewModel.FormatHPText(this.Model.MaxHP);
                this.MaxHPUpperText = hp.UpperPart;
                this.MaxHPBottomText = hp.BottomPart;
            }

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

        public static (string UpperPart, string BottomPart) FormatHPText(
            double hpValue)
        {
            var result = default((string UpperPart, string BottomPart));

            var hp = (long)hpValue;
            if (hp < 10000)
            {
                result.UpperPart = hp.ToString("N0");
                result.BottomPart = string.Empty;
            }
            else
            {
                result.UpperPart = (hp / 1000).ToString("N0");
                result.BottomPart = " ," + (hp % 1000).ToString("000");
            }

            return result;
        }
    }
}
