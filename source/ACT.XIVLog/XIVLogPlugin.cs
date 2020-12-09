using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using Advanced_Combat_Tracker;
using FFXIV.Framework.Common;
using FFXIV.Framework.Extensions;
using FFXIV.Framework.XIVHelper;
using Sharlayan.Core.Enums;

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
            AssemblyResolver.Instance.Initialize(this);
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

            // 設定ファイルをロードする
            _ = Config.Instance;

            // 設定Panelを追加する
            pluginScreenSpace.Controls.Add(new ElementHost()
            {
                Child = new ConfigView(),
                Dock = DockStyle.Fill,
            });

            EnvironmentHelper.WaitInitActDone();

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
        private readonly StringBuilder writeBuffer = new StringBuilder(5120);
        private DateTime lastFlushTimestamp = DateTime.MinValue;
        private volatile bool isForceFlush = false;
        private int wipeoutCounter = 1;
        private int fileNo = 1;

        private DateTime lastWroteTimestamp = DateTime.MaxValue;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void InitTask()
        {
            this.dumpLogTask = ThreadWorker.Run(
                doWork,
                TimeSpan.FromSeconds(Config.Instance.WriteInterval).TotalMilliseconds,
                "XIVLog Worker",
                ThreadPriority.Lowest);

            ActGlobals.oFormActMain.OnLogLineRead -= this.OnLogLineRead;
            ActGlobals.oFormActMain.OnLogLineRead += this.OnLogLineRead;

            void doWork()
            {
                var isNeedsFlush = false;

                if (string.IsNullOrEmpty(Config.Instance.OutputDirectory))
                {
                    Thread.Sleep(TimeSpan.FromSeconds(Config.Instance.WriteInterval));
                    return;
                }

                if (LogQueue.IsEmpty)
                {
                    if ((DateTime.Now - this.lastWroteTimestamp).TotalSeconds > 10)
                    {
                        this.lastWroteTimestamp = DateTime.MaxValue;
                        isNeedsFlush = true;
                    }
                    else
                    {
                        Thread.Sleep(TimeSpan.FromSeconds(Config.Instance.WriteInterval));
                        return;
                    }
                }

                if ((DateTime.Now - this.lastFlushTimestamp).TotalSeconds
                    >= Config.Instance.FlushInterval)
                {
                    isNeedsFlush = true;
                }

                if (this.currentLogfileName != this.LogfileName)
                {
                    if (this.writter != null)
                    {
                        if (this.writeBuffer.Length > 0)
                        {
                            this.writter.Write(this.writeBuffer.ToString());
                            this.writeBuffer.Clear();
                        }

                        this.writter.Flush();
                        this.writter.Close();
                        this.writter.Dispose();
                    }

                    if (!Directory.Exists(Config.Instance.OutputDirectory))
                    {
                        Directory.CreateDirectory(Config.Instance.OutputDirectory);
                    }

                    this.writter = new StreamWriter(
                        new FileStream(
                            this.LogfileName,
                            FileMode.Append,
                            FileAccess.Write,
                            FileShare.Read),
                        new UTF8Encoding(false));
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

                    this.writeBuffer.AppendLine(xivlog.ToCSVLine());
                    this.lastWroteTimestamp = DateTime.Now;
                    Thread.Yield();
                }

                if (isNeedsFlush ||
                    this.isForceFlush ||
                    this.writeBuffer.Length > 5000)
                {
                    if (this.writeBuffer.Length > 0)
                    {
                        this.writter.Write(this.writeBuffer.ToString());
                        this.writeBuffer.Clear();
                    }

                    if (isNeedsFlush || this.isForceFlush)
                    {
                        this.isForceFlush = false;
                        this.lastFlushTimestamp = DateTime.Now;
                        this.writter?.Flush();
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void EndTask()
        {
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
        private async void OnLogLineRead(
            bool isImport,
            LogLineEventArgs logInfo)
        {
            if (string.IsNullOrEmpty(Config.Instance.OutputDirectory) &&
                !Config.Instance.IsEnabledRecording &&
                !Config.Instance.IsShowTitleCard)
            {
                return;
            }

            var xivlog = default(XIVLog);

            await Task.Run(() =>
            {
                xivlog = new XIVLog(isImport, logInfo);
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
            });
        }

        public void EnqueueLogLine(
            string logMessageType,
            string message,
            string zone = null)
        {
            if (string.IsNullOrEmpty(Config.Instance.OutputDirectory) &&
                !Config.Instance.IsEnabledRecording &&
                !Config.Instance.IsShowTitleCard)
            {
                return;
            }

            zone ??= this.currentZoneName;

            var log = new LogLineEventArgs(
                $"[{DateTime.Now:HH:mm:ss.fff}] {logMessageType}:{message}",
                int.Parse(logMessageType),
                DateTime.Now,
                zone,
                true);

            LogQueue.Enqueue(new XIVLog(false, log));
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
                SystemSounds.Beep.Play();
                Task.Run(() => Process.Start(this.LogfileName));
            }

            if (logLine.ContainsIgnoreCase(CommandKeywordFlush))
            {
                this.isForceFlush = true;
                SystemSounds.Beep.Play();
                return;
            }
        }

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
            [08:20:19.383] 00:0000:clear stacks of Loading....
            */

            var line = this.LogInfo.logLine;

            // 18文字未満のログは書式エラーになるため無視する
            if (line.Length < 18)
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

            this.LogType = line.Substring(15, 2);
            this.Log = line.Substring(15);
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

        public string Log { get; private set; }

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

            // 古くなったエントリを削除する
            var olds = PCNameDictionary.Where(x =>
                (DateTime.Now - x.Value.Timestamp).TotalMinutes >= 10.0)
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
                    DateTime.Now);
                PCNameDictionary[com.Name] = alias;
                PCNameDictionary[com.NameFI] = alias;
                PCNameDictionary[com.NameIF] = alias;
                Thread.Yield();
            }
        }

        public string GetReplacedLog()
        {
            if (!Config.Instance.IsReplacePCName)
            {
                return this.Log;
            }

            var result = this.Log;

            foreach (var entry in PCNameDictionary)
            {
                var before = result;
                result = result.Replace(entry.Key, entry.Value.Replacement);
                if (result != before)
                {
                    entry.Value.Timestamp = DateTime.Now;
                }

                Thread.Yield();
            }

            return result;
        }

        public string ToCSVLine() =>
            $"{this.No:000000000}," +
            $"{this.Timestamp:yyyy-MM-dd HH:mm:ss.fff}," +
            $"{(this.IsImport ? 1 : 0)}," +
            $"\"{this.LogType}\"," +
            $"\"{this.GetReplacedLog()}\"," +
            $"\"{this.ZoneName}\"";

        public override string ToString() => this.GetReplacedLog();

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
