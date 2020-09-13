using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Advanced_Combat_Tracker;
using FFXIV.Framework.Common;
using NLog;
using SLOBSharp.Client;
using SLOBSharp.Client.Requests;
using WindowsInput;

namespace ACT.XIVLog
{
    public class VideoCapture
    {
        #region Lazy Instance

        private static readonly Lazy<VideoCapture> LazyInstance = new Lazy<VideoCapture>(() => new VideoCapture());

        public static VideoCapture Instance => LazyInstance.Value;

        private VideoCapture()
        {
        }

        #endregion Lazy Instance

        #region Logger

        private Logger Logger => AppLog.DefaultLogger;

        #endregion Logger

        private readonly InputSimulator Input = new InputSimulator();

        private static readonly Regex StartCountdownRegex = new Regex(
            @"^00:...9:戦闘開始まで.+）$",
            RegexOptions.Compiled);

        private static readonly Regex FeastStartRegex = new Regex(
            @"^21:[0-9a-fA-F]{8}:40000001:168",
            RegexOptions.Compiled);

        private static readonly Regex FeastEndRegex = new Regex(
            @"^21:[0-9a-fA-F]{8}:80000004:257",
            RegexOptions.Compiled);

        private static readonly Regex ContentStartLogRegex = new Regex(
            @"^00:0839:「(?<content>.+)」の攻略を開始した。",
            RegexOptions.Compiled);

        private static readonly Regex ContentEndLogRegex = new Regex(
            @"^00:0839:.+を終了した。$",
            RegexOptions.Compiled);

        private static readonly Regex PlayerChangedLogRegex = new Regex(
            @"^02:Changed primary player to (?<player>.+)\.",
            RegexOptions.Compiled);

        private static readonly string[] StopVideoKeywords = new string[]
        {
            "wipeout",
            "End-of-Timeline has been detected.",
        };

        private string defeatedLog = "19:Naoki Yoshida was defeated";

        private bool inFeast;

        public void DetectCapture(
            XIVLog xivlog)
        {
            if (!xivlog.Log.StartsWith("00:") &&
                !xivlog.Log.StartsWith("01:") &&
                !xivlog.Log.StartsWith("02:") &&
                !xivlog.Log.StartsWith("19:") &&
                !xivlog.Log.StartsWith("21:"))
            {
                return;
            }

            // 攻略を開始した
            var match = ContentStartLogRegex.Match(xivlog.Log);
            if (match.Success)
            {
                this.contentName = match.Groups["content"]?.Value;

                var contentName = !string.IsNullOrEmpty(this.contentName) ?
                    this.contentName :
                    ActGlobals.oFormActMain.CurrentZone;

                if (Config.Instance.TryCountContentName != contentName ||
                    (DateTime.Now - Config.Instance.TryCountTimestamp) >=
                    TimeSpan.FromHours(Config.Instance.TryCountResetInterval))
                {
                    this.TryCount = 0;
                }

                return;
            }

            // 攻略を終了した
            match = ContentEndLogRegex.Match(xivlog.Log);
            if (match.Success)
            {
                this.FinishRecording();
                this.contentName = string.Empty;
                WPFHelper.Invoke(() => TitleCardView.CloseTitleCard());
                return;
            }

            var isStart = StartCountdownRegex.IsMatch(xivlog.Log);

            if (!isStart)
            {
                isStart = FeastStartRegex.IsMatch(xivlog.Log);
                if (isStart)
                {
                    this.inFeast = true;
                    this.contentName = ActGlobals.oFormActMain.CurrentZone;

                    if (Config.Instance.TryCountContentName != this.contentName ||
                        (DateTime.Now - Config.Instance.TryCountTimestamp) >=
                        TimeSpan.FromHours(Config.Instance.TryCountResetInterval))
                    {
                        this.TryCount = 0;
                    }
                }
            }

            if (isStart ||
                xivlog.Log.Contains("/xivlog rec"))
            {
                this.deathCount = 0;
                this.StartRecording();
                return;
            }

            var isCancel = xivlog.Log.EndsWith("戦闘開始カウントがキャンセルされました。");
            if (isCancel)
            {
                this.TryCount--;
            }

            if (isCancel ||
                xivlog.Log.Contains("/xivlog stop") ||
                StopVideoKeywords.Any(x => xivlog.Log.Contains(x)) ||
                (this.inFeast && FeastEndRegex.IsMatch(xivlog.Log)))
            {
                this.FinishRecording();
                return;
            }

            // Player change
            match = PlayerChangedLogRegex.Match(xivlog.Log);
            if (match.Success)
            {
                this.defeatedLog = $"19:{match.Groups["player"]?.Value} was defeated";
                return;
            }

            // Player defeated
            if (xivlog.Log.StartsWith(this.defeatedLog))
            {
                this.deathCount++;
                return;
            }
        }

        private DateTime startTime;
        private string contentName = string.Empty;
        private int deathCount = 0;

        private int TryCount
        {
            get => Config.Instance.VideoTryCount;
            set => WPFHelper.Invoke(() =>
            {
                Config.Instance.VideoTryCount = value;
                Config.Instance.TryCountTimestamp = DateTime.Now;

                var contentName = !string.IsNullOrEmpty(this.contentName) ?
                    this.contentName :
                    ActGlobals.oFormActMain.CurrentZone;

                Config.Instance.TryCountContentName = contentName;
            });
        }

