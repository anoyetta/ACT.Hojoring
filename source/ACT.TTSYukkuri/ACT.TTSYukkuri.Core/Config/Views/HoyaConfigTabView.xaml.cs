using FFXIV.Framework.Bridge;
using System.Windows.Controls;

namespace ACT.TTSYukkuri.Config.Views
{
    /// <summary>
    /// HoyaConfigTabView.xaml の相互作用ロジック
    /// </summary>
    public partial class HoyaConfigTabView : UserControl
    {
        public HoyaConfigTabView()
        {
            InitializeComponent();
            TabDefault.Content = new HoyaConfigView(VoicePalettes.Default);
            TabExt1.Content = new HoyaConfigView(VoicePalettes.Ext1);
            TabExt2.Content = new HoyaConfigView(VoicePalettes.Ext2);
            TabExt3.Content = new HoyaConfigView(VoicePalettes.Ext3);
        }
    }
}
