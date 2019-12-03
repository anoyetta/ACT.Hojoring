using ACT.TTSYukkuri.Boyomichan;
using Prism.Commands;
using Prism.Mvvm;

namespace ACT.TTSYukkuri.Config.ViewModels
{
    public class BoyomiConfigViewModel : BindableBase
    {
        public Settings Config => Settings.Default;

        private DelegateCommand setDefaultPortCommand;

        public DelegateCommand SetDefaultPortCommand =>
            this.setDefaultPortCommand ?? (this.setDefaultPortCommand = new DelegateCommand(this.ExecuteSetDefaultPortCommand));

        private void ExecuteSetDefaultPortCommand()
        {
            this.Config.BoyomiPort = BoyomichanSpeechController.BoyomichanServicePort;
        }
    }
}
