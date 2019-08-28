using System.Collections.Generic;
using System.Speech.Synthesis;
using ACT.TTSYukkuri.SAPI5;
using Prism.Mvvm;
using FFXIV.Framework.Bridge;

namespace ACT.TTSYukkuri.Config.ViewModels
{
    public class SAPI5ConfigViewModel : BindableBase
    {
        VoicePalettes VoicePalette { get; set; }

        public SAPI5ConfigViewModel(VoicePalettes voicePalette = VoicePalettes.Default)
        {
            this.VoicePalette = voicePalette;
        }

        public SAPI5Configs Config
        {
            get
            {
                SAPI5Configs config;
                switch (VoicePalette)
                {
                    case VoicePalettes.Default:
                        config = Settings.Default.SAPI5Settings;
                        break;
                    case VoicePalettes.Ext1:
                        config = Settings.Default.SAPI5SettingsExt1;
                        break;
                    case VoicePalettes.Ext2:
                        config = Settings.Default.SAPI5SettingsExt2;
                        break;
                    case VoicePalettes.Ext3:
                        config = Settings.Default.SAPI5SettingsExt3;
                        break;
                    default:
                        config = Settings.Default.SAPI5Settings;
                        break;
                }
                return config;
            }
        }

        public IReadOnlyList<InstalledVoice> Voices => SAPI5SpeechController.Synthesizers;

        public IReadOnlyList<KeyValuePair<Pitches, string>> PitchList => new List<KeyValuePair<Pitches, string>>()
        {
            new KeyValuePair<SAPI5.Pitches, string>(Pitches.Default, Pitches.Default.ToXML()),
            new KeyValuePair<SAPI5.Pitches, string>(Pitches.XLow, Pitches.XLow.ToXML()),
            new KeyValuePair<SAPI5.Pitches, string>(Pitches.Low, Pitches.Low.ToXML()),
            new KeyValuePair<SAPI5.Pitches, string>(Pitches.Medium, Pitches.Medium.ToXML()),
            new KeyValuePair<SAPI5.Pitches, string>(Pitches.High, Pitches.High.ToXML()),
            new KeyValuePair<SAPI5.Pitches, string>(Pitches.XHigh, Pitches.XHigh.ToXML()),
        };
    }
}
