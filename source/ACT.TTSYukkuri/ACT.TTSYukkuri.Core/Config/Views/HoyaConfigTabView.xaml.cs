using System.Windows.Controls;
using FFXIV.Framework.Bridge;

namespace ACT.TTSYukkuri.Config.Views
{
    /// <summary>
    /// HoyaConfigTabView.xaml の相互作用ロジック
    /// </summary>
    public partial class HoyaConfigTabView : UserControl
    {
        public HoyaConfigTabView()
        {
            this.InitializeComponent();
            this.TabDefault.Content = new HoyaConfigView(VoicePalettes.Default);
            this.TabExt1.Content = new HoyaConfigView(VoicePalettes.Ext1);
            this.TabExt2.Content = new HoyaConfigView(VoicePalettes.Ext2);
            this.TabExt3.Content = new HoyaConfigView(VoicePalettes.Ext3);
        }
    }
}
