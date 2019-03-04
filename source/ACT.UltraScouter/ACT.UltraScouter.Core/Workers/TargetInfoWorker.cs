using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using ACT.UltraScouter.Config;
using ACT.UltraScouter.Models;
using ACT.UltraScouter.ViewModels;
using ACT.UltraScouter.ViewModels.Bases;
using ACT.UltraScouter.Views;
using FFXIV.Framework.Common;
using FFXIV.Framework.FFXIVHelper;
using FFXIV.Framework.WPF.Views;
using NLog;
using Sharlayan.Core.Enums;

namespace ACT.UltraScouter.Workers
{
    public class TargetInfoWorker
    {
        #region Singleton

        private static TargetInfoWorker instance;

        public static TargetInfoWorker Instance => instance;

        public static void Initialize() => instance = new TargetInfoWorker();

        public static void Free() => instance = null;

        protected TargetInfoWorker()
        {
        }

        #endregion Singleton

        #region Logger

        private Logger logger = AppLog.DefaultLogger;

        #endregion Logger

        /// <summary>
        /// ターゲット系のオーバーレイか？
        /// </summary>
        protected virtual bool IsTargetOverlay { get; } = true;

        /// <summary>
        /// Viewに接続されるデータモデル
        /// </summary>
        public virtual TargetInfoModel Model => TargetInfoModel.Instance;

        private MainWorker.GetDataDelegate getDataDelegate;
        private MainWorker.UpdateOverlayDataDelegate updateOverlayDataDelegate;

        private MainWorker.GetDataDelegate GetDataDelegate =>
            this.getDataDelegate ??
            (this.getDataDelegate = new MainWorker.GetDataDelegate(this.GetData));

        private MainWorker.UpdateOverlayDataDelegate UpdateOverlayDataDelegate =>
            this.updateOverlayDataDelegate ??
            (this.updateOverlayDataDelegate = new MainWorker.UpdateOverlayDataDelegate(this.UpdateOverlayData));

        /// <summary>
        /// 開始
        /// </summary>
        public void Start()
        {
            MainWorker.Instance.GetDataMethod -= this.GetDataDelegate;
            MainWorker.Instance.UpdateOverlayDataMethod -= this.UpdateOverlayDataDelegate;

            MainWorker.Instance.GetDataMethod += this.GetDataDelegate;
            MainWorker.Instance.UpdateOverlayDataMethod += this.UpdateOverlayDataDelegate;
        }

        /// <summary>
        /// 終了
        /// </summary>
        public virtual void End()
        {
            MainWorker.Instance.GetDataMethod -= this.GetDataDelegate;
            MainWorker.Instance.UpdateOverlayDataMethod -= this.UpdateOverlayDataDelegate;

            lock (MainWorker.Instance.ViewRefreshLocker)
            {
                WPFHelper.Invoke(() =>
                {
                    foreach (var item in this.ViewList)
                    {
                        item.ViewModel?.Dispose();
                        item.View?.Close();
                    }

                    this.ViewList.Clear();
                });

                this.nameVM = null;
                this.hpVM = null;
                this.hpBarVM = null;
                this.actionVM = null;
                this.distanceVM = null;
            }
        }

        #region Data Controllers

        public Combatant TargetInfo { get; protected set; }

        protected Combatant TargetInfoClone
        {
            get
            {
                lock (this.TargetInfoLock)
                {
                    return this.TargetInfo?.Clone();
                }
            }
        }

        protected object TargetInfoLock { get; set; } = new object();

        private Stopwatch hoverLife = new Stopwatch();
        private uint previousHoverID = 0;

        private void GetData()
        {
            if (!this.IsAllViewOff)
            {
                this.GetCombatant();
            }
        }

        protected virtual void GetCombatant()
        {
            var targetInfo = default(Combatant);

            if (Settings.Instance.UseHoverTarget)
            {
                this.GetCombatantHoverOn(ref targetInfo);
            }
            else
            {
                this.GetCombatantHoverOff(ref targetInfo);
            }

            this.GetEnmityList(ref targetInfo);

            lock (this.TargetInfoLock)
            {
                this.TargetInfo = targetInfo;
            }
        }

