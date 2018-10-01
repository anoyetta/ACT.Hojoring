#region using

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Advanced_Combat_Tracker;

#endregion using

#region AssemblyInfo

[assembly: AssemblyTitle("ACT.XIVLog")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("ACT.XIVLog")]
[assembly: AssemblyCopyright("Copyright © RINGS 2018")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
[assembly: Guid("d504e286-56ec-494f-82cd-fd71aefef606")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

#endregion AssemblyInfo

namespace ACT.XIVLog
{
    public class XIVLogPlugin :
        IActPluginV1
    {
        /// <summary>
        /// 出力先ディレクトリ
        /// </summary>
        /// <remarks>
        /// 自分で変えて使用してください
        /// </remarks>
        private const string OutputDirectory =
            @"E:\\games\\FFXIV\\RawLogs";

        private string LogfileName
        {
            get
            {
                return Path.Combine(
                    OutputDirectory,
                    "XIVLog." + DateTime.Now.ToString("yyyy-MM-dd") + ".csv");
            }
        }

        private Label pluginLabel;

        private readonly ConcurrentQueue<XIVLog> LogQueue
            = new ConcurrentQueue<XIVLog>();

        private Task dumpLogTask;
        private volatile bool isRunning = false;
        private StreamWriter writter;

        public void InitPlugin(
            TabPage pluginScreenSpace,
            Label pluginStatusText)
        {
            pluginScreenSpace.Text = "XIVLog";
            this.pluginLabel = pluginStatusText;

            this.InitTask();

            ActGlobals.oFormActMain.OnLogLineRead -= this.OnLogLineRead;
            ActGlobals.oFormActMain.OnLogLineRead += this.OnLogLineRead;

            this.pluginLabel.Text = "Plugin Started";
        }

        public void DeInitPlugin()
        {
            ActGlobals.oFormActMain.OnLogLineRead -= this.OnLogLineRead;

            this.isRunning = false;

            if (dumpLogTask != null)
            {
                this.dumpLogTask.Wait();
                this.dumpLogTask.Dispose();
            }

            if (this.writter != null)
            {
                this.writter.Flush();
                this.writter.Dispose();
            }

            this.pluginLabel.Text = "Plugin Exited";
        }

        private string currentZoneName;

        private void InitTask()
        {
            if (!Directory.Exists(OutputDirectory))
            {
                Directory.CreateDirectory(OutputDirectory);
            }

            this.writter = new StreamWriter(
                this.LogfileName,
                true,
                new UTF8Encoding(false));

            this.isRunning = true;
            Thread.Sleep(TimeSpan.FromSeconds(0.1));

            this.dumpLogTask = Task.Run(() =>
            {
                while (this.isRunning)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(5));

                    if (this.writter != null)
                    {
                        XIVLog xivlog;
                        while (this.LogQueue.TryDequeue(out xivlog))
                        {
                            Thread.Yield();
                            this.writter.WriteLine(xivlog.ToCSVLine());
                        }

                        if (this.currentZoneName != ActGlobals.oFormActMain.CurrentZone)
                        {
                            this.currentZoneName = ActGlobals.oFormActMain.CurrentZone;
                            this.writter.Flush();
                        }
                    }
                }
            });
        }

        private void OnLogLineRead(
            bool isImport,
            LogLineEventArgs logInfo)
        {
            this.LogQueue.Enqueue(new XIVLog(isImport, logInfo));
        }
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
            this.Timestamp = DateTime.Parse(
                DateTime.Now.ToString("yyyy-MM-dd") + " " + timeString);

            this.LogType = line.Substring(15, 2);

            this.Log = line.Substring(15);
        }

        public DateTime Timestamp { get; private set; }

        public bool IsImport { get; private set; }

        public string LogType { get; private set; }

        public string Log { get; private set; }

        public LogLineEventArgs LogInfo { get; set; }

        public string ToCSVLine()
        {
            return
                this.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff") + "," +
                this.IsImport + "," +
                "0x" + this.LogType + "," +
                @"""" + this.Log + @"""";
        }
    }
}
