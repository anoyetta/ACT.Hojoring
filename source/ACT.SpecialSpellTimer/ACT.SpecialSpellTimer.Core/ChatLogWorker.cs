using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ACT.SpecialSpellTimer.Config;
using FFXIV.Framework.Common;

namespace ACT.SpecialSpellTimer
{
    public class ChatLogWorker
    {
        #region Singleton

        private static ChatLogWorker instance = new ChatLogWorker();

        public static ChatLogWorker Instance => instance;

        #endregion Singleton

        private readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(10);
        private readonly Encoding UTF8Encoding = new UTF8Encoding(false);
        private readonly StringBuilder LogBuffer = new StringBuilder(128 * 5120);

        private ThreadWorker worker;

        private string OutputDirectory => Settings.Default.SaveLogDirectory;

        private bool OutputEnabled =>
            Settings.Default.SaveLogEnabled &&
            !string.IsNullOrEmpty(this.OutputFile);

        public string OutputFile =>
            !string.IsNullOrEmpty(OutputDirectory) ?
            Path.Combine(
                this.OutputDirectory,
                $@"CombatLog.{DateTime.Now.ToString("yyyy-MM-dd")}.log") :
            string.Empty;

        public Task AppendLinesAsync(
            List<XIVLog> logList)
            => Task.Run(() => this.AppendLines(logList));

        public void AppendLines(
            List<XIVLog> logList)
        {
            try
            {
                if (!this.OutputEnabled)
                {
                    return;
                }

                lock (this.LogBuffer)
                {
                    foreach (var log in logList)
                    {
                        this.LogBuffer.AppendLine(log.LogLine);
                        Thread.Yield();
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        public void Begin()
        {
            lock (this.LogBuffer)
            {
                this.LogBuffer.Clear();
            }

            this.worker = new ThreadWorker(
                this.Flush,
                FlushInterval.TotalMilliseconds,
                "CombatLogFlushWorker",
                ThreadPriority.Lowest);

            this.worker.Run();
        }

        public void End()
        {
            if (this.worker != null)
            {
                this.worker.Abort();
                this.worker = null;
            }

            this.Flush();
        }

        public void Flush()
        {
            try
            {
                if (this.LogBuffer.Length <= 0)
                {
                    return;
                }

                lock (this.LogBuffer)
                {
                    if (!string.IsNullOrEmpty(this.OutputDirectory))
                    {
                        if (!Directory.Exists(this.OutputDirectory))
                        {
                            Directory.CreateDirectory(this.OutputDirectory);
                        }
                    }

                    File.AppendAllText(
                        this.OutputFile,
                        this.LogBuffer.ToString(),
                        this.UTF8Encoding);

                    this.LogBuffer.Clear();
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
