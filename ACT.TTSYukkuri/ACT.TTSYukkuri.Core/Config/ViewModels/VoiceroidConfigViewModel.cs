using ACT.TTSYukkuri.Voiceroid;
using Prism.Mvvm;

namespace ACT.TTSYukkuri.Config.ViewModels
{
    public class VoiceroidConfigViewModel : BindableBase
    {
        public VoiceroidConfig Config => Settings.Default.VoiceroidSettings;

        public VoiceroidSpeechController Controller => SpeechController.Default as VoiceroidSpeechController;
    }
}
