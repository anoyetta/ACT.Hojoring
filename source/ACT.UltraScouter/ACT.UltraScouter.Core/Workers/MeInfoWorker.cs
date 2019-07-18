using ACT.UltraScouter.Config;
using ACT.UltraScouter.Models;
using ACT.UltraScouter.ViewModels;
using ACT.UltraScouter.Views;
using FFXIV.Framework.XIVHelper;

namespace ACT.UltraScouter.Workers
{
    public class MeInfoWorker :
        TargetInfoWorker
    {
        #region Singleton

        private static MeInfoWorker instance;
        public static new MeInfoWorker Instance => instance;

        public static new void Initialize() => instance = new MeInfoWorker();

        public static new void Free() => instance = null;

        private MeInfoWorker()
        {
        }

        #endregion Singleton

        public override TargetInfoModel Model => MeInfoModel.Instance;

        public override void End()
        {
            base.End();

            lock (MainWorker.Instance.ViewRefreshLocker)
            {
                this.mpTickerVM = null;
            }
        }

        protected override void GetCombatant()
        {
            var ti = CombatantsManager.Instance.Player;

            if (!string.IsNullOrEmpty(this.DummyAction))
            {
                ti = null;
            }

            lock (this.TargetInfoLock)
            {
                this.TargetInfo = ti;
            }
        }

        protected override NameViewModel NameVM => null;

        protected override HPViewModel HpVM => null;

        protected override HPBarViewModel HpBarVM => null;

        protected override ActionViewModel ActionVM =>
            this.actionVM ?? (this.actionVM = new ActionViewModel(Settings.Instance.MeAction, this.Model));

        protected override DistanceViewModel DistanceVM => null;

        protected override FFLogsViewModel FFLogsVM => null;

        protected override EnmityViewModel EnmityVM => null;

        #region MyMarker

        // HP
        protected MyHPView myHPView;

        public MyHPView MyHPView => this.myHPView;

        protected MyHPViewModel myHPVM;

        protected MyHPViewModel MyHPVM => this.myHPVM ??= new MyHPViewModel();

        // MP
        protected MyMPView myMPView;

        public MyMPView MyMPView => this.myMPView;

        protected MyMPViewModel myMPVM;

        protected MyMPViewModel MyMPVM => this.myMPVM ??= new MyMPViewModel();

        #endregion MyMarker

        #region MPTicker

        protected MPTickerView mpTickerView;

        public MPTickerView MPTickerView => this.mpTickerView;

        protected MPTickerViewModel mpTickerVM;

        protected MPTickerViewModel MPTickerVM =>
            this.mpTickerVM ?? (this.mpTickerVM = new MPTickerViewModel());

        #endregion MPTicker

        #region MyMarker

        protected MyMarkerView myMarkerView;

        public MyMarkerView MyMarkerView => this.myMarkerView;

        protected MyMarkerViewModel myMarkerVM;

        protected MyMarkerViewModel MyMarkerVM =>
            this.myMarkerVM ?? (this.myMarkerVM = new MyMarkerViewModel());

        #endregion MyMarker

        protected override bool IsAllViewOff =>
            !XIVPluginHelper.Instance.IsFFXIVActive ||
            (
                !(Settings.Instance?.MeAction?.Visible ?? false) &&
                !(Settings.Instance?.MyHP?.Visible ?? false) &&
                !(Settings.Instance?.MyMP?.Visible ?? false) &&
                !(Settings.Instance?.MPTicker?.Visible ?? false) &&
                !(Settings.Instance?.MyMarker?.Visible ?? false)
            );

        protected override void CreateViews()
        {
            base.CreateViews();

            this.CreateView(ref this.myHPView, this.MyHPVM);
            this.TryAddViewAndViewModel(this.myHPView, this.myHPView?.ViewModel);

            this.CreateView(ref this.myMPView, this.MyMPVM);
            this.TryAddViewAndViewModel(this.myMPView, this.myMPView?.ViewModel);

            this.CreateView(ref this.mpTickerView, this.MPTickerVM);
            this.TryAddViewAndViewModel(this.MPTickerView, this.MPTickerView?.ViewModel);

            this.CreateView(ref this.myMarkerView, this.MyMarkerVM);
            this.TryAddViewAndViewModel(this.MyMarkerView, this.MyMarkerView?.ViewModel);
        }

        protected override void RefreshModel(
            CombatantEx targetInfo)
        {
            base.RefreshModel(targetInfo);

            // MyHP・MPを更新する
            this.RefreshStatusView(targetInfo);

            // MPTickerを更新する
            this.RefreshMPTickerView(targetInfo);
        }

        protected virtual void RefreshStatusView(
            CombatantEx targetInfo)
        {
            if (this.MyHPView == null &&
                this.MyMPView == null)
            {
                return;
            }

            if (!this.MyHPView.ViewModel.OverlayVisible &&
                !this.MyMPView.ViewModel.OverlayVisible)
            {
                return;
            }

            var model = MyStatusModel.Instance;
            model.Update(targetInfo);
        }

        protected virtual void RefreshMPTickerView(
            CombatantEx targetInfo)
        {
            if (this.MPTickerView == null)
            {
                return;
            }

            if (!this.MPTickerView.ViewModel.OverlayVisible)
            {
                return;
            }

            TickerModel.Instance.Update(targetInfo);
        }
    }
}
