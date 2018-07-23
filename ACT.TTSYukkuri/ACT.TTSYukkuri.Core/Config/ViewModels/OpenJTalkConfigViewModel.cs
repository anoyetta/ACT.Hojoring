using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;

namespace ACT.TTSYukkuri.Config.ViewModels
{
    public class OpenJTalkConfigViewModel : BindableBase
    {
        public OpenJTalkConfig Config => Settings.Default.OpenJTalkSettings;

        public OpenJTalkVoice[] Voices => Settings.Default.OpenJTalkSettings.EnumerateVoice();

        private ICommand setRecommendCommand;

        public ICommand SetRecommendCommand =>
            this.setRecommendCommand ?? (this.setRecommendCommand = new DelegateCommand(() =>
            {
                this.Config.SetRecommend();
            }));
    }
}
