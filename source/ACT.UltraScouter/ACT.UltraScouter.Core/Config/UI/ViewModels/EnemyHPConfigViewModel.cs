using System.Linq;
using System.Windows.Input;
using System.Windows.Media;
using Prism.Commands;
using Prism.Mvvm;

namespace ACT.UltraScouter.Config.UI.ViewModels
{
    public class EnemyHPConfigViewModel :
        BindableBase
    {
        public EnemyHPConfigViewModel()
        {
        }

        public virtual EnemyHP Config => Settings.Instance.EnemyHP;

        private ICommand displayTextFontCommand;

        public ICommand DisplayTextFontCommand =>
            this.displayTextFontCommand ??
            (this.displayTextFontCommand =
            new ChangeFontCommand((font) => this.Config.DisplayText.Font = font,
                () => this.RefreshCommand?.Execute(null)));

        private ICommand displayTextColorCommand;

        public ICommand DisplayTextColorCommand =>
            this.displayTextColorCommand ??
            (this.displayTextColorCommand =
            new ChangeColorCommand(
                (color) => this.Config.DisplayText.Color = color,
                () => this.RefreshCommand?.Execute(null)));

        private ICommand displayTextOutlineColorCommand;

        public ICommand DisplayTextOutlineColorCommand =>
            this.displayTextOutlineColorCommand ??
            (this.displayTextOutlineColorCommand =
            new ChangeColorCommand(
                (color) => this.Config.DisplayText.OutlineColor = color,
                () => this.RefreshCommand?.Execute(null)));

        private ICommand progressBarOutlineColorCommand;

        public ICommand ProgressBarOutlineColorCommand =>
            this.progressBarOutlineColorCommand ??
            (this.progressBarOutlineColorCommand =
            new ChangeColorCommand(
                (color) => this.Config.ProgressBar.OutlineColor = color,
                () => this.RefreshCommand?.Execute(null)));

        private ICommand backgroundColorCommand;

        public ICommand BackgroundColorCommand =>
            this.backgroundColorCommand ??
            (this.backgroundColorCommand =
            new ChangeColorCommand((color) => this.Config.Background = color));

        private ICommand barAddCommand;

        public ICommand BarAddCommand =>
            this.barAddCommand ??
            (this.barAddCommand = new DelegateCommand(() =>
            {
                var maxRange = this.Config.ProgressBar.ColorRange
                    .OrderByDescending(x => x.Max)
                    .FirstOrDefault();

                this.Config.ProgressBar.ColorRange.Add(new ProgressBarColorRange()
                {
                    Color = maxRange?.Color ?? Colors.White,
                    Min = maxRange?.Max ?? 0,
                    Max = maxRange?.Max ?? 0,
                });

                this.RefreshCommand?.Execute(null);
            }));

        private ICommand refreshCommand;

        public ICommand RefreshCommand =>
            this.refreshCommand ?? (this.refreshCommand = new DelegateCommand(
                () => this.Config.ExecuteRefreshViewCommand()));
    }
}
