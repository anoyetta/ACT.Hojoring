using Prism.Mvvm;

namespace ACT.TTSYukkuri.Config.ViewModels
{
    public class BoyomiConfigViewModel : BindableBase
    {
        public Settings Config => Settings.Default;
    }
}
