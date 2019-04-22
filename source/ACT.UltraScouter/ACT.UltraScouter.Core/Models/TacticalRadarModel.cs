using System.Collections.Generic;
using Sharlayan.Core;

namespace ACT.UltraScouter.Models
{
    public class TacticalRadarModel :
        TargetInfoModel
    {
        public new static TacticalRadarModel Instance { get; } = new TacticalRadarModel();

        public List<ActorItem> TargetActors { get; } = new List<ActorItem>(8);

        public TacticalRadarModel()
        {
        }
    }
}
