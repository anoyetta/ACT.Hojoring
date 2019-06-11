using System;

namespace ACT.UltraScouter.Models
{
    public class CastingEventArgs : EventArgs
    {
        public TargetInfoModel Source { get; set; }
        public string Actor { get; set; }
        public DateTime CastingDateTime { get; set; }
        public float CastDurationCurrent { get; set; }
        public float CastDurationMax { get; set; }
        public string CastSkillName { get; set; }
        public uint CastSkillID { get; set; }
    }
}
