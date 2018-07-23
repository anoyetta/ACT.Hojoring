using ACT.UltraScouter.Workers;

namespace ACT.UltraScouter.Config.UI.ViewModels
{
    public class ToTConfigViewModel :
        TargetConfigViewModel
    {
        protected override ViewCategories ViewCategory => ViewCategories.TargetOfTarget;

        public override TargetName Name => Settings.Instance.ToTName;
        public override TargetHP HP => Settings.Instance.ToTHP;
        public override TargetAction Action => Settings.Instance.ToTAction;
        public override TargetDistance Distance => Settings.Instance.ToTDistance;

        #region TargetAction

        public override string DummyAction
        {
            get => ToTInfoWorker.Instance.DummyAction;
            set => ToTInfoWorker.Instance.DummyAction = value;
        }

        #endregion TargetAction
    }
}
