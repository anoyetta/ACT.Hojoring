using System;
using System.IO;
using System.Media;
using Advanced_Combat_Tracker;
using FFXIV.Framework.Bridge;

namespace FFXIV.Framework.Common
{
    public class CommonSounds
    {
        #region Lazy Singleton

        private static readonly Lazy<CommonSounds> LazyInstance = new Lazy<CommonSounds>(() => new CommonSounds());

        public static CommonSounds Instance => LazyInstance.Value;

        private CommonSounds()
        {
        }

        #endregion Lazy Singleton

        private readonly Lazy<string> WaveDirectory = new Lazy<string>(()
            => DirectoryHelper.FindSubDirectory(@"resources\wav"));

        public void PlayAsterisk()
            => this.PlayWave("_asterisk.wav");

        public void PlayBeep()
            => this.PlayWave("_beep.wav");

        public void PlayWipeout()
            => this.PlayWave("_wipeout.wav");

        private void PlayWave(
            string fileName)
        {
            if (Config.Instance.CommonSoundVolume <= 0f)
            {
                return;
            }

            var wave = Path.Combine(
                this.WaveDirectory.Value,
                fileName);

            if (!File.Exists(wave))
            {
                SystemSounds.Asterisk.Play();
                return;
            }

            if (PlayBridge.Instance.IsAvailable)
            {
                PlayBridge.Instance.PlayMain(wave, Config.Instance.CommonSoundVolume);
            }
            else
            {
                ActGlobals.oFormActMain.PlaySound(wave);
            }
        }
    }
}
