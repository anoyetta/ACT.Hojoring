using Advanced_Combat_Tracker;
using FFXIV.Framework.Common;
using FFXIV.Framework.XIVHelper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using SLOBSharp.Client;
using SLOBSharp.Client.Requests;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WindowsInput;
using WebSocketSharp;

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

        private NLog.Logger AppLogger => AppLog.DefaultLogger;

        #endregion Logger

        private readonly InputSimulator Input = new InputSimulator();

        private static readonly Regex StartCountdownRegex = new Regex(
            @"^00:...9::戦闘開始まで.+）$",
            RegexOptions.Compiled);

        private static readonly Regex ContentStartLogRegex = new Regex(
            @"^00:0839::「(?<content>.+)」の攻略を開始した。",
            RegexOptions.Compiled);

        private static readonly Regex ContentEndLogRegex = new Regex(
            @"^00:0839::.+を終了した。$",
            RegexOptions.Compiled);

        private static readonly Regex PlayerChangedLogRegex = new Regex(
            @"^02:[0-9a-fA-F]{8}:(?<player>.+)",
            RegexOptions.Compiled);

        private static readonly Regex FeastStartRegex = new Regex(
            @"^21:[0-9a-fA-F]{8}:40000001:168",
            RegexOptions.Compiled);

        private static readonly Regex FeastEndRegex = new Regex(
            @"^21:[0-9a-fA-F]{8}:80000004:257",
            RegexOptions.Compiled);

        private static readonly string[] StopVideoKeywords = new string[]
        {
            WipeoutKeywords.WipeoutLog,
            WipeoutKeywords.WipeoutLogEcho,
            "End-of-Timeline has been detected.",
        };

        private static Regex CreateDefeatedLogRegex(string playerName) => new Regex(
            $@"^19:[0-9a-fA-F]{8}:{playerName}:",
            RegexOptions.Compiled);

        private Regex defeatedLogRegex = CreateDefeatedLogRegex("Naoki Yoshida");

        private bool inFeast;

        private bool ignoreWipeout = false;

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
                    (DateTime.UtcNow - Config.Instance.TryCountTimestamp) >=
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
                        (DateTime.UtcNow - Config.Instance.TryCountTimestamp) >=
                        TimeSpan.FromHours(Config.Instance.TryCountResetInterval))
                    {
                        this.TryCount = 0;
                    }
                }
            }

            if (isStart ||
                xivlog.Log.Contains("/xivlog rec"))
            {
                SystemSounds.Beep.Play();
                this.deathCount = 0;
                this.StartRecording();
                return;
            }

            // 録画を止めないようにする
            if (xivlog.Log.Contains("reset-on-wipe-out disabled"))
            {
                ignoreWipeout = true;
            }
            // 元に戻す
            if (xivlog.Log.Contains("reset-on-wipe-out enabled"))
            {
                ignoreWipeout = false;
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
                if (!ignoreWipeout)
                {
                    this.FinishRecording();
                    SystemSounds.Beep.Play();
                    return;
                }
            }

            // Player change
            match = PlayerChangedLogRegex.Match(xivlog.Log);
            if (match.Success)
            {
                this.defeatedLogRegex = CreateDefeatedLogRegex(this.playerName);
                return;
            }

            // Player defeated
            if (this.defeatedLogRegex.IsMatch(xivlog.Log))
            {
                this.deathCount++;
                return;
            }
        }

        private DateTime startTime;
        private string contentName = string.Empty;
        private string playerName = string.Empty;
        private int deathCount = 0;

        private int TryCount
        {
            get => Config.Instance.VideoTryCount;
            set => WPFHelper.Invoke(() =>
            {
                Config.Instance.VideoTryCount = value;
                Config.Instance.TryCountTimestamp = DateTime.UtcNow;

                var contentName = !string.IsNullOrEmpty(this.contentName) ?
                    this.contentName :
                    ActGlobals.oFormActMain.CurrentZone;

                Config.Instance.TryCountContentName = contentName;
            });
        }

        /// <summary>
        /// 戦闘終了後に録画停止していない場合に停止させるためのタイマー
        /// </summary>
        private System.Timers.Timer StopRecordingSubscriber;

        private bool prevInCombat;
        private DateTime endCombatDateTime;

        private void DetectStopRecording()
        {
            var min = Config.Instance.StopRecordingAfterCombatMinutes;

            if (min <= 0d)
            {
                return;
            }

            if (!Config.Instance.IsRecording)
            {
                return;
            }

            var inCombat = XIVPluginHelper.Instance.InCombat;

            if (this.prevInCombat != inCombat)
            {
                if (!inCombat)
                {
                    this.endCombatDateTime = DateTime.UtcNow;
                }
            }

            if (!inCombat)
            {
                if ((DateTime.UtcNow - this.endCombatDateTime) >= TimeSpan.FromMinutes(min))
                {
                    this.FinishRecording();
                }
            }

            this.prevInCombat = inCombat;
        }

        public async void StartRecording()
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
                    bool ret = await this.SendToggleRecording();
                    if (!ret)
                    {
                        return;
                    }
                }
            }

            await WPFHelper.InvokeAsync(async () =>
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

                this.playerName = CombatantsManager.Instance.Player.Name;

                if (Config.Instance.IsShowTitleCard)
                {
                    TitleCardView.ShowTitleCard(
                        contentName,
                        this.TryCount,
                        this.startTime);
                }
            });

            if (Config.Instance.StopRecordingAfterCombatMinutes > 0)
            {
                lock (this)
                {
                    if (this.StopRecordingSubscriber == null)
                    {
                        var interval = Config.Instance.StopRecordingSubscribeInterval;
                        if (interval <= 0)
                        {
                            interval = 10;
                        }

                        this.StopRecordingSubscriber = new System.Timers.Timer(interval * 1000);
                        this.StopRecordingSubscriber.Elapsed += (_, __) => this.DetectStopRecording();
                    }

                    this.endCombatDateTime = DateTime.UtcNow;
                    this.StopRecordingSubscriber.Start();
                }
            }
        }

        private static readonly string VideoDurationPlaceholder = "#duration#";

        public async void FinishRecording()
        {
            this.StopRecordingSubscriber?.Stop();

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
                bool ret = await this.SendToggleRecording();
                if (!ret)
                {
                    return;
                }
            }

            var contentName = !string.IsNullOrEmpty(this.contentName) ?
                this.contentName :
                ActGlobals.oFormActMain.CurrentZone;

            if (!string.IsNullOrEmpty(Config.Instance.VideoSaveDictory) &&
                Directory.Exists(Config.Instance.VideoSaveDictory))
            {
                await Task.Run(async () =>
                {
                    var now = DateTime.Now;

                    var prefix = Config.Instance.VideFilePrefix.Trim();
                    prefix = string.IsNullOrEmpty(prefix) ?
                        string.Empty :
                        $"{prefix} ";

                    var deathCountText = this.deathCount > 1 ?
                        $" death{this.deathCount - 1}" :
                        string.Empty;

                    var f = $"{prefix}{this.startTime:yyyy-MM-dd HH-mm} {contentName} try{this.TryCount:00} {VideoDurationPlaceholder}{deathCountText}.ext";

                    await Task.Delay(TimeSpan.FromSeconds(4));

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
                                tf.Tag.Album = $"FFXIV - {contentName}";
                                tf.Tag.AlbumArtists = new[] { "FFXIV", this.playerName }.Where(x => !string.IsNullOrEmpty(x)).ToArray();
                                tf.Tag.Genres = new[] { "Game" };
                                tf.Tag.Comment =
                                    $"{prefix} - {contentName}\n" +
                                    $"{this.startTime:yyyy-MM-dd HH:mm} try{this.TryCount}{deathCountText}";
                                tf.Save();
                            }

                            int i = 0;
                            bool result = false;
                            do
                            {
                                try
                                {
                                    File.Move(
                                        original,
                                        dest);
                                    result = true;
                                    break;
                                }
                                catch (Exception ex)
                                {
                                    XIVLogPlugin.Instance.EnqueueLogLine(
                                        $"[XIVLog] rename failed, retry... {ex.Message}");
                                    await Task.Delay(5);
                                    i++;
                                }
                            } while (i < 5);

                            XIVLogPlugin.Instance.EnqueueLogLine(
                                $"[XIVLog] The video was saved. {Path.GetFileName(dest)}");
                            if (!result)
                            {
                                XIVLogPlugin.Instance.EnqueueLogLine(
                                    $"[XIVLog] rename failed.");
                            }
                        }
                    }
                });
            }

            await WPFHelper.InvokeAsync(() =>
            {
                lock (this)
                {
                    Config.Instance.IsRecording = false;
                }
            });
        }

        private readonly Lazy<object> LazySLOBSClient = new Lazy<object>(() => new SlobsPipeClient("slobs"));
        private TaskCompletionSource<bool> identifyReceived;
        private TaskCompletionSource<bool> toggleReceived;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private async Task<bool> SendToggleRecording()
        {
            if (Config.Instance.UseObsWS)
            {
                try
                {
                    WebSocket ws;
                    identifyReceived = new TaskCompletionSource<bool>();
                    toggleReceived = new TaskCompletionSource<bool>();

                    ws = new WebSocket("ws://127.0.0.1:4455");

                    ws.OnMessage += (sender, e) =>
                    {
                        try
                        {
                            var json = JObject.Parse(e.Data);
                            int op = json["op"]?.Value<int>() ?? -1;

                            if (op == 2) // Identify 応答
                            {
                                var authObj = json["d"]["authentication"];
                                if (authObj != null)
                                {
                                    this.AppLogger.Info("[OBS Websocket] Identify failed: OBS requires authentication");
                                    identifyReceived.TrySetResult(false);
                                }
                                else
                                {
                                    this.AppLogger.Info("[OBS Websocket] Identify response received (no auth required)");
                                    identifyReceived.TrySetResult(true);
                                }
                            }
                            else if (op == 7) // リクエスト応答
                            {
                                var status = json["d"]["requestStatus"];
                                bool result = status["result"]?.Value<bool>() ?? false;
                                int code = status["code"]?.Value<int>() ?? 0;
                                string comment = status["comment"]?.Value<string>() ?? "";

                                this.AppLogger.Info($"[OBS WebSocket] ToggleRecord response : Code={code}, Result={(result ? "Success" : "Failure")}, Reason={(string.IsNullOrEmpty(comment) ? "(empty)" : comment)}");

                                toggleReceived.TrySetResult(result);
                            }
                        }
                        catch (Exception ex)
                        {
                            this.AppLogger.Info("[OBS Websocket] JSON Parse error : " + ex.Message);
                        }
                    };

                    ws.OnError += (sender, e) =>
                    {
                        this.AppLogger.Info("[OBS Websocket] error : " + e.Message);
                    };

                    ws.OnClose += (sender, e) =>
                    {
                        this.AppLogger.Info($"[OBS WebSocket] close : Code={e.Code}, Reason={(string.IsNullOrEmpty(e.Reason) ? "(empty)" : e.Reason)}");
                    };

                    ws.Connect();

                    if (!ws.IsAlive)
                    {
                        this.AppLogger.Info("[OBS Websocket] WebSocket connection failed. Please check that OBS is running.");
                        return false;
                    }

                    // Step 1: Identify
                    var identify = new
                    {
                        op = 1,
                        d = new
                        {
                            rpcVersion = 1,
                            authentication = ""
                        }
                    };
                    ws.Send(JsonConvert.SerializeObject(identify));

                    var identifyDone = await Task.WhenAny(identifyReceived.Task, Task.Delay(2000));
                    if (identifyDone != identifyReceived.Task || !identifyReceived.Task.Result)
                    {
                        this.AppLogger.Info("[OBS Websocket] Identify failed or OBS requires authentication");
                        ws.Close();
                        return false;
                    }

                    // Step 2: ToggleRecord リクエスト
                    var toggleRequest = new
                    {
                        op = 6,
                        d = new
                        {
                            requestType = "ToggleRecord",
                            requestId = Guid.NewGuid().ToString()
                        }
                    };
                    ws.Send(JsonConvert.SerializeObject(toggleRequest));

                    var toggleDone = await Task.WhenAny(toggleReceived.Task, Task.Delay(1000));
                    if (toggleDone == toggleReceived.Task && toggleReceived.Task.Result)
                    {
                        this.AppLogger.Info("[OBS Websocket] Success: ToggleRecord completed");
                        ws.Close();
                        return true;
                    }
                    else
                    {
                        this.AppLogger.Info("[OBS Websocket] Failure: ToggleRecord timed out or failed");
                        ws.Close();
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    this.AppLogger.Info(ex.ToString());
                    return false;
                }
            }
            else
            {
                var p = Process.GetProcessesByName("Streamlabs OBS");
                if (p == null ||
                    p.Length < 1)
                {
                    this.AppLogger.Info("Tried to record, but Streamlabs OBS is not found.");
                    return false;
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

                return true;
            }
        }
    }
}
