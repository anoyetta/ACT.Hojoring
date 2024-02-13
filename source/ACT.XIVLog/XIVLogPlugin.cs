using ACT.Hojoring.Shared;
using Advanced_Combat_Tracker;
using FFXIV.Framework.Common;
using FFXIV.Framework.Extensions;
using FFXIV.Framework.XIVHelper;
using Sharlayan.Core.Enums;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Media;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Integration;

namespace ACT.XIVLog
{
    public class XIVLogPlugin :
        IActPluginV1,
        INotifyPropertyChanged
    {
        #region Singleton

        private static XIVLogPlugin instance;

        public static XIVLogPlugin Instance => instance;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public XIVLogPlugin()
        {
            instance = this;
            CosturaUtility.Initialize();
            AssemblyResolver.Initialize(() => ActGlobals.oFormActMain?.PluginGetSelfData(this)?.pluginFile.DirectoryName);
        }

        #endregion Singleton

        #region IActPluginV1

        private Label pluginLabel;

        public async void InitPlugin(
            TabPage pluginScreenSpace,
            Label pluginStatusText)
        {
            pluginScreenSpace.Text = "XIVLog";
            this.pluginLabel = pluginStatusText;

            DirectoryHelper.GetPluginRootDirectoryDelegate = () => ActGlobals.oFormActMain?.PluginGetSelfData(this)?.pluginFile.DirectoryName;

            // 設定ファイルをロードする
            _ = Config.Instance;

            // 設定Panelを追加する
            pluginScreenSpace.Controls.Add(new ElementHost()
            {
                Child = new ConfigView(),
                Dock = DockStyle.Fill,
            });

            await EnvironmentHelper.WaitInitActDoneAsync();

            this.InitTask();
            this.pluginLabel.Text = "Plugin Started";

            // 設定ファイルをバックアップする
            await EnvironmentHelper.BackupFilesAsync(
                Config.FileName);
        }

        public void DeInitPlugin()
        {
            this.EndTask();
            this.pluginLabel.Text = "Plugin Exited";
            GC.Collect();
        }

        #endregion IActPluginV1

        private static readonly string[] StopLoggingKeywords = new string[]
        {
            WipeoutKeywords.WipeoutLog,
            WipeoutKeywords.WipeoutLogEcho,
            "の攻略を終了した。",
            "クロスワールドパーティが解散されました。",
            "End-of-Timeline has been detected.",
        };

        public string LogfileNameWithoutParent => Path.GetFileName(this.LogfileName);

        public string LogfileName =>
            Path.Combine(
                Config.Instance.OutputDirectory,
                $"{DateTime.Now:yyMMdd}-{this.fileNo:000}.{this.GetZoneNameForFile()}.take{this.wipeoutCounter:00}.csv");

        private static readonly char[] InvalidChars = Path.GetInvalidFileNameChars();

        private string GetZoneNameForFile()
        {
            var zone = this.currentZoneName;

            if (string.IsNullOrEmpty(zone))
            {
                zone = "Unknown Zone";
            }
            else
            {
                zone = string.Concat(zone.Where(c => !InvalidChars.Contains(c)));
            }

            zone = zone.Replace(" ", "_");

            return zone;
        }

        private volatile string currentLogfileName = string.Empty;
        private volatile string currentZoneName = string.Empty;

        private static readonly ConcurrentQueue<XIVLog> LogQueue = new ConcurrentQueue<XIVLog>();
        private ThreadWorker dumpLogTask;
        private StreamWriter writter;
        private DateTime lastFlushTimestamp = DateTime.MinValue;
        private volatile bool isForceFlush = false;
        private int wipeoutCounter = 1;
        private int fileNo = 1;

        private DateTime lastWroteTimestamp = DateTime.MaxValue;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void InitTask()
        {
            // FFXIV.Framework.config を読み込ませる
            lock (FFXIV.Framework.Config.ConfigBlocker)
            {
                _ = FFXIV.Framework.Config.Instance;
            }

            var config = Config.Instance;

            // WriteIntervalの初期値をマイグレーションする
            if (config.WriteInterval >= 30)
            {
                config.WriteInterval = Config.WriteIntervalDefault;
            }

            this.dumpLogTask = ThreadWorker.Run(
                doWork,
                TimeSpan.FromSeconds(config.WriteInterval).TotalMilliseconds,
                "XIVLog Worker",
                ThreadPriority.Lowest);

            ActGlobals.oFormActMain.OnLogLineRead -= this.OnLogLineRead;
            ActGlobals.oFormActMain.OnLogLineRead += this.OnLogLineRead;

            void doWork()
            {
                var isNeedsFlush = false;

                if (string.IsNullOrEmpty(config.OutputDirectory))
                {
                    Thread.Sleep(TimeSpan.FromSeconds(config.WriteInterval));
                    return;
                }

                if (LogQueue.IsEmpty)
                {
                    if ((DateTime.UtcNow - this.lastWroteTimestamp).TotalSeconds > 10)
                    {
                        this.lastWroteTimestamp = DateTime.MaxValue;
                        isNeedsFlush = true;
                    }
                    else
                    {
                        if (!this.isForceFlush)
                        {
                            Thread.Sleep(TimeSpan.FromSeconds(config.WriteInterval));
                            return;
                        }
                    }
                }

                if ((DateTime.UtcNow - this.lastFlushTimestamp).TotalSeconds
                    >= config.FlushInterval)
                {
                    isNeedsFlush = true;
                }

                if (this.currentLogfileName != this.LogfileName)
                {
                    if (this.writter != null)
                    {
                        this.writter.Flush();
                        this.writter.Close();
                        this.writter.Dispose();
                    }

                    if (!Directory.Exists(config.OutputDirectory))
                    {
                        Directory.CreateDirectory(config.OutputDirectory);
                    }

                    this.writter = new StreamWriter(
                        new FileStream(
                            this.LogfileName,
                            FileMode.Append,
                            FileAccess.Write,
                            FileShare.Read,
                            64 * 1024),
                        new UTF8Encoding(config.WithBOM));
                    this.currentLogfileName = this.LogfileName;

                    this.RaisePropertyChanged(nameof(this.LogfileName));
                    this.RaisePropertyChanged(nameof(this.LogfileNameWithoutParent));
                }

                XIVLog.RefreshPCNameDictionary();

                while (LogQueue.TryDequeue(out XIVLog xivlog))
                {
                    if (this.currentZoneName != xivlog.ZoneName)
                    {
                        this.currentZoneName = xivlog.ZoneName;
                        this.wipeoutCounter = 1;
                        this.fileNo++;
                        isNeedsFlush = true;
                    }

                    if (StopLoggingKeywords.Any(x => xivlog.Log.Contains(x)))
                    {
                        this.wipeoutCounter++;
                        this.fileNo++;
                        isNeedsFlush = true;
                    }

                    // ログをParseする
                    xivlog.Parse();

                    this.writter.WriteLine(xivlog.ToCSVLine());
                    this.lastWroteTimestamp = DateTime.UtcNow;
                    Thread.Yield();
                }

                if (isNeedsFlush ||
                    this.isForceFlush)
                {
                    if (isNeedsFlush || this.isForceFlush)
                    {
                        this.isForceFlush = false;
                        this.lastFlushTimestamp = DateTime.UtcNow;
                        this.writter?.Flush();
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void EndTask()
        {
            Config.Save();

            ActGlobals.oFormActMain.OnLogLineRead -= this.OnLogLineRead;

            VideoCapture.Instance.FinishRecording();

            if (dumpLogTask != null)
            {
                this.dumpLogTask.Abort();
                this.dumpLogTask = null;
            }

            if (this.writter != null)
            {
                this.writter.Flush();
                this.writter.Close();
                this.writter.Dispose();
                this.writter = null;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void OnLogLineRead(
            bool isImport,
            LogLineEventArgs logInfo)
        {
            if (string.IsNullOrEmpty(Config.Instance.OutputDirectory) &&
                !Config.Instance.IsEnabledRecording &&
                !Config.Instance.IsShowTitleCard)
            {
                return;
            }

            var xivlog = new XIVLog(isImport, logInfo);
            if (string.IsNullOrEmpty(xivlog.Log))
            {
                return;
            }

            LogQueue.Enqueue(xivlog);

            if (!isImport)
            {
                this.OpenXIVLog(logInfo.logLine);
                VideoCapture.Instance.DetectCapture(xivlog);
            }
        }

        public void EnqueueLogLine(
            string message)
        {
            if (string.IsNullOrEmpty(Config.Instance.OutputDirectory) &&
                !Config.Instance.IsEnabledRecording &&
                !Config.Instance.IsShowTitleCard)
            {
                return;
            }

            LogParser.RaiseLog(DateTime.Now, message);
        }

        /*
        private string ConvertZoneNameToLog()
        {
            var result = this.currentZoneName;

            if (string.IsNullOrEmpty(result))
            {
                result = "GLOBAL";
            }
            else
            {
                // 無効な文字を置き換える
                result = string.Concat(
                    result.Select(c =>
                        Path.GetInvalidFileNameChars().Contains(c) ?
                        '_' :
                        c));
            }

            return result;
        }
        */

        private const string CommandKeywordOpen = "/xivlog open";
        private const string CommandKeywordFlush = "/xivlog flush";

        private void OpenXIVLog(
            string logLine)
        {
            if (string.IsNullOrEmpty(logLine))
            {
                return;
            }

            if (!File.Exists(this.LogfileName))
            {
                return;
            }

            if (logLine.ContainsIgnoreCase(CommandKeywordOpen))
            {
                this.isForceFlush = true;

                Task.Run(async () =>
                {
                    for (int i = 0; i < 5; i++)
                    {
                        if (!this.isForceFlush)
                        {
                            break;
                        }

                        await Task.Delay(TimeSpan.FromSeconds(0.1));
                    }

                    Process.Start(this.LogfileName);
                    SystemSounds.Beep.Play();
                });
            }

            if (logLine.ContainsIgnoreCase(CommandKeywordFlush))
            {
                this.isForceFlush = true;
                SystemSounds.Beep.Play();
                return;
            }
        }

        public async Task ForceFlushAsync()
            => await Task.Run(() =>
        {
            this.isForceFlush = true;

            Task.Run(async () =>
            {
                for (int i = 0; i < 10; i++)
                {
                    if (!this.isForceFlush)
                    {
                        break;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(0.1));
                }

                SystemSounds.Beep.Play();
            });
        });

        #region INotifyPropertyChanged

        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(
            [CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }

        protected virtual bool SetProperty<T>(
            ref T field,
            T value,
            [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value))
            {
                return false;
            }

            field = value;
            this.PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));

            return true;
        }

        #endregion INotifyPropertyChanged
    }

    public class XIVLog
    {
        public XIVLog(
            bool isImport,
            LogLineEventArgs logInfo)
        {
            if (logInfo == null ||
                string.IsNullOrEmpty(logInfo.logLine))
            {
                return;
            }

            this.IsImport = isImport;
            this.LogInfo = logInfo;

            // ログの書式の例
            /*
            旧形式：
            [08:20:19.383] 00:0000:clear stacks of Loading....
            */

            /*
            新形式：
            [00:40:19.102] AddCombatant 03:40000C94:木人:00:1:0000:00::541:901:44:44:0:10000:::94.38:55.71:7.07:2.43
            */

            var line = this.LogInfo.logLine;

            // 15文字未満のログは書式エラーになるため無視する
            if (line.Length < 15)
            {
                return;
            }

            var timeString = line.Substring(1, 12);

            var timestampString = DateTime.Now.ToString("yyyy-MM-dd") + " " + timeString;
            if (DateTime.TryParse(timestampString, out DateTime d))
            {
                this.Timestamp = d;
            }
            else
            {
                // タイムスタンプ書式が不正なものは無視する
                return;
            }

            // タイムスタンプの後を取り出す
            var message = line.Substring(15);

            // ログタイプを除去する
            var i = message.IndexOf(" ");
            if (i < 0)
            {
                return;
            }

            message = message.Substring(i + 1);

            this.LogType = message.Substring(0, message.IndexOf(":"));
            this.Log = message;
            this.ZoneName = !string.IsNullOrEmpty(logInfo.detectedZone) ?
                logInfo.detectedZone :
                "NO DATA";

            if (currentNo >= int.MaxValue)
            {
                currentNo = 0;
            }

            currentNo++;
            this.No = currentNo;
        }

        private static volatile int currentNo = 0;

        public int No { get; private set; }

        public DateTime Timestamp { get; private set; }

        public bool IsImport { get; private set; }

        public string LogType { get; private set; }

        public string ZoneName { get; private set; }

        /// <summary>
        /// FFXIV_ACT_Plugin からの生のログ
        /// </summary>
        public string Log { get; private set; } = string.Empty;

        /// <summary>
        /// Log に対してPC名を無読化置換したログ
        /// </summary>
        public string LogReplacedPCName { get; private set; } = string.Empty;

        /// <summary>
        /// LogReplacedPCName に対してHojoring内の判定で使用している形式にParseしたログ
        /// </summary>
        public string ParsedLog { get; private set; } = string.Empty;

        public LogLineEventArgs LogInfo { get; set; }

        private static readonly Dictionary<string, Alias> PCNameDictionary = new Dictionary<string, Alias>(512);

        private static readonly string[] JobAliases = new[]
        {
            "Jackson",  // 0
            "Olivia",   // 1
            "Harry",    // 2
            "Lily",     // 3
            "Lucas",    // 4
            "Sophia",   // 5
            "Jack",     // 6
            "Emily",    // 7
            "Michael",  // 8
            "Amelia",   // 9
        };

        public static void RefreshPCNameDictionary()
        {
            if (!Config.Instance.IsReplacePCName)
            {
                return;
            }

            var combatants = CombatantsManager.Instance.GetCombatants()
                .Where(x => x.ActorType == Actor.Type.PC);

            if (combatants == null)
            {
                return;
            }

            var now = DateTime.Now;
            // 古くなったエントリを削除する
            var olds = PCNameDictionary.Where(x =>
                (now - x.Value.Timestamp).TotalMinutes >= 10.0)
                .ToArray();
            foreach (var toRemove in olds)
            {
                PCNameDictionary.Remove(toRemove.Key);
                Thread.Yield();
            }

            foreach (var com in combatants)
            {
                var alias = new Alias(
                    $"{com.JobInfo?.NameEN.Replace(" ", string.Empty) ?? "Unknown"} {JobAliases[com.Job % 10]}",
                    now);
                PCNameDictionary[com.Name] = alias;
                PCNameDictionary[com.NameFI] = alias;
                PCNameDictionary[com.NameIF] = alias;
                Thread.Yield();
            }
        }

        public void Parse()
        {
            // PC名を置換する
            this.ReplacePCName();

            var log = this.LogReplacedPCName;

            int.TryParse(
                this.LogType,
                NumberStyles.HexNumber,
                CultureInfo.InvariantCulture,
                out int detectedType);

            // ツールチップシンボル, ワールド名を除去する
            log = LogParser.RemoveTooltipSynbols(log);
            log = LogParser.RemoveWorldName(log);

            // ログを互換形式に変換する
            log = LogParser.FormatLogLine(
                detectedType,
                log);

            this.ParsedLog = log;
        }

        public void ReplacePCName()
        {
            if (!Config.Instance.IsReplacePCName)
            {
                this.LogReplacedPCName = this.Log;
                return;
            }

            var result = this.Log;

            foreach (var entry in PCNameDictionary)
            {
                if (string.IsNullOrEmpty(entry.Key))
                {
                    continue;
                }

                var before = result;
                result = result.Replace(entry.Key, entry.Value.Replacement);
                if (result != before)
                {
                    entry.Value.Timestamp = DateTime.Now;
                }
            }

            this.LogReplacedPCName = result;
        }

        public string ToCSVLine()
        {
            var csv =
                $"{this.No:000000000}," +
                $"{this.Timestamp:yyyy-MM-dd HH:mm:ss.fff}," +
                $"{(this.IsImport ? 1 : 0)}," +
                $"\"{this.LogType}\"," +
                $"\"{this.ParsedLog}\"," +
                $"\"{this.ZoneName}\"";

            if (Config.Instance.IsAlsoOutputsRawLogLine)
            {
                csv += $",\"{this.Log}\"";
            }

            return csv;
        }

        public override string ToString() => string.IsNullOrEmpty(this.LogReplacedPCName) ?
            this.Log :
            this.LogReplacedPCName;

        public class Alias
        {
            public Alias(
                string replacement,
                DateTime timestamp)
            {
                this.Replacement = replacement;
                this.Timestamp = timestamp;
            }

            public string Replacement { get; set; }
            public DateTime Timestamp { get; set; }
        }
    }
}
