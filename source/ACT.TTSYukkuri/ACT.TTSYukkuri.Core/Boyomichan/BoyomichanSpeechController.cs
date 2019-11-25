using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Cache;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Timers;
using ACT.TTSYukkuri.Config;
using FFXIV.Framework.Bridge;

namespace ACT.TTSYukkuri.Boyomichan
{
    /// <summary>
    /// 棒読みちゃんスピーチコントローラ
    /// </summary>
    public class BoyomichanSpeechController :
        ISpeechController
    {
        #region Constants

        /// <summary>
        /// 棒読みちゃんサーバ
        /// </summary>
        public static readonly string BoyomichanServer = "127.0.0.1";

        /// <summary>
        /// 棒読みちゃんサーバのポート
        /// </summary>
        public static readonly int BoyomichanServicePort = 50080;

        /// <summary>
        /// 棒読みちゃんの後続キューを破棄させる
        /// </summary>
        public static readonly short BoyomiCommandCancel = 0x0040;

        /// <summary>
        /// 棒読みちゃんの現在の読み上げを中断させる
        /// </summary>
        public static readonly short BoyomiCommandInterrupt = 0x0030;

        /// <summary>
        /// 棒読みちゃんへのCommand 0:メッセージ読上げ
        /// </summary>
        public static readonly short BoyomiCommand = 0x0001;

        /// <summary>
        /// 棒読みちゃんの早さ -1:棒読みちゃんの画面上の設定に従う
        /// </summary>
        public static readonly short BoyomiSpeed = -1;

        /// <summary>
        /// 棒読みちゃんへのテキストのエンコード 0:UTF-8
        /// </summary>
        public static readonly byte BoyomiTextEncoding = 0;

        /// <summary>
        /// 棒読みちゃんの音程 -1:棒読みちゃんの画面上の設定に従う
        /// </summary>
        public static readonly short BoyomiTone = -1;

        /// <summary>
        /// 棒読みちゃんの声質 0:棒読みちゃんの画面上の設定に従う
        /// </summary>
        public static readonly short BoyomiVoice = 0;

        /// <summary>
        /// 棒読みちゃんの音量 -1:棒読みちゃんの画面上の設定に従う
        /// </summary>
        public static readonly short BoyomiVolume = -1;

        #endregion Constants

        private static BoyomichanSpeechController current;

        public BoyomichanSpeechController()
        {
            current = this;
        }

        /// <summary>
        /// 初期化する
        /// </summary>
        public void Initialize()
        {
            this.LazySpeakQueueSubscriber.Value.Start();
        }

        public void Free()
        {
            this.LazySpeakQueueSubscriber.Value.Stop();
            current = null;
        }

        private string lastText;
        private DateTime lastTextTimestamp;

        /// <summary>
        /// テキストを読み上げる
        /// </summary>
        /// <param name="text">読み上げるテキスト</param>
        /// <param name="isSync">使用しない</param>
        /// <param name="playDevice">使用しない</param>
        /// <param name="volume">使用しない</param>
        public void Speak(
            string text,
            PlayDevices playDevice = PlayDevices.Both,
            bool isSync = false,
            float? volume = null)
            => Speak(text, playDevice, VoicePalettes.Default, isSync, volume);

        /// <summary>
        /// テキストを読み上げる
        /// </summary>
        /// <param name="text">読み上げるテキスト</param>
        /// <param name="isSync">使用しない</param>
        /// <param name="playDevice">使用しない</param>
        /// <param name="voicePalette">使用しない</param>
        /// <param name="volume">使用しない</param>
        public void Speak(
            string text,
            PlayDevices playDevice = PlayDevices.Both,
            VoicePalettes voicePalette = VoicePalettes.Default,
            bool isSync = false,
            float? volume = null)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            if (this.lastText == text &&
                (DateTime.Now - this.lastTextTimestamp).TotalSeconds
                <= Settings.Default.GlobalSoundInterval)
            {
                return;
            }

            this.lastText = text;
            this.lastTextTimestamp = DateTime.Now;

            this.Queue.Enqueue(text.Trim());
        }

        private readonly ConcurrentQueue<string> Queue = new ConcurrentQueue<string>();

        private static volatile bool semaphore = false;

        private readonly Lazy<System.Timers.Timer> LazySpeakQueueSubscriber = new Lazy<System.Timers.Timer>(() =>
        {
            var timer = new System.Timers.Timer()
            {
                Interval = CommandInterval,
                AutoReset = true,
            };

            timer.Elapsed += Timer_Elapsed;
            return timer;
        });

        private readonly Lazy<HttpClient> LazyRESTClient = new Lazy<HttpClient>(() =>
        {
            ServicePointManager.DefaultConnectionLimit = 32;
            ServicePointManager.SecurityProtocol &= ~SecurityProtocolType.Tls;
            ServicePointManager.SecurityProtocol &= ~SecurityProtocolType.Tls11;
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            var client = new HttpClient(new WebRequestHandler()
            {
                CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore),
            });

            client.DefaultRequestHeaders.UserAgent.ParseAdd("Hojoring/1.0");
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            client.Timeout = TimeSpan.FromSeconds(10);

            return client;
        });

        private static void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                if (semaphore)
                {
                    return;
                }

                semaphore = true;
                current?.SpeakCore();
            }
            catch (Exception ex)
            {
                current?.GetLogger().Error(ex, "Exception occurred sending to Boyomi a TTS queue.");
            }
            finally
            {
                semaphore = false;
            }
        }

        private static readonly int CommandInterval = 40;
        private volatile bool isError = false;

        /// <summary>
        /// テキストを読み上げる
        /// </summary>
        private async void SpeakCore()
        {
            if (this.Queue.IsEmpty)
            {
                return;
            }

            var server = Settings.Default.BoyomiServer;
            var port = Settings.Default.BoyomiPort;

            if (string.IsNullOrEmpty(server))
            {
                if (!this.isError)
                {
                    this.GetLogger().Error("Boyomi server name is empty.");
                }

                this.isError = true;
                clearQueue();
                return;
            }

            if (port > 65535 ||
                port < 1)
            {
                if (!this.isError)
                {
                    this.GetLogger().Error("Boyomi port number is invalid.");
                }

                this.isError = true;
                clearQueue();
                return;
            }

            if (server.StartsWith("127.0.0."))
            {
                server = "localhost";
            }

            var client = this.LazyRESTClient.Value;
            var baseUri = $"http://{server}:{port}/";

            try
            {
                if (Settings.Default.IsBoyomiInterruptNotication)
                {
                    await client.GetAsync($"{baseUri}clear");
                    await client.GetAsync($"{baseUri}skip");
                    await Task.Delay(0);
                }

                while (this.Queue.TryDequeue(out string text))
                {
                    await client.GetAsync($"{baseUri}talk?text={Uri.EscapeUriString(text)}");
                    await Task.Delay(CommandInterval);
                }
            }
            catch (Exception ex)
            {
                if (!this.isError)
                {
                    this.GetLogger().Error(ex, "Boyomi REST client error.");
                }

                this.isError = true;
                clearQueue();
            }

            void clearQueue()
            {
                while (this.Queue.TryDequeue(out string t)) ;
            }
        }
    }
}
