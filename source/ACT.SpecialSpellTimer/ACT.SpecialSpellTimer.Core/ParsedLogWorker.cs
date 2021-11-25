using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ACT.SpecialSpellTimer.Config;
using FFXIV.Framework.XIVHelper;

namespace ACT.SpecialSpellTimer
{
    public class ParsedLogWorker
    {
        #region Singleton

        private static ParsedLogWorker instance = new ParsedLogWorker();

        public static ParsedLogWorker Instance => instance;

        #endregion Singleton

        private readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);
        private readonly TimeSpan IdlePollingInterval = TimeSpan.FromSeconds(15);
        private readonly TimeSpan FlushInterval = TimeSpan.FromMinutes(15);

        private readonly Encoding UTF8Encoding = new UTF8Encoding(false);

        private System.Timers.Timer worker;

        private string OutputDirectory => Settings.Default.SaveLogDirectory;

        private bool OutputEnabled =>
            Settings.Default.SaveLogEnabled &&
            !string.IsNullOrEmpty(this.OutputFile);

        public string OutputFile =>
            !string.IsNullOrEmpty(OutputDirectory) ?
            Path.Combine(
                this.OutputDirectory,
                $@"ParsedLog.{DateTime.Now.ToString("yyyy-MM-dd")}.log") :
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

                lock (this)
                {
                    foreach (var log in logList)
                    {
                        this.outputStream?.WriteLine(log.OriginalLogLine);
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        public void Begin()
        {
            lock (this)
            {
                if (this.worker != null)
                {
                    this.worker.Stop();
                    this.worker.Dispose();
                    this.worker = null;
                }

                this.worker = new System.Timers.Timer(PollingInterval.TotalMilliseconds)
                {
                    AutoReset = true,
                };

                LogParser.WriteLineDebugLogDelegate = (timestamp, line) =>
                {
                    lock (this)
                    {
                        this.outputStream?.WriteLine($"[{timestamp:HH:mm:ss.fff}] {line} [DEBUG]");
                    }
                };

                this.worker.Elapsed += (_, _) => this.Flush();
                this.worker.Start();
            }
        }

        public void End()
        {
            lock (this)
            {
                if (this.worker != null)
                {
                    this.worker.Stop();
                    this.worker.Dispose();
                    this.worker = null;
                }

                this.Flush();
                this.Close();
            }
        }

        public void Open()
        {
            lock (this)
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

                    this.outputStream = new StreamWriter(
                        new FileStream(
                            this.OutputFile,
                            FileMode.Append,
                            FileAccess.Write,
                            FileShare.Read,
                            64 * 1024),
                        UTF8Encoding);
                }
            }
        }

        public void Close()
        {
            lock (this)
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

        private DateTime lastFlushTimestamp = DateTime.MinValue;

        public void Flush(
            bool isForceFlush = false)
        {
            if (!this.OutputEnabled)
            {
                if (this.worker != null)
                {
                    if (this.worker.Interval != IdlePollingInterval.TotalMilliseconds)
                    {
                        this.worker.Interval = IdlePollingInterval.TotalMilliseconds;
                    }
                }

                return;
            }

            try
            {
                this.Open();

                if (!isForceFlush)
                {
                    if ((DateTime.Now - this.lastFlushTimestamp) < FlushInterval)
                    {
                        return;
                    }
                }

                this.lastFlushTimestamp = DateTime.Now;

                lock (this)
                {
                    this.outputStream?.Flush();
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                if (this.worker != null)
                {
                    if (this.worker.Interval != PollingInterval.TotalMilliseconds)
                    {
                        this.worker.Interval = PollingInterval.TotalMilliseconds;
                    }
                }
            }
        }
    }
}
