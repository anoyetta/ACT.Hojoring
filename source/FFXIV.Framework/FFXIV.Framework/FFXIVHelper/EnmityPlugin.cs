using System;
using System.Collections.Generic;
using System.Threading;
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
        private Timer timer;

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
                    FollowFFXIVPlugin = true,
                    ScanInterval = ProcessScanInterval,
                    DisableTarget = false,
                    DisableAggroList = true,
                    DisableEnmityList = true,
                };

                // ダミーオーバーレイを生成する
                this.enmityOverlay = new EnmityOverlay(this.enmityConfig);

                // タイマーを開始する
                this.timer = new Timer(this.TimerCallback, null, 100, Timeout.Infinite);

                this.isInitialized = true;
            }
        }

        public void Dispose()
        {
            lock (this)
            {
                this.timer?.Dispose();
                this.timer = null;

                this.enmityReader?.Dispose();
                this.enmityReader = null;

                this.enmityOverlay?.Dispose();
                this.enmityOverlay = null;

                this.enmityConfig = null;

                this.isInitialized = false;
            }
        }

        private void TimerCallback(
            object state)
        {
            lock (this)
            {
                try
                {
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
                    this.timer?.Change(TimeSpan.FromSeconds(5).Milliseconds, Timeout.Infinite);
                }
            }
        }

        /// <summary>
        /// カレントターゲットの敵視情報を取得する
        /// </summary>
        public List<EnmityEntry> GetEnmityEntryList() => this.enmityReader?.GetEnmityEntryList();
    }
}
