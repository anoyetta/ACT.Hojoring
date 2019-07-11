namespace ACT.UltraScouter.Config.UI.ViewModels
{
    public class MyMPConfigViewModel :
        MyHPConfigViewModel
    {
        public override MyStatus Config => Settings.Instance.MyMP;
    }
}
