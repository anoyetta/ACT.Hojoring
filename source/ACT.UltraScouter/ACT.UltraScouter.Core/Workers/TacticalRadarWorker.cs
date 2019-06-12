using System.Collections.Generic;
using System.Linq;
using ACT.UltraScouter.Config;
using ACT.UltraScouter.Models;
using ACT.UltraScouter.ViewModels;
using ACT.UltraScouter.Views;
using FFXIV.Framework.FFXIVHelper;
using Sharlayan.Core;
using Sharlayan.Core.Enums;

namespace ACT.UltraScouter.Workers
{
    public class TacticalRadarWorker :
        TargetInfoWorker
    {
        #region Singleton

        private static TacticalRadarWorker instance;
        public static new TacticalRadarWorker Instance => instance;

        public static new void Initialize() => instance = new TacticalRadarWorker();

        public static new void Free() => instance = null;

        private TacticalRadarWorker()
        {
        }

        #endregion Singleton

        /// <summary>
        /// 任意ターゲット系のオーバーレイではない
        /// </summary>
        protected override bool IsTargetOverlay => false;

        /// <summary>
        /// サブオーバーレイである
        /// </summary>
        protected override bool IsSubOverlay => true;

        public override TargetInfoModel Model => TacticalRadarModel.Instance;

        public override void End()
        {
            lock (MainWorker.Instance.ViewRefreshLocker)
            {
                this.tacticalRadarVM = null;
            }
        }

        private static readonly CombatantEx DummyCombatant = new CombatantEx()
        {
            Name = "DUMMY",
            Type = (byte)Actor.Type.Monster,
        };

        protected override void GetCombatant()
        {
            var model = this.Model as TacticalRadarModel;
            var config = Settings.Instance.TacticalRadar;

            if (config.IsDesignMode)
            {
                clear();
                this.TargetInfo = DummyCombatant;
                return;
            }

            if (!config.Visible ||
                !SharlayanHelper.Instance.IsExistsActors ||
                !config.TacticalItems.Any(x => x.IsEnabled))
            {
                clear();
                return;
            }

            var player = SharlayanHelper.Instance.CurrentPlayer ??
                new ActorItem()
                {
                    Coordinate = new Coordinate()
                };

            var actors = SharlayanHelper.Instance.Actors;

            var query =
                from x in actors.Where(x => !string.IsNullOrEmpty(x?.Name))
                join y in config.TacticalItems
                on x.Name.ToLower() equals y.TargetName.ToLower()
                where
                y.IsEnabled
                select new
                {
                    Actor = x,
                    Config = y,
                    Distance2D = x.Coordinate.Distance2D(player.Coordinate),
                };

            if (!query.Any())
            {
                clear();
            }

            var targets =
                from x in query
                where
                x.Distance2D >= x.Config.DetectRangeMinimum &&
                x.Distance2D <= x.Config.DetectRangeMaximum &&
                x.Actor.HPMax != 0 &&
                x.Actor.HPMax != 1 &&
                (x.Actor.X + x.Actor.Y + x.Actor.Z) != 0 &&
                (
                    (
                        x.Actor.Type == Actor.Type.Monster &&
                        x.Actor.HPCurrent < x.Actor.HPMax &&
                        x.Actor.HPCurrent > 0
                    )
                    ||
                    (
                        x.Actor.Type == Actor.Type.PC &&
                        x.Actor.HPCurrent >= 0
                    )
                    ||
                    (
                        x.Actor.Type != Actor.Type.Monster &&
                        x.Actor.Type != Actor.Type.PC
                    )
                )
                group x by x.Actor.Name into g
                select (
                    from y in g
                    orderby
                    y.Distance2D,
                    y.Actor.Heading != 0 ? 0 : 1
                    select
                    y).First().Actor;

            lock (this.TargetInfoLock)
            {
                model.TargetActors.Clear();
                model.TargetActors.AddRange(targets);

                if (model.TargetActors.Count > 0)
                {
                    this.TargetInfo = DummyCombatant;
                }
            }

            void clear()
            {
                this.TargetInfo = null;

                if (model.TargetActors.Count > 0)
                {
                    lock (this.TargetInfoLock)
                    {
                        model.TargetActors.Clear();
                    }
                }
            }
        }

        protected override NameViewModel NameVM => null;

        protected override HPViewModel HpVM => null;

        protected override HPBarViewModel HpBarVM => null;

        protected override ActionViewModel ActionVM => null;

        protected override DistanceViewModel DistanceVM => null;

        protected override FFLogsViewModel FFLogsVM => null;

        protected override EnmityViewModel EnmityVM => null;

        #region TacticalRadar

        protected TacticalRadarView tacticalRadarView;

        public TacticalRadarView TacticalRadarView => this.tacticalRadarView;

        protected TacticalRadarViewModel tacticalRadarVM;

        protected TacticalRadarViewModel TacticalRadarVM =>
            this.tacticalRadarVM ?? (this.tacticalRadarVM = new TacticalRadarViewModel());

        #endregion TacticalRadar

        protected override bool IsAllViewOff =>
            !(Settings.Instance?.TacticalRadar?.Visible ?? false);

        protected override void CreateViews()
        {
            base.CreateViews();

            this.CreateView(ref this.tacticalRadarView, this.TacticalRadarVM);
            this.TryAddViewAndViewModel(this.TacticalRadarView, this.TacticalRadarView?.ViewModel);
        }

        protected override void RefreshModel(
            CombatantEx targetInfo)
        {
            base.RefreshModel(targetInfo);
            this.RefreshTacticalRadarView(targetInfo);
        }

        protected virtual void RefreshTacticalRadarView(
            CombatantEx targetInfo)
        {
            if (this.TacticalRadarView == null)
            {
                return;
            }

            if (!this.TacticalRadarView.ViewModel.OverlayVisible)
            {
                return;
            }

            var model = this.Model as TacticalRadarModel;

            var actors = default(IEnumerable<ActorItem>);

            lock (this.TargetInfoLock)
            {
                actors = model.TargetActors.ToArray();
            }

            this.TacticalRadarVM.UpdateTargets(actors);
        }
    }
}
