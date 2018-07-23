using Prism.Mvvm;

namespace ACT.TTSYukkuri.Config.ViewModels
{
    public class StatusAlertConfigViewModel : BindableBase
    {
        public StatusAlertConfig Config => Settings.Default.StatusAlertSettings;
    }
}
