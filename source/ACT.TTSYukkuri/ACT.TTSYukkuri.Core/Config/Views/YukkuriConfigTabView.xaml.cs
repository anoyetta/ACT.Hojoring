using System.Windows.Controls;
using FFXIV.Framework.Bridge;

namespace ACT.TTSYukkuri.Config.Views
{
    /// <summary>
    /// YukkuriConfigTabView.xaml の相互作用ロジック
    /// </summary>
    public partial class YukkuriConfigTabView : UserControl
    {
        public YukkuriConfigTabView()
        {
            this.InitializeComponent();
            this.TabDefault.Content = new YukkuriConfigView(VoicePalettes.Default);
            this.TabExt1.Content = new YukkuriConfigView(VoicePalettes.Ext1);
            this.TabExt2.Content = new YukkuriConfigView(VoicePalettes.Ext2);
            this.TabExt3.Content = new YukkuriConfigView(VoicePalettes.Ext3);
        }
    }
}
