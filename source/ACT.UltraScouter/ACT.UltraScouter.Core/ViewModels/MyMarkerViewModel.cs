using ACT.UltraScouter.Config;
using ACT.UltraScouter.ViewModels.Bases;
using FFXIV.Framework.FFXIVHelper;

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

        public bool OverlayVisible
        {
            get
            {
                if (!this.Config.Visible)
                {
                    return false;
                }

                if (this.Config.HideInNotCombat &&
                    !FFXIVPlugin.Instance.InCombat)
                {
                    return false;
                }

                return true;
            }
        }
    }
}
