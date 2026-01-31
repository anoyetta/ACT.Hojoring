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

        /// <summary>
        /// アップデート（ファイル置換）の直前に実行すべき処理のデリゲート
        /// </summary>
        public delegate void BeforeUpdateDelegate();

        /// <summary>
        /// アップデート（ファイル置換）の直前に実行すべき処理のコールバック
        /// </summary>
        public BeforeUpdateDelegate BeforeUpdateCallback;
    }
}