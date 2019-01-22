using System;
using System.Collections.Generic;
using System.Threading;
using FFXIV.Framework.Common;
using Tamagawa.EnmityPlugin;

namespace FFXIV.Framework.FFXIVHelper
{
    public class EnmityPlugin :
        IDisposable
    {
        #region Singleton

        private static EnmityPlugin instance;

        public static EnmityPlugin Instance => instance ?? (instance = new EnmityPlugin());

        private EnmityPlugin()
        {
        }

        public static void Free()
        {
            instance?.Dispose();
            instance = null;
        }

        #endregion Singleton

        private const int ProcessScanInterval = 3000;

        private volatile bool isInitialized = false;
        private EnmityOverlayConfig enmityConfig;
        private EnmityOverlay enmityOverlay;
        private FFXIVMemory enmityReader;
        private ThreadWorker worker;

        public void Initialize()
        {
            lock (this)
            {
                if (this.isInitialized)
                {
                    return;
                }

                // ダミー設定を生成する
                this.enmityConfig = new EnmityOverlayConfig("InnerEnmity")
                {
                    IsVisible = false,
                    FollowFFXIVPlugin = true,
                    ScanInterval = ProcessScanInterval,
                    DisableTarget = true,
                    DisableAggroList = true,
                    DisableEnmityList = true,
                };

                // ダミーオーバーレイを生成する
                this.enmityOverlay = new EnmityOverlay(this.enmityConfig);

                this.worker = ThreadWorker.Run(
                    DoWork,
                    200,
                    "EnmityPluginWorker",
                    ThreadPriority.Lowest);

                this.isInitialized = true;
            }
        }

        public void Dispose()
        {
            lock (this)
            {
                this.worker?.Abort();
                this.worker = null;

                this.enmityReader?.Dispose();
                this.enmityReader = null;

                this.enmityOverlay?.Dispose();
                this.enmityOverlay = null;

                this.enmityConfig = null;

                this.isInitialized = false;
            }
        }

        private void DoWork()
        {
            lock (this)
            {
                try
                {
                    if (this.enmityOverlay == null ||
                        this.enmityConfig == null)
                    {
                        return;
                    }

                    if (FFXIVPlugin.Instance?.Process == null)
                    {
                        if (this.enmityReader != null)
                        {
                            this.enmityReader.Dispose();
                            this.enmityReader = null;
                        }

                        return;
                    }

                    if (this.enmityReader == null ||
                        this.enmityReader.Process == null ||
                        this.enmityReader.Process.HasExited ||
                        this.enmityReader.Process.Id != FFXIVPlugin.Instance.Process.Id)
                    {
                        if (this.enmityReader != null)
                        {
                            this.enmityReader.Dispose();
                            this.enmityReader = null;
                        }

                        this.enmityReader = new FFXIVMemory(this.enmityOverlay, FFXIVPlugin.Instance.Process);
                    }
                }
                finally
                {
                    if (this.enmityReader != null)
                    {
                        Thread.Sleep(TimeSpan.FromSeconds(5));
                    }
                    else
                    {
                        Thread.Yield();
                    }
                }
            }
        }

        /// <summary>
        /// カレントターゲットの敵視情報を取得する
        /// </summary>
        public List<EnmityEntry> GetEnmityEntryList() => this.enmityReader?.GetEnmityEntryList();
    }
}
