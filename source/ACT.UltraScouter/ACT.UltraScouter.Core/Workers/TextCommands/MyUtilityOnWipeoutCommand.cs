using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ACT.UltraScouter.Config;
using FFXIV.Framework.Bridge;
using FFXIV.Framework.Common;
using FFXIV.Framework.XIVHelper;

namespace ACT.UltraScouter.Workers.TextCommands
{
    public class MyUtilityOnWipeoutCommand
    {
        #region Lazy Singleton

        private static readonly Lazy<MyUtilityOnWipeoutCommand> LazyInstance
            = new Lazy<MyUtilityOnWipeoutCommand>(() => new MyUtilityOnWipeoutCommand());

        public static MyUtilityOnWipeoutCommand Instance => LazyInstance.Value;

        #endregion Lazy Singleton

        private static readonly string WipeoutLog = "wipeout";
        private static readonly string ChangedZoneLog = "01:Changed Zone to";

        public MyUtility Config => Settings.Instance.MyUtility;

        public void Subscribe()
        {
            TextCommandBridge.Instance.Subscribe(
                new TextCommand(this.WasWipeout, this.OnWipeout) { IsSilent = true });

            TextCommandBridge.Instance.Subscribe(
                new TextCommand(this.WasZoneChanged, this.OnZoneChanged) { IsSilent = true });
        }

        private bool WasWipeout(
            string logLine,
            out Match match)
        {
            match = null;

            if (!this.Config.ExtendMealEffect.IsEnabled &&
                !this.Config.RestoreTankStance.IsEnabled &&
                !this.Config.SummonFairy.IsEnabled &&
                !this.Config.DrawCard.IsEnabled &&
                !this.Config.SummonEgi.IsEnabled)
            {
                return false;
            }

            this.StoreTankStance(logLine);

            return logLine.Contains(WipeoutLog);
        }

