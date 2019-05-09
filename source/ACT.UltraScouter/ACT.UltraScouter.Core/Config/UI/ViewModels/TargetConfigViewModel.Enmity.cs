using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace ACT.UltraScouter.Config.UI.ViewModels
{
    public partial class TargetConfigViewModel
    {
        private ICommand enmityDisplayTextFontCommand;
        private ICommand enmityDisplayTextColorCommand;
        private ICommand enmityDisplayTextOutlineColorCommand;
        private ICommand enmityBackgroundColorCommand;
        private ICommand enmityNearColorCommand;

        public ICommand EnmityDisplayTextFontCommand =>
            this.enmityDisplayTextFontCommand ??
            (this.enmityDisplayTextFontCommand =
            new ChangeFontCommand((font) => this.Enmity.DisplayText.Font = font));

        public ICommand EnmityDisplayTextColorCommand =>
            this.enmityDisplayTextColorCommand ??
            (this.enmityDisplayTextColorCommand =
            new ChangeColorCommand((color) => this.Enmity.DisplayText.Color = color));

        public ICommand EnmityDisplayTextOutlineColorCommand =>
            this.enmityDisplayTextOutlineColorCommand ??
            (this.enmityDisplayTextOutlineColorCommand =
            new ChangeColorCommand((color) => this.Enmity.DisplayText.OutlineColor = color));

        public ICommand EnmityBackgroundColorCommand =>
            this.enmityBackgroundColorCommand ??
            (this.enmityBackgroundColorCommand =
            new ChangeColorCommand((color) => this.Enmity.Background = color));

        public ICommand EnmityNearColorCommand =>
            this.enmityNearColorCommand ??
            (this.enmityNearColorCommand =
            new ChangeColorCommand((color) => this.Enmity.NearColor = color));

        public IEnumerable<VerticalAlignment> VerticalAlignments => Enum.GetValues(typeof(VerticalAlignment)).Cast<VerticalAlignment>();

        public IEnumerable<HorizontalAlignment> HorizontalAlignments => Enum.GetValues(typeof(HorizontalAlignment)).Cast<HorizontalAlignment>();
    }
}
