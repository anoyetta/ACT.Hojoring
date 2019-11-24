using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Advanced_Combat_Tracker;
using FFXIV.Framework.Common;
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

        private readonly InputSimulator Input = new InputSimulator();

        private static readonly Regex ContentStartLogRegex = new Regex(
            "^00:0839:「(?<content>.+)」の攻略を開始した。",
            RegexOptions.Compiled);

        private static readonly Regex ContentEndLogRegex = new Regex(
            "^00:0839:「.+」の攻略を終了した。",
            RegexOptions.Compiled);

        private static readonly Regex PlayerChangedLogRegex = new Regex(
            @"^02:Changed primary player to (?<player>.+)\.",
            RegexOptions.Compiled);

        private string defeatedLog = "19:Naoki Yoshida was defeated";

        public void DetectCapture(
            XIVLog xivlog)
        {
            if (!xivlog.Log.StartsWith("00:") &&
                !xivlog.Log.StartsWith("02:") &&
                !xivlog.Log.StartsWith("19:"))
            {
                return;
            }

            var match = ContentStartLogRegex.Match(xivlog.Log);
            if (match.Success)
            {
                this.contentName = match.Groups["content"]?.Value;
                this.TryCount = 0;
                return;
            }

            match = ContentEndLogRegex.Match(xivlog.Log);
            if (match.Success)
            {
                this.FinishRecording();
                this.contentName = string.Empty;
                this.TryCount = 0;
                return;
            }

            if (xivlog.Log.StartsWith("00:0139:戦闘開始まで") ||
                xivlog.Log.StartsWith("00:00b9:戦闘開始まで") ||
                xivlog.Log.Contains("/xivlog rec"))
            {
                this.deathCount = 0;
                this.StartRecording();
                return;
            }

            if (xivlog.Log.Contains("wipeout") ||
                xivlog.Log.EndsWith("戦闘開始カウントがキャンセルされました。") ||
                xivlog.Log.Contains("/xivlog stop"))
            {
                this.FinishRecording();
                return;
            }

            match = PlayerChangedLogRegex.Match(xivlog.Log);
            if (match.Success)
            {
                this.defeatedLog = $"19:{match.Groups["player"]?.Value} was defeated";
                return;
            }

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
            set => WPFHelper.Invoke(() => Config.Instance.VideoTryCount = value);
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
                this.Input.Keyboard.ModifiedKeyStroke(
                    Config.Instance.StartRecordingShortcut.GetModifiers(),
                    Config.Instance.StartRecordingShortcut.GetKeys());
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

                TitleCardView.Show(
                    contentName,
                    this.TryCount,
                    this.startTime);
            });
        }

        public void FinishRecording()
        {
            lock (this)
            {
                if (!Config.Instance.IsRecording)
                {
                    return;
                }
            }

            this.Input.Keyboard.ModifiedKeyStroke(
                Config.Instance.StopRecordingShortcut.GetModifiers(),
                Config.Instance.StopRecordingShortcut.GetKeys());

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
                        $"{prefix}{this.startTime:yyyy-MM-dd HH-mm} {contentName} try{this.TryCount:00} death{this.deathCount - 1}.mp4" :
                        $"{prefix}{this.startTime:yyyy-MM-dd HH-mm} {contentName} try{this.TryCount:00}.mp4";

                    await Task.Delay(TimeSpan.FromSeconds(8));

                    var files = Directory.GetFiles(
                        Config.Instance.VideoSaveDictory,
                        "*.mp4");

                    var original = files
                        .OrderByDescending(x => File.GetLastWriteTime(x))
                        .FirstOrDefault();

                    if (!string.IsNullOrEmpty(original))
                    {
                        var timestamp = File.GetLastWriteTime(original);
                        if (timestamp >= now.AddSeconds(-10))
                        {
                            var dest = Path.Combine(
                                Path.GetDirectoryName(original),
                                f);

                            File.Move(
                                original,
                                dest);
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
    }
}
