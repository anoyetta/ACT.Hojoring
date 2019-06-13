using System.Collections.Generic;
using FFXIV.Framework.FFXIVHelper;

namespace ACT.UltraScouter.Models
{
    public class TacticalRadarModel :
        TargetInfoModel
    {
        public new static TacticalRadarModel Instance { get; } = new TacticalRadarModel();

        public List<CombatantEx> TargetActors { get; } = new List<CombatantEx>(8);

        public TacticalRadarModel()
        {
        }
    }
}
