using FFXIV.Framework.Bridge;
using System.Windows.Controls;

namespace ACT.TTSYukkuri.Config.Views
{
    /// <summary>
    /// YukkuriConfigTabView.xaml の相互作用ロジック
    /// </summary>
    public partial class YukkuriConfigTabView : UserControl
    {
        public YukkuriConfigTabView()
        {
            InitializeComponent();
            TabDefault.Content = new YukkuriConfigView(VoicePalettes.Default);
            TabExt1.Content = new YukkuriConfigView(VoicePalettes.Ext1);
            TabExt2.Content = new YukkuriConfigView(VoicePalettes.Ext2);
            TabExt3.Content = new YukkuriConfigView(VoicePalettes.Ext3);
        }
    }
}
