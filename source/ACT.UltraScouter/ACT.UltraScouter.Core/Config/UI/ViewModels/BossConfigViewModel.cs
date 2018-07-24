using ACT.UltraScouter.Workers;

namespace ACT.UltraScouter.Config.UI.ViewModels
{
    public class BossConfigViewModel :
        TargetConfigViewModel
    {
        protected override ViewCategories ViewCategory => ViewCategories.Boss;

        public override TargetName Name => Settings.Instance.BossName;
        public override TargetHP HP => Settings.Instance.BossHP;
        public override TargetAction Action => Settings.Instance.BossAction;
        public override TargetDistance Distance => Settings.Instance.BossDistance;

        #region TargetAction

        public override string DummyAction
        {
            get => BossInfoWorker.Instance.DummyAction;
            set => BossInfoWorker.Instance.DummyAction = value;
        }

        #endregion TargetAction
    }
}
