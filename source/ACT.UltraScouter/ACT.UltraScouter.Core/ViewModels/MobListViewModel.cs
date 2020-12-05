using System;
using System.Windows.Threading;
using ACT.UltraScouter.Config;
using ACT.UltraScouter.Models;
using ACT.UltraScouter.ViewModels.Bases;
using FFXIV.Framework.XIVHelper;

namespace ACT.UltraScouter.ViewModels
{
    public class MobListViewModel :
        OverlayViewModelBase,
        IOverlayViewModel
    {
        public static MobListViewModel Current { get; private set; }

        public MobListViewModel()
        {
            Current = this;
            this.Initialize();
        }

        public override void Initialize()
        {
            if (this.refreshTimer != null)
            {
                this.refreshTimer.Tick += (x, y) => this.RefreshOriginAngle();
                this.refreshTimer.Start();
            }

            this.Model.RaiseAllPropertiesChanged();
        }

        public override void Dispose()
        {
            if (this.refreshTimer != null)
            {
                this.refreshTimer.Stop();
                this.refreshTimer = null;
            }

            base.Dispose();
        }

        public virtual Settings RootConfig => Settings.Instance;

        public virtual MobList Config => Settings.Instance.MobList;

        public virtual MobListModel Model => MobListModel.Instance;

        public bool OverlayVisible => this.Config.Visible;

        private double originAngle = 0;

        public double OriginAngle
        {
            get => this.originAngle;
            set => this.SetProperty(ref this.originAngle, value);
        }

        private DispatcherTimer refreshTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(0.03),
        };

        private void RefreshOriginAngle()
        {
            if (!this.OverlayVisible)
            {
                return;
            }

            if (this.Model.MobListCount < 1)
            {
                return;
            }

            var angle = 0d;

            switch (this.Config.DirectionOrigin)
            {
                case DirectionOrigin.North:
                    angle = 0;
                    break;

                case DirectionOrigin.Me:
                    var player = CombatantsManager.Instance.Player;
                    if (player != null)
                    {
                        angle = player.HeadingDegree * -1;
                    }
                    break;

                case DirectionOrigin.Camera:
                    CameraInfo.Instance.Refresh();
                    angle = CameraInfo.Instance.HeadingDegree * -1;
                    break;
            }

            // 補正角度を加算する
            this.OriginAngle = angle + this.Config.DirectionAdjustmentAngle;
        }
    }
}
