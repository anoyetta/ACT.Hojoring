using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Xml.Linq;
using Advanced_Combat_Tracker;
using FFXIV.Framework.Common;
using FFXIV.Framework.Extensions;
using Prism.Mvvm;
using TamanegiMage.FFXIV_MemoryReader.Core;
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

        private dynamic MemoryPlugin { get; set; } = null;

        private PluginCore Core { get; set; } = null;

        private System.Timers.Timer timer = new System.Timers.Timer(5 * 1000);

        public FFXIVReader()
        {
            this.StartTimer();
        }

        /// <summary>
        /// MemoryReaderがスタートするまで待つ
        /// </summary>
        public Task WaitForReaderToStartedAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    var config = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                            @"Advanced Combat Tracker\Config\Advanced Combat Tracker.config.xml");

                    if (!File.Exists(config))
                    {
                        return;
                    }

                    using (var sr = new StreamReader(config, new UTF8Encoding(false)))
                    {
                        var xdoc = XDocument.Load(sr);

                        var reader = (
                            from plugin in xdoc.Descendants("Plugin")
                            where
                            plugin.Attribute("Path").Value.ContainsIgnoreCase("FFXIV_MemoryReader") &&
                            plugin.Attribute("Enabled").Value.ContainsIgnoreCase("True")
                            select
                            plugin).FirstOrDefault();

                        if (reader == null)
                        {
                            return;
                        }
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
            });
        }

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

        private void Timer_Elapsed(
            object sender,
            ElapsedEventArgs e)
        {
            try
            {
                var plugin = ActGlobals.oFormActMain.ActPlugins
                    .FirstOrDefault(x =>
                        x.pluginFile.Name.ContainsIgnoreCase("FFXIV_MemoryReader"));

                if (plugin == null)
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

                WPFHelper.BeginInvoke(() =>
                    this.IsAvailable =
                        this.MemoryPlugin != null &&
                        this.Core != null);
            }
            catch (Exception)
            {
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
