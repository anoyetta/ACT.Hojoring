using System;
using System.Xml.Serialization;

namespace ACT.SpecialSpellTimer.Config
{
    [Serializable]
    public class SerializableSize
    {
        [XmlAttribute]
        public int Height { get; set; }

        [XmlAttribute]
        public int Width { get; set; }
    }
}