using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;

namespace ACT.UltraScouter.Config.UI.ViewModels
{
    public class MyMarkerConfigViewModel :
        BindableBase
    {
        public MyMarker MyMarker => Settings.Instance.MyMarker;

        private ICommand displayTextFontCommand;

        public ICommand DisplayTextFontCommand =>
            this.displayTextFontCommand ??
            (this.displayTextFontCommand =
            new ChangeFontCommand(
                (font) => this.MyMarker.DisplayText.Font = font));

        private ICommand displayTextColorCommand;

        public ICommand DisplayTextColorCommand =>
            this.displayTextColorCommand ??
            (this.displayTextColorCommand =
            new ChangeColorCommand(
                (color) => this.MyMarker.DisplayText.Color = color));

        private ICommand displayTextOutlineColorCommand;

        public ICommand DisplayTextOutlineColorCommand =>
            this.displayTextOutlineColorCommand ??
            (this.displayTextOutlineColorCommand =
            new ChangeColorCommand(
                (color) => this.MyMarker.DisplayText.OutlineColor = color));

        private DelegateCommand toCenterCommand;

        public DelegateCommand ToCenterCommand =>
            this.toCenterCommand ?? (this.toCenterCommand = new DelegateCommand(this.ExecuteToCenterCommand));

        private void ExecuteToCenterCommand()
        {
            var overlaySize = this.MyMarker.GetOverlaySizeCallback?.Invoke();
            if (overlaySize == null)
            {
                return;
            }

            this.MyMarker.Location.X = (SystemParameters.PrimaryScreenWidth / 2) - (overlaySize.Value.Width / 2);
            this.MyMarker.Location.Y = (SystemParameters.PrimaryScreenHeight / 2) - (overlaySize.Value.Height / 2);
        }

        public IEnumerable<MyMarkerTypes> MarkerTypes => Enum.GetValues(typeof(MyMarkerTypes)).Cast<MyMarkerTypes>();
    }
}
