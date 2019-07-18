using System.Linq;
using System.Windows.Input;
using System.Windows.Media;
using ACT.UltraScouter.ViewModels;
using ACT.UltraScouter.Workers;
using Prism.Commands;
using Prism.Mvvm;

namespace ACT.UltraScouter.Config.UI.ViewModels
{
    public class MPTickerConfigViewModel :
        BindableBase
    {
        public MPTicker MPTicker => Settings.Instance.MPTicker;

        private MPTickerViewModel GetViewModel() =>
            MainWorker.Instance.GetViewModelList(ViewCategories.Me).FirstOrDefault(x => x is MPTickerViewModel) as MPTickerViewModel;

        private ICommand targetActionDisplayTextFontCommand;
        private ICommand targetActionDisplayTextColorCommand;
        private ICommand targetActionDisplayTextOutlineColorCommand;

        private ICommand targetActionProgressBarOutlineColorCommand;
        private ICommand targetActionBarAddCommand;

        public ICommand TargetActionDisplayTextFontCommand =>
            this.targetActionDisplayTextFontCommand ??
            (this.targetActionDisplayTextFontCommand =
            new ChangeFontCommand(
                (font) => this.MPTicker.DisplayText.Font = font));

        public ICommand TargetActionDisplayTextColorCommand =>
            this.targetActionDisplayTextColorCommand ??
            (this.targetActionDisplayTextColorCommand =
            new ChangeColorCommand(
                (color) => this.MPTicker.DisplayText.Color = color));

        public ICommand TargetActionDisplayTextOutlineColorCommand =>
            this.targetActionDisplayTextOutlineColorCommand ??
            (this.targetActionDisplayTextOutlineColorCommand =
            new ChangeColorCommand(
                (color) => this.MPTicker.DisplayText.OutlineColor = color));

        public ICommand TargetActionProgressBarOutlineColorCommand =>
            this.targetActionProgressBarOutlineColorCommand ??
            (this.targetActionProgressBarOutlineColorCommand =
            new ChangeColorCommand(
                (color) => this.MPTicker.ProgressBar.OutlineColor = color,
                () => this.RefreshMPTickerCommand?.Execute(null)));

        public ICommand TargetActionBarAddCommand =>
            this.targetActionBarAddCommand ??
            (this.targetActionBarAddCommand = new DelegateCommand(() =>
            {
                var maxRange = this.MPTicker.ProgressBar.ColorRange
                    .OrderByDescending(x => x.Max)
                    .FirstOrDefault();

                this.MPTicker.ProgressBar.ColorRange.Add(new ProgressBarColorRange()
                {
                    Color = maxRange?.Color ?? Colors.White,
                    Min = maxRange?.Max ?? 0,
                    Max = maxRange?.Max ?? 0,
                });

                this.RefreshMPTickerCommand?.Execute(null);
            }));

        private ICommand refreshMPTickerCommand;

        public ICommand RefreshMPTickerCommand =>
            this.refreshMPTickerCommand ?? (this.refreshMPTickerCommand = new DelegateCommand(
                () => this.GetViewModel()?.RaiseAllPropertiesChanged()));
    }
}
