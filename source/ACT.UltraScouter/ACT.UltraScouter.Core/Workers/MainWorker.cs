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
        MobList
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
        private ThreadWorker scanMemoryWorker;

        public delegate void GetDataDelegate();

        public delegate void UpdateOverlayDataDelegate();

        public GetDataDelegate GetDataMethod;
        public UpdateOverlayDataDelegate UpdateOverlayDataMethod;

        public void Start()
        {
            // 子ワーカを初期化する
            TargetInfoWorker.Initialize();
            FTInfoWorker.Initialize();
            ToTInfoWorker.Initialize();
            BossInfoWorker.Initialize();
            MeInfoWorker.Initialize();
            MobListWorker.Initialize();

            TargetInfoWorker.Instance.Start();
            FTInfoWorker.Instance.Start();
            ToTInfoWorker.Instance.Start();
            BossInfoWorker.Instance.Start();
            MeInfoWorker.Instance.Start();
            MobListWorker.Instance.Start();

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

            // 子ワーカを開放する
            TargetInfoWorker.Instance?.End();
            FTInfoWorker.Instance?.End();
            ToTInfoWorker.Instance?.End();
            BossInfoWorker.Instance?.End();
            MeInfoWorker.Instance?.End();
            MobListWorker.Instance?.End();

            TargetInfoWorker.Free();
            FTInfoWorker.Free();
            ToTInfoWorker.Free();
            BossInfoWorker.Free();
            MeInfoWorker.Free();
            MobListWorker.Free();

            // 参照を開放する
            this.scanMemoryWorker = null;
            this.refreshViewWorker = null;
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

                var isScaning = false;
                this.scanMemoryWorker = new ThreadWorker(() =>
                {
                    if (isScaning)
                    {
                        return;
                    }

                    isScaning = true;

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
                    finally
                    {
                        isScaning = false;
                    }
                },
                Settings.Instance.PollingRate,
                this.GetType().Name,
                Settings.Instance.ScanMemoryThreadPriority);

                // 開始する
                this.scanMemoryWorker.Run();
            }
        }

        private volatile bool isRefreshingViews = false;

        /// <summary>
        /// Viewの更新タイマをリスタートする
        /// </summary>
        public void RestartRefreshViewWorker()
        {
            lock (this)
            {
                if (this.refreshViewWorker != null)
                {
                    this.refreshViewWorker.Stop();
                    this.refreshViewWorker = null;
                }

                this.refreshViewWorker = new DispatcherTimer(Settings.Instance.UIThreadPriority)
                {
                    Interval = TimeSpan.FromMilliseconds(Settings.Instance.OverlayRefreshRate)
                };

                this.refreshViewWorker.Tick += (x, y) =>
                {
                    if (this.isRefreshingViews)
                    {
                        return;
                    }

                    try
                    {
                        this.isRefreshingViews = true;
                        this.UpdateOverlayDataMethod?.Invoke();
                    }
                    finally
                    {
                        this.isRefreshingViews = false;
                    }
                };

                // 開始する
                this.refreshViewWorker.Start();
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
                MobListWorker.Instance.RefreshViewsQueue = true;
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
                MobListWorker.Instance == null)
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
                    list.AddRange(MobListWorker.Instance.ViewList.Select(x => x.ViewModel));
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

                case ViewCategories.MobList:
                    list.AddRange(MobListWorker.Instance.ViewList.Select(x => x.ViewModel));
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
