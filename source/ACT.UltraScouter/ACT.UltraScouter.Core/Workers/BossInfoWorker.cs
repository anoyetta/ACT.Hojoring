using ACT.UltraScouter.Config;
using ACT.UltraScouter.Models;
using ACT.UltraScouter.ViewModels;
using FFXIV.Framework.FFXIVHelper;

namespace ACT.UltraScouter.Workers
{
    public class BossInfoWorker :
        TargetInfoWorker
    {
        #region Singleton

        private static BossInfoWorker instance;
        public static new BossInfoWorker Instance => instance;

        public static new void Initialize() => instance = new BossInfoWorker();

        public static new void Free() => instance = null;

        private BossInfoWorker()
        {
        }

        #endregion Singleton

        public override TargetInfoModel Model => BossInfoModel.Instance;

        protected override void GetCombatant()
        {
            var ti = FFXIVPlugin.Instance?.GetBossInfo(
                Settings.Instance.BossHPThreshold);

            if (ti != null)
            {
                if (Settings.Instance.BossVSTargetHideBoss)
                {
                    if (TargetInfoWorker.Instance?.TargetInfo != null &&
                        TargetInfoWorker.Instance?.TargetInfo.ID == ti?.ID)
                    {
                        ti = null;
                    }
                }
            }

            if (ti != null)
            {
                if (Settings.Instance.BossVSFTHideBoss)
                {
                    if (FTInfoWorker.Instance?.TargetInfo != null &&
                        FTInfoWorker.Instance?.TargetInfo.ID == ti?.ID)
                    {
                        ti = null;
                    }
                }
            }

            if (ti != null)
            {
                if (Settings.Instance.BossVSToTHideBoss)
                {
                    if (ToTInfoWorker.Instance?.TargetInfo != null &&
                        ToTInfoWorker.Instance?.TargetInfo.ID == ti?.ID)
                    {
                        ti = null;
                    }
                }
            }

            lock (this.TargetInfoLock)
            {
                this.TargetInfo = ti;
            }
        }

        protected override NameViewModel NameVM =>
            this.nameVM ?? (this.nameVM = new NameViewModel(Settings.Instance.BossName, this.Model));

        protected override HPViewModel HpVM =>
            this.hpVM ?? (this.hpVM = new HPViewModel(Settings.Instance.BossHP, this.Model));

        protected override HPBarViewModel HpBarVM =>
            this.hpBarVM ?? (this.hpBarVM = new HPBarViewModel(Settings.Instance.BossHP, this.Model));

        protected override ActionViewModel ActionVM =>
            this.actionVM ?? (this.actionVM = new ActionViewModel(Settings.Instance.BossAction, this.Model));

        protected override DistanceViewModel DistanceVM =>
            this.distanceVM ?? (this.distanceVM = new DistanceViewModel(Settings.Instance.BossDistance, this.Model));

        protected override FFLogsViewModel FFLogsVM => null;

        protected override EnmityViewModel EnmityVM => null;

        protected override bool IsAllViewOff =>
            !FFXIVPlugin.Instance.IsFFXIVActive ||
            (
                !(Settings.Instance?.BossName?.Visible ?? false) &&
                !(Settings.Instance?.BossAction?.Visible ?? false) &&
                !(Settings.Instance?.BossHP?.Visible ?? false) &&
                !(Settings.Instance?.BossDistance?.Visible ?? false)
            );
    }
}
