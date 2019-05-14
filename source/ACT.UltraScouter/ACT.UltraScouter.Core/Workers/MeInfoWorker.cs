using ACT.UltraScouter.Config;
using ACT.UltraScouter.Models;
using ACT.UltraScouter.ViewModels;
using ACT.UltraScouter.Views;
using FFXIV.Framework.FFXIVHelper;

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
            var ti = FFXIVPlugin.Instance.GetPlayer();

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
            !FFXIVPlugin.Instance.IsFFXIVActive ||
            (
                !(Settings.Instance?.MeAction?.Visible ?? false) &&
                !(Settings.Instance?.MPTicker?.Visible ?? false) &&
                !(Settings.Instance?.MyMarker?.Visible ?? false)
            );

        protected override void CreateViews()
        {
            base.CreateViews();

            this.CreateView(ref this.mpTickerView, this.MPTickerVM);
            this.TryAddViewAndViewModel(this.MPTickerView, this.MPTickerView?.ViewModel);

            this.CreateView(ref this.myMarkerView, this.MyMarkerVM);
            this.TryAddViewAndViewModel(this.MyMarkerView, this.MyMarkerView?.ViewModel);
        }

        protected override void RefreshModel(
            Combatant targetInfo)
        {
            base.RefreshModel(targetInfo);

            // MPTickerを更新する
            this.RefreshMPTickerView(targetInfo);
        }

        protected virtual void RefreshMPTickerView(
            Combatant targetInfo)
        {
            if (this.MPTickerView == null)
            {
                return;
            }

            if (!this.MPTickerView.ViewModel.OverlayVisible)
            {
                return;
            }

            var model = this.Model as MeInfoModel;
            if (model != null)
            {
                model.JobID = targetInfo.JobID;
                model.MaxMP = targetInfo.MaxMP;
                model.CurrentMP = targetInfo.CurrentMP;
            }
        }
    }
}
