using Prism.Mvvm;
using FFXIV.Framework.Bridge;

namespace ACT.TTSYukkuri.Config.ViewModels
{
    public class HoyaConfigViewModel : BindableBase
    {
        VoicePalettes VoicePalette { get; set; }
        public HoyaConfigViewModel(VoicePalettes voicePalette = VoicePalettes.Default)
        {
            this.VoicePalette = voicePalette;
        }

        public HOYAConfig Config
        {
            get
            {
                HOYAConfig config;
                switch (VoicePalette)
                {
                    case VoicePalettes.Default:
                        config = Settings.Default.HOYASettings;
                        break;
                    case VoicePalettes.Ext1:
                        config = Settings.Default.HOYASettingsExt1;
                        break;
                    case VoicePalettes.Ext2:
                        config = Settings.Default.HOYASettingsExt2;
                        break;
                    case VoicePalettes.Ext3:
                        config = Settings.Default.HOYASettingsExt3;
                        break;
                    default:
                        config = Settings.Default.HOYASettings;
                        break;
                }
                return config;
            }
        }
    }
}