        protected virtual void GetCombatantHoverOff(ref Combatant targetInfo)
            => targetInfo = FFXIVPlugin.Instance.GetTargetInfo(OverlayType.Target);

        protected virtual void GetCombatantHoverOn(ref Combatant targetInfo)
        {
            var info = default(Combatant);

            var ti = FFXIVPlugin.Instance.GetTargetInfo(OverlayType.Target);
            var hi = FFXIVPlugin.Instance.GetTargetInfo(OverlayType.HoverTarget);

            if (hi == null)
            {
                info = ti;
            }
            else
            {
                if (ti != null)
                {
                    info = hi;
                }
                else
                {
                    if (this.previousHoverID != hi.ID)
                    {
                        info = hi;
                        this.hoverLife.Restart();
                    }
                    else
                    {
                        if (this.hoverLife.Elapsed.TotalSeconds
                            <= Settings.Instance.HoverLifeLimit)
                        {
                            info = hi;
                        }
                        else
                        {
                            this.hoverLife.Stop();
                        }
                    }

                    this.previousHoverID = hi.ID;
                }
            }

            targetInfo = info;
        }

        private void GetEnmityList(ref Combatant targetInfo)
        {
            if (!Settings.Instance.Enmity.Visible)
            {
                EnmityPlugin.Instance.Dispose();
                return;
            }

            if (targetInfo != null &&
                !Settings.Instance.Enmity.IsDesignMode &&
                targetInfo.ObjectType == Actor.Type.Monster)
            {
                EnmityPlugin.Instance.Initialize();

                var enmityList = EnmityPlugin.Instance.EnmityEntryList;
                if (enmityList != null &&
                    enmityList.Count > 0)
                {
                    lock (this.TargetInfoLock)
                    {
                        targetInfo.EnmityEntryList.Clear();
                        targetInfo.EnmityEntryList.AddRange(enmityList);
                    }
                }
            }
        }

        #endregion Data Controllers

        #region View Controllers

        /// <summary>
        /// ダミー表示用のアクション名
        /// </summary>
        public string DummyAction { get; set; } = string.Empty;

        /// <summary>
        /// ViewをRefreshさせるキュー
        /// </summary>
        public bool RefreshViewsQueue { get; set; }

        /// <summary>
        /// 全てのView表示設定がOFFか？
        /// </summary>
        protected virtual bool IsAllViewOff =>
            Settings.Instance == null ||
            !FFXIVPlugin.Instance.IsFFXIVActive ||
            (
                !Settings.Instance.TargetName.Visible &&
                !Settings.Instance.TargetAction.Visible &&
                !Settings.Instance.TargetHP.Visible &&
                !Settings.Instance.TargetDistance.Visible &&
                !Settings.Instance.FFLogs.Visible &&
                !Settings.Instance.Enmity.Visible
            );

        public List<ViewAndViewModel> ViewList
        {
            get;
            protected set;
        } = new List<ViewAndViewModel>();

        protected NameView nameView;
        protected HPView hpView;
        protected HPBarView hpBarView;
        protected ActionView actionView;
        protected DistanceView distanceView;
        protected FFLogsView ffLogsView;
        protected EnmityView enmityView;

        public NameView NameView => this.nameView;
        public HPView HPView => this.hpView;
        public HPBarView HPBarView => this.hpBarView;
        public ActionView ActionView => this.actionView;
        public DistanceView DistanceView => this.distanceView;
        public FFLogsView FFLogsView => this.ffLogsView;
        public EnmityView EnmityView => this.enmityView;

        protected NameViewModel nameVM;
        protected HPViewModel hpVM;
        protected HPBarViewModel hpBarVM;
        protected ActionViewModel actionVM;
        protected DistanceViewModel distanceVM;
        protected FFLogsViewModel ffLogsVM;
        protected EnmityViewModel enmityVM;

