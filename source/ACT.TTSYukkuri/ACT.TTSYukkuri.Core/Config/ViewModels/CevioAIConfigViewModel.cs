using Prism.Mvvm;

namespace ACT.TTSYukkuri.Config.ViewModels
{
    public class CevioAIConfigViewModel : BindableBase
    {
        public CevioAIConfig Config => Settings.Default.CevioAISettings;
    }
}
