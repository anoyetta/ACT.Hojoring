using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using ACT.UltraScouter.Models;
using ACT.UltraScouter.Workers;
using FFXIV.Framework.Common;
using NLog;
using Prism.Commands;
using Prism.Mvvm;

namespace ACT.UltraScouter.Config.UI.ViewModels
{
    public class MobListConfigViewModel :
        BindableBase
    {
        #region Logger

        private Logger logger = AppLog.DefaultLogger;

        #endregion Logger

        public MobList MobList => Settings.Instance.MobList;

        public DirectionOrigin[] DirectionOrigins => Enum.GetValues(typeof(DirectionOrigin)) as DirectionOrigin[];

        private ICommand testModeCommand;

        public ICommand TestModeCommand =>
            this.testModeCommand ?? (this.testModeCommand = new DelegateCommand<bool?>((testMode) =>
            {
                MobListModel.Instance.ClearMobList();
            }));

        private ICommand refreshViewCommand;

        public ICommand RefreshViewCommand =>
            this.refreshViewCommand ?? (this.refreshViewCommand = new DelegateCommand(() =>
            {
                MobListWorker.Instance.RefreshViewsQueue = true;
            }));

        private ICommand openTargetMobListCommand;

        public ICommand OpenTargetMobListCommand =>
            this.openTargetMobListCommand ?? (this.openTargetMobListCommand = new DelegateCommand(() =>
            {
                var f = this.MobList.MobListFile;

                if (!File.Exists(f))
                {
                    this.logger.Error($"TargetMobList not found. {f}");
                    return;
                }

                Process.Start(f);
            }));

        private ICommand reloadTargetMobListCommand;

        public ICommand ReloadTargetMobListCommand =>
            this.reloadTargetMobListCommand ?? (this.reloadTargetMobListCommand = new DelegateCommand(() =>
            {
                var f = this.MobList.MobListFile;

                if (!File.Exists(f))
                {
                    this.logger.Error($"TargetMobList not found. {f}");
                    return;
                }

                this.MobList.LoadTargetMobList();
            }));

        private ICommand refreshMobListCommand;

        public ICommand RefreshMobListCommand =>
            this.refreshMobListCommand ?? (this.refreshMobListCommand = new DelegateCommand(() =>
            {
                MobListModel.Instance.ClearMobList();
            }));

        #region Me Text

        private ICommand changeFontCommand1;
        private ICommand changeTextColorCommand1;
        private ICommand changeTextOutlineColorCommand1;

        public ICommand ChangeFontCommand1 =>
            this.changeFontCommand1 ??
            (this.changeFontCommand1 =
            new ChangeFontCommand(
                (font) => this.MobList.MeDisplayText.Font = font));

        public ICommand ChangeTextColorCommand1 =>
            this.changeTextColorCommand1 ??
            (this.changeTextColorCommand1 =
            new ChangeColorCommand(
                (color) => this.MobList.MeDisplayText.Color = color));

        public ICommand ChangeTextOutlineColorCommand1 =>
            this.changeTextOutlineColorCommand1 ??
            (this.changeTextOutlineColorCommand1 =
            new ChangeColorCommand(
                (color) => this.MobList.MeDisplayText.OutlineColor = color));

        #endregion Me Text

        #region Mob Font

        private ICommand changeMobFontCommand;

        public ICommand ChangeMobFontCommand =>
            this.changeMobFontCommand ??
            (this.changeMobFontCommand =
            new ChangeFontCommand(
                (font) => MobInfo.UpdateFont(font)));

        #endregion Mob Font

        #region MobEX Text

        private ICommand changeTextColorCommand2;
        private ICommand changeTextOutlineColorCommand2;

        public ICommand ChangeTextColorCommand2 =>
            this.changeTextColorCommand2 ??
            (this.changeTextColorCommand2 =
            new ChangeColorCommand(
                (color) =>
                {
                    MobInfo.MobEXText.Color = color;
                    this.MobList.MobEXColor.Color = color;
                }));

        public ICommand ChangeTextOutlineColorCommand2 =>
            this.changeTextOutlineColorCommand2 ??
            (this.changeTextOutlineColorCommand2 =
            new ChangeColorCommand(
                (color) =>
                {
                    MobInfo.MobEXText.OutlineColor = color;
                    this.MobList.MobEXColor.OutlineColor = color;
                }));

        #endregion MobEX Text

        #region MobS Text

        private ICommand changeTextColorCommand3;
        private ICommand changeTextOutlineColorCommand3;

        public ICommand ChangeTextColorCommand3 =>
            this.changeTextColorCommand3 ??
            (this.changeTextColorCommand3 =
            new ChangeColorCommand(
                (color) =>
                {
                    MobInfo.MobSText.Color = color;
                    this.MobList.MobSColor.Color = color;
                }));

        public ICommand ChangeTextOutlineColorCommand3 =>
            this.changeTextOutlineColorCommand3 ??
            (this.changeTextOutlineColorCommand3 =
            new ChangeColorCommand(
                (color) =>
                {
                    MobInfo.MobSText.OutlineColor = color;
                    this.MobList.MobSColor.OutlineColor = color;
                }));

        #endregion MobS Text

        #region MobA Text

        private ICommand changeTextColorCommand4;
        private ICommand changeTextOutlineColorCommand4;

        public ICommand ChangeTextColorCommand4 =>
            this.changeTextColorCommand4 ??
            (this.changeTextColorCommand4 =
            new ChangeColorCommand(
                (color) =>
                {
                    MobInfo.MobAText.Color = color;
                    this.MobList.MobAColor.Color = color;
                }));

        public ICommand ChangeTextOutlineColorCommand4 =>
            this.changeTextOutlineColorCommand4 ??
            (this.changeTextOutlineColorCommand4 =
            new ChangeColorCommand(
                (color) =>
                {
                    MobInfo.MobAText.OutlineColor = color;
                    this.MobList.MobAColor.OutlineColor = color;
                }));

        #endregion MobA Text

        #region MobB Text

        private ICommand changeTextColorCommand5;
        private ICommand changeTextOutlineColorCommand5;

        public ICommand ChangeTextColorCommand5 =>
            this.changeTextColorCommand5 ??
            (this.changeTextColorCommand5 =
            new ChangeColorCommand(
                (color) =>
                {
                    MobInfo.MobBText.Color = color;
                    this.MobList.MobBColor.Color = color;
                }));

        public ICommand ChangeTextOutlineColorCommand5 =>
            this.changeTextOutlineColorCommand5 ??
            (this.changeTextOutlineColorCommand5 =
            new ChangeColorCommand(
                (color) =>
                {
                    MobInfo.MobBText.OutlineColor = color;
                    this.MobList.MobBColor.OutlineColor = color;
                }));

        #endregion MobB Text

        #region MobOther Text

        private ICommand changeTextColorCommand6;
        private ICommand changeTextOutlineColorCommand6;

        public ICommand ChangeTextColorCommand6 =>
            this.changeTextColorCommand6 ??
            (this.changeTextColorCommand6 =
            new ChangeColorCommand(
                (color) =>
                {
                    MobInfo.MobOtherText.Color = color;
                    this.MobList.MobOtherColor.Color = color;
                }));

        public ICommand ChangeTextOutlineColorCommand6 =>
            this.changeTextOutlineColorCommand6 ??
            (this.changeTextOutlineColorCommand6 =
            new ChangeColorCommand(
                (color) =>
                {
                    MobInfo.MobOtherText.OutlineColor = color;
                    this.MobList.MobOtherColor.OutlineColor = color;
                }));

        #endregion MobOther Text
    }
}
