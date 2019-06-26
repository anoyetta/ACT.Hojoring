using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using FFXIV.Framework.Bridge;
using FFXIV.Framework.Common;
using FFXIV.Framework.Globalization;

namespace FFXIV.Framework.WPF.Views
{
    /// <summary>
    /// CommonView.xaml の相互作用ロジック
    /// </summary>
    public partial class CommonView : UserControl
    {
        private Locales UILocale => ConfigBridge.Instance.GetUILocaleCallback?.Invoke() ?? Locales.EN;

        public CommonView()
        {
            this.InitializeComponent();

            // HelpViewを設定する
            this.HelpView.SetLocale(this.UILocale);
            this.HelpView.ViewModel.ReloadConfigAction = null;
        }

        private void SendAmazonGiftCard_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (this.UILocale == Locales.JA)
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

            CommonSounds.Instance.PlayAsterisk();
        }
    }
}
