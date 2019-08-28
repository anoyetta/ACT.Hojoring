using FFXIV.Framework.Bridge;
using System.Windows.Controls;

namespace ACT.TTSYukkuri.Config.Views
{
    /// <summary>
    /// PollyConfigTabView.xaml の相互作用ロジック
    /// </summary>
    public partial class PollyConfigTabView : UserControl
    {
        public PollyConfigTabView()
        {
            InitializeComponent();
            TabDefault.Content = new PollyConfigView(VoicePalettes.Default);
            TabExt1.Content = new PollyConfigView(VoicePalettes.Ext1);
            TabExt2.Content = new PollyConfigView(VoicePalettes.Ext2);
            TabExt3.Content = new PollyConfigView(VoicePalettes.Ext3);
        }
    }
}
