using FFXIV.Framework.Bridge;
using System.Windows.Controls;

namespace ACT.TTSYukkuri.Config.Views
{
    /// <summary>
    /// OpenJTalkConfigTabView.xaml の相互作用ロジック
    /// </summary>
    public partial class OpenJTalkConfigTabView : UserControl
    {
        public OpenJTalkConfigTabView()
        {
            InitializeComponent();
            TabDefault.Content = new OpenJTalkConfigView(VoicePalettes.Default);
            TabExt1.Content = new OpenJTalkConfigView(VoicePalettes.Ext1);
            TabExt2.Content = new OpenJTalkConfigView(VoicePalettes.Ext2);
            TabExt3.Content = new OpenJTalkConfigView(VoicePalettes.Ext3);
        }
    }
}
