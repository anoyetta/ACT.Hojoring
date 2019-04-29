using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using ACT.SpecialSpellTimer.Config.Models;
using FFXIV.Framework.Common;
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

        string PartyCompositionFilter { get; set; }

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
            var enabledByPartyComposition = false;

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

            // パーティ構成によるフィルタをかける
            if (string.IsNullOrEmpty(trigger.PartyCompositionFilter))
            {
                enabledByPartyComposition = true;
            }
            else
            {
                var currentPartyComposition = FFXIVPlugin.Instance.PartyComposition.ToString();
                var filters = trigger.PartyCompositionFilter.Split(',');
                if (filters.Any(x =>
                    string.Equals(
                        x,
                        currentPartyComposition,
                        StringComparison.OrdinalIgnoreCase)))
                {
                    enabledByPartyComposition = true;
                }
            }

            return enabledByJob && enabledByZone && enabledByPartyJob && enabledByPartyComposition;
        }

        /// <summary>
        /// XML化する
        /// </summary>
        /// <param name="t"></param>
        /// <returns>XML</returns>
        public static Task<string> ToXMLAsync(
            this ITrigger t)
            => Task.Run(() =>
            {
                if (t == null)
                {
                    return string.Empty;
                }

                var xws = new XmlWriterSettings()
                {
                    OmitXmlDeclaration = true,
                    Indent = true,
                };

                var ns = new XmlSerializerNamespaces();
                ns.Add(string.Empty, string.Empty);

                var sb = new StringBuilder();
                using (var xw = XmlWriter.Create(sb, xws))
                {
                    var xs = new XmlSerializer(t.GetType());
                    WPFHelper.Invoke(() => xs.Serialize(xw, t, ns));
                }

                sb.Replace("utf-16", "utf-8");

                return sb.ToString() + Environment.NewLine;
            });

        /// <summary>
        /// XMLからオブジェクト化する
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="xml">XML</param>
        /// <returns>オブジェクト</returns>
        public static Task<T> FromXMLAsync<T>(
            this T t,
            string xml) where T : class, ITrigger
            => Task.Run(() =>
            {
                if (string.IsNullOrEmpty(xml))
                {
                    return null;
                }

                var obj = default(T);

                using (var sr = new StringReader(xml))
                using (var xr = XmlReader.Create(sr))
                {
                    var xs = new XmlSerializer(typeof(T));
                    WPFHelper.Invoke(() =>
                    {
                        obj = xs.Deserialize(xr) as T;
                    });
                }

                return obj;
            });

        /// <summary>
        /// インポートしないプロパティのリスト
        /// </summary>
        private static readonly string[] ImportIgnoreProperties = new string[]
        {
            nameof(Spell.ID),
            nameof(Spell.Guid),
            nameof(Spell.Panel),
            nameof(Spell.PanelID),
            nameof(Spell.PanelName),
            nameof(Spell.SortPriority),
            nameof(Spell.IsChecked),
            nameof(Spell.IsExpanded),
            nameof(Spell.IsSelected),
            nameof(Spell.IsDesignMode),
            nameof(Spell.Enabled),
        };

        /// <summary>
        /// プロパティを取り込む
        /// </summary>
        /// <param name="t"></param>
        /// <param name="source">取り込む元のオブジェクト</param>
        public static async void ImportProperties(
            this ITrigger t,
            ITrigger source)
        {
            if (source == null ||
                t == null)
            {
                return;
            }

            var properties = source.GetType().GetProperties(
                BindingFlags.Public |
                BindingFlags.Instance)
                .Where(x =>
                    x.CanRead &&
                    x.CanWrite);

            await WPFHelper.InvokeAsync(() =>
            {
                foreach (var pi in properties)
                {
                    if (t.GetType().GetProperty(pi.Name) == null ||
                        ImportIgnoreProperties.Contains(pi.Name))
                    {
                        continue;
                    }

                    var attrs = pi.GetCustomAttributes(true);
                    if (attrs.Any(a => a is XmlIgnoreAttribute))
                    {
                        continue;
                    }

                    pi.SetValue(t, pi.GetValue(source));
                    Thread.Yield();

                    (t as TreeItemBase)?.ExecuteRaisePropertyChanged(pi.Name);
                    Thread.Yield();
                }

                switch (t)
                {
                    case Spell spell:
                        spell.Enabled = true;
                        break;

                    case Ticker ticker:
                        ticker.Enabled = true;
                        break;
                }
            });
        }
    }
}
