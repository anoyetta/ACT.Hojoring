using System.Diagnostics;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using FFXIV.Framework.Globalization;

namespace ACT.SpecialSpellTimer.Config
{
    /// <summary>
    /// BaseView.xaml の相互作用ロジック
    /// </summary>
    public partial class BaseView : UserControl
    {
        public BaseView(
            System.Drawing.Font font = null)
        {
            this.InitializeComponent();

            // HelpViewを設定する
            this.HelpView.SetLocale(Settings.Default.UILocale);

            this.HelpView.ViewModel.ConfigFile = Settings.Default.FileName;
            this.HelpView.ViewModel.ReloadConfigAction = null;
        }

        private void SendAmazonGiftCard_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (Settings.Default.UILocale == Locales.JA)
            {
                Process.Start(@"https://www.amazon.co.jp/dp/BT00DHI8G4");
            }
            else
            {
                Process.Start(@"https://www.amazon.com/dp/B004LLIKVU");
            }
        }

        private void CopyAddress_Click(
            object sender,
            RoutedEventArgs e)
        {
            Clipboard.SetData(
                DataFormats.Text,
                "anoyetta@gmail.com");

            SystemSounds.Asterisk.Play();
        }
    }
}
