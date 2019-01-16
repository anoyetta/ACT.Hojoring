namespace FFXIV.Framework.Bridge
{
    public class HelpBridge
    {
        #region Singleton

        private static HelpBridge instance;

        public static HelpBridge Instance =>
            instance ?? (instance = new HelpBridge());

        private HelpBridge()
        {
        }

        #endregion Singleton

        public delegate void BackupDelegate(
            string destinationDirectory);

        public BackupDelegate BackupCallback;
    }
}