        private void OnWipeout(
            string logLine,
            Match match) => Task.Run(async () =>
        {
            // wipeoutの検出からのディレイ
            await Task.Delay(TimeSpan.FromSeconds(this.Config.DelayFromWipeout));

            var sendKeySetList = new List<KeyShortcut>();

            var player = CombatantsManager.Instance.Player;
            var playerEffects = player.Effects;
            var partyCount = CombatantsManager.Instance.PartyCount;

            // タンクスタンスを復元する
            if (this.Config.RestoreTankStance.IsAvailable())
            {
                if (player.Role == Roles.Tank &&
                    this.inTankStance.HasValue)
                {
                    var inTankStanceNow = playerEffects.Any(x =>
                        x != null &&
                        TankStanceEffectIDs.Contains(x.BuffID));

                    if (this.inTankStance.Value != inTankStanceNow)
                    {
                        sendKeySetList.Add(this.Config.RestoreTankStance.KeySet);
                    }
                }
            }

            // フェアリーを召喚する
            if (this.Config.SummonFairy.IsAvailable())
            {
                if (player.JobID == JobIDs.SCH)
                {
                    sendKeySetList.Add(this.Config.SummonFairy.KeySet);
                }
            }

            // カードをドローする
            if (this.Config.DrawCard.IsAvailable())
            {
                if (player.JobID == JobIDs.AST)
                {
                    sendKeySetList.Add(this.Config.DrawCard.KeySet);
                }
            }

            // エギを召喚する
            if (this.Config.SummonEgi.IsAvailable())
            {
                if (player.JobID == JobIDs.SMN)
                {
                    sendKeySetList.Add(this.Config.SummonEgi.KeySet);
                }
            }

            // 食事効果を延長する
            if (this.Config.ExtendMealEffect.IsAvailable())
            {
                var remainOfWellFed = playerEffects.FirstOrDefault(x =>
                    x != null &&
                    x.BuffID == WellFedEffectID)?.Timer ?? 0;

                if (0 < remainOfWellFed && remainOfWellFed < (this.Config.ExtendMealEffect.RemainingTimeThreshold * 60))
                {
                    sendKeySetList.Add(this.Config.ExtendMealEffect.KeySet);
                }
            }

            // キーを送る
            var isFirst = true;
            foreach (var keySet in sendKeySetList)
            {
                if (!isFirst)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(3));
                    isFirst = false;
                }

                keySet.SendKey(times: 3, interval: 100);
            }
        });

        private bool WasZoneChanged(
            string logLine,
            out Match match)
        {
            match = null;

            if (!this.Config.RestoreTankStance.IsEnabled &&
                !this.Config.SummonFairy.IsEnabled &&
                !this.Config.SummonEgi.IsEnabled)
            {
                return false;
            }

            return logLine.Contains(ChangedZoneLog);
        }

        private void OnZoneChanged(
            string logLine,
            Match match) => Task.Run(async () =>
        {
            // wipeoutの検出からのディレイ を兼用する
            await Task.Delay(TimeSpan.FromSeconds(this.Config.DelayFromWipeout));

            var sendKeySetList = new List<KeyShortcut>();

            var player = CombatantsManager.Instance.Player;
            var playerEffects = player.Effects;
            var party = CombatantsManager.Instance.GetPartyList();

            // タンクスタンスを復元する
            if (this.Config.RestoreTankStance.IsAvailable())
            {
                // 自分がタンクかつ、タンクが自分のみ？
                if (player.Role == Roles.Tank &&
                    party.Count(x => x.Role == Roles.Tank) <= 1)
                {
                    var inTankStanceNow = playerEffects.Any(x =>
                        x != null &&
                        TankStanceEffectIDs.Contains(x.BuffID));

                    if (!inTankStanceNow)
                    {
                        sendKeySetList.Add(this.Config.RestoreTankStance.KeySet);
                    }
                }
            }

            // フェアリーを召喚する
            if (this.Config.SummonFairy.IsAvailable())
            {
                if (player.JobID == JobIDs.SCH &&
                    party.Count(x =>
                        x.Role == Roles.PetsEgi &&
                        x.OwnerID == player.ID) < 1)
                {
                    sendKeySetList.Add(this.Config.SummonFairy.KeySet);
                }
            }

            // エギを召喚する
            if (this.Config.SummonEgi.IsAvailable())
            {
                if (player.JobID == JobIDs.SMN &&
                    party.Count(x =>
                        x.Role == Roles.PetsEgi &&
                        x.OwnerID == player.ID) < 1)
                {
                    sendKeySetList.Add(this.Config.SummonEgi.KeySet);
                }
            }

            // キーを送る
            var isFirst = true;
            foreach (var keySet in sendKeySetList)
            {
                if (!isFirst)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(3));
                    isFirst = false;
                }

                keySet.SendKey(times: 3, interval: 100);
            }
        });

        private static readonly string[] EngageLogs = new string[]
        {
            "00:0039:戦闘開始",
            "00:0039:Engage!",
            "00:0039:전투 시작!",
            "00:0039:战斗开始！",
        };

        private static readonly uint[] TankStanceEffectIDs = new uint[]
        {
            91,     // ディフェンダー
            1833,   // ロイヤルガード
            79,     // アイアンウィル
            743,    // グリットスタンス
        };

        /// <summary>食事効果のエフェクトID</summary>
        private static readonly uint WellFedEffectID = 48;

        private bool? inTankStance;

        private void StoreTankStance(
            string logLine)
        {
            if (!this.Config.RestoreTankStance.IsEnabled)
            {
                return;
            }

            if (!EngageLogs.Any(x => logLine.Contains(x)))
            {
                return;
            }

            var player = CombatantsManager.Instance.Player;
            var playerEffects = player.Effects;

            if (player.Role != Roles.Tank)
            {
                return;
            }

            this.inTankStance = playerEffects.Any(x =>
                x != null &&
                TankStanceEffectIDs.Contains(x.BuffID));
        }
    }
}
