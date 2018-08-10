using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ACT.SpecialSpellTimer.Config;

namespace ACT.SpecialSpellTimer
{
    public class ChatLogWorker
    {
        #region Singleton

        private static ChatLogWorker instance = new ChatLogWorker();

        public static ChatLogWorker Instance => instance;

        #endregion Singleton

        private readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(3);
        private readonly Encoding UTF8Encoding = new UTF8Encoding(false);
        private readonly StringBuilder LogBuffer = new StringBuilder(128 * 5120);

        private System.Timers.Timer worker;

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

        private StreamWriter outputStream;

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
                    Thread.Sleep(5);

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

            if (this.worker != null)
            {
                this.worker.Stop();
                this.worker.Dispose();
                this.worker = null;
            }

            this.worker = new System.Timers.Timer(FlushInterval.TotalMilliseconds)
            {
                AutoReset = true,
            };

            this.worker.Elapsed += (x, y) => this.Write();
            this.worker.Start();
        }

        public void End()
        {
            if (this.worker != null)
            {
                this.worker.Stop();
                this.worker.Dispose();
                this.worker = null;
            }

            this.Write();
            this.Close();
        }

        public void Open()
        {
            lock (this.LogBuffer)
            {
                if (this.outputStream == null)
                {
                    if (!string.IsNullOrEmpty(this.OutputDirectory))
                    {
                        if (!Directory.Exists(this.OutputDirectory))
                        {
                            Directory.CreateDirectory(this.OutputDirectory);
                        }
                    }

                    this.outputStream = new StreamWriter(this.OutputFile, true, this.UTF8Encoding);
                }
            }
        }

        public void Close()
        {
            lock (this.LogBuffer)
            {
                if (this.outputStream != null)
                {
                    this.outputStream.Flush();
                    this.outputStream.Close();
                    this.outputStream.Dispose();
                    this.outputStream = null;
                }
            }

            GC.Collect();
        }

        public void Write()
        {
            const double LongInterval = 10 * 1000;

            try
            {
                if (this.LogBuffer.Length <= 0 ||
                    !this.OutputEnabled)
                {
                    if (this.worker.Interval != LongInterval)
                    {
                        this.worker.Interval = LongInterval;
                    }

                    return;
                }

                this.Open();

                lock (this.LogBuffer)
                {
                    this.outputStream?.Write(this.LogBuffer.ToString());
                    this.LogBuffer.Clear();

                    if (this.worker.Interval != FlushInterval.TotalMilliseconds)
                    {
                        this.worker.Interval = FlushInterval.TotalMilliseconds;
                    }
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
