using ACT.UltraScouter.Config;
using ACT.UltraScouter.Models;
using ACT.UltraScouter.ViewModels;
using FFXIV.Framework.FFXIVHelper;

namespace ACT.UltraScouter.Workers
{
    public class FTInfoWorker :
        TargetInfoWorker
    {
        #region Singleton

        private static FTInfoWorker instance;
        public static new FTInfoWorker Instance => instance;

        public static new void Initialize() => instance = new FTInfoWorker();

        public static new void Free() => instance = null;

        private FTInfoWorker()
        {
        }

        #endregion Singleton

        public override TargetInfoModel Model => FTInfoModel.Instance;

        protected override void GetCombatant()
        {
            var ti = FFXIVPlugin.Instance.GetTargetInfo(OverlayType.FocusTarget);
            lock (this.TargetInfoLock)
            {
                this.TargetInfo = ti;
            }
        }

        protected override NameViewModel NameVM =>
            this.nameVM ?? (this.nameVM = new NameViewModel(Settings.Instance.FTName, this.Model));

        protected override HPViewModel HpVM =>
            this.hpVM ?? (this.hpVM = new HPViewModel(Settings.Instance.FTHP, this.Model));

        protected override HPBarViewModel HpBarVM =>
            this.hpBarVM ?? (this.hpBarVM = new HPBarViewModel(Settings.Instance.FTHP, this.Model));

        protected override ActionViewModel ActionVM =>
            this.actionVM ?? (this.actionVM = new ActionViewModel(Settings.Instance.FTAction, this.Model));

        protected override DistanceViewModel DistanceVM =>
            this.distanceVM ?? (this.distanceVM = new DistanceViewModel(Settings.Instance.FTDistance, this.Model));

        protected override bool IsAllViewOff =>
            !FFXIVPlugin.Instance.IsFFXIVActive ||
            (
                !(Settings.Instance?.FTName?.Visible ?? false) &&
                !(Settings.Instance?.FTAction?.Visible ?? false) &&
                !(Settings.Instance?.FTHP?.Visible ?? false) &&
                !(Settings.Instance?.FTDistance?.Visible ?? false)
            );
    }
}
