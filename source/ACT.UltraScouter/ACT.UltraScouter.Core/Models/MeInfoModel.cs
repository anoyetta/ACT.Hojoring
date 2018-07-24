using System;
using System.Linq;
using System.Windows.Threading;
using ACT.UltraScouter.Config;
using FFXIV.Framework.FFXIVHelper;

namespace ACT.UltraScouter.Models
{
    public class MeInfoModel :
        TargetInfoModel
    {
        #region Singleton

        private static MeInfoModel instance = new MeInfoModel();
        public new static MeInfoModel Instance => instance;

        #endregion Singleton

        public event EventHandler MPRecovered;

        public void OnMPRecovered() => this.MPRecovered?.Invoke(this, new EventArgs());

        protected int[] mpRecoveryValues = new int[0];
        protected bool inCombat = !(Settings.Instance.MPTicker.ExplationTimeForDisplay > 0);
        protected bool inTargetJobToMPTicker;
#if DEBUG
        protected bool mpTickerAvailable = true;
#else
        protected bool mpTickerAvailable;
#endif
        protected double previousMP;
        protected double currentMP;
        protected double maxMP;
        protected JobIDs jobID;

        protected DispatcherTimer inCombatTimer = new DispatcherTimer(DispatcherPriority.Background);

        public MeInfoModel()
        {
            this.inCombatTimer.Tick -= this.InCombatTimerOnTick;
            this.inCombatTimer.Tick += this.InCombatTimerOnTick;
        }

        public bool InCombat
        {
            get => this.inCombat;
            set
            {
                if (this.SetProperty(ref this.inCombat, value))
                {
                    this.MPTickerAvailable =
                        (this.InCombat && this.InTargetJobToMPTicker) ||
                        Settings.Instance.MPTicker.TestMode;
                }
            }
        }

        public bool InTargetJobToMPTicker
        {
            get => this.inTargetJobToMPTicker;
            set
            {
                if (this.SetProperty(ref this.inTargetJobToMPTicker, value))
                {
                    this.MPTickerAvailable =
                        (this.InCombat && this.InTargetJobToMPTicker) ||
                        Settings.Instance.MPTicker.TestMode;
                }
            }
        }

        public bool MPTickerAvailable
        {
            get => this.mpTickerAvailable;
            set => this.SetProperty(ref this.mpTickerAvailable, value);
        }

        public double CurrentMP
        {
            get => this.currentMP;
            set
            {
                this.PreviousMP = this.currentMP;

                // MPが変化した？
                if (this.SetProperty(ref this.currentMP, value))
                {
                    this.BeginMPTicker();
                }
            }
        }

        public void BeginMPTicker(
            bool force = false)
        {
            // 終了タイマをとめる
            if (this.inCombatTimer.IsEnabled)
            {
                this.inCombatTimer.Stop();
            }

            // 対象ジョブでなければ何もしない
            if (!Settings.Instance.MPTicker.TestMode)
            {
                if (!this.InTargetJobToMPTicker)
                {
                    return;
                }
            }

            if (force)
            {
                // 強制モード
                this.OnMPRecovered();
            }
            else
            {
                // 回復量がいずれかの規定値か？
                var recoverdValue = this.currentMP - this.previousMP;
                if (this.mpRecoveryValues.Any(x => x == recoverdValue))
                {
                    this.OnMPRecovered();
                }
            }

            // 戦闘中と判断する
            this.InCombat = true;

            // 終了タイマを開始する
            if (!Settings.Instance.MPTicker.TestMode)
            {
                if (Settings.Instance.MPTicker.ExplationTimeForDisplay > 0)
                {
                    this.inCombatTimer.Interval =
                        TimeSpan.FromSeconds(Settings.Instance.MPTicker.ExplationTimeForDisplay);
                    this.inCombatTimer.Start();
                }
            }
        }

        private void InCombatTimerOnTick(
            object sender,
            EventArgs e)
        {
            this.inCombatTimer.Stop();
            this.InCombat = false;
        }

        public double PreviousMP
        {
            get => this.previousMP;
            set => this.SetProperty(ref this.previousMP, value);
        }

        public double MaxMP
        {
            get => this.maxMP;
            set
            {
                if (this.SetProperty(ref this.maxMP, value))
                {
                    this.RefreshMPRecoveryValues();
                }
            }
        }

        /// <summary>
        /// 最大MPから各状況のMP回復量を算出する
        /// </summary>
        public void RefreshMPRecoveryValues()
        {
            var normal = (int)Math.Floor(this.MaxMP * Constants.MPRecoveryRate.Normal);
            var combat = (int)Math.Floor(this.MaxMP * Constants.MPRecoveryRate.InCombat);
            var ui1 = (int)Math.Floor(this.MaxMP * Constants.MPRecoveryRate.UmbralIce1);
            var ui2 = (int)Math.Floor(this.MaxMP * Constants.MPRecoveryRate.UmbralIce2);
            var ui3 = (int)Math.Floor(this.MaxMP * Constants.MPRecoveryRate.UmbralIce3);

            this.mpRecoveryValues = new int[]
            {
                normal,
                normal + ui1,
                normal + ui2,
                normal + ui3,
                combat,
                combat + ui1,
                combat + ui2,
                combat + ui3,
            };
        }

        public JobIDs JobID
        {
            get => this.jobID;
            set
            {
                if (this.SetProperty(ref this.jobID, value))
                {
                    var targetJobIDs = Settings.Instance.MPTicker.TargetJobs;

                    if (targetJobIDs == null ||
                        targetJobIDs.Count < 1)
                    {
                        this.InTargetJobToMPTicker = true;
                    }
                    else
                    {
                        this.InTargetJobToMPTicker = targetJobIDs.Any(x =>
                            x.Job == this.jobID &&
                            x.Available);
                    }
                }
            }
        }

        #region Constants

        public static class Constants
        {
            /// MP回復周期
            /// エオルゼアタイムの1分とは無関係に実時間3秒周期である
            /// </summary>
            public const double MPRecoverySpan = 3;

            /// <summary>
            /// MP回復割合
            /// </summary>
            public static class MPRecoveryRate
            {
                /// <summary>
                /// 戦闘中
                /// </summary>
                public const double InCombat = 0.02d;

                /// <summary>
                /// 通常(非戦闘時)
                /// </summary>
                public const double Normal = 0.06d;

                /// <summary>
                /// アンブラルブリザード1による増量
                /// </summary>
                public const double UmbralIce1 = 0.30d;

                /// <summary>
                /// アンブラルブリザード2による増量
                /// </summary>
                public const double UmbralIce2 = 0.45d;

                /// <summary>
                /// アンブラルブリザード3による増量
                /// </summary>
                public const double UmbralIce3 = 0.60d;
            }
        }

        #endregion Constants
    }
}
