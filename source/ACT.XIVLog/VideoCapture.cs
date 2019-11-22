using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FFXIV.Framework.Common;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;

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

        private MediaCapture mediaCapture;

        private async Task<MediaCapture> GetMediaCaptureAsync() => this.mediaCapture ??= await this.InitMediaCaptureAsync();

        private async Task<MediaCapture> InitMediaCaptureAsync()
        {
            var mediaCapture = new MediaCapture();
            await mediaCapture.InitializeAsync();
            return mediaCapture;
        }

        private LowLagMediaRecording mediaRecording;

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

        private string contentName = string.Empty;
        private int tryCount = 0;
        private int deathCount = 0;

        public async void StartRecording()
        {
            if (!Config.Instance.IsEnabledRecording)
            {
                return;
            }

            this.tryCount++;

            var myVideos = await StorageLibrary.GetLibraryAsync(
                KnownLibraryId.Videos);

            if (string.IsNullOrEmpty(contentName))
            {
                contentName = "UNKNOWN";
            }

            var file = await myVideos.SaveFolder.CreateFileAsync(
                $"{DateTime.Now:yyyy-MM-dd HH-mm} {contentName} take{tryCount}.mp4",
                CreationCollisionOption.ReplaceExisting);

            var capture = await this.GetMediaCaptureAsync();

            this.mediaRecording = await capture.PrepareLowLagRecordToStorageFileAsync(
                MediaEncodingProfile.CreateMp4(VideoEncodingQuality.Auto),
                file);

            await this.mediaRecording.StartAsync();

            await WPFHelper.InvokeAsync(() =>
            {
                Config.Instance.IsRecording = true;
                Config.Instance.CurrentVideoFileName = file.Name;
            });
        }

        public async void FinishRecording()
        {
            if (this.mediaRecording == null)
            {
                return;
            }

            await this.mediaRecording.FinishAsync();
            this.mediaRecording = null;

            if (this.deathCount > 1)
            {
                await Task.Delay(10);

                var f = Path.Combine(
                    Path.GetDirectoryName(Config.Instance.CurrentVideoFileName),
                    $"{Path.GetFileNameWithoutExtension(Config.Instance.CurrentVideoFileName)} death{this.deathCount - 1}.mp4");

                File.Move(
                    Config.Instance.CurrentVideoFileName,
                    f);
            }

            await WPFHelper.InvokeAsync(() =>
            {
                Config.Instance.IsRecording = false;
                Config.Instance.CurrentVideoFileName = string.Empty;
            });
        }
    }
}
