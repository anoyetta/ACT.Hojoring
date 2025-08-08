using Advanced_Combat_Tracker;
using FFXIV.Framework.Common;
using FFXIV.Framework.XIVHelper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp;
using WindowsInput;

namespace ACT.XIVLog
{
    public class VideoCapture
    {
        #region Lazy Instance

        private static readonly Lazy<VideoCapture> LazyInstance = new Lazy<VideoCapture>(() => new VideoCapture());

        public static VideoCapture Instance => LazyInstance.Value;

        #region Logger

        private NLog.Logger AppLogger => FFXIV.Framework.Common.AppLog.DefaultLogger;

        #endregion Logger

        private VideoCapture()
        {
        }

        #endregion Lazy Instance

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
            if (Config.Instance.IsRecording)
            {
                return;
            }

            lock (this)
            {
                this.TryCount++;
                this.startTime = DateTime.Now;
            }

            try
            {
                if (!Config.Instance.IsEnabledRecording)
                {
                    return;
                }

                if (!Config.Instance.UseObsRpc)
                {
                    this.Input.Keyboard.ModifiedKeyStroke(
                        Config.Instance.StartRecordingShortcut.GetModifiers(),
                        Config.Instance.StartRecordingShortcut.GetKeys());
                }
                else
                {
                    var (success, _) = await this.SendToggleRecording(true);
                    if (!success)
                    {
                        return;
                    }
                }

                await WPFHelper.InvokeAsync(async () =>
                {
                    var contentName = !string.IsNullOrEmpty(this.contentName) ?
                        this.contentName :
                        ActGlobals.oFormActMain.CurrentZone;

                    this.playerName = CombatantsManager.Instance.Player.Name;

                    if (Config.Instance.IsShowTitleCard)
                    {
                        TitleCardView.ShowTitleCard(contentName, this.TryCount, this.startTime);
                    }

                    await Task.Delay(TimeSpan.FromSeconds(5));
                });
            }
            catch (Exception ex)
            {
                AppLogger.Info($"[XIVLog] StartRecording exception: {ex.Message}");
            }
            finally
            {
                await WPFHelper.InvokeAsync(() =>
                {
                    if (Config.Instance.IsEnabledRecording)
                    {
                        lock (this)
                        {
                            Config.Instance.IsRecording = true;
                        }
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
        }


        private static readonly string VideoDurationPlaceholder = "#duration#";
        public async void FinishRecording()
        {
            this.StopRecordingSubscriber?.Stop();

            try
            {
                lock (this)
                {
                    if (!Config.Instance.IsRecording)
                        return;
                }

                if (!Config.Instance.IsEnabledRecording)
                    return;

                string outputPath = null;

                if (!Config.Instance.UseObsRpc)
                {
                    this.Input.Keyboard.ModifiedKeyStroke(
                        Config.Instance.StopRecordingShortcut.GetModifiers(),
                        Config.Instance.StopRecordingShortcut.GetKeys());
                }
                else
                {
                    var (success, path) = await this.SendToggleRecording(false);
                    if (!success || string.IsNullOrEmpty(path))
                        return;

                    for (int i = 0; i < 3; i++)
                    {
                        if (IsFileReady(path))
                        {
                            outputPath = path;
                            break;
                        }
                        await Task.Delay(500);
                    }

                    if (string.IsNullOrEmpty(outputPath))
                    {
                        AppLogger.Info("[XIVLog] output file not ready.");
                        return;
                    }
                }

                var contentName = !string.IsNullOrEmpty(this.contentName) ?
                    this.contentName :
                    ActGlobals.oFormActMain.CurrentZone;

                if (!string.IsNullOrEmpty(Config.Instance.VideoSaveDictory) &&
                    Directory.Exists(Config.Instance.VideoSaveDictory))
                {
                    await Task.Run(() =>
                    {
                        var now = DateTime.Now;
                        var prefix = Config.Instance.VideFilePrefix.Trim();
                        prefix = string.IsNullOrEmpty(prefix) ? string.Empty : $"{prefix} ";
                        var deathCountText = this.deathCount > 1 ? $" death{this.deathCount - 1}" : string.Empty;
                        var f = $"{prefix}{this.startTime:yyyy-MM-dd HH-mm} {contentName} try{this.TryCount:00} {VideoDurationPlaceholder}{deathCountText}.ext";

                        if (File.Exists(outputPath))
                        {
                            var ext = Path.GetExtension(outputPath);
                            f = f.Replace(".ext", ext);
                            var dest = Path.Combine(Path.GetDirectoryName(outputPath), f);

                            try
                            {
                                using (var tf = TagLib.File.Create(outputPath))
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
                            }
                            catch (Exception ex)
                            {
                                AppLogger.Info($"[XIVLog] TagLib Save failed: {ex.Message}");
                            }

                            int i = 0;
                            bool result = false;
                            do
                            {
                                try
                                {
                                    File.Move(outputPath, dest);
                                    result = true;
                                    break;
                                }
                                catch (Exception ex)
                                {
                                    AppLogger.Info($"[XIVLog] rename failed, retry... {ex.Message}");
                                    Thread.Sleep(5);
                                    i++;
                                }
                            } while (i < 5);

                            AppLogger.Info($"[XIVLog] The video was saved. {Path.GetFileName(dest)}");
                            if (!result)
                                AppLogger.Info($"[XIVLog] rename failed.");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                AppLogger.Info($"[XIVLog] FinishRecording exception: {ex.Message}");
            }
            finally
            {
                await WPFHelper.InvokeAsync(() =>
                {
                    lock (this)
                    {
                        Config.Instance.IsRecording = false;
                    }
                });
            }
        }

        private bool IsFileReady(string path)
        {
            try
            {
                using (var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    return stream.Length > 0;
                }
            }
            catch
            {
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public async Task<(bool success, string outputPath)> SendToggleRecording(bool start)
        {
            try
            {
                WebSocket ws;
                var helloReceived = new TaskCompletionSource<JObject>();
                var identifyReceived = new TaskCompletionSource<bool>();
                var recordStatusReceived = new TaskCompletionSource<bool?>();
                var recordControlReceived = new TaskCompletionSource<bool>();
                var recordStoppedReceived = new TaskCompletionSource<string>(); // outputPath

                ws = new WebSocket("ws://127.0.0.1:4455");

                ws.OnMessage += (sender, e) =>
                    {
                        try
                        {
                            var json = JObject.Parse(e.Data);
                            int op = json["op"]?.Value<int>() ?? -1;

                            if (op == 0) // Hello
                            {
                                helloReceived.TrySetResult(json["d"] as JObject);
                            }
                            else if (op == 2) // Identified
                            {
                                identifyReceived.TrySetResult(true);
                            }
                            else if (op == 7) // RequestResponse
                            {
                                string reqType = json["d"]["requestType"]?.Value<string>();
                                var result = json["d"]["requestStatus"]["result"]?.Value<bool>() ?? false;

                                if (reqType == "GetRecordStatus")
                                {
                                    if (!result)
                                        recordStatusReceived.TrySetResult(null);
                                    else
                                        recordStatusReceived.TrySetResult(json["d"]["responseData"]["outputActive"]?.Value<bool>());
                                }
                                else if (reqType == "StartRecord" || reqType == "StopRecord")
                                {
                                    recordControlReceived.TrySetResult(result);
                                }
                            }
                            else if (op == 5) // Event
                            {
                                string eventType = json["d"]["eventType"]?.Value<string>() ?? "";

                                if (eventType == "RecordStateChanged")
                                {
                                    string state = json["d"]["eventData"]["outputState"]?.Value<string>() ?? "";
                                    string outputPath = json["d"]["eventData"]["outputPath"]?.Value<string>() ?? "";

                                    if (state == "OBS_WEBSOCKET_OUTPUT_STOPPED" && !string.IsNullOrEmpty(outputPath))
                                    {
                                        recordStoppedReceived.TrySetResult(outputPath);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            this.AppLogger.Info("[OBS WebSocket] JSON Parse error: " + ex.Message);
                        }
                    };

                ws.Connect();
                if (!ws.IsAlive)
                {
                    this.AppLogger.Info("[OBS WebSocket] WebSocket connection failed.");
                    LogManager.Flush();
                    return (false, null);
                }

                // Hello
                var helloDone = await Task.WhenAny(helloReceived.Task, Task.Delay(2000));
                if (helloDone != helloReceived.Task)
                {
                    ws.Close();
                    return (false, null);
                }

                var helloData = helloReceived.Task.Result;
                string authString = "";
                if (helloData["authentication"] != null)
                {
                    string challenge = helloData["authentication"]["challenge"].Value<string>();
                    string salt = helloData["authentication"]["salt"].Value<string>();
                    string password = ""; // そのうちUIで設定できるようにする

                    string secret = Convert.ToBase64String(
                        System.Security.Cryptography.SHA256.Create().ComputeHash(
                            Encoding.UTF8.GetBytes(password + salt)
                        )
                    );
                    authString = Convert.ToBase64String(
                        System.Security.Cryptography.SHA256.Create().ComputeHash(
                            Encoding.UTF8.GetBytes(secret + challenge)
                        )
                    );
                }

                // Identify（eventIntentにOutputsを含める = 1 << 6 = 64）
                var identify = new
                {
                    op = 1,
                    d = new
                    {
                        rpcVersion = helloData["rpcVersion"].Value<int>(),
                        authentication = string.IsNullOrEmpty(authString) ? null : authString,
                        eventSubscriptions = 64
                    }
                };
                ws.Send(JsonConvert.SerializeObject(identify));

                var identifyDone = await Task.WhenAny(identifyReceived.Task, Task.Delay(2000));
                if (identifyDone != identifyReceived.Task || !identifyReceived.Task.Result)
                {
                    ws.Close();
                    return (false, null);
                }

                // GetRecordStatus
                var statusRequest = new
                {
                    op = 6,
                    d = new
                    {
                        requestType = "GetRecordStatus",
                        requestId = Guid.NewGuid().ToString()
                    }
                };
                ws.Send(JsonConvert.SerializeObject(statusRequest));
                var statusDone = await Task.WhenAny(recordStatusReceived.Task, Task.Delay(2000));
                bool? isRecording = statusDone == recordStatusReceived.Task ? recordStatusReceived.Task.Result : null;

                if (isRecording == null)
                {
                    ws.Close();
                    return (false, null);
                }

                // 実行条件判定
                if ((start && !isRecording.Value) || (!start && isRecording.Value))
                {
                    string reqType = start ? "StartRecord" : "StopRecord";
                    var recordRequest = new
                    {
                        op = 6,
                        d = new
                        {
                            requestType = reqType,
                            requestId = Guid.NewGuid().ToString()
                        }
                    };
                    ws.Send(JsonConvert.SerializeObject(recordRequest));

                    var controlDone = await Task.WhenAny(recordControlReceived.Task, Task.Delay(2000));
                    if (controlDone != recordControlReceived.Task || !recordControlReceived.Task.Result)
                    {
                        ws.Close();
                        return (false, null);
                    }

                    // StopRecord時に outputPath を待つ
                    if (!start)
                    {
                        var stoppedDone = await Task.WhenAny(recordStoppedReceived.Task, Task.Delay(10000));
                        ws.Close();
                        if (stoppedDone == recordStoppedReceived.Task)
                            return (true, recordStoppedReceived.Task.Result);
                        else
                            return (false, null);
                    }

                    ws.Close();
                    return (true, null);
                }
                else
                {
                    this.AppLogger.Info("[OBS WebSocket] No action needed.");
                    LogManager.Flush();
                    ws.Close();
                    return (true, null);
                }
            }
            catch (Exception ex)
            {
                this.AppLogger.Info("[OBS WebSocket] Exception: " + ex.ToString());
                LogManager.Flush();
                return (false, null);
            }
        }
    }
}
