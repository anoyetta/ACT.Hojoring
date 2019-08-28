using FFXIV.Framework.Bridge;
using System.Windows.Controls;

namespace ACT.TTSYukkuri.Config.Views
{
    /// <summary>
    /// GoogleCloudTextToSpeechConfigTabView.xaml の相互作用ロジック
    /// </summary>
    public partial class GoogleCloudTextToSpeechConfigTabView : UserControl
    {
        public GoogleCloudTextToSpeechConfigTabView()
        {
            InitializeComponent();
            TabDefault.Content = new GoogleCloudTextToSpeechConfigView(VoicePalettes.Default);
            TabExt1.Content = new GoogleCloudTextToSpeechConfigView(VoicePalettes.Ext1);
            TabExt2.Content = new GoogleCloudTextToSpeechConfigView(VoicePalettes.Ext2);
            TabExt3.Content = new GoogleCloudTextToSpeechConfigView(VoicePalettes.Ext3);
        }
    }
}
