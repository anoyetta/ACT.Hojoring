using Prism.Mvvm;

namespace ACT.UltraScouter.Config.UI.ViewModels
{
    public class MyUtilityConfigViewModel :
        BindableBase
    {
        public MyUtilityConfigViewModel() : base()
        {
        }

        public MyUtility Config => Settings.Instance.MyUtility;
    }
}
