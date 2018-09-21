using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Threading;
using System.Xml.Linq;
using Advanced_Combat_Tracker;
using FFXIV.Framework.Common;
using FFXIV.Framework.Extensions;
using FFXIV.Framework.WPF.Views;
using Prism.Mvvm;
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
                instance.timer?.Stop();
                instance.timer?.Dispose();
                instance.timer = null;
                instance = null;
            }
        }

        #endregion Singleton

        #region Logger

        private static NLog.Logger AppLogger => AppLog.DefaultLogger;

        #endregion Logger

        private dynamic MemoryPlugin { get; set; } = null;

        private dynamic Core { get; set; } = null;

        private System.Timers.Timer timer = new System.Timers.Timer(5 * 1000);

        public FFXIVReader()
        {
            this.StartTimer();
        }

        private volatile bool isAddedMemoryReader = false;

        /// <summary>
        /// MemoryReaderがスタートするまで待つ
        /// </summary>
        public Task WaitForReaderToStartedAsync() => Task.Run(() =>
        {
            var config = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    @"Advanced Combat Tracker\Config\Advanced Combat Tracker.config.xml");

            if (!File.Exists(config))
            {
                return;
            }

            try
            {
                lock (this)
                {
                    // 今回の処理でMemoryReaderを追加した場合は即時に抜ける
                    if (this.isAddedMemoryReader)
                    {
                        return;
                    }

                    var xdoc = default(XDocument);
                    using (var sr = new StreamReader(config))
                    {
                        xdoc = XDocument.Load(sr);
                    }

                    if (xdoc == null)
                    {
                        return;
                    }

                    var reader = (
                        from plugin in xdoc.Descendants("Plugin")
                        where
                        plugin.Attribute("Path").Value.ContainsIgnoreCase("FFXIV_MemoryReader") &&
                        plugin.Attribute("Enabled").Value.ContainsIgnoreCase("True")
                        select
                        plugin).FirstOrDefault();

                    if (reader == null)
                    {
                        // FFXIV_MemoryPlugin を追加する
                        this.isAddedMemoryReader = AddMemoryPlugin();
                    }
                }

                if (this.isAddedMemoryReader)
                {
                    return;
                }

                var interval = 0.5;
                var timeout = 30;
                var detectTimes = timeout / interval;

                for (int i = 0; i < detectTimes; i++)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(interval));

                    var reader = ActGlobals.oFormActMain.ActPlugins
                        .FirstOrDefault(x =>
                            x.pluginFile.Name.ContainsIgnoreCase("FFXIV_MemoryReader"));

                    if (reader != null &&
                        reader.lblPluginStatus != null &&
                        reader.lblPluginStatus.Text != null)
                    {
                        if (reader.lblPluginStatus.Text.ContainsIgnoreCase("Started"))
                        {
                            Thread.Sleep(CommonHelper.GetRandomTimeSpan(0.01, 0.1));
                            break;
                        }
                    }
                }
            }
            catch (Exception)
            {
            }

            // FFXIV_MemoryReader を追加する
            bool AddMemoryPlugin()
            {
                // FFXIV_MemoryReader の設定を追加する
                var path = Path.Combine(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    "FFXIV_MemoryReader.dll");

                if (!File.Exists(path))
                {
                    return false;
                }

                WPFHelper.Invoke(() =>
                {
                    try
                    {
                        var plugin = ActGlobals.oFormActMain.AddPluginPanel(path, true);
                        if (plugin != null &&
                            plugin.cbEnabled != null)
                        {
                            plugin.cbEnabled.Checked = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        const string Failed = "Load FFXIV_MemoryReader failed.";

                        ModernMessageBox.ShowDialog(
                            Failed,
                            "Error",
                            MessageBoxButton.OK,
                            ex);

                        AppLogger.Error(ex, Failed);
                    }
                }, DispatcherPriority.Normal);

                const string Caption = "Please Reboot";
                const string Message =
                    "\n\n" +
                    "FFXIV_MemoryReader を追加しました。\n" +
                    "プラグインを有効にするためにACTを再起動してください。\n\n\n" +
                    "FFXIV_MemoryReader plugin was added.\n" +
                    "Please reboot ACT to enable FFXIV_MemoryReader.\n\n";

                WPFHelper.BeginInvoke(
                    () => ModernMessageBox.ShowDialog(Message, Caption),
                    DispatcherPriority.Background);

                return true;
            }
        });

        private bool isAvailable = false;

        public bool IsAvailable
        {
            get => this.isAvailable;
            set => this.SetProperty(ref this.isAvailable, value);
        }

        private void StartTimer()
        {
            this.timer.AutoReset = true;
            this.timer.Elapsed += this.Timer_Elapsed;
            this.timer.Start();
        }

        private volatile bool logged = false;
        private volatile bool loggedError = false;

        private void Timer_Elapsed(
            object sender,
            ElapsedEventArgs e)
        {
            lock (this)
            {
                try
                {
                    var plugin = ActGlobals.oFormActMain.ActPlugins
                        .FirstOrDefault(x =>
                            x.pluginFile.Name.ContainsIgnoreCase("FFXIV_MemoryReader"));

                    if (!FFXIVPlugin.Instance.IsAvilableFFXIVPlugin ||
                        plugin == null)
                    {
                        this.MemoryPlugin = null;
                        this.Core = null;

                        WPFHelper.BeginInvoke(() => this.IsAvailable = false);

                        return;
                    }

                    if (this.MemoryPlugin == null)
                    {
                        this.MemoryPlugin = plugin.pluginObj as dynamic;
                    }

                    if (this.Core == null)
                    {
                        this.Core = this.MemoryPlugin?.Core;
                    }

                    var result =
                        this.MemoryPlugin != null &&
                        this.Core != null;

                    WPFHelper.BeginInvoke(() =>
                        this.IsAvailable = result);

                    if (result)
                    {
                        if (!logged)
                        {
                            logged = true;
                            AppLogger.Info("FFXV_MemoryReader Availabled.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (!loggedError)
                    {
                        loggedError = true;
                        AppLogger.Error(ex, "Handled excption at attaching to FFXIV_MemoryReader.");
                    }
                }
            }
        }

        public List<CombatantV1> GetCombatantsV1()
            => this.Core?.GetCombatantsV1();

        public CameraInfoV1 GetCameraInfoV1()
            => this.Core?.GetCameraInfoV1();

        public List<HotbarRecastV1> GetHotbarRecastV1()
            => this.Core?.GetHotbarRecastV1();
    }
}
