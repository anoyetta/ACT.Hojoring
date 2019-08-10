using ACT.TTSYukkuri.Boyomichan;
using ACT.TTSYukkuri.Config;
using ACT.TTSYukkuri.GoogleCloudTextToSpeech;
using ACT.TTSYukkuri.HOYA;
using ACT.TTSYukkuri.OpenJTalk;
using ACT.TTSYukkuri.Polly;
using ACT.TTSYukkuri.SAPI5;
using ACT.TTSYukkuri.Sasara;
using ACT.TTSYukkuri.Voiceroid;
using ACT.TTSYukkuri.Yukkuri;

namespace ACT.TTSYukkuri
{
    /// <summary>
    /// スピーチコントローラ
    /// </summary>
    public class SpeechController :
        ISpeechController
    {
        /// <summary>
        /// 現在のTTSタイプ
        /// </summary>
        private static string ttsType;

        /// <summary>
        /// Lockオブジェクト
        /// </summary>
        private static object lockObject = new object();

        /// <summary>
        /// シングルトンinstance
        /// </summary>
        private static ISpeechController instance;

        /// <summary>
        /// シングルトンinstanceを返す
        /// </summary>
        public static ISpeechController Default
        {
            get
            {
                lock (lockObject)
                {
                    if (SpeechController.instance == null ||
                        SpeechController.ttsType != Settings.Default.TTS)
                    {
                        if (SpeechController.instance != null)
                        {
                            SpeechController.instance.Free();
                        }

                        switch (Settings.Default.TTS)
                        {
                            case TTSType.Yukkuri:
                                SpeechController.instance = new YukkuriSpeechController();
                                break;

                            case TTSType.Sasara:
                                SpeechController.instance = new SasaraSpeechController();
                                break;

                            case TTSType.Boyomichan:
                                SpeechController.instance = new BoyomichanSpeechController();
                                break;

                            case TTSType.OpenJTalk:
                                SpeechController.instance = new OpenJTalkSpeechController();
                                break;

                            case TTSType.HOYA:
                                SpeechController.instance = new HOYASpeechController();
                                break;

                            case TTSType.VOICEROID:
                                SpeechController.instance = new VoiceroidSpeechController();
                                break;

                            case TTSType.SAPI5:
                                SpeechController.instance = new SAPI5SpeechController();
                                break;

                            case TTSType.GoogleCloudTextToSpeech:
                                SpeechController.instance = new GoogleCloudTextToSpeechSpeechController();
                                break;

                            case TTSType.Polly:
                                SpeechController.instance = new PollySpeechController();
                                break;

                            default:
                                SpeechController.instance = new YukkuriSpeechController();
                                break;
                        }

                        SpeechController.instance.Initialize();

                        SpeechController.ttsType = Settings.Default.TTS;

                        // 監視スレッドにスピークdelegateを与える
                        FFXIVWatcher.Default.SpeakDelegate = SpeechController.instance.Speak;
                    }

                    return SpeechController.instance;
                }
            }
        }

        /// <summary>
        /// 初期化する
        /// </summary>
        public void Initialize() =>
            SpeechController.Default.Initialize();

        /// <summary>
        /// 開放する
        /// </summary>
        public void Free()
        {
            FFXIVWatcher.Default.SpeakDelegate = null;
            SpeechController.Default.Free();
            SpeechController.ttsType = string.Empty;
            SpeechController.instance = null;
        }

        /// <summary>
        /// TTSに話してもらう
        /// </summary>
        /// <param name="text">読上げるテキスト</param>
        public void Speak(
            string text,
            PlayDevices playDevice = PlayDevices.Both,
            bool isSync = false,
            float? volume = null)
            => SpeechController.instance.Speak(text, playDevice, isSync, volume);
    }
}
