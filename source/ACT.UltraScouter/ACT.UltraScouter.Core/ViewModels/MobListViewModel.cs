using System;
using System.Windows.Threading;
using ACT.UltraScouter.Config;
using ACT.UltraScouter.Models;
using ACT.UltraScouter.ViewModels.Bases;
using FFXIV.Framework.FFXIVHelper;

namespace ACT.UltraScouter.ViewModels
{
    public class MobListViewModel :
        OverlayViewModelBase,
        IOverlayViewModel
    {
        public MobListViewModel()
        {
            this.Initialize();
        }

        public override void Initialize()
        {
            if (this.refreshTimer != null)
            {
                this.refreshTimer.Tick += (x, y) => this.RefreshOriginAngle();
                this.refreshTimer.Start();
            }
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

            if (!FFXIVReader.Instance.IsAvailable)
            {
                this.OriginAngle = 0;
            }
            else
            {
                switch (this.Config.DirectionOrigin)
                {
                    case DirectionOrigin.North:
                        this.OriginAngle = 0;
                        break;

                    case DirectionOrigin.Me:
                        var player = FFXIVPlugin.Instance.GetPlayer();
                        if (player != null)
                        {
                            this.OriginAngle = player.HeadingDegree * -1;
                        }
                        break;

                    case DirectionOrigin.Camera:
                        CameraInfo.Instance.Refresh();
                        this.OriginAngle = CameraInfo.Instance.HeadingDegree * -1;
                        break;
                }
            }
        }
    }
}
