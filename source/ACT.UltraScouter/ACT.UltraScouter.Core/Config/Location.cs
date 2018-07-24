using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;

using Prism.Mvvm;

namespace ACT.UltraScouter.Config
{
    /// <summary>
    /// 場所
    /// </summary>
    [Serializable]
    [DataContract(Namespace = "")]
    public class Location :
        BindableBase
    {
        [XmlIgnore]
        private double x = 0;

        [XmlIgnore]
        private double y = 0;

        /// <summary>
        /// X
        /// </summary>
        [XmlAttribute]
        [DataMember]
        public double X
        {
            get => this.x;
            set => SetProperty(ref this.x, Math.Round(value));
        }

        /// <summary>
        /// Y
        /// </summary>
        [XmlAttribute]
        [DataMember]
        public double Y
        {
            get => this.y;
            set => this.SetProperty(ref this.y, Math.Round(value));
        }
    }
}
