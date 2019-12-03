using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using Advanced_Combat_Tracker;
using Prism.Commands;

namespace ACT.XIVLog
{
    /// <summary>
    /// ConfigView.xaml の相互作用ロジック
    /// </summary>
    public partial class ConfigView :
        UserControl
    {
        public ConfigView()
        {
            this.InitializeComponent();

            this.StartRecordingTextBox.KeyDown += this.StartRecordingTextBox_KeyDown;
            this.StopRecordingTextBox.KeyDown += this.StopRecordingTextBox_KeyDown;
        }

        public Config Config => Config.Instance;

        public XIVLogPlugin Plugin => XIVLogPlugin.Instance;

        public string VersionInfo
        {
            get
            {
                var result = string.Empty;

                var plugin = ActGlobals.oFormActMain.PluginGetSelfData(XIVLogPlugin.Instance);
                if (plugin != null)
                {
                    var vi = FileVersionInfo.GetVersionInfo(plugin.pluginFile.FullName);
                    result = $"{vi.ProductName} v{vi.FileVersion}";
                }

                return result;
            }
        }

        private ICommand oepnLogCommand;

        public ICommand OpenLogCommand =>
            this.oepnLogCommand ?? (this.oepnLogCommand = new DelegateCommand(async () => await Task.Run(() =>
            {
                if (File.Exists(this.Plugin.LogfileName))
                {
                    Process.Start(this.Plugin.LogfileName);
                }
            })));

        private ICommand oepnLogDirectoryCommand;

        public ICommand OpenLogDirectoryCommand =>
            this.oepnLogDirectoryCommand ?? (this.oepnLogDirectoryCommand = new DelegateCommand(async () => await Task.Run(() =>
            {
                var directory = Path.GetDirectoryName(this.Plugin.LogfileName);

                if (Directory.Exists(directory))
                {
                    Process.Start(directory);
                }
            })));

        private ICommand oepnVideoDirectoryCommand;

        public ICommand OepnVideoDirectoryCommand =>
            this.oepnVideoDirectoryCommand ?? (this.oepnVideoDirectoryCommand = new DelegateCommand(async () => await Task.Run(() =>
            {
                var directory = Path.GetDirectoryName(this.Config.VideoSaveDictory);

                if (Directory.Exists(directory))
                {
                    Process.Start(directory);
                }
            })));

        private void StartRecordingTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            var shortcut = this.Config.StartRecordingShortcut;
            shortcut.Key = e.Key;
            e.Handled = true;
        }

        private void StopRecordingTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            var shortcut = this.Config.StopRecordingShortcut;
            shortcut.Key = e.Key;
            e.Handled = true;
        }

        private DelegateCommand startRecordingCommand;

        public DelegateCommand StartRecordingCommand =>
            this.startRecordingCommand ?? (this.startRecordingCommand = new DelegateCommand(this.ExecuteStartRecordingCommand));

        private void ExecuteStartRecordingCommand()
            => VideoCapture.Instance.StartRecording();

        private DelegateCommand stopRecordingCommand;

        public DelegateCommand StopRecordingCommand =>
            this.stopRecordingCommand ?? (this.stopRecordingCommand = new DelegateCommand(this.ExecuteStopRecordingCommand));

        private void ExecuteStopRecordingCommand()
            => VideoCapture.Instance.FinishRecording();
    }
}
