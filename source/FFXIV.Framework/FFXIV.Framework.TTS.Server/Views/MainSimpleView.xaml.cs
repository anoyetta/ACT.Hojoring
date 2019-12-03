using System;
using System.Threading.Tasks;
using System.Windows;
using FFXIV.Framework.Common;
using FFXIV.Framework.TTS.Server.ViewModels;
using NLog;

namespace FFXIV.Framework.TTS.Server.Views
{
    /// <summary>
    /// MainView.xaml の相互作用ロジック
    /// </summary>
    public partial class MainView :
        Window
    {
        #region Singleton

        private static MainView instance = new MainView();

        public static MainView Instance => instance;

        #endregion Singleton

        #region Logger

        private Logger Logger => AppLog.DefaultLogger;

        #endregion Logger

        public MainView()
        {
            this.InitializeComponent();
            this.ViewModel.View = this;
            this.StateChanged += this.MainView_StateChanged;
            this.Closed += async (_, __) =>
            {
                (App.Current as App).CloseApp();
                await Task.Delay(200);
                App.Current.Shutdown();
            };
        }

        public MainSimpleViewModel ViewModel => (MainSimpleViewModel)this.DataContext;

        public void ShowMessage(
            string title,
            string message)
        {
            MessageBox.Show(
                message,
                title,
                MessageBoxButton.OK);
        }

        #region Window state

        private WindowState previousWindowState;

        public void RestoreWindowState()
        {
            this.WindowState = this.previousWindowState;
        }

        private void MainView_StateChanged(object sender, EventArgs e)
        {
            switch (this.WindowState)
            {
                case WindowState.Normal:
                case WindowState.Maximized:
                    this.ShowInTaskbar = true;
                    this.WindowStyle = WindowStyle.SingleBorderWindow;
                    this.previousWindowState = this.WindowState;
                    break;

                case WindowState.Minimized:
                    this.ShowInTaskbar = false;
                    this.WindowStyle = WindowStyle.ToolWindow;
                    break;
            }
        }

        #endregion Window state
    }
}