        protected virtual NameViewModel NameVM =>
            this.nameVM ?? (this.nameVM = new NameViewModel(Settings.Instance.TargetName, this.Model));

        protected virtual HPViewModel HpVM =>
            this.hpVM ?? (this.hpVM = new HPViewModel(Settings.Instance.TargetHP, this.Model));

        protected virtual HPBarViewModel HpBarVM =>
            this.hpBarVM ?? (this.hpBarVM = new HPBarViewModel(Settings.Instance.TargetHP, this.Model));

        protected virtual ActionViewModel ActionVM =>
            this.actionVM ?? (this.actionVM = new ActionViewModel(Settings.Instance.TargetAction, this.Model));

        protected virtual DistanceViewModel DistanceVM =>
            this.distanceVM ?? (this.distanceVM = new DistanceViewModel(Settings.Instance.TargetDistance, this.Model));

        protected virtual FFLogsViewModel FFLogsVM =>
            this.ffLogsVM ?? (this.ffLogsVM = new FFLogsViewModel(Settings.Instance.FFLogs, this.Model));

        protected virtual EnmityViewModel EnmityVM =>
            this.enmityVM ?? (this.enmityVM = new EnmityViewModel(Settings.Instance.Enmity, this.Model));

        /// <summary>
        /// Viewに接続されたデータモデルを更新する
        /// </summary>
        private void UpdateOverlayData()
        {
            try
            {
                this.UpdateOverlayDataCore();
            }
            catch (Exception ex)
            {
                this.logger.Fatal(ex, "UpdateOverlayData error.");
            }
        }

        /// <summary>
        /// Viewに接続されたデータモデルを更新する
        /// </summary>
        protected void UpdateOverlayDataCore()
        {
            // 表示を更新するメソッド
            void updateVisibility(
                bool visible)
            {
                foreach (var entry in this.ViewList)
                {
                    // Clickスルーを設定する
                    entry.ViewModel.SetTransparentWindow(Settings.Instance.ClickThrough);

                    // 表示を切り替える
                    var overlay = entry.View as IOverlay;
                    var viewModel = entry.ViewModel as IOverlayViewModel;

                    overlay.OverlayVisible =
                        visible &
                        viewModel.OverlayVisible;
                }
            }

            // Windowをリフレッシュする
            lock (MainWorker.Instance.ViewRefreshLocker)
            {
                this.RefreshViews();
            }

            // すべてがOFFだったら？
            if (this.IsAllViewOff)
            {
                updateVisibility(false);
                return;
            }

            // ターゲット情報のクローンを取得する
            var targetInfo = this.TargetInfoClone;

            // データを更新する
            var overlayVisible = false;
            if (targetInfo != null)
            {
                overlayVisible = true;

                if (this.IsTargetOverlay)
                {
                    switch (targetInfo.ObjectType)
                    {
                        case Actor.Type.PC:
                        case Actor.Type.Monster:
                        case Actor.Type.NPC:
                            break;

                        case Actor.Type.Aetheryte:
                        case Actor.Type.Gathering:
                        case Actor.Type.Minion:
                        default:
                            overlayVisible = false;
                            break;
                    }

                    if (targetInfo.MaxHP <= 0)
                    {
                        overlayVisible = false;
                    }
                }

                if (overlayVisible)
                {
                    this.RefreshModel(targetInfo);
                }
            }
            else
            {
                this.Model.Name = string.Empty;
                this.Model.CurrentHP = 0;
                this.Model.MaxHP = 0;
                this.Model.CurrentHPRate = 0;
                this.Model.Distance = 0;

                // ダミーアクションの処理
                if (string.IsNullOrEmpty(this.DummyAction))
                {
                    this.Model.CastSkillName = string.Empty;
                    this.Model.CastDurationCurrent = 0;
                    this.Model.CastDurationMax = 0;
                    this.Model.IsCasting = false;
                }
                else
                {
                    lock (this.Model)
                    {
                        if (!this.Model.IsCasting)
                        {
                            this.Model.CastSkillName = this.DummyAction;
                            this.Model.CastDurationMax = 5.5f;
                            this.Model.CastDurationCurrent = 0;

                            // イベントが発生するので最後にセットする
                            this.Model.IsCasting = true;
                        }
                    }

                    overlayVisible = true;
                }

                // デザインモード？
                if (Settings.Instance.MPTicker.TestMode ||
                    Settings.Instance.FFLogs.IsDesignMode ||
                    Settings.Instance.Enmity.IsDesignMode ||
                    TargetInfoModel.IsAvailableParseTotalTextCommand)
                {
                    if (Settings.Instance.FFLogs.IsDesignMode ||
                        TargetInfoModel.IsAvailableParseTotalTextCommand)
                    {
                        this.RefreshFFLogsView(null);
                    }

                    if (Settings.Instance.Enmity.IsDesignMode)
                    {
                        this.RefreshEnmityView(null);
                    }

                    overlayVisible = true;
                }
            }
#if false
#if DEBUG
            // ダミーデータを表示する
            overlayVisible = true;

            if (string.IsNullOrWhiteSpace(this.Model.Name) ||
                this.Model.Name.Contains("D:"))
            {
                this.Model.Name = "DEBUG:ハリカルナッソス";

                this.Model.MaxHP = (DateTime.Now.Minute % 2) == 0 ?
                    3_647_895_124 :
                    4871;

                this.Model.CurrentHP =
                    this.Model.MaxHP *
                    (30 - (DateTime.Now.Second % 30)) / 30;

                this.Model.CurrentHPRate =
                    this.Model.CurrentHP /
                    this.Model.MaxHP;

                this.Model.IsEffectiveDistance = true;
                this.Model.Distance = 60 - DateTime.Now.Second;
            }
#endif
#endif
            // 表示を更新する
            updateVisibility(overlayVisible);
        }

