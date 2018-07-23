using System.Collections.Generic;
using ACT.UltraScouter.Config;
using Advanced_Combat_Tracker;
using FFXIV.Framework.Bridge;

namespace ACT.UltraScouter.Common
{
    public enum TTSDevices
    {
        Normal = 0,
        OnlyMain,
        OnlySub,
    }

    public static class TTSDevicesExtensions
    {
        public static string ToText(
            this TTSDevices device)
            => new Dictionary<TTSDevices, string>()
            {
                { TTSDevices.Normal, "Normal" },
                { TTSDevices.OnlyMain, "Only main playback device" },
                { TTSDevices.OnlySub, "Only sub playback device" },
            }[device];
    }

    public static class TTSWrapper
    {
        public static void Speak(
            string tts)
        {
            switch (Settings.Instance.TTSDevice)
            {
                case TTSDevices.Normal:
                    ActGlobals.oFormActMain?.TTS(tts);
                    break;

                case TTSDevices.OnlyMain:
                    PlayBridge.Instance.PlayMain(tts);
                    break;

                case TTSDevices.OnlySub:
                    PlayBridge.Instance.PlaySub(tts);
                    break;
            }
        }
    }
}
