using ACT.UltraScouter.Config;
using ACT.UltraScouter.Models;
using ACT.UltraScouter.ViewModels.Bases;

namespace ACT.UltraScouter.ViewModels
{
    public class DistanceViewModel :
        OverlayViewModelBase,
        IOverlayViewModel
    {
        public DistanceViewModel()
        {
        }

        public DistanceViewModel(
            TargetDistance config,
            TargetInfoModel model)
        {
            this.config = config ?? Settings.Instance.TargetDistance;
            this.model = model ?? TargetInfoModel.Instance;

            this.Initialize();
        }

        public override void Initialize()
        {
        }

        private TargetDistance config;
        private TargetInfoModel model;

        public virtual TargetDistance Config => this.config;
        public virtual TargetInfoModel Model => this.model;

        public bool OverlayVisible => this.Config.Visible;
    }
}
