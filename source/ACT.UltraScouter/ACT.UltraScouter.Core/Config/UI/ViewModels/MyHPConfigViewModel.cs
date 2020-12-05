using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Prism.Commands;
using Prism.Mvvm;

namespace ACT.UltraScouter.Config.UI.ViewModels
{
    public class MyHPConfigViewModel :
        BindableBase
    {
        public MyHPConfigViewModel()
        {
            this.Config.PropertyChanged += (_, e) =>
            {
                switch (e.PropertyName)
                {
                    case nameof(this.Config.BarStyle):
                        this.RaisePropertyChanged(nameof(this.BarSize1));
                        this.RaisePropertyChanged(nameof(this.BarSize2));
                        break;
                }
            };
        }

        public virtual MyStatus Config => Settings.Instance.MyHP;

        public virtual bool IsMPConfig => false;

        public IEnumerable<StatusStyles> Styles => Enum.GetValues(typeof(StatusStyles))
            .Cast<StatusStyles>();

        public string BarSize1 => this.Config.BarStyle switch
        {
            StatusStyles.Horizontal => "W",
            StatusStyles.Vertical => "H",
            StatusStyles.Circle => "D",
            _ => string.Empty,
        };

        public string BarSize2 => this.Config.BarStyle switch
        {
            StatusStyles.Horizontal => "H",
            StatusStyles.Vertical => "W",
            StatusStyles.Circle => "T",
            _ => string.Empty,
        };

        public IEnumerable<HorizontalAlignment> HorizontalAlignments => Enum.GetValues(typeof(HorizontalAlignment))
            .Cast<HorizontalAlignment>();

        private ICommand displayTextFontCommand;
        private ICommand displayTextColorCommand;
        private ICommand displayTextOutlineColorCommand;

        private ICommand progressBarOutlineColorCommand;
        private ICommand barAddCommand;

        public ICommand DisplayTextFontCommand =>
            this.displayTextFontCommand ??
            (this.displayTextFontCommand =
            new ChangeFontCommand((font) => this.Config.DisplayText.Font = font));

        public ICommand DisplayTextColorCommand =>
            this.displayTextColorCommand ??
            (this.displayTextColorCommand =
            new ChangeColorCommand(
                (color) => this.Config.DisplayText.Color = color,
                () => this.RefreshViewCommand?.Execute(null)));

        public ICommand DisplayTextOutlineColorCommand =>
            this.displayTextOutlineColorCommand ??
            (this.displayTextOutlineColorCommand =
            new ChangeColorCommand(
                (color) => this.Config.DisplayText.OutlineColor = color,
                () => this.RefreshViewCommand?.Execute(null)));

        public ICommand ProgressBarOutlineColorCommand =>
            this.progressBarOutlineColorCommand ??
            (this.progressBarOutlineColorCommand =
            new ChangeColorCommand(
                (color) => this.Config.ProgressBar.OutlineColor = color,
                () => this.RefreshViewCommand?.Execute(null)));

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

                this.RefreshViewCommand?.Execute(null);
            }));

        private ICommand refreshViewCommand;

        public ICommand RefreshViewCommand =>
            this.refreshViewCommand ?? (this.refreshViewCommand = new DelegateCommand(
                () => this.Config.ExecuteRefreshViewCommand()));
    }
}
