using System;
using System.Windows.Input;
using System.Windows.Media;
using ACT.SpecialSpellTimer.Config.Views;
using FFXIV.Framework.Common;
using FFXIV.Framework.Dialog;
using FFXIV.Framework.Extensions;
using Prism.Commands;

namespace ACT.SpecialSpellTimer.Config.ViewModels
{
    public partial class TickerConfigViewModel
    {
        #region Change Font

        private ICommand CreateChangeFontCommand(
            Func<FontInfo> getCurrentoFont,
            Action<FontInfo> changeFont)
            => new DelegateCommand(() =>
            {
                var result = FontDialogWrapper.ShowDialog(getCurrentoFont());
                if (result.Result)
                {
                    changeFont.Invoke(result.Font);
                }
            });

        private ICommand changeFontCommand;

        public ICommand ChangeFontCommand =>
            this.changeFontCommand ?? (this.changeFontCommand = this.CreateChangeFontCommand(
                () => this.Model.Font,
                (font) =>
                {
                    this.Model.Font.FontFamily = font.FontFamily;
                    this.Model.Font.Size = font.Size;
                    this.Model.Font.Style = font.Style;
                    this.Model.Font.Weight = font.Weight;
                    this.Model.Font.Stretch = font.Stretch;
                    this.Model.Font.RaisePropertyChanged(nameof(this.Model.Font.DisplayText));
                }));

        #endregion Change Font

        #region Change Colors

        private ICommand CreateChangeColorCommand(
            Func<string> getCurrentColor,
            Action<string> changeColorAction)
            => new DelegateCommand(() =>
            {
                var result = ColorDialogWrapper.ShowDialog(getCurrentColor().FromHTML(), true);
                if (result.Result)
                {
                    changeColorAction.Invoke(result.LegacyColor.ToHTML());
                }
            });

        private ICommand CreateChangeAlphaColorCommand(
            Func<string> getCurrentColor,
            Func<int> getCurrentAlpha,
            Action<string, int> changeColorAction)
            => new DelegateCommand(() =>
            {
                var baseColor = getCurrentColor().FromHTML();
                var color = Color.FromArgb(
                    (byte)getCurrentAlpha(),
                    baseColor.R,
                    baseColor.G,
                    baseColor.B);

                var result = ColorDialogWrapper.ShowDialog(color, false);
                if (result.Result)
                {
                    changeColorAction.Invoke(
                        result.LegacyColor.ToHTML(),
                        (int)result.Color.A);
                }
            });

        private ICommand changeFontColorCommand;

        public ICommand ChangeFontColorCommand =>
            this.changeFontColorCommand ?? (this.changeFontColorCommand = this.CreateChangeColorCommand(
                () => this.Model.FontColor,
                (color) => this.Model.FontColor = color));

        private ICommand changeFontOutlineColorCommand;

        public ICommand ChangeFontOutlineColorCommand =>
            this.changeFontOutlineColorCommand ?? (this.changeFontOutlineColorCommand = this.CreateChangeColorCommand(
                () => this.Model.FontOutlineColor,
                (color) => this.Model.FontOutlineColor = color));

        private ICommand changeBackgroundColorCommand;

        public ICommand ChangeBackgroundColorCommand =>
            this.changeBackgroundColorCommand ?? (this.changeBackgroundColorCommand = this.CreateChangeAlphaColorCommand(
                () => this.Model.BackgroundColor,
                () => this.Model.BackgroundAlpha,
                (color, alpha) =>
                {
                    this.Model.BackgroundColor = color;
                    this.Model.BackgroundAlpha = alpha;
                }));

        #endregion Change Colors

        private ICommand copyConfigCommand;

        public ICommand CopyConfigCommand =>
            this.copyConfigCommand ?? (this.copyConfigCommand = new DelegateCommand(() =>
            {
                var view = new CopyConfigView(this.Model);
                view.Show();
            }));
    }
}
