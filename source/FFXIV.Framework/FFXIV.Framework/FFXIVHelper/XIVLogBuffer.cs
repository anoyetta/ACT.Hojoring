using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Advanced_Combat_Tracker;
using FFXIV.Framework.Common;

namespace FFXIV.Framework.FFXIVHelper
{
    public class XIVLogBuffer :
        IDisposable
    {
        #region Singleton

        private static XIVLogBuffer instance;

        public static XIVLogBuffer Instance => instance ?? (instance = new XIVLogBuffer());

        private XIVLogBuffer()
        {
        }

        public static void Free()
        {
            instance?.Dispose();
            instance = null;
        }

        #endregion Singleton

        #region IDisposable

        public void Dispose()
        {
            if (this.worker != null)
            {
                this.worker.Abort();
                this.worker = null;
            }

            if (ActGlobals.oFormActMain != null)
            {
                ActGlobals.oFormActMain.OnLogLineRead -= this.OnLogLineRead;
            }

            this.isHandled = false;
            this.xivLogBufferLibrary?.Clear();
        }

        #endregion IDisposable

        private readonly ConcurrentQueue<(LogLineEventArgs e, bool isImport)> logInfoStorage = new ConcurrentQueue<(LogLineEventArgs e, bool isImport)>(new Queue<(LogLineEventArgs e, bool isImport)>(5120));
        private readonly Dictionary<object, XIVLogBufferContainer> xivLogBufferLibrary = new Dictionary<object, XIVLogBufferContainer>();

        private volatile bool isHandled = false;
        private ThreadWorker worker;

        public void StartPolling()
        {
            if (!this.isHandled)
            {
                this.isHandled = true;
                ActGlobals.oFormActMain.OnLogLineRead -= this.OnLogLineRead;
                ActGlobals.oFormActMain.OnLogLineRead += this.OnLogLineRead;
            }

            if (this.worker == null)
            {
                this.worker = new ThreadWorker(
                    () => this.StoreXIVLog(false),
                    1d,
                    nameof(XIVLogBuffer),
                    ThreadPriority.Lowest);
            }

            if (!this.worker.IsRunning)
            {
                this.worker.Run();
            }
        }

        public void EnqueueLogLine(
            LogLineEventArgs args)
            => this.logInfoStorage.Enqueue((args, false));

        private void OnLogLineRead(
            bool isImport,
            LogLineEventArgs logInfo)
        {
            if (logInfo.logLine.Length <= 18)
            {
                return;
            }

            this.logInfoStorage.Enqueue((logInfo, isImport));
            this.CountLPS();
        }

        public Func<IEnumerable<XIVLog>> Subscribe(
            object listener,
            Predicate<string> filter = null,
            Func<string, string> converter = null)
        {
            if (!this.xivLogBufferLibrary.ContainsKey(listener))
            {
                this.xivLogBufferLibrary[listener] = new XIVLogBufferContainer()
                {
                    Filter = filter,
                    Converter = converter,
                };
            }

            return () => this.GetLogs(listener);
        }

        public void Unsubscribe(
            object listener)
        {
            if (this.xivLogBufferLibrary.ContainsKey(listener))
            {
                while (this.xivLogBufferLibrary[listener].Buffer.TryDequeue(out XIVLog x))
                {
                    ;
                }

                this.xivLogBufferLibrary.Remove(listener);
            }
        }

        private IEnumerable<XIVLog> GetLogs(
            object listener)
        {
            try
            {
                this.interrupt = true;
                this.StoreXIVLog(true);

                var container = this.xivLogBufferLibrary.ContainsKey(listener) ?
                    this.xivLogBufferLibrary[listener] :
                    null;

                if (container != null &&
                    container.Buffer != null)
                {
                    while (container.Buffer.TryDequeue(out XIVLog xivLog))
                    {
                        yield return xivLog;
                    }
                }
            }
            finally
            {
                this.interrupt = false;
            }
        }

        private readonly List<Predicate<string>> GlobalFilterList = new List<Predicate<string>>();

        public void SetGlobalFilters(
            IEnumerable<Predicate<string>> filters)
        {
            lock (this.GlobalFilterList)
            {
                this.GlobalFilterList.Clear();

                this.GlobalFilterList.Add(this.IgnoreDuplicate);
                this.GlobalFilterList.AddRange(filters);
            }
        }

        private static long currentID = 0;
        private volatile bool interrupt = false;

        private void StoreXIVLog(
            bool isForce = false)
        {
            if (!isForce && this.interrupt)
            {
                Thread.Yield();
                return;
            }

            if (this.logInfoStorage.IsEmpty)
            {
                return;
            }

            lock (this)
            {
                while (this.logInfoStorage.TryDequeue(out (LogLineEventArgs e, bool isImport) entry))
                {
                    var line = entry.e.logLine;

                    var isSkip = false;
                    foreach (var filter in this.GlobalFilterList)
                    {
                        if (!filter.Invoke(line))
                        {
                            isSkip = true;
                            break;
                        }
                    }

                    if (isSkip)
                    {
                        continue;
                    }

                    var xivLog = new XIVLog(
                        entry.e.detectedTime,
                        line,
                        entry.e.detectedZone,
                        entry.isImport)
                    {
                        ID = currentID++
                    };

                    foreach (var container in this.xivLogBufferLibrary.Values)
                    {
                        if (container.Filter != null &&
                            !container.Filter.Invoke(line))
                        {
                            continue;
                        }

                        if (container.Converter != null)
                        {
                            xivLog.Log = container.Converter(xivLog.Log);
                        }

                        container.Buffer?.Enqueue(xivLog);
                    }

                    if (!isForce && this.interrupt)
                    {
                        return;
                    }
                }
            }
        }

        private string[] preLog = new string[4];
        private int preLogIndex = 0;

        private bool IgnoreDuplicate(
            string line)
        {
            if (this.preLog.Any(x => x == line))
            {
                return false;
            }

            this.preLog[preLogIndex++] = line;
            if (this.preLogIndex >= this.preLog.GetUpperBound(0))
            {
                this.preLogIndex = 0;
            }

            return true;
        }

        #region LPS

        private double[] lpss = new double[60];
        private int currentLpsIndex;
        private long currentLineCount;
        private Stopwatch lineCountTimer = new Stopwatch();

        public double LPS
        {
            get
            {
                var availableLPSs = this.lpss.Where(x => x > 0);
                if (!availableLPSs.Any())
                {
                    return 0;
                }

                return availableLPSs.Sum() / availableLPSs.Count();
            }
        }

        private void CountLPS()
        {
            this.currentLineCount++;

            if (!this.lineCountTimer.IsRunning)
            {
                this.lineCountTimer.Restart();
            }

            if (this.lineCountTimer.Elapsed >= TimeSpan.FromSeconds(1))
            {
                this.lineCountTimer.Stop();

                var secounds = this.lineCountTimer.Elapsed.TotalSeconds;
                if (secounds > 0)
                {
                    var lps = this.currentLineCount / secounds;
                    if (lps > 0)
                    {
                        if (this.currentLpsIndex > this.lpss.GetUpperBound(0))
                        {
                            this.currentLpsIndex = 0;
                        }

                        this.lpss[this.currentLpsIndex] = lps;
                        this.currentLpsIndex++;
                    }
                }

                this.currentLineCount = 0;
            }
        }

        #endregion LPS

        private class XIVLogBufferContainer
        {
            public Predicate<string> Filter { get; set; }

            public Func<string, string> Converter { get; set; }

            public ConcurrentQueue<XIVLog> Buffer { get; } = new ConcurrentQueue<XIVLog>(new Queue<XIVLog>(5120));
        }
    }
}
