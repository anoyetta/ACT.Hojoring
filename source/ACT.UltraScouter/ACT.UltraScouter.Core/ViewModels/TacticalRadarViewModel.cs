using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows.Data;
using System.Windows.Threading;
using ACT.UltraScouter.Common;
using ACT.UltraScouter.Config;
using ACT.UltraScouter.Models;
using ACT.UltraScouter.ViewModels.Bases;
using FFXIV.Framework.Common;
using FFXIV.Framework.FFXIVHelper;
using Sharlayan.Core;
using Sharlayan.Core.Enums;

namespace ACT.UltraScouter.ViewModels
{
    public class TacticalRadarViewModel :
        OverlayViewModelBase,
        IOverlayViewModel
    {
        public TacticalRadarViewModel()
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

            if (WPFHelper.IsDesignMode)
            {
                this.UpdateTargets(null);
            }

            var src = this.TacticalTargetListSource;
            src.Source = this.TacticalTargetList;
            src.IsLiveSortingRequested = true;
            src.SortDescriptions.Add(new SortDescription(nameof(TacticalTarget.Order), ListSortDirection.Ascending));
            src.View.Refresh();
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

        public virtual TacticalRadar Config => Settings.Instance.TacticalRadar;

        private readonly ObservableCollection<TacticalTarget> TacticalTargetList = new ObservableCollection<TacticalTarget>();

        private readonly CollectionViewSource TacticalTargetListSource = new CollectionViewSource();

        public ICollectionView TacticalTargetView => this.TacticalTargetListSource.View;

        public void UpdateTargets(
            IEnumerable<ActorItem> actors)
        {
            if (this.Config.IsDesignMode ||
                WPFHelper.IsDesignMode)
            {
                actors = DesignTimeActorList;
            }

            using (this.TacticalTargetListSource.DeferRefresh())
            {
                foreach (var actor in actors)
                {
                    var clone = actor.Clone();

                    var exists = this.TacticalTargetList.FirstOrDefault(x => x.ID == clone.UUID);
                    if (exists != null)
                    {
                        exists.TargetActor = clone;
                    }
                    else
                    {
                        var config = this.Config.TacticalItems.FirstOrDefault(x =>
                            x.TargetName.ToLower() == clone.Name.ToLower());

                        exists = new TacticalTarget()
                        {
                            TargetActor = clone,
                            TargetConfig = config,
                        };

                        this.TacticalTargetList.Add(exists);

                        if (exists.TargetConfig != null &&
                            exists.TargetConfig.IsNoticeEnabled &&
                            !string.IsNullOrEmpty(exists.TargetConfig.TTS))
                        {
                            TTSWrapper.Speak(exists.TargetConfig.TTS);
                        }
                    }

                    exists.UpdateTargetInfo();
                    Thread.Yield();
                }

                var toRemoveTargets = this.TacticalTargetList
                    .Where(x => !actors.Any(y => y.UUID == x.ID))
                    .ToArray();

                foreach (var item in toRemoveTargets)
                {
                    this.TacticalTargetList.Remove(item);
                    Thread.Yield();
                }

                var ordered = (
                    from x in this.TacticalTargetList
                    orderby
                    x.TargetActor.Type descending,
                    x.Distance ascending,
                    x.ID ascending
                    select
                    x).ToArray();

                var order = 1;
                foreach (var target in ordered)
                {
                    target.Order = order++;
                }
            }

            this.RaisePropertyChanged(nameof(this.IsExistsTargets));
        }

        public bool OverlayVisible => this.Config.Visible;

        public bool IsExistsTargets => this.TacticalTargetList.Count > 0;

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

            if (this.TacticalTargetList.Count < 1)
            {
                return;
            }

            var angle = 0d;

            if (!FFXIVReader.Instance.IsAvailable)
            {
                angle = 0;
            }
            else
            {
                switch (this.Config.DirectionOrigin)
                {
                    case DirectionOrigin.North:
                        angle = 0;
                        break;

                    case DirectionOrigin.Me:
                        var player = FFXIVPlugin.Instance.GetPlayer();
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
            }

            // 補正角度を加算する
            this.OriginAngle = angle;
        }

        #region Design Time List

        private static readonly ActorItem[] DesignTimeActorList = new[]
        {
            new ActorItem()
            {
                UUID = Guid.NewGuid().ToString(),
                Name = "ガルーダ",
                Type = Actor.Type.Monster,
                Coordinate = new Coordinate(0, 0, 0),
            },

            new ActorItem()
            {
                UUID = Guid.NewGuid().ToString(),
                Name = "イフリート",
                Type = Actor.Type.Monster,
                Coordinate = new Coordinate(0, 0, 0),
            },

            new ActorItem()
            {
                UUID = Guid.NewGuid().ToString(),
                Name = "タイタン",
                Type = Actor.Type.Monster,
                Coordinate = new Coordinate(0, 0, 0),
            },

            new ActorItem()
            {
                UUID = Guid.NewGuid().ToString(),
                Name = "Himechan Hanako",
                Type = Actor.Type.PC,
                Job = Actor.Job.WHM,
                Coordinate = new Coordinate(0, 0, 0),
            },

            new ActorItem()
            {
                UUID = Guid.NewGuid().ToString(),
                Name = "Fairy Princess",
                Type = Actor.Type.PC,
                Job = Actor.Job.SCH,
                Coordinate = new Coordinate(0, 0, 0),
            },
        };

        #endregion Design Time List
    }
}
