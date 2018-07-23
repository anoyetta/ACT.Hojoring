using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using ACT.TTSYukkuri.Yukkuri;
using Prism.Commands;
using Prism.Mvvm;

namespace ACT.TTSYukkuri.Config.ViewModels
{
    public class YukkuriConfigViewModel : BindableBase
    {
        public YukkuriConfig Config => Settings.Default.YukkuriSettings;

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
