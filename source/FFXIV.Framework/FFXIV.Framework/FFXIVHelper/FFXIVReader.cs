using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Advanced_Combat_Tracker;
using FFXIV.Framework.Common;
using FFXIV.Framework.Extensions;
using FFXIV.Framework.WPF.Views;
using Prism.Mvvm;
using TamanegiMage.FFXIV_MemoryReader.Base;
using TamanegiMage.FFXIV_MemoryReader.Model;

namespace FFXIV.Framework.FFXIVHelper
{
    public class FFXIVReader :
        BindableBase
    {
        #region Singleton

        private static FFXIVReader instance;

        public static FFXIVReader Instance =>
            instance ?? (instance = new FFXIVReader());

        public static void Free()
        {
            if (instance != null)
            {
                if (instance.MemoryPlugin != null)
                {
                    instance.MemoryPlugin.DeInitPlugin();
                    instance.MemoryPlugin = null;
                }

                instance = null;
            }
        }

        #endregion Singleton

        private MemoryPlugin MemoryPlugin { get; set; } = null;

        public Version Version => typeof(MemoryPlugin)?.Assembly?.GetName()?.Version;

        public AssemblyName AssemblyName => typeof(MemoryPlugin)?.Assembly?.GetName();

        public Task<FFXIVReaderStartingResult> WaitForReaderToStartedAsync(
            TabPage baseTabPage) => Task.Run(() =>
        {
            var result = new FFXIVReaderStartingResult();

            lock (this)
            {
                // 旧の単独DLL版が存在したら念のため削除する
                var dll = Path.Combine(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    "FFXIV_MemoryReader.dll");
                if (File.Exists(dll))
                {
                    var message = string.Empty;

                    try
                    {
                        File.Delete(dll);

                        message = string.Empty;
                        message += $@"""FFXIV_MemoryReader.dll"" was deleted.\n";
                        message += $@"Please, restart ACT.";
                        WPFHelper.BeginInvoke(() => ModernMessageBox.ShowDialog(
                            message,
                            "Warning",
                            MessageBoxButton.OK));
                    }
                    catch (Exception)
                    {
                    }

                    if (File.Exists(dll))
                    {
                        message = string.Empty;
                        message += $@"""FFXIV_MemoryReader.dll"" is exists yet.\n";
                        message += $@"You should delete ""FFXIV_MemoryReader.dll"".\n";
                        message += Environment.NewLine;
                        message += "Path:\n";
                        message += dll;

                        WPFHelper.BeginInvoke(() => ModernMessageBox.ShowDialog(
                            message,
                            "Warning",
                            MessageBoxButton.OK));
                    }
                }

                if (this.MemoryPlugin != null)
                {
                    result.Status = FFXIVReaderStartingStatus.AlreadyStarted;
                    return result;
                }

                var succeeded = false;

                this.ActInvoke(() =>
                {
                    var parentTabControl = baseTabPage.Parent as TabControl;
                    if (parentTabControl == null)
                    {
                        return;
                    }

                    var memoryReaderTabPage = new TabPage();
                    var dummyLabel = new Label();
                    parentTabControl.TabPages.Add(memoryReaderTabPage);

                    try
                    {
                        this.MemoryPlugin = new MemoryPlugin();
                        this.MemoryPlugin.InitPlugin(memoryReaderTabPage, dummyLabel);

                        succeeded = dummyLabel.Text.ContainsIgnoreCase("Started");

                        if (succeeded)
                        {
                            result.Status = FFXIVReaderStartingStatus.Started;
                        }
                        else
                        {
                            result.Status = FFXIVReaderStartingStatus.Error;
                            result.Message = "Error occurred initializing FFXIV_MemoryReader.";
                            this.MemoryPlugin?.DeInitPlugin();
                            this.MemoryPlugin = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Status = FFXIVReaderStartingStatus.Error;
                        result.Message = "Fatal error occurred initializing FFXIV_MemoryReader.";
                        result.Exception = ex;

                        WPFHelper.BeginInvoke(() => ModernMessageBox.ShowDialog(
                            result.Message,
                            "Fatal Error",
                            MessageBoxButton.OK,
                            ex));
                    }
                });

                WPFHelper.Invoke(() => this.IsAvailable = succeeded);
            }

            return result;
        });

        private bool isAvailable;

        public bool IsAvailable
        {
            get => this.isAvailable;
            set => this.SetProperty(ref this.isAvailable, value);
        }

        public List<CombatantV1> GetCombatantsV1()
            => this.MemoryPlugin?.GetCombatantsV1();

        public CameraInfoV1 GetCameraInfoV1()
            => this.MemoryPlugin?.GetCameraInfoV1();

        public List<HotbarRecastV1> GetHotbarRecastV1()
            => this.MemoryPlugin?.GetHotbarRecastV1();

        private void ActInvoke(Action action)
        {
            if (ActGlobals.oFormActMain.InvokeRequired)
            {
                ActGlobals.oFormActMain.Invoke(action);
            }
            else
            {
                action();
            }
        }
    }

    public enum FFXIVReaderStartingStatus
    {
        Started = 0,
        AlreadyStarted,
        Error
    }

    public class FFXIVReaderStartingResult
    {
        public FFXIVReaderStartingStatus Status { get; set; }

        public string Message { get; set; }

        public Exception Exception { get; set; }

        public void WriteLog(
            NLog.Logger logger)
        {
            if (logger == null)
            {
                return;
            }

            switch (this.Status)
            {
                case FFXIVReaderStartingStatus.AlreadyStarted:
                    return;

                case FFXIVReaderStartingStatus.Started:
                    logger.Trace(FFXIVReader.Instance.AssemblyName + " start.");
                    break;

                case FFXIVReaderStartingStatus.Error:
                    logger.Error(this.Exception, this.Message);
                    break;
            }
        }
    }
}
