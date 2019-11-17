using ACT.UltraScouter.Config;
using ACT.UltraScouter.Models;
using ACT.UltraScouter.ViewModels;
using ACT.UltraScouter.Views;
using FFXIV.Framework.XIVHelper;
using Sharlayan.Core.Enums;

namespace ACT.UltraScouter.Workers
{
    public class EnemyListWorker :
        TargetInfoWorker
    {
        #region Singleton

        private static EnemyListWorker instance;
        public static new EnemyListWorker Instance => instance;

        public static new void Initialize() => instance = new EnemyListWorker();

        public static new void Free() => instance = null;

        private EnemyListWorker()
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
                this.enemyHPVM = null;
            }
        }

        private static readonly CombatantEx DummyCombatant = new CombatantEx()
        {
            Name = "DUMMY",
            Type = (byte)Actor.Type.Monster,
        };

        protected override void GetCombatant()
        {
            var config = Settings.Instance.EnemyHP;

            if (!config.IsDesignMode)
            {
                if (!config.Visible ||
                    CombatantsManager.Instance.CombatantsMainCount <= 0)
                {
                    this.TargetInfo = null;
                    return;
                }
            }

            this.TargetInfo = DummyCombatant;
        }

        protected override NameViewModel NameVM => null;

        protected override HPViewModel HpVM => null;

        protected override HPBarViewModel HpBarVM => null;

        protected override ActionViewModel ActionVM => null;

        protected override DistanceViewModel DistanceVM => null;

        protected override FFLogsViewModel FFLogsVM => null;

        protected override EnmityViewModel EnmityVM => null;

        #region Enemy List

        protected EnemyHPView enemyHPView;

        public EnemyHPView EnemyHPView => this.enemyHPView;

        protected EnemyHPViewModel enemyHPVM;

        protected EnemyHPViewModel EnemyHPVM => this.enemyHPVM ??= new EnemyHPViewModel();

        #endregion Enemy List

        protected override bool IsAllViewOff =>
            !(Settings.Instance?.EnemyHP?.Visible ?? false);

        protected override void CreateViews()
        {
            base.CreateViews();

            this.CreateView(ref this.enemyHPView, this.EnemyHPVM);
            this.TryAddViewAndViewModel(this.EnemyHPView, this.EnemyHPView?.ViewModel);
        }

        protected override void RefreshModel(
            CombatantEx targetInfo)
        {
            base.RefreshModel(targetInfo);
            this.RefreshEnemyListView();
        }

        private void RefreshEnemyListView()
        {
            if (this.EnemyHPView == null)
            {
                return;
            }

            if (!this.EnemyHPView.ViewModel.OverlayVisible)
            {
                return;
            }

            EnemyHPListModel.Instance.Update();
        }
    }
}
