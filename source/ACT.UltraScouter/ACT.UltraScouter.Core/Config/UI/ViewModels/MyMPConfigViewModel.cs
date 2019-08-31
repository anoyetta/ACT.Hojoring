namespace ACT.UltraScouter.Config.UI.ViewModels
{
    public class MyMPConfigViewModel :
        MyHPConfigViewModel
    {
        public MyMPConfigViewModel() : base()
        {
            this.Config.InitTargetJobs();
        }

        public override MyStatus Config => Settings.Instance.MyMP;

        public override bool IsMPConfig => true;
    }
}
