using Prism.Mvvm;

namespace ACT.TTSYukkuri.Config.ViewModels
{
    public class HoyaConfigViewModel : BindableBase
    {
        public HOYAConfig Config => Settings.Default.HOYASettings;
    }
}
