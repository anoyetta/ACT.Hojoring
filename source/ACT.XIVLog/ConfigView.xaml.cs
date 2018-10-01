using System.Windows.Controls;

namespace ACT.XIVLog
{
    /// <summary>
    /// ConfigView.xaml の相互作用ロジック
    /// </summary>
    public partial class ConfigView : UserControl
    {
        public ConfigView()
        {
            this.InitializeComponent();
        }

        public Config Config => Config.Instance;
    }
}
