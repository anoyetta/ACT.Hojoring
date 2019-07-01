using System.Windows.Controls;

namespace ACT.UltraScouter.Config.UI.Views
{
    /// <summary>
    /// BaseView.xaml の相互作用ロジック
    /// </summary>
    public partial class BaseView : UserControl
    {
        public static BaseView Instance { get; private set; }

        public BaseView(System.Drawing.Font font = null)
        {
            Instance = this;
            this.InitializeComponent();
        }

        public void SetActivationStatus(
            bool isAllow)
            => this.DenyMessageLabel.Visibility = isAllow ?
                System.Windows.Visibility.Collapsed :
                System.Windows.Visibility.Visible;
    }
}
