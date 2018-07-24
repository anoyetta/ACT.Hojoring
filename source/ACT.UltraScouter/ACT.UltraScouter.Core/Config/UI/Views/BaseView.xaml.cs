using System.Windows.Controls;

namespace ACT.UltraScouter.Config.UI.Views
{
    /// <summary>
    /// BaseView.xaml の相互作用ロジック
    /// </summary>
    public partial class BaseView : UserControl
    {
        public BaseView()
        {
            this.InitializeComponent();

            // HelpViewを設定する
            this.HelpView.SetLocale(Settings.Instance.UILocale);
            this.HelpView.ViewModel.ConfigFile = Settings.Instance.FileName;
            this.HelpView.ViewModel.ReloadConfigAction = () => Settings.Instance.Load();
        }
    }
}
