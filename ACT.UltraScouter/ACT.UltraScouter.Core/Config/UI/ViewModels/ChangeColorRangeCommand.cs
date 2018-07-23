using System;
using System.Windows.Input;
using FFXIV.Framework.Dialog;

namespace ACT.UltraScouter.Config.UI.ViewModels
{
    public class ChangeColorRangeCommand :
        ICommand
    {
        public ChangeColorRangeCommand(
            Action refreshViewAction = null)
        {
            this.refreshViewAction = refreshViewAction;
            this.CanExecuteChanged?.Invoke(this, new EventArgs());
        }

        private Action refreshViewAction;

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) => true;

        public void Execute(object parameter)
        {
            var range = (ProgressBarColorRange)parameter;

            var result = ColorDialogWrapper.ShowDialog(range.Color);
            if (result.Result)
            {
                range.Color = result.Color;
                this.refreshViewAction?.Invoke();
            }
        }
    }
}
