using ACT.UltraScouter.Config;
using ACT.UltraScouter.Models;
using ACT.UltraScouter.ViewModels.Bases;
using FFXIV.Framework.FFXIVHelper;

namespace ACT.UltraScouter.ViewModels
{
    public class EnmityViewModel :
        OverlayViewModelBase,
        IOverlayViewModel
    {
        public EnmityViewModel() : this(null, null)
        {
        }

        public EnmityViewModel(
            Enmity config,
            TargetInfoModel model)
        {
            this.config = config ?? Settings.Instance.Enmity;
            this.model = model ?? TargetInfoModel.Instance;

            this.RaisePropertyChanged(nameof(Config));
            this.RaisePropertyChanged(nameof(Model));

            this.Initialize();
        }

        public override void Initialize()
        {
        }

        private Enmity config;
        private TargetInfoModel model;

        public virtual Enmity Config => this.config;
        public virtual TargetInfoModel Model => this.model;

        public bool OverlayVisible
        {
            get
            {
                if (!this.Config.Visible)
                {
                    return false;
                }

                if (this.Config.IsDesignMode)
                {
                    return true;
                }

                if (this.Config.HideInNotCombat &&
                    !FFXIVPlugin.Instance.InCombat)
                {
                    return false;
                }

                if (this.Config.HideInSolo)
                {
                    if (FFXIVPlugin.Instance.PartyMemberCount <= 1)
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}
