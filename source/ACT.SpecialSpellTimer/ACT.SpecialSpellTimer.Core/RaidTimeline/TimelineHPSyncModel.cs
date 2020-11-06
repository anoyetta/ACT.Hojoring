using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using FFXIV.Framework.XIVHelper;

namespace ACT.SpecialSpellTimer.RaidTimeline
{
    [Serializable]
    [XmlType(TypeName = "hp-sync")]
    public class TimelineHPSyncModel :
        TimelineBase
    {
        [XmlIgnore]
        public override TimelineElementTypes TimelineType => TimelineElementTypes.HPSync;

        public override IList<TimelineBase> Children => null;

        [XmlAttribute(AttributeName = "name")]
        public override string Name
        {
            get => this.name;
            set
            {
                if (this.SetProperty(ref this.name, value))
                {
                    if (string.IsNullOrEmpty(this.name))
                    {
                        this.NameRegex = null;
                    }
                    else
                    {
                        this.NameRegex = new Regex(
                            this.name,
                            RegexOptions.Compiled);
                    }
                }
            }
        }

        private Regex nameRegex = null;

        [XmlIgnore]
        public Regex NameRegex
        {
            get => this.nameRegex;
            private set => this.SetProperty(ref this.nameRegex, value);
        }

        private double? hpp = null;

        [XmlIgnore]
        public double? HPP
        {
            get => this.hpp;
            set => this.SetProperty(ref this.hpp, value);
        }

        [XmlAttribute(AttributeName = "hpp")]
        public string HPPXML
        {
            get => this.HPP?.ToString();
            set => this.HPP = double.TryParse(value, out var v) ? v : (double?)null;
        }

        [XmlIgnore]
        public bool IsSynced { get; set; }

        public bool IsMatch(
            IEnumerable<CombatantEx> combatants)
        {
            if (!this.hpp.HasValue)
            {
                return false;
            }

            if (this.IsSynced)
            {
                return false;
            }

            var hpp = this.hpp / 100;

            var targets =
                from x in combatants
                where
                x.MaxHP > 0 &&
                x.CurrentHP > 0 &&
                x.CurrentHP < x.MaxHP &&
                x.CurrentHPRate <= hpp &&
                !string.IsNullOrEmpty(x.Name) &&
                (this.nameRegex?.IsMatch(x.Name) ?? false)
                select
                x;

            return targets.Any();
        }
    }
}
