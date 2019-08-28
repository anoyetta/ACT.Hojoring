using System.Windows.Input;
using Prism.Commands;
using FFXIV.Framework.Bridge;

namespace ACT.TTSYukkuri.Config.ViewModels
{
    public class GoogleCloudTextToSpeechConfigViewModel
    {
        VoicePalettes VoicePalette { get; set; }

        public GoogleCloudTextToSpeechConfigViewModel(VoicePalettes voicePalette = VoicePalettes.Default)
        {
            this.VoicePalette = voicePalette;
        }

        public GoogleCloudTextToSpeechConfig Config
        {
            get
            {
                GoogleCloudTextToSpeechConfig config;
                switch (VoicePalette)
                {
                    case VoicePalettes.Default:
                        config = Settings.Default.GoogleCloudTextToSpeechSettings;
                        break;
                    case VoicePalettes.Ext1:
                        config = Settings.Default.GoogleCloudTextToSpeechSettingsExt1;
                        break;
                    case VoicePalettes.Ext2:
                        config = Settings.Default.GoogleCloudTextToSpeechSettingsExt2;
                        break;
                    case VoicePalettes.Ext3:
                        config = Settings.Default.GoogleCloudTextToSpeechSettingsExt3;
                        break;
                    default:
                        config = Settings.Default.GoogleCloudTextToSpeechSettings;
                        break;
                }
                return config;
            }
        }

        public GoogleCloudTextToSpeechLanguageCode[] LanguageCodeList => Settings.Default.GoogleCloudTextToSpeechSettings.EnumerateLanguageCode();

        public GoogleCloudTextToSpeechSampleRateHertz[] SampleRateHertzList => Settings.Default.GoogleCloudTextToSpeechSettings.EnumerateSampleRateHertz();

        private ICommand setRecommendCommand;

        public ICommand SetRecommendCommand =>
            this.setRecommendCommand ?? (this.setRecommendCommand = new DelegateCommand(() =>
            {
                this.Config.SetRecommend();
            }));
    }
}
