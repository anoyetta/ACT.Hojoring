using System;
using System.Xml.Serialization;

namespace ACT.SpecialSpellTimer.Config.Models
{
    [Serializable]
    [XmlType(TypeName = "Item")]
    public class ExpandedContainer
    {
        public string Key { get; set; }
        public bool IsExpanded { get; set; }
    }
}