        /// <summary>
        /// 全てのViewを閉じて開き直す
        /// </summary>
        protected void RefreshViews()
        {
            lock (this)
            {
                if (!this.RefreshViewsQueue)
                {
                    return;
                }

                // すでにWindowがあるなら閉じる
                foreach (var item in this.ViewList)
                {
                    (item.View as IOverlay).OverlayVisible = false;
                    item.View.Close();
                }

                this.ViewList.Clear();

                // Viewのインスタンスを作る
                this.CreateViews();

                foreach (var item in this.ViewList)
                {
                    // Windowを表示する
                    item.View.Show();
                }

                this.RefreshViewsQueue = false;
            }
        }

        /// <summary>
        /// Viewの新しいインスタンスを生成する
        /// </summary>
        protected virtual void CreateViews()
        {
            this.CreateView(ref this.nameView, this.NameVM);
            this.CreateView(ref this.hpView, this.HpVM);
            this.CreateView(ref this.hpBarView, this.HpBarVM);
            this.CreateView(ref this.actionView, this.ActionVM);
            this.CreateView(ref this.distanceView, this.DistanceVM);
            this.CreateView(ref this.ffLogsView, this.FFLogsVM);
            this.CreateView(ref this.enmityView, this.EnmityVM);

            // Viewリストに登録する
            // HPBar→HPText の順番に登録しHPTextのほうがあとに開かれるようにする
            this.TryAddViewAndViewModel(this.NameView, this.NameView?.ViewModel);
            this.TryAddViewAndViewModel(this.HPBarView, this.HPBarView?.ViewModel);
            this.TryAddViewAndViewModel(this.HPView, this.HPView?.ViewModel);
            this.TryAddViewAndViewModel(this.ActionView, this.ActionView?.ViewModel);
            this.TryAddViewAndViewModel(this.DistanceView, this.DistanceView?.ViewModel);
            this.TryAddViewAndViewModel(this.FFLogsView, this.FFLogsView?.ViewModel);
            this.TryAddViewAndViewModel(this.EnmityView, this.EnmityView?.ViewModel);
        }

