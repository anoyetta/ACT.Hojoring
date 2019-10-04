using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using FFXIV.Framework.Bridge;

namespace ACT.TTSYukkuri.Config.ViewModels
{
    public class OpenJTalkConfigViewModel : BindableBase
    {
        VoicePalettes VoicePalette { get; set; }

        public OpenJTalkConfigViewModel(VoicePalettes voicePalette = VoicePalettes.Default)
        {
            this.VoicePalette = voicePalette;
        }

        public OpenJTalkConfig Config
        {
            get
            {
                OpenJTalkConfig config;
                switch (VoicePalette)
                {
                    case VoicePalettes.Default:
                        config = Settings.Default.OpenJTalkSettings;
                        break;
                    case VoicePalettes.Ext1:
                        config = Settings.Default.OpenJTalkSettingsExt1;
                        break;
                    case VoicePalettes.Ext2:
                        config = Settings.Default.OpenJTalkSettingsExt2;
                        break;
                    case VoicePalettes.Ext3:
                        config = Settings.Default.OpenJTalkSettingsExt3;
                        break;
                    default:
                        config = Settings.Default.OpenJTalkSettings;
                        break;
                }
                return config;
            }
        }

        public OpenJTalkVoice[] Voices => Settings.Default.OpenJTalkSettings.EnumerateVoice();

        private ICommand setRecommendCommand;

        public ICommand SetRecommendCommand =>
            this.setRecommendCommand ?? (this.setRecommendCommand = new DelegateCommand(() =>
            {
                this.Config.SetRecommend();
            }));
    }
}
