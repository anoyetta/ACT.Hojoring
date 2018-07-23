using System;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace FFXIV.Framework.Common
{
    public class TaskWorker
    {
        #region Logger

        private static Logger Logger => AppLog.DefaultLogger;

        #endregion Logger

        private CancellationTokenSource tokenSource;
        private Task task;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="doWorkAction">
        /// 定期的に実行するアクション</param>
        /// <param name="interval">
        /// インターバル。ミリ秒</param>
        public TaskWorker(
            Action doWorkAction,
            double interval,
            string name = "")
        {
            this.DoWorkAction = doWorkAction;
            this.Interval = interval;
            this.Name = name;
        }

        public Action DoWorkAction { get; set; }

        public double Interval { get; set; }

        public string Name { get; set; }

        public static TaskWorker Run(
            Action doWorkAction,
            double interval,
            string name = "")
        {
            var worker = new TaskWorker(doWorkAction, interval, name);
            worker.Run();
            return worker;
        }

        public void Run()
        {
            this.tokenSource = new CancellationTokenSource();

            this.task = Task.Factory.StartNew(
                () => this.DoWorkLoop(this.tokenSource.Token),
                this.tokenSource.Token);
        }

        public void Cancel()
        {
            this.tokenSource.Cancel();
            this.task.Wait(100);
            this.task.Dispose();
            this.task = null;
            this.tokenSource.Dispose();
            this.tokenSource = null;
        }

        private void DoWorkLoop(
            CancellationToken token)
        {
            Thread.CurrentThread.IsBackground = true;

            Thread.Sleep((int)this.Interval);
            Logger.Trace($"TaskWorker - {this.Name} start.");

            while (true)
            {
                if (token.IsCancellationRequested)
                {
                    Logger.Trace($"TaskWorker - {this.Name} cancel.");
                    return;
                }

                try
                {
                    this.DoWorkAction?.Invoke();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"TaskWorker - {this.Name} error.");
                }

                Thread.Sleep((int)this.Interval);
            }
        }
    }
}