        protected void CreateView<T>(ref T view, OverlayViewModelBase vm)
            where T : Window, new()
        {
            if (vm != null)
            {
                view = new T() { DataContext = vm };
                vm.View = view;
            }
        }

        protected void TryAddViewAndViewModel(
            Window window,
            OverlayViewModelBase vm)
        {
            if (window != null && vm != null)
            {
                this.ViewList.Add(new ViewAndViewModel(window, vm));
            }
        }

        protected virtual void RefreshModel(
            Combatant targetInfo)
        {
            this.Model.Name = targetInfo?.Name ?? string.Empty;
            this.Model.ObjectType = targetInfo?.ObjectType ?? Actor.Type.Unknown;

            this.RefreshActionView(targetInfo);
            this.RefreshHPView(targetInfo);
            this.RefreshDistanceView(targetInfo);
            this.RefreshEnmityView(targetInfo);

            // FFLogsの判定は最後に行う
            this.RefreshFFLogsView(targetInfo);
        }

        protected virtual void RefreshHPView(
            Combatant targetInfo)
        {
            if (this.HPView == null)
            {
                return;
            }

            if (this.HPView.ViewModel.OverlayVisible ||
                this.HPBarView.ViewModel.OverlayVisible)
            {
                this.Model.MaxHP = targetInfo.MaxHP;
                this.Model.CurrentHP = targetInfo.CurrentHP;
                this.Model.CurrentHPRate = targetInfo.CurrentHPRate;
            }
        }

        protected virtual void RefreshActionView(
            Combatant targetInfo)
        {
            if (this.ActionView == null)
            {
                return;
            }

            if (!this.ActionView.ViewModel.OverlayVisible)
            {
                return;
            }

            // ダミーアクションが設定されていないときの通常の処理
            if (string.IsNullOrEmpty(this.DummyAction))
            {
                this.Model.CastSkillID = targetInfo.CastBuffID;
                this.Model.CastSkillName = targetInfo.CastSkillName;
                this.Model.CastDurationMax = targetInfo.CastDurationMax;
                this.Model.CastDurationCurrent = targetInfo.CastDurationCurrent;

                // イベントが発生するので最後にセットする
                this.Model.IsCasting = targetInfo.IsCasting;
            }
        }

        protected virtual void RefreshDistanceView(
            Combatant targetInfo)
        {
            if (this.DistanceView == null)
            {
                return;
            }

            if (!this.DistanceView.ViewModel.OverlayVisible)
            {
                return;
            }

            this.Model.IsEffectiveDistance = targetInfo.IsAvailableEffectiveDictance;

            this.Model.Distance =
                targetInfo.IsAvailableEffectiveDictance ?
                (double)targetInfo.EffectiveDistance :
                targetInfo.HorizontalDistanceByPlayer;
        }

        private void RefreshFFLogsView(
            Combatant targetInfo)
        {
            if (this.FFLogsView == null)
            {
                return;
            }

            this.Model.ObjectType = Settings.Instance.FFLogs.IsDesignMode || targetInfo == null ?
                Actor.Type.PC :
                targetInfo.ObjectType;

            if (targetInfo != null)
            {
                this.Model.Job = targetInfo.JobID;
                this.Model.WorldID = targetInfo.WorldID;
                this.Model.WorldName = targetInfo.WorldName;
            }

            if (!this.FFLogsView.ViewModel.OverlayVisible)
            {
                return;
            }

            this.Model.RefreshFFLogsInfo();
        }

        private void RefreshEnmityView(
            Combatant targetInfo)
        {
            if (this.EnmityView == null)
            {
                return;
            }

            if (!this.EnmityView.ViewModel.OverlayVisible)
            {
                return;
            }

            this.Model.RefreshEnmityList(targetInfo?.EnmityEntryList);
        }

        #endregion View Controllers
    }
}