        public void StartRecording()
        {
            lock (this)
            {
                if (Config.Instance.IsRecording)
                {
                    return;
                }
            }

            this.TryCount++;
            this.startTime = DateTime.Now;

            if (Config.Instance.IsEnabledRecording)
            {
                if (!Config.Instance.UseObsRpc)
                {
                    this.Input.Keyboard.ModifiedKeyStroke(
                        Config.Instance.StartRecordingShortcut.GetModifiers(),
                        Config.Instance.StartRecordingShortcut.GetKeys());
                }
                else
                {
                    this.SendToggleRecording();
                }
            }

            WPFHelper.InvokeAsync(async () =>
            {
                if (Config.Instance.IsEnabledRecording)
                {
                    lock (this)
                    {
                        Config.Instance.IsRecording = true;
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(5));

                var contentName = !string.IsNullOrEmpty(this.contentName) ?
                    this.contentName :
                    ActGlobals.oFormActMain.CurrentZone;

                if (Config.Instance.IsShowTitleCard)
                {
                    TitleCardView.ShowTitleCard(
                        contentName,
                        this.TryCount,
                        this.startTime);
                }
            });
        }

        private static readonly string VideoDurationPlaceholder = "#duration#";

        public void FinishRecording()
        {
            lock (this)
            {
                if (!Config.Instance.IsRecording)
                {
                    return;
                }
            }

            if (!Config.Instance.IsEnabledRecording)
            {
                return;
            }

            if (!Config.Instance.UseObsRpc)
            {
                this.Input.Keyboard.ModifiedKeyStroke(
                    Config.Instance.StopRecordingShortcut.GetModifiers(),
                    Config.Instance.StopRecordingShortcut.GetKeys());
            }
            else
            {
                this.SendToggleRecording();
            }

            var contentName = !string.IsNullOrEmpty(this.contentName) ?
                this.contentName :
                ActGlobals.oFormActMain.CurrentZone;

            if (!string.IsNullOrEmpty(Config.Instance.VideoSaveDictory) &&
                Directory.Exists(Config.Instance.VideoSaveDictory))
            {
                Task.Run(async () =>
                {
                    var now = DateTime.Now;

                    var prefix = Config.Instance.VideFilePrefix.Trim();
                    prefix = string.IsNullOrEmpty(prefix) ?
                        string.Empty :
                        $"{prefix} ";

                    var f = this.deathCount > 1 ?
                        $"{prefix}{this.startTime:yyyy-MM-dd HH-mm} {contentName} try{this.TryCount:00} {VideoDurationPlaceholder} death{this.deathCount - 1}.ext" :
                        $"{prefix}{this.startTime:yyyy-MM-dd HH-mm} {contentName} try{this.TryCount:00} {VideoDurationPlaceholder}.ext";

                    await Task.Delay(TimeSpan.FromSeconds(8));

                    var files = Directory.GetFiles(
                        Config.Instance.VideoSaveDictory,
                        "*.*");

                    var original = files
                        .OrderByDescending(x => File.GetLastWriteTime(x))
                        .FirstOrDefault();

                    if (!string.IsNullOrEmpty(original))
                    {
                        var timestamp = File.GetLastWriteTime(original);
                        if (timestamp >= now.AddSeconds(-10))
                        {
                            var ext = Path.GetExtension(original);
                            f = f.Replace(".ext", ext);

                            var dest = Path.Combine(
                                Path.GetDirectoryName(original),
                                f);

                            using (var tf = TagLib.File.Create(original))
                            {
                                dest = dest.Replace(
                                    VideoDurationPlaceholder,
                                    $"{tf.Properties.Duration.TotalSeconds:N0}s");

                                tf.Tag.Title = Path.GetFileNameWithoutExtension(dest);
                                tf.Tag.Subtitle = $"{prefix} - {contentName}";
                                tf.Tag.Comment =
                                    $"{prefix} - {contentName}\n" +
                                    $"{this.startTime:yyyy-MM-dd HH:mm} try{this.TryCount} death{this.deathCount - 1}";
                                tf.Save();
                            }

                            File.Move(
                                original,
                                dest);

                            XIVLogPlugin.Instance.EnqueueLogLine(
                                "00",
                                $"[XIVLog] The video was saved. {Path.GetFileName(dest)}");
                        }
                    }
                });
            }

            WPFHelper.InvokeAsync(() =>
            {
                lock (this)
                {
                    Config.Instance.IsRecording = false;
                }
            });
        }

        private readonly Lazy<object> LazySLOBSClient = new Lazy<object>(() => new SlobsPipeClient("slobs"));

        [MethodImpl(MethodImplOptions.NoInlining)]
        private async void SendToggleRecording()
        {
            var p = Process.GetProcessesByName("Streamlabs OBS");
            if (p == null ||
                p.Length < 1)
            {
                this.Logger.Info("Tried to record, but Streamlabs OBS is not found.");
                return;
            }

            var client = this.LazySLOBSClient.Value as SlobsPipeClient;

            var req = SlobsRequestBuilder
                .NewRequest()
                .SetMethod("toggleRecording")
                .SetResource("StreamingService")
                .BuildRequest();

            await client
                .ExecuteRequestAsync(req)
                .ConfigureAwait(false);
        }
    }
}
