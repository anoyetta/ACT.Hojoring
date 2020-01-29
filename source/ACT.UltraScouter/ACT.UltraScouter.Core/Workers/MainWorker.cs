using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Threading;
using ACT.UltraScouter.Config;
using ACT.UltraScouter.ViewModels.Bases;
using FFXIV.Framework.Common;
using NLog;

namespace ACT.UltraScouter.Workers
{
    public enum ViewCategories
    {
        Nothing = 0,
        Target,
        FocusTarget,
        TargetOfTarget,
        Boss,
        Me,
        MobList,
        Enmity,
        TacticalRadar,
        EnemyList
    }

    public class MainWorker
    {
        #region Singleton

        private static readonly object locker = new object();

        private static MainWorker instance;

        public static MainWorker Instance
        {
            get
            {
                lock (locker)
                {
                    if (instance == null)
                    {
                        instance = new MainWorker();
                    }
                }

                return instance;
            }
        }

        public static void Free()
        {
            lock (locker)
            {
                instance = null;
            }
        }

        #endregion Singleton

        #region Logger

        private Logger logger = AppLog.DefaultLogger;

        #endregion Logger

        /// <summary>
        /// ViewのRefresh用Lockオブジェクト
        /// </summary>
        private static readonly object viewRefreshLocker = new object();

        public object ViewRefreshLocker => viewRefreshLocker;

        private DispatcherTimer refreshViewWorker;
        private DispatcherTimer refreshSubViewWorker;
        private ThreadWorker scanMemoryWorker;

        public delegate void GetDataDelegate();

        public delegate void UpdateOverlayDataDelegate();

        public GetDataDelegate GetDataMethod;
        public UpdateOverlayDataDelegate UpdateOverlayDataMethod;
        public UpdateOverlayDataDelegate UpdateSubOverlayDataMethod;

        public void Start()
        {
            // 子ワーカを初期化する
            TargetInfoWorker.Initialize();
            FTInfoWorker.Initialize();
            ToTInfoWorker.Initialize();
            BossInfoWorker.Initialize();
            MeInfoWorker.Initialize();
            EnmityInfoWorker.Initialize();
            MobListWorker.Initialize();
            TacticalRadarWorker.Initialize();
            EnemyListWorker.Initialize();

            TargetInfoWorker.Instance.Start();
            FTInfoWorker.Instance.Start();
            ToTInfoWorker.Instance.Start();
            BossInfoWorker.Instance.Start();
            MeInfoWorker.Instance.Start();
            EnmityInfoWorker.Instance.Start();
            MobListWorker.Instance.Start();
            TacticalRadarWorker.Instance.Start();
            EnemyListWorker.Instance.Start();

            // メモリスキャンタスクを開始する
            this.RestartScanMemoryWorker();

            // Viewの更新タスクを開始する
            this.RestartRefreshViewWorker();

            // すべてのビューを更新する
            this.RefreshAllViews();
        }

        public void End()
        {
            // ワーカを止める
            this.scanMemoryWorker?.Abort();
            this.refreshViewWorker?.Stop();
            this.refreshSubViewWorker?.Stop();

            // 子ワーカを開放する
            TargetInfoWorker.Instance?.End();
            FTInfoWorker.Instance?.End();
            ToTInfoWorker.Instance?.End();
            BossInfoWorker.Instance?.End();
            MeInfoWorker.Instance?.End();
            EnmityInfoWorker.Instance?.End();
            MobListWorker.Instance?.End();
            TacticalRadarWorker.Instance?.End();
            EnemyListWorker.Instance?.End();

            TargetInfoWorker.Free();
            FTInfoWorker.Free();
            ToTInfoWorker.Free();
            BossInfoWorker.Free();
            MeInfoWorker.Free();
            EnmityInfoWorker.Free();
            MobListWorker.Free();
            TacticalRadarWorker.Free();
            EnemyListWorker.Free();

            // 参照を開放する
            this.scanMemoryWorker = null;
            this.refreshViewWorker = null;
            this.refreshSubViewWorker = null;
        }

        /// <summary>
        /// メモリ監視スレッドを開始する
        /// </summary>
        public void RestartScanMemoryWorker()
        {
            lock (this)
            {
                if (this.scanMemoryWorker != null)
                {
                    this.scanMemoryWorker.Abort();
                    this.scanMemoryWorker = null;
                }

                this.scanMemoryWorker = new ThreadWorker(() =>
                {
                    try
                    {
                        this.GetDataMethod?.Invoke();
                    }
                    catch (ThreadAbortException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        this.logger.Fatal(ex, "GetOverlayData error.");
                    }
                },
                Settings.Instance.PollingRate,
                this.GetType().Name,
                Settings.Instance.ScanMemoryThreadPriority);

                // 開始する
                this.scanMemoryWorker.Run();
            }
        }

        private int isRefreshingViewsLock;
        private int isRefreshingSubViewsLock;

