using FFXIV.Framework.TTS.Common;

namespace ACT.TTSYukkuri.TTSServer
{
    public static class TTSServerController
    {
        public static void End()
        {
            RemoteTTSClient.Instance.Close();
#if DEBUG
            RemoteTTSClient.Instance.ShutdownTTSServer();
#endif
        }

        public static void Start()
        {
            RemoteTTSClient.Instance.BaseDirectory = PluginCore.Instance.PluginDirectory;
            RemoteTTSClient.Instance.StartTTSServer();
            RemoteTTSClient.Instance.Open();
        }
    }
}
