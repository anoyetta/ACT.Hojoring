namespace FFXIV.Framework.TTS.Common
{
    public static class Constants
    {
        public const string RemoteAudioObjectName = "AudioModel";
        public const string RemoteTTSObjectName = "TTSModel";
        public const string TTSServerChannelName = "FFXIV.Framework.TTS.Server";
        public const string TTSServerProcessName = "FFXIV.Framework.TTS.Server";

        public static string RemoteAudioServiceUri => $"{Constants.TTSServerUri}/{Constants.RemoteAudioObjectName}";
        public static string RemoteTTSServiceUri => $"{Constants.TTSServerUri}/{Constants.RemoteTTSObjectName}";
        public static string TTSServerUri => $"ipc://{Constants.TTSServerChannelName}";
    }
}
