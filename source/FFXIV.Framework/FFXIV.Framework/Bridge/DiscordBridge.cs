namespace FFXIV.Framework.Bridge
{
    public class DiscordBridge
    {
        #region Singleton

        private static DiscordBridge instance;

        public static DiscordBridge Instance =>
            instance ?? (instance = new DiscordBridge());

        private DiscordBridge()
        {
        }

        #endregion Singleton

        public delegate void SendMessage(
            string message,
            bool tts = false);

        public delegate void SendSpeaking(
            string wave);

        public SendMessage SendMessageDelegate;
        public SendSpeaking SendSpeakingDelegate;
    }
}
