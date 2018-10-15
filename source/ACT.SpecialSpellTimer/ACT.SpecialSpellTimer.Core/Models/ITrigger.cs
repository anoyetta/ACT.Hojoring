using System;
using System.Collections.Generic;
using System.Linq;
using ACT.SpecialSpellTimer.Config.Models;
using FFXIV.Framework.FFXIVHelper;

namespace ACT.SpecialSpellTimer.Models
{
    public interface ITrigger
    {
        ItemTypes ItemType { get; }

        void MatchTrigger(string logLine);
    }

    public interface IFilterizableTrigger : ITrigger
    {
        string JobFilter { get; set; }

        string PartyJobFilter { get; set; }

        string ZoneFilter { get; set; }
    }

    public static class TriggerExtensions
    {
        public static Guid GetID(
            this ITrigger t)
        {
            switch (t)
            {
                case SpellPanel p:
                    return p.ID;

                case Spell s:
                    return s.Guid;

                case Ticker ti:
                    return ti.Guid;

                case Tag tag:
                    return tag.ID;

                default:
                    return Guid.Empty;
            }
        }

        public static bool PredicateFilters(
            this IFilterizableTrigger trigger,
            Combatant player,
            IEnumerable<Combatant> partyList,
            int? currentZoneID)
        {
            // パーティリストからプレイヤーを除外する
            var combatants = partyList?.Where(x => x.ID != (player?.ID ?? 0));

            var enabledByJob = false;
            var enabledByPartyJob = false;
            var enabledByZone = false;

            // ジョブフィルタをかける
            if (string.IsNullOrEmpty(trigger.JobFilter))
            {
                enabledByJob = true;
            }
            else
            {
                var jobs = trigger.JobFilter.Split(',');
                if (jobs.Any(x => x == player.Job.ToString()))
                {
                    enabledByJob = true;
                }
            }

            // filter by specific jobs in party
            if (string.IsNullOrEmpty(trigger.PartyJobFilter))
            {
                enabledByPartyJob = true;
            }
            else
            {
                if (combatants != null)
                {
                    var jobs = trigger.PartyJobFilter.Split(',');
                    foreach (var combatant in combatants)
                    {
                        if (jobs.Contains(combatant.Job.ToString()))
                        {
                            enabledByPartyJob = true;
                        }
                    }
                }
            }

            // ゾーンフィルタをかける
            if (string.IsNullOrEmpty(trigger.ZoneFilter))
            {
                enabledByZone = true;
            }
            else
            {
                if (currentZoneID.HasValue)
                {
                    var zoneIDs = trigger.ZoneFilter.Split(',');
                    if (zoneIDs.Any(x => x == currentZoneID.ToString()))
                    {
                        enabledByZone = true;
                    }
                }
            }

            return enabledByJob && enabledByZone && enabledByPartyJob;
        }
    }
}
