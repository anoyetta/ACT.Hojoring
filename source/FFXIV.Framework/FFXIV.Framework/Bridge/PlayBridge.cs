using System;

namespace FFXIV.Framework.Bridge
{
    public enum VoicePalettes : int
    {
        Default = 0,
        Ext1 = 1,
        Ext2 = 2,
        Ext3 = 3,
    }

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
            VoicePalettes voicePalette,
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
            this.PlayBothDelegate?.Invoke(message, 0, false, null);

        public void Play(string message, bool isSync) =>
            this.PlayBothDelegate?.Invoke(message, 0, isSync, null);

        public void Play(string message, float? volume) =>
            this.PlayBothDelegate?.Invoke(message, 0, false, volume);

        public void Play(string message, bool isSync, float? volume) =>
            this.PlayBothDelegate?.Invoke(message, 0, isSync, volume);

        public void Play(string message, VoicePalettes voicePalette, bool isSync, float? volume) =>
            this.PlayBothDelegate?.Invoke(message, voicePalette, isSync, volume);

        #endregion Play Both

        #region Play Main

        public void PlayMain(string message) =>
            this.PlayMainDeviceDelegate?.Invoke(message, 0, false, null);

        public void PlayMain(string message, bool isSync) =>
            this.PlayMainDeviceDelegate?.Invoke(message, 0, isSync, null);

        public void PlayMain(string message, VoicePalettes voicePalette, bool isSync) =>
            this.PlayMainDeviceDelegate?.Invoke(message, voicePalette, isSync, null);

        public void PlayMain(string message, float? volume) =>
            this.PlayMainDeviceDelegate?.Invoke(message, 0, false, volume);

        public void PlayMain(string message, bool isSync, float? volume) =>
            this.PlayMainDeviceDelegate?.Invoke(message, 0, isSync, volume);

        public void PlayMain(string message, VoicePalettes voicePalette, bool isSync, float? volume) =>
            this.PlayMainDeviceDelegate?.Invoke(message, voicePalette, isSync, volume);

        #endregion Play Main

        #region Play Sub

        public void PlaySub(string message) =>
            this.PlaySubDeviceDelegate?.Invoke(message, 0, false, null);

        public void PlaySub(string message, bool isSync) =>
            this.PlaySubDeviceDelegate?.Invoke(message, 0, isSync, null);

        public void PlaySub(string message, VoicePalettes voicePalette, bool isSync) =>
            this.PlaySubDeviceDelegate?.Invoke(message, voicePalette, isSync, null);

        public void PlaySub(string message, float? volume) =>
            this.PlaySubDeviceDelegate?.Invoke(message, 0, false, volume);

        public void PlaySub(string message, bool isSync, float? volume) =>
            this.PlaySubDeviceDelegate?.Invoke(message, 0, isSync, volume);

        public void PlaySub(string message, VoicePalettes voicePalette, bool isSync, float? volume) =>
            this.PlaySubDeviceDelegate?.Invoke(message, voicePalette, isSync, volume);

        #endregion Play Sub
    }
}
