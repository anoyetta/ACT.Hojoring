using ACT.UltraScouter.Config;
using ACT.UltraScouter.Models;
using ACT.UltraScouter.ViewModels.Bases;

namespace ACT.UltraScouter.ViewModels
{
    public class FFLogsViewModel :
        OverlayViewModelBase,
        IOverlayViewModel
    {
        public FFLogsViewModel() : this(null, null)
        {
        }

        public FFLogsViewModel(
            FFLogs config,
            TargetInfoModel model)
        {
            this.config = config ?? Settings.Instance.FFLogs;
            this.model = model ?? TargetInfoModel.Instance;

            this.Initialize();
        }

        public override void Initialize()
        {
        }

        private FFLogs config;
        private TargetInfoModel model;

        public virtual FFLogs Config => this.config;
        public virtual TargetInfoModel Model => this.model;

        public bool OverlayVisible =>
            this.Config.Visible &&
            !string.IsNullOrEmpty(this.Config.ApiKey);
    }
}
