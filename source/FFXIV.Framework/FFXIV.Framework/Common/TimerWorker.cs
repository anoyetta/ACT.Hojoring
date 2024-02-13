using System;
using System.Timers;
using NLog;

namespace FFXIV.Framework.Common
{
    public class TimerWorker
    {
        #region Logger

        private static Logger AppLogger => AppLog.DefaultLogger;

        #endregion Logger

        private volatile bool isAbort;
        private Timer timer;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="doWorkAction">
        /// 定期的に実行するアクション</param>
        /// <param name="interval">
        /// インターバル。ミリ秒</param>
        /// <param name="name">
        /// ワーカの名前</param>
        public TimerWorker(
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

        public static TimerWorker Run(
            Action doWorkAction,
            double interval,
            string name = "")
        {
            var worker = new TimerWorker(doWorkAction, interval, name);
            worker.Run();
            return worker;
        }

        public void Abort()
        {
            this.isAbort = true;

            if (this.timer != null)
            {
                this.timer.Stop();
                this.timer.Dispose();
                this.timer = null;
            }

            AppLogger.Trace($"TimerWorker - {this.Name} end.");
        }

        public void Run()
        {
            this.isAbort = false;

            this.timer = new Timer();
            this.timer.Interval = this.Interval;
            this.timer.Elapsed += this.Elapsed;
            this.timer.Start();

            AppLogger.Trace($"TimerWorker - {this.Name} start.");
        }

        private void Elapsed(object sender, ElapsedEventArgs e)
        {
            this.timer.Interval = this.Interval;

            if (!this.isAbort)
            {
                try
                {
                    this.DoWorkAction?.Invoke();
                }
                catch (Exception ex)
                {
                    AppLogger.Error(ex, $"TimerWorker - {this.Name} error.");
                }
            }
        }
    }
}
