using ACT.UltraScouter.Config;
using ACT.UltraScouter.Models;
using ACT.UltraScouter.ViewModels;
using FFXIV.Framework.FFXIVHelper;

namespace ACT.UltraScouter.Workers
{
    public class ToTInfoWorker :
        TargetInfoWorker
    {
        #region Singleton

        private static ToTInfoWorker instance;
        public static new ToTInfoWorker Instance => instance;

        public static new void Initialize() => instance = new ToTInfoWorker();

        public static new void Free() => instance = null;

        private ToTInfoWorker()
        {
        }

        #endregion Singleton

        public override TargetInfoModel Model => ToTInfoModel.Instance;

        protected override void GetCombatant()
        {
            var ti = FFXIVPlugin.Instance.GetTargetInfo(OverlayType.TargetOfTarget);
            lock (this.TargetInfoLock)
            {
                this.TargetInfo = ti;
            }
        }

        protected override NameViewModel NameVM =>
            this.nameVM ?? (this.nameVM = new NameViewModel(Settings.Instance.ToTName, this.Model));

        protected override HPViewModel HpVM =>
            this.hpVM ?? (this.hpVM = new HPViewModel(Settings.Instance.ToTHP, this.Model));

        protected override HPBarViewModel HpBarVM =>
            this.hpBarVM ?? (this.hpBarVM = new HPBarViewModel(Settings.Instance.ToTHP, this.Model));

        protected override ActionViewModel ActionVM =>
            this.actionVM ?? (this.actionVM = new ActionViewModel(Settings.Instance.ToTAction, this.Model));

        protected override DistanceViewModel DistanceVM =>
            this.distanceVM ?? (this.distanceVM = new DistanceViewModel(Settings.Instance.ToTDistance, this.Model));

        protected override FFLogsViewModel FFLogsVM => null;

        protected override bool IsAllViewOff =>
            !FFXIVPlugin.Instance.IsFFXIVActive ||
            (
                !(Settings.Instance?.ToTName?.Visible ?? false) &&
                !(Settings.Instance?.ToTAction?.Visible ?? false) &&
                !(Settings.Instance?.ToTHP?.Visible ?? false) &&
                !(Settings.Instance?.ToTDistance?.Visible ?? false)
            );
    }
}
