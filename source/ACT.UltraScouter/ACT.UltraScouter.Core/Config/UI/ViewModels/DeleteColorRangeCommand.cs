using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using ACT.UltraScouter.Workers;

namespace ACT.UltraScouter.Config.UI.ViewModels
{
    public class DeleteColorRangeCommand :
        ICommand
    {
        public DeleteColorRangeCommand(
            Action refreshViewAction = null)
        {
            this.CanExecuteChanged?.Invoke(this, new EventArgs());
            this.refreshViewAction = refreshViewAction;
        }

        private Action refreshViewAction;
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) => true;

        public void Execute(object parameter)
        {
            var id = (Guid)parameter;

            var colorRangeLists = new IList<ProgressBarColorRange>[]
            {
                Settings.Instance.TargetHP.ProgressBar.ColorRange,
                Settings.Instance.TargetAction.ProgressBar.ColorRange,

                Settings.Instance.FTHP.ProgressBar.ColorRange,
                Settings.Instance.FTAction.ProgressBar.ColorRange,

                Settings.Instance.ToTHP.ProgressBar.ColorRange,
                Settings.Instance.ToTAction.ProgressBar.ColorRange,

                Settings.Instance.BossHP.ProgressBar.ColorRange,
                Settings.Instance.BossAction.ProgressBar.ColorRange,

                Settings.Instance.MeAction.ProgressBar.ColorRange,
                Settings.Instance.MPTicker.ProgressBar.ColorRange,
            };

            var target = default(ProgressBarColorRange);
            foreach (var range in colorRangeLists)
            {
                target = range.FirstOrDefault(x => x.ID == id);
                if (target != null)
                {
                    range.Remove(target);
                    this.refreshViewAction?.Invoke();
                    break;
                }
            }
        }
    }
}
