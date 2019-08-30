using System.Windows.Controls;
using FFXIV.Framework.Bridge;

namespace ACT.TTSYukkuri.Config.Views
{
    /// <summary>
    /// PollyConfigTabView.xaml の相互作用ロジック
    /// </summary>
    public partial class PollyConfigTabView : UserControl
    {
        public PollyConfigTabView()
        {
            this.InitializeComponent();
            this.TabDefault.Content = new PollyConfigView(VoicePalettes.Default);
            this.TabExt1.Content = new PollyConfigView(VoicePalettes.Ext1);
            this.TabExt2.Content = new PollyConfigView(VoicePalettes.Ext2);
            this.TabExt3.Content = new PollyConfigView(VoicePalettes.Ext3);
        }
    }
}
