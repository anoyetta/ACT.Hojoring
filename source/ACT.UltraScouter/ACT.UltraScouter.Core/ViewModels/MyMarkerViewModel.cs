using ACT.UltraScouter.Config;
using ACT.UltraScouter.ViewModels.Bases;

namespace ACT.UltraScouter.ViewModels
{
    public class MyMarkerViewModel :
        OverlayViewModelBase,
        IOverlayViewModel
    {
        public MyMarkerViewModel()
        {
            this.Initialize();
        }

        public override void Initialize()
        {
        }

        public override void Dispose()
        {
            base.Dispose();
        }

        public virtual Settings RootConfig => Settings.Instance;

        public virtual MyMarker Config => Settings.Instance.MyMarker;

        public bool OverlayVisible => this.Config.Visible;
    }
}
