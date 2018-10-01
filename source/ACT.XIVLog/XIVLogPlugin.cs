using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using Advanced_Combat_Tracker;
using FFXIV.Framework.Common;
using FFXIV.Framework.Extensions;
using FFXIV.Framework.FFXIVHelper;
using TamanegiMage.FFXIV_MemoryReader.Model;

namespace ACT.XIVLog
{
    public class XIVLogPlugin :
        IActPluginV1
    {
        #region IActPluginV1

        private Label pluginLabel;

        public void InitPlugin(
            TabPage pluginScreenSpace,
            Label pluginStatusText)
        {
            pluginScreenSpace.Text = "XIVLog";
            this.pluginLabel = pluginStatusText;
            var i = Config.Instance;

            // 設定Panelを追加する
            pluginScreenSpace.Controls.Add(new ElementHost()
            {
                Child = new ConfigView(),
                Dock = DockStyle.Fill,
            });

            this.InitTask();
            this.pluginLabel.Text = "Plugin Started";
        }

        public void DeInitPlugin()
        {
            this.EndTask();
            Config.Save();
            this.pluginLabel.Text = "Plugin Exited";
            GC.Collect();
        }

        #endregion IActPluginV1

        private string LogfileName =>
            Path.Combine(
                Config.Instance.OutputDirectory,
                $"XIVLog.{DateTime.Now.ToString("yyyy-MM-dd")}.csv");

        private volatile string currentLogfileName = string.Empty;
        private volatile string currentZoneName = string.Empty;

        private static readonly ConcurrentQueue<XIVLog> LogQueue = new ConcurrentQueue<XIVLog>();
        private ThreadWorker dumpLogTask;
        private StreamWriter writter;
        private StringBuilder writeBuffer = new StringBuilder(5120);
        private DateTime lastFlushTimestamp = DateTime.MinValue;

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
                if (string.IsNullOrEmpty(Config.Instance.OutputDirectory) ||
                    LogQueue.IsEmpty)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(Config.Instance.WriteInterval));
                    return;
                }

                if (this.currentZoneName != ActGlobals.oFormActMain.CurrentZone)
                {
                    this.currentZoneName = ActGlobals.oFormActMain.CurrentZone;
                    this.lastFlushTimestamp = DateTime.Now;
                    this.writter?.Flush();
                }

                if ((DateTime.Now - this.lastFlushTimestamp).TotalSeconds
                    >= Config.Instance.FlushInterval)
                {
                    this.lastFlushTimestamp = DateTime.Now;
                    this.writter?.Flush();
                }

                if (this.currentLogfileName != this.LogfileName)
                {
                    if (this.writter != null)
                    {
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
                }

                XIVLog.RefreshPCNameDictionary();

                this.writeBuffer.Clear();
                while (LogQueue.TryDequeue(out XIVLog xivlog))
                {
                    this.writeBuffer.AppendLine(xivlog.ToCSVLine());
                    Thread.Yield();
                }

                if (this.writeBuffer.Length > 0)
                {
                    this.writter.Write(this.writeBuffer.ToString());
                }
            }
        }

        private void EndTask()
        {
            ActGlobals.oFormActMain.OnLogLineRead -= this.OnLogLineRead;

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

        private void OnLogLineRead(
            bool isImport,
            LogLineEventArgs logInfo)
        {
            if (string.IsNullOrEmpty(Config.Instance.OutputDirectory))
            {
                return;
            }

            LogQueue.Enqueue(new XIVLog(isImport, logInfo));

            if (!isImport)
            {
                this.OpenXIVLogAsync(logInfo.logLine);
            }
        }

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

        private Task OpenXIVLogAsync(
            string logLine) => Task.Run(() =>
            {
                const string CommandKeyword = "/xivlog open";

                if (string.IsNullOrEmpty(logLine))
                {
                    return;
                }

                if (!File.Exists(this.LogfileName))
                {
                    return;
                }

                if (logLine.ContainsIgnoreCase(CommandKeyword))
                {
                    Process.Start(this.LogfileName);
                }
            });
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

            /*
            [08:20:19.383] 00:0000:clear stacks of Loading....
            */

            var line = this.LogInfo.logLine;
            var timeString = line.Substring(1, 12);
            this.Timestamp = DateTime.Parse(DateTime.Now.ToString("yyyy-MM-dd") + " " + timeString);
            this.LogType = line.Substring(15, 2);
            this.Log = line.Substring(15);
            this.ZoneName = ActGlobals.oFormActMain?.CurrentZone ?? string.Empty;

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

        private static readonly Dictionary<string, string> PCNameDictionary = new Dictionary<string, string>();

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

        public static async void RefreshPCNameDictionary()
        {
            if (!Config.Instance.IsReplacePCName)
            {
                return;
            }

            await Task.Run(() =>
            {
                var combatants = FFXIVPlugin.Instance?.GetCombatantList()
                    .Where(x => x.type == ObjectType.PC);

                if (combatants == null)
                {
                    return;
                }

                foreach (var com in combatants)
                {
                    var alias = $"{com.AsJob()?.NameEN ?? "Unknown"} {JobAliases[com.Job % 10]}";
                    PCNameDictionary[com.Name] = alias;
                    PCNameDictionary[com.NameFI] = alias;
                    PCNameDictionary[com.NameIF] = alias;
                }
            });
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
                result = result.Replace(entry.Key, entry.Value);
            }

            return result;
        }

        public string ToCSVLine() =>
            $"{this.No}," +
            $"{this.Timestamp:yyyy-MM-dd HH:mm:ss.fff}," +
            $"{(this.IsImport ? 1 : 0)}," +
            $"\"{this.LogType}\"," +
            $"\"{this.GetReplacedLog()}\"," +
            $"\"{this.ZoneName}\"";
    }
}
