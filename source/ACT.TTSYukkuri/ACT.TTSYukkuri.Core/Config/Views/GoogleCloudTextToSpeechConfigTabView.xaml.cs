using System.Windows.Controls;
using FFXIV.Framework.Bridge;

namespace ACT.TTSYukkuri.Config.Views
{
    /// <summary>
    /// GoogleCloudTextToSpeechConfigTabView.xaml の相互作用ロジック
    /// </summary>
    public partial class GoogleCloudTextToSpeechConfigTabView : UserControl
    {
        public GoogleCloudTextToSpeechConfigTabView()
        {
            this.InitializeComponent();
            this.TabDefault.Content = new GoogleCloudTextToSpeechConfigView(VoicePalettes.Default);
            this.TabExt1.Content = new GoogleCloudTextToSpeechConfigView(VoicePalettes.Ext1);
            this.TabExt2.Content = new GoogleCloudTextToSpeechConfigView(VoicePalettes.Ext2);
            this.TabExt3.Content = new GoogleCloudTextToSpeechConfigView(VoicePalettes.Ext3);
        }
    }
}
