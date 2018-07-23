using System;
using System.Windows.Input;
using FFXIV.Framework.Common;
using FFXIV.Framework.Dialog;

namespace ACT.UltraScouter.Config.UI.ViewModels
{
    public class ChangeFontCommand :
        ICommand
    {
        public ChangeFontCommand()
        {
        }

        public ChangeFontCommand(
            ChangeFontCallback changeFontDelegate,
            Action refreshViewAction = null)
        {
            this.ChangeFontDelegate = changeFontDelegate;
            this.refreshViewAction = refreshViewAction;
            this.CanExecuteChanged?.Invoke(this, new EventArgs());
        }

        private Action refreshViewAction;

        public delegate void ChangeFontCallback(FontInfo changedFont);

        public ChangeFontCallback ChangeFontDelegate;

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) => true;

        public void Execute(object parameter)
        {
            var font = (FontInfo)parameter;

            var result = FontDialogWrapper.ShowDialog(font);
            if (result.Result)
            {
                this.ChangeFontDelegate?.Invoke(result.Font);
                this.refreshViewAction?.Invoke();
            }
        }
    }
}
