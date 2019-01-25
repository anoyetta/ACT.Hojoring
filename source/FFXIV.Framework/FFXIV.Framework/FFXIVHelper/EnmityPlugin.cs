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
        private ThreadWorker scanWorker;

        public void Initialize()
        {
            if (this.isInitialized)
            {
                return;
            }

            this.isInitialized = true;

            lock (this)
            {
                // ダミー設定を生成する
                this.enmityConfig = new EnmityOverlayConfig("InnerEnmity")
                {
                    IsVisible = true,
                    IsClickThru = true,
                    IsLocked = true,
                    Size = new System.Drawing.Size(0, 0),
                    FollowFFXIVPlugin = true,
                    ScanInterval = ProcessScanInterval,
                    DisableTarget = true,
                    DisableAggroList = true,
                    DisableEnmityList = true,
                };

                // ダミーオーバーレイを生成する
                this.enmityOverlay = new EnmityOverlay(this.enmityConfig);

                this.worker = ThreadWorker.Run(
                    this.AttachPlugin,
                    200d,
                    "EnmityPluginAttachWorker",
                    ThreadPriority.Lowest);

                this.scanWorker = ThreadWorker.Run(
                    this.ScanEnmity,
                    100d,
                    "ScanEnmityWorker",
                    ThreadPriority.BelowNormal);
            }
        }

        public void Dispose()
        {
            this.isInitialized = false;

            lock (this)
            {
                this.scanWorker?.Abort();
                this.scanWorker = null;

                this.worker?.Abort();
                this.worker = null;

                this.enmityReader?.Dispose();
                this.enmityReader = null;

                this.enmityOverlay?.Dispose();
                this.enmityOverlay = null;

                this.enmityConfig = null;
            }
        }

        private void AttachPlugin()
        {
            try
            {
                lock (this)
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

        private List<EnmityEntry> enmityList;

        private void ScanEnmity()
        {
            lock (this)
            {
                this.enmityList = this.enmityReader?.GetEnmityEntryList();
            }
        }

        /// <summary>
        /// カレントターゲットの敵視情報を取得する
        /// </summary>
        public List<EnmityEntry> GetEnmityEntryList()
        {
            lock (this)
            {
                return this.enmityList?.Clone();
            }
        }
    }
}
