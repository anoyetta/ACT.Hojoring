using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Serialization;
using FFXIV.Framework.XIVHelper;

namespace ACT.SpecialSpellTimer.RaidTimeline
{
    [Serializable]
    [XmlType(TypeName = "dump")]
    public class TimelineDumpModel :
        TimelineBase
    {
        [XmlIgnore]
        public override TimelineElementTypes TimelineType => TimelineElementTypes.Dump;

        public override IList<TimelineBase> Children => null;

        private DumpTargets target = DumpTargets.Position;

        [XmlAttribute(AttributeName = "target")]
        public DumpTargets Target
        {
            get => this.target;
            set => this.SetProperty(ref this.target, value);
        }

        private string log;

        [XmlAttribute(AttributeName = "log")]
        public string Log
        {
            get => this.log;
            set => this.SetProperty(ref this.log, value);
        }

        public void ExcuteDump()
        {
            if (!this.Enabled.HasValue ||
                !this.Enabled.Value)
            {
                return;
            }

            switch (this.target)
            {
                case DumpTargets.Position:
                    this.ExecuteDumpPosition();
                    break;

                case DumpTargets.Log:
                    this.ExecuteDumpLog();
                    break;
            }
        }

        private async void ExecuteDumpPosition() => await Task.Run(() =>
        {
            var name = !string.IsNullOrEmpty(this.Name) ?
                this.Name :
                this.Parent?.Name;

            var from = string.IsNullOrEmpty(name) ?
                string.Empty :
                $@" from=""{this.Name}""";

            var combatants = CombatantsManager.Instance.GetCombatants();

            foreach (var c in combatants)
            {
                if (string.IsNullOrEmpty(c?.Name))
                {
                    continue;
                }

                var log = $@"Dump pos{from} id=""0x{c.ID:X8}"" name=""{c.Name}"" X=""{c.PosXMap:N2}"" Y=""{c.PosYMap:N2}"" Z=""{c.PosZMap:N2}"" hp=""{c.CurrentHP}"" max_hp=""{c.MaxHP}""";
                TimelineController.RaiseLog($"{TimelineConstants.LogSymbol} {log}");
            }
        });

        private async void ExecuteDumpLog() => await Task.Run(() =>
        {
            if (string.IsNullOrEmpty(this.Log))
            {
                return;
            }

            TimelineController.RaiseLog(this.Log);
        });
    }

    public enum DumpTargets
    {
        Position,
        Log
    }
}
