using System;
using System.Runtime.Serialization;
using ACT.UltraScouter.Models.FFLogs;
using Prism.Mvvm;

namespace ACT.UltraScouter.Config
{
    [Serializable]
    [DataContract(Namespace = "")]
    public class FFLogs :
        BindableBase
    {
        [DataMember]
        public DisplayText DisplayText { get; set; } = new DisplayText();

        [DataMember]
        public Location Location { get; set; } = new Location();

        private bool visible;

        [DataMember]
        public bool Visible
        {
            get => this.visible;
            set => this.SetProperty(ref this.visible, value);
        }

        private string apiKey;

        [DataMember]
        public string ApiKey
        {
            get => this.apiKey;
            set => this.SetProperty(ref this.apiKey, value);
        }

        private FFLogsRegions serverRegion;

        [DataMember]
        public FFLogsRegions ServerRegion
        {
            get => this.serverRegion;
            set => this.SetProperty(ref this.serverRegion, value);
        }
    }
}
