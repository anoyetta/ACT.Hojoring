using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using ACT.UltraScouter.Models;
using ACT.UltraScouter.Models.FFLogs;
using ACT.UltraScouter.ViewModels;
using ACT.UltraScouter.ViewModels.Bases;
using ACT.UltraScouter.Workers;
using Advanced_Combat_Tracker;
using FFXIV.Framework.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Prism.Commands;
using Prism.Mvvm;

namespace ACT.UltraScouter.Config.UI.ViewModels
{
    public class TargetConfigViewModel :
        BindableBase
    {
        /// <summary>
        /// このConfigがどのCategoryのビューを扱うのか？
        /// </summary>
        protected virtual ViewCategories ViewCategory => ViewCategories.Target;

        public virtual TargetName Name => Settings.Instance.TargetName;
        public virtual TargetHP HP => Settings.Instance.TargetHP;
        public virtual TargetAction Action => Settings.Instance.TargetAction;
        public virtual TargetDistance Distance => Settings.Instance.TargetDistance;
        public virtual FFLogs FFLogs => Settings.Instance.FFLogs;

        private OpenFileDialog selectWaveSoundDialog = new OpenFileDialog()
        {
            RestoreDirectory = true,
            Filter = "WAVE soundfile|*.wav",
            DefaultExt = ".wav",
            SupportMultiDottedExtensions = true,
            InitialDirectory = DirectoryHelper.FindSubDirectory(@"resources\wav"),
        };

        /// <summary>
        /// ViewModelを取得する
        /// </summary>
        /// <typeparam name="T">
        /// ViewModelの型</typeparam>
        /// <returns>
        /// ViewModel</returns>
        protected T GetViewModel<T>() where T : OverlayViewModelBase
            => MainWorker.Instance.GetViewModelList(this.ViewCategory).FirstOrDefault(x => x is T) as T;

        #region TargetName

        private ICommand targetNameDisplayTextFontCommand;
        private ICommand targetNameDisplayTextColorCommand;
        private ICommand targetNameDisplayTextOutlineColorCommand;

        public ICommand TargetNameDisplayTextFontCommand =>
            this.targetNameDisplayTextFontCommand ??
            (this.targetNameDisplayTextFontCommand =
            new ChangeFontCommand((font) => this.Name.DisplayText.Font = font));

        public ICommand TargetNameDisplayTextColorCommand =>
            this.targetNameDisplayTextColorCommand ??
            (this.targetNameDisplayTextColorCommand =
            new ChangeColorCommand((color) => this.Name.DisplayText.Color = color));

        public ICommand TargetNameDisplayTextOutlineColorCommand =>
            this.targetNameDisplayTextOutlineColorCommand ??
            (this.targetNameDisplayTextOutlineColorCommand =
            new ChangeColorCommand((color) => this.Name.DisplayText.OutlineColor = color));

        #endregion TargetName

        #region TargetAction

        public virtual string DummyAction
        {
            get => TargetInfoWorker.Instance.DummyAction;
            set => TargetInfoWorker.Instance.DummyAction = value;
        }

        private ICommand targetActionDisplayTextFontCommand;
        private ICommand targetActionDisplayTextColorCommand;
        private ICommand targetActionDisplayTextOutlineColorCommand;

        private ICommand targetActionProgressBarOutlineColorCommand;
        private ICommand targetActionBarAddCommand;

        private ICommand openFileDialogActionCommand;
        private ICommand playSoundActionCommand;

        private ICommand dummyActionResetCommand;

        public ICommand TargetActionDisplayTextFontCommand =>
            this.targetActionDisplayTextFontCommand ??
            (this.targetActionDisplayTextFontCommand =
            new ChangeFontCommand((font) => this.Action.DisplayText.Font = font));

        public ICommand TargetActionDisplayTextColorCommand =>
            this.targetActionDisplayTextColorCommand ??
            (this.targetActionDisplayTextColorCommand =
            new ChangeColorCommand((color) => this.Action.DisplayText.Color = color));

        public ICommand TargetActionDisplayTextOutlineColorCommand =>
            this.targetActionDisplayTextOutlineColorCommand ??
            (this.targetActionDisplayTextOutlineColorCommand =
            new ChangeColorCommand((color) => this.Action.DisplayText.OutlineColor = color));

        public ICommand TargetActionProgressBarOutlineColorCommand =>
            this.targetActionProgressBarOutlineColorCommand ??
            (this.targetActionProgressBarOutlineColorCommand =
            new ChangeColorCommand(
                (color) => this.Action.ProgressBar.OutlineColor = color,
                () => this.RefreshActionCommand?.Execute(null)));

        public ICommand TargetActionBarAddCommand =>
            this.targetActionBarAddCommand ??
            (this.targetActionBarAddCommand = new DelegateCommand(() =>
            {
                var maxRange = this.Action.ProgressBar.ColorRange
                    .OrderByDescending(x => x.Max)
                    .FirstOrDefault();

                this.Action.ProgressBar.ColorRange.Add(new ProgressBarColorRange()
                {
                    Color = maxRange?.Color ?? Colors.White,
                    Min = maxRange?.Max ?? 0,
                    Max = maxRange?.Max ?? 0,
                });

                this.RefreshActionCommand?.Execute(null);
            }));

        public ICommand OpenFileDialogActionCommand =>
            this.openFileDialogActionCommand ??
            (this.openFileDialogActionCommand = new DelegateCommand(() =>
            {
                this.selectWaveSoundDialog.FileName = this.Action.WaveFile;
                var result = this.selectWaveSoundDialog.ShowDialog(
                    ActGlobals.oFormActMain);

                if (result == DialogResult.OK)
                {
                    this.Action.WaveFile = this.selectWaveSoundDialog.FileName;
                }
            }));

        public ICommand PlaySoundActionCommand =>
            this.playSoundActionCommand ??
            (this.playSoundActionCommand = new DelegateCommand(() =>
            {
                if (!string.IsNullOrEmpty(this.Action.WaveFile))
                {
                    WavePlayer.Instance.Play(
                        this.Action.WaveFile,
                        Settings.Instance.WaveVolume / 100);
                }
            }));

        public ICommand DummyActionResetCommand =>
            this.dummyActionResetCommand ??
            (this.dummyActionResetCommand = new DelegateCommand(async () =>
           {
               var text = this.DummyAction;
               this.DummyAction = string.Empty;
               await Task.Delay(TimeSpan.FromSeconds(0.2));
               this.DummyAction = text;
           }));

        private ICommand refreshActionCommand;

        public ICommand RefreshActionCommand =>
            this.refreshActionCommand ?? (this.refreshActionCommand = new DelegateCommand(
                () => this.GetViewModel<ActionViewModel>()?.RaiseAllPropertiesChanged()));

        #endregion TargetAction

        #region TargetDistance

        private ICommand targetDistanceDisplayTextFontCommand;
        private ICommand targetDistanceDisplayTextColorCommand;
        private ICommand targetDistanceDisplayTextOutlineColorCommand;

        public ICommand TargetDistanceDisplayTextFontCommand =>
            this.targetDistanceDisplayTextFontCommand ??
            (this.targetDistanceDisplayTextFontCommand =
            new ChangeFontCommand((font) => this.Distance.DisplayText.Font = font));

        public ICommand TargetDistanceDisplayTextColorCommand =>
            this.targetDistanceDisplayTextColorCommand ??
            (this.targetDistanceDisplayTextColorCommand =
            new ChangeColorCommand((color) => this.Distance.DisplayText.Color = color));

        public ICommand TargetDistanceDisplayTextOutlineColorCommand =>
            this.targetDistanceDisplayTextOutlineColorCommand ??
            (this.targetDistanceDisplayTextOutlineColorCommand =
            new ChangeColorCommand((color) => this.Distance.DisplayText.OutlineColor = color));

        #endregion TargetDistance

        #region TargetHP

        private ICommand targetHPDisplayTextFontCommand;
        private ICommand targetHPDisplayTextColorCommand;
        private ICommand targetHPDisplayTextOutlineColorCommand;

        private ICommand targetHPProgressBarOutlineColorCommand;
        private ICommand targetHPBarAddCommand;

        public ICommand TargetHPDisplayTextFontCommand =>
            this.targetHPDisplayTextFontCommand ??
            (this.targetHPDisplayTextFontCommand =
            new ChangeFontCommand((font) => this.HP.DisplayText.Font = font));

        public ICommand TargetHPDisplayTextColorCommand =>
            this.targetHPDisplayTextColorCommand ??
            (this.targetHPDisplayTextColorCommand =
            new ChangeColorCommand(
                (color) => this.HP.DisplayText.Color = color,
                () => this.RefreshHPCommand?.Execute(null)));

        public ICommand TargetHPDisplayTextOutlineColorCommand =>
            this.targetHPDisplayTextOutlineColorCommand ??
            (this.targetHPDisplayTextOutlineColorCommand =
            new ChangeColorCommand(
                (color) => this.HP.DisplayText.OutlineColor = color,
                () => this.RefreshHPCommand?.Execute(null)));

        public ICommand TargetHPProgressBarOutlineColorCommand =>
            this.targetHPProgressBarOutlineColorCommand ??
            (this.targetHPProgressBarOutlineColorCommand =
            new ChangeColorCommand(
                (color) => this.HP.ProgressBar.OutlineColor = color,
                () => this.RefreshHPCommand?.Execute(null)));

        public ICommand TargetHPBarAddCommand =>
            this.targetHPBarAddCommand ??
            (this.targetHPBarAddCommand = new DelegateCommand(() =>
            {
                var maxRange = this.HP.ProgressBar.ColorRange
                    .OrderByDescending(x => x.Max)
                    .FirstOrDefault();

                this.HP.ProgressBar.ColorRange.Add(new ProgressBarColorRange()
                {
                    Color = maxRange?.Color ?? Colors.White,
                    Min = maxRange?.Max ?? 0,
                    Max = maxRange?.Max ?? 0,
                });

                this.RefreshHPCommand?.Execute(null);
            }));

        private ICommand refreshHPCommand;

        public ICommand RefreshHPCommand =>
            this.refreshHPCommand ?? (this.refreshHPCommand = new DelegateCommand(
                () => this.GetViewModel<HPViewModel>()?.RaiseAllPropertiesChanged()));

        #endregion TargetHP

        #region FFLogs

        public IEnumerable<FFLogsRegions> FFLogsRegions => Enum.GetValues(typeof(FFLogsRegions)).Cast<FFLogsRegions>();

        private ICommand ffLogsDisplayTextFontCommand;
        private ICommand ffLogsDisplayTextColorCommand;
        private ICommand ffLogsDisplayTextOutlineColorCommand;
        private ICommand ffLogsBackgroundColorCommand;
        private ICommand ffLogsTestAPICommand;
        private ICommand ffLogsGetAPIKeyCommand;

        public ICommand FFLogsDisplayTextFontCommand =>
            this.ffLogsDisplayTextFontCommand ??
            (this.ffLogsDisplayTextFontCommand =
            new ChangeFontCommand((font) => this.FFLogs.DisplayText.Font = font));

        public ICommand FFLogsDisplayTextColorCommand =>
            this.ffLogsDisplayTextColorCommand ??
            (this.ffLogsDisplayTextColorCommand =
            new ChangeColorCommand((color) => this.FFLogs.DisplayText.Color = color));

        public ICommand FFLogsDisplayTextOutlineColorCommand =>
            this.ffLogsDisplayTextOutlineColorCommand ??
            (this.ffLogsDisplayTextOutlineColorCommand =
            new ChangeColorCommand((color) => this.FFLogs.DisplayText.OutlineColor = color));

        public ICommand FFLogsBackgroundColorCommand =>
            this.ffLogsBackgroundColorCommand ??
            (this.ffLogsBackgroundColorCommand =
            new ChangeColorCommand((color) => this.FFLogs.Background = color));

        public ICommand FFLogsTestAPICommand =>
            this.ffLogsTestAPICommand ??
            (this.ffLogsTestAPICommand =
            new DelegateCommand(this.ExecuteTestAPI));

        public ICommand FFLogsGetAPIKeyCommand =>
            this.ffLogsGetAPIKeyCommand ??
            (this.ffLogsGetAPIKeyCommand =
            new DelegateCommand(() => Process.Start("https://www.fflogs.com/profile")));

        public ParseTotalModel FFLogsTestParseTotalModel { get; } = new ParseTotalModel();

        private string ffLogsTestResult;

        public string FFLogsTestResult
        {
            get => this.ffLogsTestResult;
            set => this.SetProperty(ref this.ffLogsTestResult, value);
        }

        private async void ExecuteTestAPI()
        {
            var model = this.FFLogsTestParseTotalModel;

            try
            {
                if (string.IsNullOrEmpty(this.FFLogs.ApiKey))
                {
                    this.FFLogsTestResult = @"""API Key"" required.";
                    return;
                }

                if (string.IsNullOrEmpty(model.CharacterNameFull))
                {
                    this.FFLogsTestResult = @"""Character Name"" required.";
                    return;
                }

                if (string.IsNullOrEmpty(model.Server))
                {
                    this.FFLogsTestResult = @"""Server"" required.";
                    return;
                }

                await model.GetParseAsync(
                    model.CharacterNameFull,
                    model.Server,
                    this.FFLogs.ServerRegion,
                    null,
                    true);

                this.FFLogsTestResult = string.Empty;
                this.FFLogsTestResult += $"Status Code : {model.HttpStatusCode} ({(int)model.HttpStatusCode})\n";
                this.FFLogsTestResult += $"\n";
                this.FFLogsTestResult +=
                    model.HttpStatusCode == HttpStatusCode.OK ?
                    $"Content :\n{JValue.Parse(model.ResponseContent).ToString(Formatting.Indented)}\n" :
                    $"Content : NO DATA\n";

                TargetInfoModel.APITestResultParseTotal = model;
            }
            catch (Exception ex)
            {
                this.FFLogsTestResult = ex.ToString();
            }
        }

        #endregion FFLogs
    }
}
