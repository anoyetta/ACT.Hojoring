using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ACT.SpecialSpellTimer.Config.Views;
using ACT.SpecialSpellTimer.Image;
using FFXIV.Framework.Common;
using FFXIV.Framework.Dialog;
using FFXIV.Framework.Extensions;
using FFXIV.Framework.WPF.Views;
using FFXIV.Framework.xivapi;
using Prism.Commands;

namespace ACT.SpecialSpellTimer.Config.ViewModels
{
    public partial class SpellConfigViewModel
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

        private ICommand changeWarningFontColorCommand;

        public ICommand ChangeWarningFontColorCommand =>
            this.changeWarningFontColorCommand ?? (this.changeWarningFontColorCommand = this.CreateChangeColorCommand(
                () => this.Model.WarningFontColor,
                (color) => this.Model.WarningFontColor = color));

        private ICommand changeWarningFontOutlineColorCommand;

        public ICommand ChangeWarningFontOutlineColorCommand =>
            this.changeWarningFontOutlineColorCommand ?? (this.changeWarningFontOutlineColorCommand = this.CreateChangeColorCommand(
                () => this.Model.WarningFontOutlineColor,
                (color) => this.Model.WarningFontOutlineColor = color));

        private ICommand changeBarColorCommand;

        public ICommand ChangeBarColorCommand =>
            this.changeBarColorCommand ?? (this.changeBarColorCommand = this.CreateChangeColorCommand(
                () => this.Model.BarColor,
                (color) => this.Model.BarColor = color));

        private ICommand changeBarOutlineColorCommand;

        public ICommand ChangeBarOutlineColorCommand =>
            this.changeBarOutlineColorCommand ?? (this.changeBarOutlineColorCommand = this.CreateChangeColorCommand(
                () => this.Model.BarOutlineColor,
                (color) => this.Model.BarOutlineColor = color));

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

        public IEnumerable<VerticalAlignment> TitleVerticalAlignments => Enum.GetValues(typeof(VerticalAlignment))
            .Cast<VerticalAlignment>()
            .Where(x => x != VerticalAlignment.Stretch);

        private ICommand selectIconCommand;

        public ICommand SelectIconCommand =>
            this.selectIconCommand ?? (this.selectIconCommand = new DelegateCommand(() =>
            {
                var view = new IconBrowserView();

                view.SelectedIconName = this.Model?.SpellIcon;

                if (view.ShowDialog() ?? false)
                {
                    this.Model.SpellIcon = view.SelectedIconName;
                }
            }));

        private ICommand copyConfigCommand;

        public ICommand CopyConfigCommand =>
            this.copyConfigCommand ?? (this.copyConfigCommand = new DelegateCommand(() =>
            {
                var view = new CopyConfigView(this.Model);
                view.Show();
            }));

        private ICommand getIconsCommand;

        public ICommand GetIconsCommand =>
            this.getIconsCommand ?? (this.getIconsCommand = new DelegateCommand(async () =>
            {
                try
                {
                    this.InProgress = true;

                    // APIクライアントにロケールを設定する
                    XIVAPIController.Instance.Language = Settings.Default.UILocale;

                    this.MaxValue = 100;
                    this.CurrentValue = 0;
                    this.ProgressMessage = $"Downloading Actions...";

                    var actionList = await XIVAPIController.Instance.GetActionsAsync(async (e) =>
                    {
                        await WPFHelper.InvokeAsync(() =>
                        {
                            this.MaxValue = e.Max;
                            this.CurrentValue = e.Current;
                            this.ProgressMessage = $"Downloading Actions...\n{e.Current} / {e.Max}";
                        });
                    });

                    this.MaxValue = 100;
                    this.CurrentValue = 0;
                    this.ProgressMessage = $"Downloading Action Icons...";

                    IconController.Instance.DisposeIcon();

                    await XIVAPIController.Instance.DownloadActionIcons(
                        DirectoryHelper.FindSubDirectory(@"resources\xivdb"),
                        actionList,
                        async (e) =>
                        {
                            await WPFHelper.InvokeAsync(() =>
                            {
                                this.MaxValue = e.Max;
                                this.CurrentValue = e.Current;
                                this.ProgressMessage =
                                    $"Downloading Action Icons..." + Environment.NewLine +
                                    $"{e.CurrentObject.ToString()}" + Environment.NewLine +
                                    $"{e.Current:N0} / {e.Max:N0}";
                            });
                        });

                    IconController.Instance.RefreshIcon();
                }
                catch (Exception ex)
                {
                    ModernMessageBox.ShowDialog(
                        "Download Error!",
                        "Action Icons Downloader",
                        MessageBoxButton.OK,
                        ex);

                    return;
                }
                finally
                {
                    this.InProgress = false;
                }

                ModernMessageBox.ShowDialog(
                    "Download completed!",
                    "Action Icons Downloader");
            }));

        private bool inProgress;

        public bool InProgress
        {
            get => this.inProgress;
            set => this.SetProperty(ref this.inProgress, value);
        }

        private double currentValue;

        public double CurrentValue
        {
            get => this.currentValue;
            set => this.SetProperty(ref this.currentValue, value);
        }

        private double maxValue;

        public double MaxValue
        {
            get => this.maxValue;
            set => this.SetProperty(ref this.maxValue, value);
        }

        private string progressMessage;

        public string ProgressMessage
        {
            get => this.progressMessage;
            set => this.SetProperty(ref this.progressMessage, value);
        }
    }
}
