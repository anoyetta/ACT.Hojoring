using System;

namespace FFXIV.Framework.Bridge
{
    public class PlayBridge
    {
        #region Singleton

        private static PlayBridge instance;

        public static PlayBridge Instance =>
            instance ?? (instance = new PlayBridge());

        private PlayBridge()
        {
        }

        #endregion Singleton

        public delegate void PlayDevice(
            string message,
            bool isSync = false);

        private PlayDevice PlayBothDelegate;
        private PlayDevice PlayMainDeviceDelegate;
        private PlayDevice PlaySubDeviceDelegate;
        private Func<bool> isSyncAvailable;

        public bool IsAvailable =>
            this.PlayMainDeviceDelegate != null &&
            this.PlaySubDeviceDelegate != null;

        public bool IsSyncAvailable => this.isSyncAvailable?.Invoke() ?? false;

        public void SetSyncStatusDelegate(
            Func<bool> func) => this.isSyncAvailable = func;

        public void SetBothDelegate(
            PlayDevice action) => this.PlayBothDelegate = action;

        public void SetMainDeviceDelegate(
            PlayDevice action) => this.PlayMainDeviceDelegate = action;

        public void SetSubDeviceDelegate(
            PlayDevice action) => this.PlaySubDeviceDelegate = action;

        public void Play(
            string message,
            bool isSync = false) => this.PlayBothDelegate?.Invoke(message, isSync);

        public void PlayMain(
            string message,
            bool isSync = false) => this.PlayMainDeviceDelegate?.Invoke(message, isSync);

        public void PlaySub(
            string message,
            bool isSync = false) => this.PlaySubDeviceDelegate?.Invoke(message, isSync);
    }
}
