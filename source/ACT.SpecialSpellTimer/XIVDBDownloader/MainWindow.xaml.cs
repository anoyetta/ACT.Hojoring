using System.Threading.Tasks;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using XIVDBDownloader.ViewModels;

namespace XIVDBDownloader
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow :
        MetroWindow
    {
        public MainWindow()
        {
            this.InitializeComponent();

            this.ViewModel.View = this;
        }

        /// <summary>ViewModel</summary>
        public MainWindowViewModel ViewModel => (MainWindowViewModel)this.DataContext;

        public Task<MessageDialogResult> ShowMessageDialogAync(
            string title,
            string message)
        {
            return this.ShowMessageAsync(
                title,
                message,
                MessageDialogStyle.AffirmativeAndNegative);
        }
    }
}
