using System;
using System.Windows.Input;

namespace FFXIV.Framework.TTS.Server.ViewModels
{
    public class Command : ICommand
    {
        private Action commandAction;

        public Command()
        {
        }

        public Command(Action action)
        {
            this.commandAction = action;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            this.commandAction?.Invoke();
        }

        private void RaiseCanExcuteChanged()
        {
            this.CanExecuteChanged?.Invoke(this, new EventArgs());
        }
    }
}
