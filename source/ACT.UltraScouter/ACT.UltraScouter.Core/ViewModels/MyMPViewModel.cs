using ACT.UltraScouter.Config;
using ACT.UltraScouter.Models;

namespace ACT.UltraScouter.ViewModels
{
    public class MyMPViewModel :
        MyHPViewModel
    {
        public MyMPViewModel() : this(null, null)
        {
        }

        public MyMPViewModel(
            MyStatus config,
            MyStatusModel model) : base(config, model)
        {
        }

        protected override MyStatus GetConfig => Settings.Instance.MyMP;

        protected override string ValuePropertyName => nameof(this.Model.CurrentMP);

        public override bool OverlayVisible => base.OverlayVisible && (this.Model?.IsAvailableMPView ?? true);

        public override double Progress => this.Model.CurrentMPRate;
    }
}
