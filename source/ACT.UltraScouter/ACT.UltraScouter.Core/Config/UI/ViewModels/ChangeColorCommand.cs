using System;
using System.Windows.Input;
using System.Windows.Media;
using FFXIV.Framework.Dialog;

namespace ACT.UltraScouter.Config.UI.ViewModels
{
    public class ChangeColorCommand :
        ICommand
    {
        public ChangeColorCommand()
        {
        }

        public ChangeColorCommand(
            ChangeColorCallback changeColorDelegate,
            Action refreshViewAction = null)
        {
            this.ChangeColorDelegate = changeColorDelegate;
            this.refreshViewAction = refreshViewAction;
            this.CanExecuteChanged?.Invoke(this, new EventArgs());
        }

        public delegate void ChangeColorCallback(Color changedColor);

        private Action refreshViewAction;
        public ChangeColorCallback ChangeColorDelegate;

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) => true;

        public void Execute(object parameter)
        {
            var color = (Color)parameter;

            var result = ColorDialogWrapper.ShowDialog(color);
            if (result.Result)
            {
                this.ChangeColorDelegate?.Invoke(result.Color);
                this.refreshViewAction?.Invoke();
            }
        }
    }
}
