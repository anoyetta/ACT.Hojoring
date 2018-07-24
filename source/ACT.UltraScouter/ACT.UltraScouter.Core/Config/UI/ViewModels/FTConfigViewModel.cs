using ACT.UltraScouter.Workers;

namespace ACT.UltraScouter.Config.UI.ViewModels
{
    public class FTConfigViewModel :
        TargetConfigViewModel
    {
        protected override ViewCategories ViewCategory => ViewCategories.FocusTarget;

        public override TargetName Name => Settings.Instance.FTName;
        public override TargetHP HP => Settings.Instance.FTHP;
        public override TargetAction Action => Settings.Instance.FTAction;
        public override TargetDistance Distance => Settings.Instance.FTDistance;

        #region TargetAction

        public override string DummyAction
        {
            get => FTInfoWorker.Instance.DummyAction;
            set => FTInfoWorker.Instance.DummyAction = value;
        }

        #endregion TargetAction
    }
}
