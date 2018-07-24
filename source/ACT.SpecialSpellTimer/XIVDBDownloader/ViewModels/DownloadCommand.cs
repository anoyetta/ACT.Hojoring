using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using FFXIV.Framework.Globalization;
using MahApps.Metro.Controls.Dialogs;
using XIVDBDownloader.Constants;
using XIVDBDownloader.Models;

namespace XIVDBDownloader.ViewModels
{
    public class DownloadCommand :
        ICommand
    {
        private MainWindowViewModel viewModel;

        public DownloadCommand(
            MainWindowViewModel viewModel)
        {
            this.viewModel = viewModel;

            this.viewModel.PropertyChanged += (s, e) =>
            {
                this.CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            };
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) =>
            !string.IsNullOrEmpty(this.viewModel.SaveDirectory);

        public async void Execute(object parameter)
        {
            try
            {
                this.viewModel.IsEnabledDownload = false;

                var alsoDownloadActionIcons = false;
                if (this.viewModel.DataModel == DataModels.Action)
                {
                    var result = await this.viewModel.View.ShowMessageDialogAync(
                        "Download Action",
                        "Also download icon images?");

                    if (result == MessageDialogResult.Affirmative)
                    {
                        alsoDownloadActionIcons = true;
                    }
                }

                await Task.Run(() => this.ExecuteCore(alsoDownloadActionIcons));
            }
            finally
            {
                this.viewModel.IsEnabledDownload = true;
            }
        }

        private void AppendLineMessages(
            string message)
        {
            var action = new Action(() =>
            {
                this.viewModel.Messages = message;
            });

            this.viewModel.View.Dispatcher.Invoke(
                action,
                DispatcherPriority.Normal,
                null);
        }

        private void ExecuteCore(
            bool alsoDownloadActionIcons)
        {
            try
            {
                switch (this.viewModel.DataModel)
                {
                    case DataModels.Action:
                        this.DownloadAction(alsoDownloadActionIcons);
                        break;

                    case DataModels.Instance:
                        this.DownloadInstance();
                        break;

                    case DataModels.Placename:
                        this.DownloadPlacename();
                        break;

                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                this.AppendLineMessages("Download Error!");
                this.AppendLineMessages(ex.ToString());
            }
        }

        #region Download Action

        private void DownloadAction(
            bool alsoDownloadActionIcons)
        {
            var model = this.DownloadActionList();

            if (alsoDownloadActionIcons)
            {
                this.DownloadActionIcons(model);
            }
        }

        private void DownloadActionIcons(
            ActionModel model)
        {
            this.AppendLineMessages("Download Action icons.");

            // アイコンを取得する
            model.DownloadIcon(this.viewModel.SaveDirectory);

            this.AppendLineMessages("Download Action icons, Done.");
        }

        private ActionModel DownloadActionList()
        {
            this.AppendLineMessages("Download Action.");

            var model = new ActionModel();
            model.WriteLogLineAction = this.AppendLineMessages;

            // XIVDB からActionのリストを取得する
            this.AppendLineMessages("Download Action list.");
            model.GET(this.viewModel.Language);

            // 取得したリストをCSVに保存する
            this.AppendLineMessages("Save to CSV.");
            model.SaveToCSV(
                Path.Combine(this.viewModel.SaveDirectory, $"Action.{this.viewModel.Language.ToText()}.csv"));

            this.AppendLineMessages("Download Action, Done.");

            return model;
        }

        #endregion Download Action

        #region Download Instance (Zone)

        private void DownloadInstance()
        {
            this.AppendLineMessages("Download Instance.");

            var model = new InstanceModel();

            // XIVDB からInstanceのリストを取得する
            model.GET(this.viewModel.Language);

            // 取得したリストをCSVに保存する
            model.SaveToCSV(
                Path.Combine(this.viewModel.SaveDirectory, $"Instance.{this.viewModel.Language.ToText()}.csv"),
                this.viewModel.Language);

            this.AppendLineMessages("Download Instance, Done.");
        }

        #endregion Download Instance (Zone)

        #region Download Placename (Zone?)

        private void DownloadPlacename()
        {
            this.AppendLineMessages("Download Placename.");

            var model = new InstanceModel();

            // XIVDB からInstanceのリストを取得する
            model.GET(this.viewModel.Language);

            // 取得したリストをCSVに保存する
            model.SaveToCSV(
                Path.Combine(this.viewModel.SaveDirectory, $"Placename.{this.viewModel.Language.ToText()}.csv"),
                this.viewModel.Language);

            this.AppendLineMessages("Download Placename, Done.");
        }

        #endregion Download Placename (Zone?)
    }
}
