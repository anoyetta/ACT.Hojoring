using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
            if (!Config.Instance.IsEnabledRecording)
            {
                return;
            }

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
                this.tryCount = 0;
                return;
            }

            match = ContentEndLogRegex.Match(xivlog.Log);
            if (match.Success)
            {
                this.FinishRecording();
                this.contentName = string.Empty;
                this.tryCount = 0;
                return;
            }

            if (xivlog.Log.StartsWith("00:0139:戦闘開始まで") ||
                xivlog.Log.Contains("/xivlog rec"))
            {
                this.deathCount = 0;
                this.StartRecording();
                return;
            }

            if (xivlog.Log.Contains("wipeout") ||
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
        private int tryCount = 0;
        private int deathCount = 0;

        public void StartRecording()
        {
            if (!Config.Instance.IsEnabledRecording)
            {
                return;
            }

            this.tryCount++;
            this.startTime = DateTime.Now;

            this.Input.Keyboard.ModifiedKeyStroke(
                Config.Instance.StartRecordingShortcut.GetModifiers(),
                Config.Instance.StartRecordingShortcut.GetKeys());

            WPFHelper.InvokeAsync(() =>
            {
                lock (this)
                {
                    Config.Instance.IsRecording = true;
                }
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
                "UNKNOWN";

            if (!string.IsNullOrEmpty(Config.Instance.VideoSaveDictory) &&
                Directory.Exists(Config.Instance.VideoSaveDictory))
            {
                Task.Run(async () =>
                {
                    var f = this.deathCount > 1 ?
                        Path.Combine(
                            Path.GetDirectoryName(Config.Instance.VideoSaveDictory),
                            $"{this.startTime:YYYY-MM-dd HH-mm} {contentName} try{this.tryCount:00} death{this.deathCount - 1}.mp4") :
                        Path.Combine(
                            Path.GetDirectoryName(Config.Instance.VideoSaveDictory),
                            $"{this.startTime:YYYY-MM-dd HH-mm} {contentName} try{this.tryCount:00}.mp4");

                    if (!File.Exists(f))
                    {
                        await Task.Delay(TimeSpan.FromSeconds(8));

                        var files = Directory.GetFiles(
                            Config.Instance.VideoSaveDictory,
                            "*.mp4");

                        var original = files
                            .OrderByDescending(x => File.GetCreationTime(x))
                            .FirstOrDefault();

                        if (!string.IsNullOrEmpty(original))
                        {
                            if (File.GetCreationTime(original) >= DateTime.Now.AddSeconds(-10))
                            {
                                File.Move(
                                    original,
                                    f);
                            }
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
