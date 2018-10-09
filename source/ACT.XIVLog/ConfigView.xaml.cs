using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
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
        }

        public Config Config => Config.Instance;

        public XIVLogPlugin Plugin => XIVLogPlugin.Instance;

        private ICommand oepnLogCommand;

        public ICommand OpenLogCommand =>
            this.oepnLogCommand ?? (this.oepnLogCommand = new DelegateCommand(async () => await Task.Run(() =>
            {
                if (File.Exists(this.Plugin.LogfileName))
                {
                    Process.Start(this.Plugin.LogfileName);
                }
            })));
    }
}
