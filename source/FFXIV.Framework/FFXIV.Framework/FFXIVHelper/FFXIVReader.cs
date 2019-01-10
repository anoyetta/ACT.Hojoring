using System;
using System.Collections.Generic;
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

        #region Logger

        private static NLog.Logger AppLogger => AppLog.DefaultLogger;

        #endregion Logger

        private MemoryPlugin MemoryPlugin { get; set; } = null;

        public Task WaitForReaderToStartedAsync(
            TabPage baseTabPage) => Task.Run(() =>
        {
            lock (this)
            {
                if (this.MemoryPlugin != null)
                {
                    return;
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
                            AppLogger.Trace("FFXIV_MemoryReader started.");
                        }
                        else
                        {
                            AppLogger.Error("Error occurred initializing FFXIV_MemoryReader.");
                            this.MemoryPlugin?.DeInitPlugin();
                            this.MemoryPlugin = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        const string Message = "Fatal error occurred initializing FFXIV_MemoryReader.";

                        ModernMessageBox.ShowDialog(
                            Message,
                            "Fatal Error",
                            MessageBoxButton.OK,
                            ex);

                        AppLogger.Error(ex, Message);
                    }
                });

                WPFHelper.Invoke(() => this.IsAvailable = succeeded);
            }
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
}
