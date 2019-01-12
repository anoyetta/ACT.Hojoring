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
            bool isSync = false,
            float? volume = null);

        private PlayDevice PlayBothDelegate;
        private PlayDevice PlayMainDeviceDelegate;
        private PlayDevice PlaySubDeviceDelegate;
        private Func<bool> isSyncAvailable;

        public bool IsAvailable =>
            this.PlayBothDelegate != null &&
            this.PlayMainDeviceDelegate != null &&
            this.PlaySubDeviceDelegate != null;

        public bool IsSyncAvailable => this.isSyncAvailable?.Invoke() ?? false;

        public void SetSyncStatusDelegate(Func<bool> func) =>
            this.isSyncAvailable = func;

        public void SetBothDelegate(PlayDevice action) =>
            this.PlayBothDelegate = action;

        public void SetMainDeviceDelegate(PlayDevice action) =>
            this.PlayMainDeviceDelegate = action;

        public void SetSubDeviceDelegate(PlayDevice action) =>
            this.PlaySubDeviceDelegate = action;

        #region Play Both

        public void Play(string message) =>
            this.PlayBothDelegate?.Invoke(message, false, null);

        public void Play(string message, bool isSync) =>
            this.PlayBothDelegate?.Invoke(message, isSync, null);

        public void Play(string message, float? volume) =>
            this.PlayBothDelegate?.Invoke(message, false, volume);

        public void Play(string message, bool isSync, float? volume) =>
            this.PlayBothDelegate?.Invoke(message, isSync, volume);

        #endregion Play Both

        #region Play Main

        public void PlayMain(string message) =>
            this.PlayMainDeviceDelegate?.Invoke(message, false, null);

        public void PlayMain(string message, bool isSync) =>
            this.PlayMainDeviceDelegate?.Invoke(message, isSync, null);

        public void PlayMain(string message, float? volume) =>
            this.PlayMainDeviceDelegate?.Invoke(message, false, volume);

        public void PlayMain(string message, bool isSync, float? volume) =>
            this.PlayMainDeviceDelegate?.Invoke(message, isSync, volume);

        #endregion Play Main

        #region Play Sub

        public void PlaySub(string message) =>
            this.PlaySubDeviceDelegate?.Invoke(message, false, null);

        public void PlaySub(string message, bool isSync) =>
            this.PlaySubDeviceDelegate?.Invoke(message, isSync, null);

        public void PlaySub(string message, float? volume) =>
            this.PlaySubDeviceDelegate?.Invoke(message, false, volume);

        public void PlaySub(string message, bool isSync, float? volume) =>
            this.PlaySubDeviceDelegate?.Invoke(message, isSync, volume);

        #endregion Play Sub
    }
}
