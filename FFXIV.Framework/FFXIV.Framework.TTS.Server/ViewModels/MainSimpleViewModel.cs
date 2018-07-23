using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;
using FFXIV.Framework.Common;
using FFXIV.Framework.TTS.Server.Models;
using FFXIV.Framework.TTS.Server.Views;

namespace FFXIV.Framework.TTS.Server.ViewModels
{
    public class MainSimpleViewModel :
        INotifyPropertyChanged
    {
        #region View

        public MainView View { get; set; }

        #endregion View

        private string ipcChannelUri;
        private ICommand refreshIPCChannelCommand;
        private ICommand startCevioCommand;

        public MainSimpleViewModel()
        {
            AppLog.AppendedLog += (s, e) =>
            {
                WPFHelper.BeginInvoke(() =>
                {
                    this.RaisePropertyChanged(nameof(this.Messages));
                });
            };
        }

        public string IPCChannelUri
        {
            get => this.ipcChannelUri;
            set => this.SetProperty(ref this.ipcChannelUri, value);
        }

        public string Messages => AppLog.Log.ToString();

        public ICommand RefreshIPCChannelCommand => (this.refreshIPCChannelCommand ?? (this.refreshIPCChannelCommand = new Command(() =>
        {
            RemoteTTSServer.Instance.Close();
            RemoteTTSServer.Instance.Open();

            this.View.ShowMessage(
                "Refresh IPC Channel",
                "Done.");
        })));

        public ICommand StartCevioCommand => (this.startCevioCommand ?? (this.startCevioCommand = new Command(() =>
        {
            try
            {
                CevioModel.Instance.StartCevio();

                this.View.ShowMessage(
                    "Start CeVIO Creative Studio",
                    "Done.");
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.AppendLine("Error.");
                sb.AppendLine();
                sb.AppendLine(ex.ToString());

                this.View.ShowMessage(
                    "Start CeVIO Creative Studio",
                    sb.ToString());
            }
        })));

        #region INotifyPropertyChanged

        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(
            [CallerMemberName]string propertyName = null)
        {
            this.PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }

        protected virtual bool SetProperty<T>(
            ref T field,
            T value,
            [CallerMemberName]string propertyName = null)
        {
            if (Equals(field, value))
            {
                return false;
            }

            field = value;
            this.PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));

            return true;
        }

        #endregion INotifyPropertyChanged
    }
}
