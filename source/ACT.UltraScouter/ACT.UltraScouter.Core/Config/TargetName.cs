using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using Prism.Mvvm;

namespace ACT.UltraScouter.Config
{
    /// <summary>
    /// ターゲットの名前
    /// </summary>
    [Serializable]
    [DataContract(Namespace = "")]
    public class TargetName :
        BindableBase
    {
        [XmlIgnore]
        private bool visible;

        /// <summary>
        /// 表示テキスト
        /// </summary>
        [DataMember]
        public DisplayText DisplayText { get; set; } = new DisplayText();

        /// <summary>
        /// 場所
        /// </summary>
        [DataMember]
        public Location Location { get; set; } = new Location();

        /// <summary>
        /// 表示？
        /// </summary>
        [DataMember]
        public bool Visible
        {
            get => this.visible;
            set => this.SetProperty(ref this.visible, value);
        }
    }
}