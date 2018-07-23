using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;

namespace ACT.TTSYukkuri.Discord.Views
{
    /// <summary>
    /// PermissionHelperView.xaml の相互作用ロジック
    /// </summary>
    public partial class PermissionHelperView : Window
    {
        public PermissionHelperView()
        {
            this.InitializeComponent();
            this.ChangeCanExecuteGrantButton();
        }

        private async void MyApps_RequestNavigate(
            object sender,
            RequestNavigateEventArgs e)
        {
            await Task.Run(() => Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri)));
            e.Handled = true;
        }

        private void ChangeCanExecuteGrantButton()
        {
            if (string.IsNullOrEmpty(this.ClientIDTextBox.Text))
            {
                this.GrantButton.Foreground = new SolidColorBrush(Colors.DimGray);
                this.GrantButton.IsEnabled = false;

                this.PermissionUrlTextBox.Text = string.Empty;
            }
            else
            {
                this.GrantButton.Foreground = new SolidColorBrush(Colors.Green);
                this.GrantButton.IsEnabled = true;

                this.PermissionUrlTextBox.Text = this.GetPermissionUrl();
            }
        }

        private void ClientIDTextBox_TextChanged(object sender, TextChangedEventArgs e)
            => this.ChangeCanExecuteGrantButton();

        private void GrantButton_Click(object sender, RoutedEventArgs e)
            => Process.Start(this.GetPermissionUrl());

        private string GetPermissionUrl() =>
            "https://discordapp.com/oauth2/authorize?client_id=" + this.ClientIDTextBox.Text + "&scope=bot&permissions=8";
    }
}
