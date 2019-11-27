using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using FFXIV.Framework.Common;
using FFXIV.Framework.TTS.Server.Models;
using FFXIV.Framework.TTS.Server.Views;

namespace FFXIV.Framework.TTS.Server
{
    public partial class TaskTrayComponent :
        Component
    {
        public TaskTrayComponent()
        {
            this.InitializeComponent();

            this.ShowMenuItem.Font = new Font(this.ShowMenuItem.Font, System.Drawing.FontStyle.Bold);

            this.NotifyIcon.Text =
                $"{EnvironmentHelper.GetProductName()}\n{EnvironmentHelper.GetVersion().ToStringShort()}";

            this.ShowMenuItem.Click += this.ShowMenuItem_Click;
            this.StartCevioMenuItem.Click += this.StartCevioMenuItem_Click;
            this.ExitMenuItem.Click += this.ExitMenuItem_Click;
            this.NotifyIcon.DoubleClick += this.NotifyIcon_DoubleClick;
        }

        public TaskTrayComponent(IContainer container)
        {
            container.Add(this);
            this.InitializeComponent();
        }

        public void HideNotifyIcon()
        {
            this.NotifyIcon.Visible = false;
        }

        private void ExitMenuItem_Click(object sender, EventArgs e)
        {
            (App.Current as App)?.CloseApp();
            Thread.Sleep(500);
            Application.Current.Shutdown();
        }

        private void NotifyIcon_DoubleClick(object sender, EventArgs e)
        {
            this.ShowMainWindow();
        }

        private void ShowMainWindow()
        {
            MainView.Instance.Show();
            MainView.Instance.RestoreWindowState();
            MainView.Instance.Activate();
        }

        private void ShowMenuItem_Click(object sender, EventArgs e)
        {
            this.ShowMainWindow();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void StartCevioMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                CevioModel.Instance.StartCevio();
            }
            catch (Exception ex)
            {
                App.ShowMessageBoxException(
                    "Start CeVIO Creative Studio Error",
                    ex);
            }
        }
    }
}
