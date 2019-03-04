using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ACT.UltraScouter.Config;
using ACT.UltraScouter.Config.UI.ViewModels;
using ACT.UltraScouter.Models;
using ACT.UltraScouter.ViewModels;
using ACT.UltraScouter.Views;
using FFXIV.Framework.Bridge;
using FFXIV.Framework.Common;
using FFXIV.Framework.FFXIVHelper;
using Sharlayan.Core;
using Sharlayan.Core.Enums;

namespace ACT.UltraScouter.Workers
{
    public class MobListWorker :
        TargetInfoWorker
    {
        #region Singleton

        private static MobListWorker instance;
        public static new MobListWorker Instance => instance;

        public static new void Initialize() => instance = new MobListWorker();

        public static new void Free() => instance = null;

        private MobListWorker()
        {
        }

        #endregion Singleton

        /// <summary>
        /// 任意ターゲット系のオーバーレイではない
        /// </summary>
        protected override bool IsTargetOverlay => false;

        public override TargetInfoModel Model => MobListModel.Instance;
        private List<MobInfo> targetMobList;

        public override void End()
        {
            base.End();

            lock (MainWorker.Instance.ViewRefreshLocker)
            {
                this.mobListVM = null;
            }
        }

        private DateTime combatantsTimestamp = DateTime.MinValue;

        protected override async void GetCombatant()
        {
            lock (this.TargetInfoLock)
            {
                if ((DateTime.Now - this.combatantsTimestamp).TotalMilliseconds
                    < Settings.Instance.MobList.RefreshRateMin)
                {
                    return;
                }

                this.combatantsTimestamp = DateTime.Now;
            }

            #region Test Mode

            // テストモード？
            if (Settings.Instance.MobList.TestMode)
            {
                var dummyPlayer = new Combatant()
                {
                    ID = 1,
                    PosX = 0,
                    PosY = 0,
                    PosZ = 0,
                };

                var dummyTargets = new List<MobInfo>();

                dummyTargets.Add(new MobInfo()
                {
                    Name = "TEST:シルバーの吉田直樹",
                    Rank = "EX",
                    Combatant = new Combatant()
                    {
                        ID = 1,
                        Name = "TEST:シルバーの吉田直樹",
                        ObjectType = Actor.Type.Monster,
                        MaxHP = 1,
                        Player = dummyPlayer,
                        PosX = 0,
                        PosY = 10,
                        PosZ = 10,
                    },
                });

                dummyTargets.Add(new MobInfo()
                {
                    Name = "TEST:イクシオン",
                    Rank = "EX",
                    Combatant = new Combatant()
                    {
                        ID = 2,
                        Name = "TEST:イクシオン",
                        ObjectType = Actor.Type.Monster,
                        MaxHP = 1,
                        Player = dummyPlayer,
                        PosX = 100,
                        PosY = 100,
                        PosZ = -10,
                    },
                });

                dummyTargets.Add(new MobInfo()
                {
                    Name = "TEST:イクシオン",
                    Rank = "EX",
                    Combatant = new Combatant()
                    {
                        ID = 21,
                        Name = "TEST:イクシオン",
                        ObjectType = Actor.Type.Monster,
                        MaxHP = 1,
                        Player = dummyPlayer,
                        PosX = 100,
                        PosY = 100,
                        PosZ = -10,
                    },
                });

                dummyTargets.Add(new MobInfo()
                {
                    Name = "TEST:ソルト・アンド・ライト",
                    Rank = "S",
                    Combatant = new Combatant()
                    {
                        ID = 3,
                        Name = "TEST:ソルト・アンド・ライト",
                        ObjectType = Actor.Type.Monster,
                        MaxHP = 1,
                        Player = dummyPlayer,
                        PosX = 10,
                        PosY = 0,
                        PosZ = 0,
                    },
                });

                dummyTargets.Add(new MobInfo()
                {
                    Name = "TEST:オルクス",
                    Rank = "A",
                    Combatant = new Combatant()
                    {
                        ID = 4,
                        Name = "TEST:オルクス",
                        ObjectType = Actor.Type.Monster,
                        MaxHP = 1,
                        Player = dummyPlayer,
                        PosX = 100,
                        PosY = -100,
                        PosZ = 0,
                    },
                });

                dummyTargets.Add(new MobInfo()
                {
                    Name = "TEST:宵闇のヤミニ",
                    Rank = "B",
                    Combatant = new Combatant()
                    {
                        ID = 5,
                        Name = "TEST:宵闇のヤミニ",
                        ObjectType = Actor.Type.Monster,
                        MaxHP = 1,
                        Player = dummyPlayer,
                        PosX = 0,
                        PosY = -100,
                        PosZ = 0,
                    },
                });

                dummyTargets.Add(new MobInfo()
                {
                    Name = Combatant.NameToInitial("Himeko Flower", ConfigBridge.Instance.PCNameStyle),
                    Rank = "DEAD",
                    Combatant = new Combatant()
                    {
                        ID = 7,
                        Name = Combatant.NameToInitial("Himeko Flower", ConfigBridge.Instance.PCNameStyle),
                        ObjectType = Actor.Type.PC,
                        Job = (byte)JobIDs.BLM,
                        MaxHP = 43462,
                        Player = dummyPlayer,
                        PosX = -100,
                        PosY = -100,
                        PosZ = 0,
                    },
                });

                lock (this.TargetInfoLock)
                {
                    this.TargetInfo = dummyTargets.First().Combatant;
                    this.targetMobList = dummyTargets;
                }

                return;
            }

            #endregion Test Mode

            var player = FFXIVPlugin.Instance.GetPlayer();

            var targetDelegates = default(IEnumerable<Func<MobInfo>>);
            var combatants = default(IEnumerable<Combatant>);
            var actors = default(IEnumerable<ActorItem>);

            await Task.Run(() =>
            {
                combatants = FFXIVPlugin.Instance.GetCombatantList();

                targetDelegates =
                    from x in combatants
                    where
                    ((x.MaxHP <= 0) || (x.MaxHP > 0 && x.CurrentHP > 0)) &&
                    Settings.Instance.MobList.TargetMobList.ContainsKey(x.Name)
                    select new Func<MobInfo>(() => new MobInfo()
                    {
                        Name = x.Name,
                        Combatant = x,
                        Rank = Settings.Instance.MobList.TargetMobList[x.Name].Rank,
                        MaxDistance = Settings.Instance.MobList.TargetMobList[x.Name].MaxDistance,
                        TTSEnabled = Settings.Instance.MobList.TargetMobList[x.Name].TTSEnabled,
                    });

                // 戦闘不能者を検出する？
                if (Settings.Instance.MobList.IsEnabledDetectDeadmen)
                {
                    var deadmenInfo = Settings.Instance.MobList.GetDetectDeadmenInfo;
                    var party = FFXIVPlugin.Instance.GetPartyList();
                    var deadmen =
                        from x in party
                        where
                        !x.IsPlayer &&
                        x.MaxHP > 0 && x.CurrentHP <= 0
                        select new Func<MobInfo>(() => new MobInfo()
                        {
                            Name = x.NameForDisplay,
                            Combatant = x,
                            Rank = deadmenInfo.Rank,
                            MaxDistance = deadmenInfo.MaxDistance,
                            TTSEnabled = deadmenInfo.TTSEnabled,
                        });

                    targetDelegates = targetDelegates.Concat(deadmen);
                }

                // sharlayanからNPCを補完する
                actors = SharlayanHelper.Instance.Actors.Values.Where(x =>
                    x.Type == Actor.Type.NPC ||
                    x.Type == Actor.Type.TreasureCoffer ||
                    x.Type == Actor.Type.EventObject);

                var addActors =
                    from x in actors
                    where
                    Settings.Instance.MobList.TargetMobList.ContainsKey(x.Name) &&
                    !combatants.Any(y => y.ID == x.ID)
                    select new Func<MobInfo>(() => new MobInfo()
                    {
                        Name = x.Name,
                        Combatant = x.ToCombatant(player),
                        Rank = Settings.Instance.MobList.TargetMobList[x.Name].Rank,
                        MaxDistance = Settings.Instance.MobList.TargetMobList[x.Name].MaxDistance,
                        TTSEnabled = Settings.Instance.MobList.TargetMobList[x.Name].TTSEnabled,
                    });

                targetDelegates = targetDelegates.Concat(addActors);

                // クエリを実行する
                targetDelegates = targetDelegates.ToList();
            });

            lock (this.TargetInfoLock)
            {
                this.targetMobList = targetDelegates
                    .Select(x => x.Invoke())
                    .Where(x => x.Distance <= x.MaxDistance)
                    .ToList();

                this.TargetInfo = this.targetMobList.FirstOrDefault()?.Combatant;

                if (this.TargetInfo == null)
                {
                    var model = this.Model as MobListModel;
                    if (model != null &&
                        model.MobList.Any())
                    {
                        WPFHelper.BeginInvoke(model.ClearMobList);
                    }
                }
            }

            if (combatants != null)
            {
                // 画面ダンプ用のCombatantsを更新する
                CombatantsViewModel.RefreshCombatants(combatants.Concat(
                    from x in actors
                    where
                    !combatants.Any(y => y.ID == x.ID)
                    select
                    x.ToCombatant(player)));
            }
        }

        protected override NameViewModel NameVM => null;

        protected override HPViewModel HpVM => null;

        protected override HPBarViewModel HpBarVM => null;

        protected override ActionViewModel ActionVM => null;

        protected override DistanceViewModel DistanceVM => null;

        protected override FFLogsViewModel FFLogsVM => null;

        protected override EnmityViewModel EnmityVM => null;

        #region MobList

        protected MobListView mobListView;

        public MobListView MobListView => this.mobListView;

        protected MobListViewModel mobListVM;

        protected MobListViewModel MobListVM =>
            this.mobListVM ?? (this.mobListVM = new MobListViewModel());

        #endregion MobList

        protected override bool IsAllViewOff =>
            !(Settings.Instance?.MobList?.Visible ?? false);

        protected override void CreateViews()
        {
            base.CreateViews();

            this.CreateView<MobListView>(ref this.mobListView, this.MobListVM);
            this.TryAddViewAndViewModel(this.MobListView, this.MobListView?.ViewModel);
        }

        protected override void RefreshModel(
            Combatant targetInfo)
        {
            base.RefreshModel(targetInfo);

            // MobListを更新する
            this.RefreshMobListView(targetInfo);
        }

        protected virtual void RefreshMobListView(
            Combatant targetInfo)
        {
            if (this.MobListView == null)
            {
                return;
            }

            if (!this.MobListView.ViewModel.OverlayVisible)
            {
                return;
            }

            var model = this.Model as MobListModel;
            if (model == null)
            {
                return;
            }

            // プレイヤー情報を更新する
            var player = FFXIVPlugin.Instance.GetPlayer();
            model.MeX = player?.PosX ?? 0;
            model.MeY = player?.PosY ?? 0;
            model.MeZ = player?.PosZ ?? 0;

            // モブリストを取り出す
            var moblist = default(IReadOnlyList<MobInfo>);
            lock (this.TargetInfoLock)
            {
                if (Settings.Instance.MobList.UngroupSameNameMobs)
                {
                    moblist = (
                        this.targetMobList.OrderBy(y => y.Distance)).ToList();
                }
                else
                {
                    moblist = (
                        from x in this.targetMobList.OrderBy(y => y.Distance)
                        group x by x.Name into g
                        select g.First().Clone((clone =>
                        {
                            clone.DuplicateCount = g.Count();
                        }))).ToList();
                }
            }

            // 表示件数によるフィルタをかけるメソッド
            void SortAndFilterMobList()
            {
                // ソートして表示を設定する
                var i = 0;
                var sorted =
                    from x in model.MobList
                    orderby
                    x.RankSortKey,
                    x.Distance
                    select new
                    {
                        Index = ++i,
                        MobInfo = x,
                    };

                foreach (var item in sorted)
                {
                    item.MobInfo.Visible = item.Index <= Settings.Instance.MobList.DisplayCount;
                    item.MobInfo.Index = item.MobInfo.Visible ?
                        item.Index :
                        9999;
                }
            }

            lock (model.MobList)
            {
                try
                {
                    // テストモード？
                    if (Settings.Instance.MobList.TestMode)
                    {
                        if (!model.MobList.Any(x => x.Name.Contains("TEST")))
                        {
                            model.MobList.Clear();

                            foreach (var mob in moblist)
                            {
                                model.MobList.Add(mob);
                            }

                            // ソートと件数のフィルタを行う
                            SortAndFilterMobList();
                        }

                        return;
                    }

                    // この先は本番モード
                    var isChanged = false;
                    var targets = moblist.Where(x => !x.Name.Contains("TEST"));

                    foreach (var mob in targets)
                    {
                        var item = model.MobList.FirstOrDefault(x =>
                            x.Combatant?.ID == mob.Combatant?.ID);

                        // 存在しないものは追加する
                        if (item == null)
                        {
                            model.MobList.Add(mob);

                            // TTSで通知する
                            mob.NotifyByTTS();

                            isChanged = true;
                            continue;
                        }

                        var distance = item.Distance;
                        var rankSortKey = item.RankSortKey;

                        // 更新する
                        item.Name = mob.Name;
                        item.Rank = mob.Rank;
                        item.Combatant = mob.Combatant;
                        item.DuplicateCount = mob.DuplicateCount;

                        if (distance != item.Distance ||
                            rankSortKey != item.RankSortKey)
                        {
                            isChanged = true;
                        }
                    }

                    // 不要になったモブを抽出する
                    var itemsForRemove = model.MobList
                        .Where(x =>
                            !targets.Any(y =>
                                y.Combatant?.ID == x.Combatant?.ID))
                        .ToArray();

                    // 除去する
                    foreach (var item in itemsForRemove)
                    {
                        model.MobList.Remove(item);
                        isChanged = true;
                    }

                    // ソートと件数のフィルタを行う
                    if (isChanged)
                    {
                        SortAndFilterMobList();
                    }
                }
                finally
                {
                    model.MobListCount = model.MobList.Count;
                }
            }
        }
    }
}
