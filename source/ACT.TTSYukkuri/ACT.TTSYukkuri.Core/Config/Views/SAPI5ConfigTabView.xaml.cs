using FFXIV.Framework.Bridge;
using System.Windows.Controls;

namespace ACT.TTSYukkuri.Config.Views
{
    /// <summary>
    /// SAPI5ConfigTabView.xaml の相互作用ロジック
    /// </summary>
    public partial class SAPI5ConfigTabView : UserControl
    {
        public SAPI5ConfigTabView()
        {
            InitializeComponent();
            TabDefault.Content = new SAPI5ConfigView(VoicePalettes.Default);
            TabExt1.Content = new SAPI5ConfigView(VoicePalettes.Ext1);
            TabExt2.Content = new SAPI5ConfigView(VoicePalettes.Ext2);
            TabExt3.Content = new SAPI5ConfigView(VoicePalettes.Ext3);
        }
    }
}
