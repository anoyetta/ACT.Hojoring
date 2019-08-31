using System.Windows.Controls;
using FFXIV.Framework.Bridge;

namespace ACT.TTSYukkuri.Config.Views
{
    /// <summary>
    /// SAPI5ConfigTabView.xaml の相互作用ロジック
    /// </summary>
    public partial class SAPI5ConfigTabView : UserControl
    {
        public SAPI5ConfigTabView()
        {
            this.InitializeComponent();
            this.TabDefault.Content = new SAPI5ConfigView(VoicePalettes.Default);
            this.TabExt1.Content = new SAPI5ConfigView(VoicePalettes.Ext1);
            this.TabExt2.Content = new SAPI5ConfigView(VoicePalettes.Ext2);
            this.TabExt3.Content = new SAPI5ConfigView(VoicePalettes.Ext3);
        }
    }
}
