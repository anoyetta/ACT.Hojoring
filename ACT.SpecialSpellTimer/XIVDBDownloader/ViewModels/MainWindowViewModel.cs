using System.IO;
using System.Text;
using System.Windows.Input;
using FFXIV.Framework.Globalization;
using Prism.Mvvm;
using XIVDBDownloader.Constants;

namespace XIVDBDownloader.ViewModels
{
    public class MainWindowViewModel :
        BindableBase
    {
        #region View

        public MainWindow View { get; set; }

        #endregion View

        #region Properties

        private DataModels dataModel = DataModels.Action;
        private bool isEnabledDownload = true;
        private Locales language = Locales.JA;
        private StringBuilder messages = new StringBuilder();
#if DEBUG
        private string saveDirectory = Path.GetFullPath(@".\resources\xivdb");
#else
        private string saveDirectory = Path.GetFullPath(@"..\..\resources\xivdb");
#endif

        public DataModels DataModel
        {
            get => this.dataModel;
            set => this.SetProperty(ref this.dataModel, value);
        }

        public bool IsEnabledDownload
        {
            get => this.isEnabledDownload;
            set => this.SetProperty(ref this.isEnabledDownload, value);
        }

        public Locales Language
        {
            get => this.language;
            set => this.SetProperty(ref this.language, value);
        }

        public string Messages
        {
            get => this.messages.ToString();
            set
            {
                this.messages.AppendLine(value);
                this.View.MessagesScrollViewer.ScrollToEnd();
                this.RaisePropertyChanged();
            }
        }

        public string SaveDirectory
        {
            get => this.saveDirectory;
            set => this.SetProperty(ref this.saveDirectory, value);
        }

        public void ClearMessages()
        {
            this.messages.Clear();
            this.RaisePropertyChanged(nameof(this.Messages));
        }

        #endregion Properties

        #region Commands

        private ICommand downloadCommand;

        public ICommand DownloadCommand =>
            this.downloadCommand ?? (this.downloadCommand = new DownloadCommand(this));

        #endregion Commands
    }
}
