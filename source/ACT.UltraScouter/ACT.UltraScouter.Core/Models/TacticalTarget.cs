using System;
using System.Windows.Media.Imaging;
using ACT.UltraScouter.Config;
using ACT.UltraScouter.ViewModels;
using FFXIV.Framework.Bridge;
using FFXIV.Framework.Common;
using FFXIV.Framework.Extensions;
using FFXIV.Framework.XIVHelper;
using Prism.Mvvm;
using Sharlayan.Core.Enums;

namespace ACT.UltraScouter.Models
{
    public class TacticalTarget : BindableBase
    {
        public Guid ID => this.targetActor?.UUID ?? Guid.Empty;

        private int order;

        public int Order
        {
            get => this.order;
            set => this.SetProperty(ref this.order, value);
        }

        public TacticalRadar Config => Settings.Instance.TacticalRadar;

        public TacticalRadarViewModel ParentViewModel => TacticalRadarViewModel.Current;

        public void UpdateTargetInfo()
        {
            this.RaiseUpdateInfo();

            if (this.TargetActor.Name.ContainsIgnoreCase("typeid"))
            {
                this.Name = string.Empty;
            }
            else
            {
                if (this.TargetActor.ActorType == Actor.Type.PC)
                {
                    this.Name = CombatantEx.NameToInitial(
                        this.TargetActor.Name,
                        ConfigBridge.Instance.PCNameStyle);
                }
                else
                {
                    this.Name = this.TargetActor.Name;
                }
            }

            this.HeadingAngle =
                (this.Heading / CameraInfo.HeadingRange) * 360.0 * -1.0;

            var player = CombatantsManager.Instance.Player;
            if (player == null)
            {
                return;
            }

            this.Distance = this.targetActor.HorizontalDistanceByPlayer;

            var x1 = player.PosX;
            var y1 = player.PosY;
            var x2 = this.targetActor.PosX;
            var y2 = this.targetActor.PosY;

            var rad = Math.Atan2(
                y2 - y1,
                x2 - x1);

            this.DirectionAngle = rad * 180.0 / Math.PI;
        }

        private CombatantEx targetActor;

        public CombatantEx TargetActor
        {
            get => this.targetActor;
            set
            {
                this.targetActor = value;

                if (this.targetActor == null ||
                    value == null ||
                    this.targetActor.UUID != value.UUID)
                {
                    this.RaisePropertyChanged();
                }

                this.RaiseUpdateInfo();
            }
        }

        private void RaiseUpdateInfo()
        {
            this.RaisePropertyChanged(nameof(this.IsPC));
            this.RaisePropertyChanged(nameof(this.IsMonster));
            this.RaisePropertyChanged(nameof(this.JobIcon));

            this.RaisePropertyChanged(nameof(this.Heading));
            this.RaisePropertyChanged(nameof(this.X));
            this.RaisePropertyChanged(nameof(this.Y));
            this.RaisePropertyChanged(nameof(this.Z));
        }

        private TacticalItem targetConfig;

        public TacticalItem TargetConfig
        {
            get => this.targetConfig;
            set => this.SetProperty(ref this.targetConfig, value);
        }

        public bool IsPC => this.TargetActor?.ActorType == Actor.Type.PC;

        public bool IsMonster => this.TargetActor?.ActorType == Actor.Type.Monster;

        public BitmapSource JobIcon => JobIconDictionary.Instance.GetIcon(this.TargetActor?.JobID ?? JobIDs.Unknown);

        public bool IsExistsName => !string.IsNullOrEmpty(this.name);

        private string name;

        public string Name
        {
            get => this.name;
            set
            {
                if (this.SetProperty(ref this.name, value))
                {
                    this.RaisePropertyChanged(nameof(this.IsExistsName));
                }
            }
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

        public double Heading => this.targetActor?.Heading ?? 0;
        public double X => this.targetActor?.PosX ?? 0;
        public double Y => this.targetActor?.PosY ?? 0;
        public double Z => this.targetActor?.PosZ ?? 0;
    }
}
