using System;
using System.Threading;
using NLog;

namespace FFXIV.Framework.Common
{
    public class ThreadWorker
    {
        #region Logger

        private static Logger Logger => AppLog.DefaultLogger;

        #endregion Logger

        private volatile bool isAbort;
        private Thread thread;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="doWorkAction">
        /// 定期的に実行するアクション</param>
        /// <param name="interval">
        /// インターバル。ミリ秒</param>
        public ThreadWorker(
            Action doWorkAction,
            double interval,
            string name = "",
            ThreadPriority priority = ThreadPriority.Normal)
        {
            this.DoWorkAction = doWorkAction;
            this.Interval = interval;
            this.Name = name;
            this.Priority = priority;
        }

        public Action DoWorkAction { get; set; }

        public double Interval { get; set; }

        public string Name { get; set; }

        public ThreadPriority Priority { get; private set; }

        public bool IsRunning { get; private set; }

        public static ThreadWorker Run(
            Action doWorkAction,
            double interval,
            string name = "",
            ThreadPriority priority = ThreadPriority.Normal)
        {
            var worker = new ThreadWorker(doWorkAction, interval, name);
            worker.Run();
            return worker;
        }

        public bool Abort(
            int timeout = 0)
        {
            var result = false;

            this.isAbort = true;

            if (timeout == 0)
            {
                timeout = (int)this.Interval;

                // 最大で500msまでしか待たない
                if (timeout > 500)
                {
                    timeout = 500;
                }
            }

            if (this.thread != null)
            {
                this.thread.Join(timeout);
                if (this.thread.IsAlive)
                {
                    this.thread.Abort();
                    result = true;
                }

                this.thread = null;
            }

            this.IsRunning = false;

            Logger.Trace($"ThreadWorker - {this.Name} end.{(result ? " aborted" : string.Empty)}");

            return result;
        }

        public void Run()
        {
            this.isAbort = false;

            this.thread = new Thread(this.DoWorkLoop);
            this.thread.IsBackground = true;
            this.thread.Priority = this.Priority;
            this.thread.Start();

            this.IsRunning = true;
        }

        private void DoWorkLoop()
        {
            Thread.Sleep((int)this.Interval);
            Logger.Trace($"ThreadWorker - {this.Name} start.");

            while (!this.isAbort)
            {
                try
                {
                    this.DoWorkAction?.Invoke();
                }
                catch (ThreadAbortException)
                {
                    this.isAbort = true;
                    Logger.Trace($"ThreadWorker - {this.Name} abort.");
                    break;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"ThreadWorker - {this.Name} error.");
                }

                if (this.isAbort)
                {
                    break;
                }

                Thread.Sleep((int)this.Interval);
            }
        }
    }
}
