using System.Windows.Controls;
using FFXIV.Framework.Bridge;

namespace ACT.TTSYukkuri.Config.Views
{
    /// <summary>
    /// OpenJTalkConfigTabView.xaml の相互作用ロジック
    /// </summary>
    public partial class OpenJTalkConfigTabView : UserControl
    {
        public OpenJTalkConfigTabView()
        {
            this.InitializeComponent();
            this.TabDefault.Content = new OpenJTalkConfigView(VoicePalettes.Default);
            this.TabExt1.Content = new OpenJTalkConfigView(VoicePalettes.Ext1);
            this.TabExt2.Content = new OpenJTalkConfigView(VoicePalettes.Ext2);
            this.TabExt3.Content = new OpenJTalkConfigView(VoicePalettes.Ext3);
        }
    }
}