        /// <summary>
        /// Viewの更新タイマをリスタートする
        /// </summary>
        public void RestartRefreshViewWorker()
        {
            lock (this)
            {
                this.isRefreshingViewsLock = 0;
                this.isRefreshingSubViewsLock = 0;

                restartDispatcher(
                    ref this.refreshViewWorker,
                    Settings.Instance.UIThreadPriority,
                    TimeSpan.FromMilliseconds(Settings.Instance.OverlayRefreshRate),
                    (x, y) =>
                    {
                        if (Interlocked.CompareExchange(ref this.isRefreshingViewsLock, 1, 0) < 1)
                        {
                            try
                            {
                                this.UpdateOverlayDataMethod?.Invoke();
                            }
                            finally
                            {
                                Interlocked.Exchange(ref this.isRefreshingViewsLock, 0);
                            }
                        }
                    });

                restartDispatcher(
                    ref this.refreshSubViewWorker,
                    DispatcherPriority.ContextIdle,
                    TimeSpan.FromMilliseconds(Settings.Instance.OverlayRefreshRate * 1.2),
                    (x, y) =>
                    {
                        if (Interlocked.CompareExchange(ref this.isRefreshingSubViewsLock, 1, 0) < 1)
                        {
                            try
                            {
                                this.UpdateSubOverlayDataMethod?.Invoke();
                            }
                            finally
                            {
                                Interlocked.Exchange(ref this.isRefreshingSubViewsLock, 0);
                            }
                        }
                    });
            }

            void restartDispatcher(
                ref DispatcherTimer timer,
                DispatcherPriority priority,
                TimeSpan interval,
                EventHandler handler)
            {
                if (timer != null)
                {
                    timer.Stop();
                    timer = null;
                }

                timer = new DispatcherTimer(priority)
                {
                    Interval = interval
                };

                timer.Tick += handler;

                timer.Start();
            }
        }

        /// <summary>
        /// すべてのViewに更新(Close → Open)を要求する
        /// </summary>
        public void RefreshAllViews()
        {
            lock (viewRefreshLocker)
            {
                TargetInfoWorker.Instance.RefreshViewsQueue = true;
                FTInfoWorker.Instance.RefreshViewsQueue = true;
                ToTInfoWorker.Instance.RefreshViewsQueue = true;
                BossInfoWorker.Instance.RefreshViewsQueue = true;
                MeInfoWorker.Instance.RefreshViewsQueue = true;
                EnmityInfoWorker.Instance.RefreshViewsQueue = true;
                MobListWorker.Instance.RefreshViewsQueue = true;
                TacticalRadarWorker.Instance.RefreshViewsQueue = true;
                EnemyListWorker.Instance.RefreshViewsQueue = true;
            }
        }

        /// <summary>
        /// ViewModelのリスト
        /// </summary>
        public IReadOnlyList<OverlayViewModelBase> GetViewModelList(
            ViewCategories category = ViewCategories.Nothing)
        {
            var list = new List<OverlayViewModelBase>();

            if (TargetInfoWorker.Instance == null ||
                FTInfoWorker.Instance == null ||
                ToTInfoWorker.Instance == null ||
                BossInfoWorker.Instance == null ||
                MeInfoWorker.Instance == null ||
                EnmityInfoWorker.Instance == null ||
                MobListWorker.Instance == null ||
                TacticalRadarWorker.Instance == null ||
                EnemyListWorker.Instance == null)
            {
                return list;
            }

            switch (category)
            {
                case ViewCategories.Nothing:
                    list.AddRange(TargetInfoWorker.Instance.ViewList.Select(x => x.ViewModel));
                    list.AddRange(FTInfoWorker.Instance.ViewList.Select(x => x.ViewModel));
                    list.AddRange(ToTInfoWorker.Instance.ViewList.Select(x => x.ViewModel));
                    list.AddRange(BossInfoWorker.Instance.ViewList.Select(x => x.ViewModel));
                    list.AddRange(MeInfoWorker.Instance.ViewList.Select(x => x.ViewModel));
                    list.AddRange(EnmityInfoWorker.Instance.ViewList.Select(x => x.ViewModel));
                    list.AddRange(MobListWorker.Instance.ViewList.Select(x => x.ViewModel));
                    list.AddRange(TacticalRadarWorker.Instance.ViewList.Select(x => x.ViewModel));
                    list.AddRange(EnemyListWorker.Instance.ViewList.Select(x => x.ViewModel));
                    break;

                case ViewCategories.Target:
                    list.AddRange(TargetInfoWorker.Instance.ViewList.Select(x => x.ViewModel));
                    break;

                case ViewCategories.FocusTarget:
                    list.AddRange(FTInfoWorker.Instance.ViewList.Select(x => x.ViewModel));
                    break;

                case ViewCategories.TargetOfTarget:
                    list.AddRange(ToTInfoWorker.Instance.ViewList.Select(x => x.ViewModel));
                    break;

                case ViewCategories.Boss:
                    list.AddRange(BossInfoWorker.Instance.ViewList.Select(x => x.ViewModel));
                    break;

                case ViewCategories.Me:
                    list.AddRange(MeInfoWorker.Instance.ViewList.Select(x => x.ViewModel));
                    break;

                case ViewCategories.Enmity:
                    list.AddRange(EnmityInfoWorker.Instance.ViewList.Select(x => x.ViewModel));
                    break;

                case ViewCategories.MobList:
                    list.AddRange(MobListWorker.Instance.ViewList.Select(x => x.ViewModel));
                    break;

                case ViewCategories.TacticalRadar:
                    list.AddRange(TacticalRadarWorker.Instance.ViewList.Select(x => x.ViewModel));
                    break;

                case ViewCategories.EnemyList:
                    list.AddRange(EnemyListWorker.Instance.ViewList.Select(x => x.ViewModel));
                    break;
            }

            return list;
        }

        /// <summary>
        /// すべてのViewModelを更新する（全Properties変更通知を出す）
        /// </summary>
        /// <param name="viewModelType">
        /// 対象のViewModelのタイプ</param>
        public void RefreshAllViewModels(
            ViewCategories category = ViewCategories.Nothing)
        {
            foreach (var vm in this.GetViewModelList(category))
            {
                vm.RaiseAllPropertiesChanged();
            }
        }
    }
}
