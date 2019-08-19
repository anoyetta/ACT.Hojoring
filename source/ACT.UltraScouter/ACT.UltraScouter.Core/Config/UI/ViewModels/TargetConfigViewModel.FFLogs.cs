using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Windows.Input;
using ACT.UltraScouter.Models;
using ACT.UltraScouter.Models.FFLogs;
using FFXIV.Framework.Common;
using FFXIV.Framework.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Prism.Commands;

namespace ACT.UltraScouter.Config.UI.ViewModels
{
    public partial class TargetConfigViewModel
    {
        public IEnumerable<FFLogsRegions> FFLogsRegions => Enum.GetValues(typeof(FFLogsRegions)).Cast<FFLogsRegions>();

        public IEnumerable<EnumContainer<FFLogsPartitions>> FFLogsPartitions => Enum.GetValues(typeof(FFLogsPartitions))
            .Cast<FFLogsPartitions>()
            .Select(x => new EnumContainer<FFLogsPartitions>(x));

        public IEnumerable<FFLogsDifficulty> FFLogsDifficulties => Enum.GetValues(typeof(FFLogsDifficulty)).Cast<FFLogsDifficulty>();

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
                this.FFLogsTestResult += $"Character Name : {model.CharacterNameFull} ({model.CharacterNameFull.GetMD5()})\n";
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
    }
}
