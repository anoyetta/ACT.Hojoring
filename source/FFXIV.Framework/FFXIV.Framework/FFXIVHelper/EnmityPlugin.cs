using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        #endregion Singleton

        private EnmityOverlayConfig enmityConfig;
        private EnmityOverlay enmityOverlay;
        private FFXIVMemory enmityReader;
        private Timer timer;

        public void Initialize(
            Process ffxivProcess)
        {
            lock (this)
            {
                // ダミー設定を生成する
                this.enmityConfig = new EnmityOverlayConfig("InnerEnmity")
                {
                    FollowFFXIVPlugin = true,
                    ScanInterval = 150,
                    DisableTarget = false,
                    DisableAggroList = true,
                    DisableEnmityList = true,
                };

                // ダミーオーバーレイを生成する
                this.enmityOverlay = new EnmityOverlay(this.enmityConfig);

                // タイマーを開始する
                this.timer = new Timer(this.TimerCallback, null, TimeSpan.FromSeconds(3).Milliseconds, Timeout.Infinite);
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
            }
        }

        private void TimerCallback(
            object state)
        {
            lock (this)
            {
                if (FFXIVPlugin.Instance.Process == null)
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
        }

        /// <summary>
        /// カレントターゲットの敵視情報を取得する
        /// </summary>
        public List<EnmityEntry> GetEnmityEntryList() => this.enmityReader?.GetEnmityEntryList();
    }
}
