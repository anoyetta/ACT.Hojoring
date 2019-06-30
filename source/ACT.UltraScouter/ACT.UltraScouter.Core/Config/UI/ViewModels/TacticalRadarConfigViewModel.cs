using System;
using System.Linq;
using System.Windows.Input;
using FFXIV.Framework.Common;
using NLog;
using Prism.Mvvm;

namespace ACT.UltraScouter.Config.UI.ViewModels
{
    public class TacticalRadarConfigViewModel :
        BindableBase
    {
        #region Logger

        private Logger logger = AppLog.DefaultLogger;

        #endregion Logger

        public TacticalRadar TacticalRadar => Settings.Instance.TacticalRadar;

        public DirectionOrigin[] DirectionOrigins =>
            (Enum.GetValues(typeof(DirectionOrigin)) as DirectionOrigin[])
            .Where(x => x != DirectionOrigin.Camera)
            .ToArray();

        private ICommand changeFontCommand;
        private ICommand changeTextColorCommand;
        private ICommand changeTextOutlineColorCommand;
        private ICommand changeBackgroundColorCommand;

        public ICommand ChangeFontCommand =>
            this.changeFontCommand ??
            (this.changeFontCommand =
            new ChangeFontCommand(
                (font) => this.TacticalRadar.DisplayText.Font = font));

        public ICommand ChangeTextColorCommand =>
            this.changeTextColorCommand ??
            (this.changeTextColorCommand =
            new ChangeColorCommand(
                (color) => this.TacticalRadar.DisplayText.Color = color));

        public ICommand ChangeTextOutlineColorCommand =>
            this.changeTextOutlineColorCommand ??
            (this.changeTextOutlineColorCommand =
            new ChangeColorCommand(
                (color) => this.TacticalRadar.DisplayText.OutlineColor = color));

        public ICommand ChangeBackgroundColorCommand =>
            this.changeBackgroundColorCommand ??
            (this.changeBackgroundColorCommand =
            new ChangeColorCommand(
                (color) => this.TacticalRadar.Background = color));
    }
}
