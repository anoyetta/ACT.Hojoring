using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using FFXIV.Framework.FFXIVHelper;
using Prism.Mvvm;

namespace ACT.UltraScouter.Config
{
    [Serializable]
    [DataContract(Namespace = "")]
    public class JobAvailablity :
        BindableBase
    {
        private JobIDs job;
        private bool available;

        /// <summary>
        /// ジョブ
        /// </summary>
        [DataMember]
        [XmlAttribute]
        public JobIDs Job
        {
            get => this.job;
            set => this.SetProperty(ref this.job, value);
        }

        /// <summary>
        /// 有効性
        /// </summary>
        [DataMember]
        [XmlAttribute]
        public bool Available
        {
            get => this.available;
            set => this.SetProperty(ref this.available, value);
        }

        /// <summary>
        /// 表示用のジョブ名
        /// </summary>
        [XmlIgnore]
        public string JobName => $"[{this.job.ToString()}] {Jobs.Find(this.job).NameEN}";
    }
}
