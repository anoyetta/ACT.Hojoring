using ACT.UltraScouter.Workers;

namespace ACT.UltraScouter.Config.UI.ViewModels
{
    public class MeConfigViewModel :
        TargetConfigViewModel
    {
        protected override ViewCategories ViewCategory => ViewCategories.Me;

        public override TargetAction Action => Settings.Instance.MeAction;

        #region TargetAction

        public override string DummyAction
        {
            get => MeInfoWorker.Instance.DummyAction;
            set => MeInfoWorker.Instance.DummyAction = value;
        }

        #endregion TargetAction
    }
}
