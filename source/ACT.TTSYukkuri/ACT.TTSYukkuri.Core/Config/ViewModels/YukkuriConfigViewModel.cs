using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using ACT.TTSYukkuri.Yukkuri;
using Prism.Commands;
using Prism.Mvvm;
using FFXIV.Framework.Bridge;

namespace ACT.TTSYukkuri.Config.ViewModels
{
    public class YukkuriConfigViewModel : BindableBase
    {
        VoicePalettes VoicePalette { get; set; }

        public YukkuriConfigViewModel(VoicePalettes voicePalette = VoicePalettes.Default)
        {
            this.VoicePalette = voicePalette;
        }

        public YukkuriConfig Config
        {
            get
            {
                YukkuriConfig config;
                switch (VoicePalette)
                {
                    case VoicePalettes.Default:
                        config = Settings.Default.YukkuriSettings;
                        break;
                    case VoicePalettes.Ext1:
                        config = Settings.Default.YukkuriSettingsExt1;
                        break;
                    case VoicePalettes.Ext2:
                        config = Settings.Default.YukkuriSettingsExt2;
                        break;
                    case VoicePalettes.Ext3:
                        config = Settings.Default.YukkuriSettingsExt3;
                        break;
                    default:
                        config = Settings.Default.YukkuriSettings;
                        break;
                }
                return config;
            }
        }

        public IReadOnlyList<AQPreset> Presets => AQVoicePresets.Presets;

        private ICommand openUserDictionaryEditorCommand;

        public ICommand OpenUserDictionaryEditorCommand =>
            this.openUserDictionaryEditorCommand ?? (this.openUserDictionaryEditorCommand = new DelegateCommand(() =>
        {
            var editor = AquesTalk.UserDictionaryEditor;

            if (File.Exists(editor))
            {
                var dir = Path.GetDirectoryName(editor);

                var pi = new ProcessStartInfo()
                {
                    FileName = editor,
                    Arguments = Path.Combine(dir, "sample_src_userdic.csv"),
                    WorkingDirectory = dir
                };

                Process.Start(pi);
            }
        }));
    }
}
