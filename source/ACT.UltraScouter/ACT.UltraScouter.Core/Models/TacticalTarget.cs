using System;
using System.Windows.Media.Imaging;
using ACT.UltraScouter.Config;
using FFXIV.Framework.Bridge;
using FFXIV.Framework.Common;
using FFXIV.Framework.FFXIVHelper;
using Prism.Mvvm;
using Sharlayan.Core;
using Sharlayan.Core.Enums;

namespace ACT.UltraScouter.Models
{
    public class TacticalTarget : BindableBase
    {
        public string ID => this.targetActor?.UUID;

        private int order;

        public int Order
        {
            get => this.order;
            set => this.SetProperty(ref this.order, value);
        }

        public TacticalRadar Config => Settings.Instance.TacticalRadar;

        public void UpdateTargetInfo()
        {
            this.RaisePropertyChanged(nameof(this.IsPC));
            this.RaisePropertyChanged(nameof(this.IsMonster));
            this.RaisePropertyChanged(nameof(this.JobIcon));

            if (this.TargetActor.Type == Actor.Type.PC)
            {
                this.Name = Combatant.NameToInitial(
                    this.TargetActor.Name,
                    ConfigBridge.Instance.PCNameStyle);
            }
            else
            {
                this.Name = this.TargetActor.Name;
            }

            this.HeadingAngle = (this.TargetActor.Heading + 3.0) / 6.0 * 360.0 * -1.0;

            var player = SharlayanHelper.Instance.CurrentPlayer;
            if (player == null)
            {
                return;
            }

            this.Distance = this.TargetActor.Coordinate.Distance2D(player.Coordinate);

            var rad = this.TargetActor.Coordinate.AngleTo(player.Coordinate);
            this.DirectionAngle = rad * 180.0 / Math.PI;
        }

        private ActorItem targetActor;

        public ActorItem TargetActor
        {
            get => this.targetActor;
            set => this.SetProperty(ref this.targetActor, value);
        }

        private TacticalItem targetConfig;

        public TacticalItem TargetConfig
        {
            get => this.targetConfig;
            set => this.SetProperty(ref this.targetConfig, value);
        }

        public bool IsPC => this.TargetActor?.Type == Actor.Type.PC;

        public bool IsMonster => this.TargetActor?.Type == Actor.Type.Monster;

        public BitmapSource JobIcon => JobIconDictionary.Instance.GetIcon(this.TargetActor?.Job ?? Actor.Job.Unknown);

        private string name;

        public string Name
        {
            get => this.name;
            set => this.SetProperty(ref this.name, value);
        }

        private double distance;

        public double Distance
        {
            get => this.distance;
            set => this.SetProperty(ref this.distance, value);
        }

        private double directionAngle;

        public double DirectionAngle
        {
            get => this.directionAngle;
            set => this.SetProperty(ref this.directionAngle, value);
        }

        private double headingAngle;

        public double HeadingAngle
        {
            get => this.headingAngle;
            set => this.SetProperty(ref this.headingAngle, value);
        }
    }
}
