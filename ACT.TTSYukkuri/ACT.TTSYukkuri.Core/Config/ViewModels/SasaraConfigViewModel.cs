using Prism.Mvvm;

namespace ACT.TTSYukkuri.Config.ViewModels
{
    public class SasaraConfigViewModel : BindableBase
    {
        public SasaraConfig Config => Settings.Default.SasaraSettings;
    }
}
