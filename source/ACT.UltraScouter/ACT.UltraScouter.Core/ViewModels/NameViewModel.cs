using ACT.UltraScouter.Config;
using ACT.UltraScouter.Models;
using ACT.UltraScouter.ViewModels.Bases;

namespace ACT.UltraScouter.ViewModels
{
    public class NameViewModel :
        OverlayViewModelBase,
        IOverlayViewModel
    {
        public NameViewModel() : this(null, null)
        {
        }

        public NameViewModel(
            TargetName config,
            TargetInfoModel model)
        {
            this.config = config ?? Settings.Instance.TargetName;
            this.model = model ?? TargetInfoModel.Instance;

            this.RaisePropertyChanged(nameof(Config));
            this.RaisePropertyChanged(nameof(Model));

            this.Initialize();
        }

        public override void Initialize()
        {
        }

        private TargetName config;
        private TargetInfoModel model;

        public virtual TargetName Config => this.config;
        public virtual TargetInfoModel Model => this.model;
        public bool OverlayVisible => this.Config.Visible;
    }
}
