namespace ACT.TTSYukkuri
{
    /// <summary>
    /// TTSの種類
    /// </summary>
    public static class TTSType
    {
        /// <summary>
        /// Yukkuri:ゆっくり
        /// </summary>
        public const string Yukkuri = "Yukkuri";

        /// <summary>
        /// Sasara:CeVIO Creative Studio
        /// </summary>
        public const string Sasara = "Sasara";

        /// <summary>
        /// Boyomichan:棒読みちゃん
        /// </summary>
        public const string Boyomichan = "Boyomichan";

        /// <summary>
        /// OpenJTalk:Open JTalk
        /// </summary>
        public const string OpenJTalk = "OpenJTalk";

        /// <summary>
        /// HOYA:HOYA
        /// </summary>
        public const string HOYA = "HOYA";

        /// <summary>
        /// VOICEROID
        /// </summary>
        public const string VOICEROID = "VOICEROID";

        /// <summary>
        /// SAPI5
        /// </summary>
        public const string SAPI5 = "SAPI5";

        /// <summary>
        /// AmazonPolly
        /// </summary>
        public const string Polly = "Polly";

        /// <summary>
        /// Google Cloud Text-to-Speech
        /// </summary>
        public const string GoogleCloudTextToSpeech = "Google Cloud Text-to-Speech";

        /// <summary>
        /// コンボボックスコレクション
        /// </summary>
        public static ComboBoxItem[] ToComboBox = new ComboBoxItem[]
        {
            new ComboBoxItem("AquesTalk (ゆっくり)", TTSType.Yukkuri),
            new ComboBoxItem("Open JTalk", TTSType.OpenJTalk),
            new ComboBoxItem("HOYA VoiceText Web API", TTSType.HOYA),
            new ComboBoxItem("Amazon Polly", TTSType.Polly),
            new ComboBoxItem("棒読みちゃん(TCPインターフェース)", TTSType.Boyomichan),
            new ComboBoxItem("CeVIO Creative Studio", TTSType.Sasara),
            new ComboBoxItem("VOICEROID", TTSType.VOICEROID),
            new ComboBoxItem("SAPI5", TTSType.SAPI5),
            new ComboBoxItem("Google Cloud Text-to-Speech", TTSType.GoogleCloudTextToSpeech),
        };
    }

    public class ComboBoxItem
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="display">表示用</param>
        /// <param name="value">値用</param>
        public ComboBoxItem(
            string display,
            string value)
        {
            this.Display = display;
            this.Value = value;
        }

        /// <summary>
        /// 表示用メンバ
        /// </summary>
        public string Display { get; set; }

        /// <summary>
        /// 値用メンバ
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// ToString
        /// </summary>
        /// <returns>文字列</returns>
        public override string ToString() => this.Display;
    }
}
