using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
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
        private static readonly string ContentStartLog = "の攻略を開始した。";

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
                    var inTankStanceNow = player.InTankStance();

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

                keySet.SendKey(times: 1, interval: 100);
            }
        });

        private volatile bool isZoneChanged;

        private bool WasZoneChanged(
            string logLine,
            out Match match)
        {
            match = null;
            this.isZoneChanged = false;

            if (!this.Config.RestoreTankStance.IsEnabled &&
                !this.Config.SummonFairy.IsEnabled &&
                !this.Config.SummonEgi.IsEnabled)
            {
                return false;
            }

            if (logLine.Contains(ChangedZoneLog))
            {
                this.isZoneChanged = true;
                return true;
            }

            if (logLine.Contains(ContentStartLog))
            {
                return true;
            }

            return false;
        }

        private void OnZoneChanged(
            string logLine,
            Match match) => Task.Run(async () =>
        {
            // wipeoutの検出からのディレイ を兼用する
            if (this.isZoneChanged)
            {
                await Task.Delay(TimeSpan.FromSeconds(this.Config.DelayFromWipeout));
            }

            var sendKeySetList = new List<KeyShortcut>();

            var player = CombatantsManager.Instance.Player;
            var playerEffects = player.Effects;

            // タンクスタンスを復元する
            if (this.Config.RestoreTankStance.IsEnabled &&
                this.Config.RestoreTankStance.KeySet.Key != Key.None &&
                this.Config.RestoreTankStance.IsSendOnZoneChanged)
            {
                // 自分がタンクかつ、タンクが自分のみ？
                if (player.Role == Roles.Tank)
                {
                    var party = CombatantsManager.Instance.GetPartyList();
                    if (party.Count(x => x.Role == Roles.Tank) <= 1)
                    {
                        if (!player.InTankStance())
                        {
                            sendKeySetList.Add(this.Config.RestoreTankStance.KeySet);
                        }
                    }
                }
            }

            // フェアリーを召喚する
            if (this.Config.SummonFairy.IsEnabled &&
                this.Config.SummonFairy.KeySet.Key != Key.None &&
                this.Config.SummonFairy.IsSendOnZoneChanged)
            {
                if (player.JobID == JobIDs.SCH)
                {
                    var combatants = CombatantsManager.Instance.GetCombatants();
                    if (!combatants.Any(x =>
                        x.OwnerID == player.ID))
                    {
                        sendKeySetList.Add(this.Config.SummonFairy.KeySet);
                    }
                }
            }

            // エギを召喚する
            if (this.Config.SummonEgi.IsEnabled &&
                this.Config.SummonEgi.KeySet.Key != Key.None &&
                this.Config.SummonEgi.IsSendOnZoneChanged)
            {
                if (player.JobID == JobIDs.SMN)
                {
                    var combatants = CombatantsManager.Instance.GetCombatants();
                    if (!combatants.Any(x =>
                        x.OwnerID == player.ID))
                    {
                        sendKeySetList.Add(this.Config.SummonEgi.KeySet);
                    }
                }
            }

            // キーを送る
            foreach (var keySet in sendKeySetList)
            {
                keySet.SendKey(times: 1, interval: 100);
                Thread.Sleep(TimeSpan.FromSeconds(4));
            }
        });

        private static readonly string[] EngageLogs = new string[]
        {
            "00:0039:戦闘開始",
            "00:0039:Engage!",
            "00:0039:전투 시작!",
            "00:0039:战斗开始！",
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

            this.inTankStance = player.InTankStance();
        }
    }
}
