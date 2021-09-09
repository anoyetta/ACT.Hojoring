using ACT.TTSYukkuri.SAPI5;
using Amazon;
using Amazon.Polly;
using Prism.Mvvm;
using System;
using System.Xml.Serialization;

namespace ACT.TTSYukkuri.Config
{
    public class PollyConfigs :
        BindableBase
    {
        private RegionEndpoint endpoint = RegionEndpoint.APNortheast1;
        private string accessKey = string.Empty;
        private string secretKey = string.Empty;
        private VoiceId voiceId = VoiceId.Mizuki;
        private Volumes volume = Volumes.Default;
        private Rates rate = Rates.Medium;
        private Pitches pitch = Pitches.Default;

        public string Region
        {
            get => this.endpoint.SystemName;
            set
            {
                var newEndpoint = RegionEndpoint.GetBySystemName(value);
                if (newEndpoint != null)
                {
                    if (this.SetProperty(ref this.endpoint, newEndpoint))
                    {
                        this.RaisePropertyChanged(nameof(this.Endpoint));
                    }
                }
            }
        }

        [XmlIgnore]
        public RegionEndpoint Endpoint => this.endpoint;

        public string AccessKey
        {
            get => this.accessKey;
            set => this.SetProperty(ref this.accessKey, value);
        }

        public string SecretKey
        {
            get => this.secretKey;
            set => this.SetProperty(ref this.secretKey, value);
        }

        public string Voice
        {
            get => this.voiceId.Value;
            set
            {
                var newVoiceId = VoiceId.FindValue(value);
                if (newVoiceId != null)
                {
                    if (this.SetProperty(ref this.voiceId, newVoiceId))
                    {
                        this.RaisePropertyChanged(nameof(this.VoiceId));
                    }
                }
            }
        }

        [XmlIgnore]
        public VoiceId VoiceId => this.voiceId;

        public Volumes Volume
        {
            get => this.volume;
            set => this.SetProperty(ref this.volume, value);
        }

        public Rates Rate
        {
            get => this.rate;
            set => this.SetProperty(ref this.rate, value);
        }

        public Pitches Pitch
        {
            get => this.pitch;
            set => this.SetProperty(ref this.pitch, value);
        }

        public override string ToString() =>
            $"{nameof(this.Voice)}:{this.Voice}," +
            $"{nameof(this.Rate)}:{this.Rate}," +
            $"{nameof(this.Volume)}:{this.Volume}," +
            $"{nameof(this.Pitch)}:{this.Pitch}";

        [Serializable]
        public class PollyVoice
        {
            [XmlAttribute(AttributeName = "Name")]
            public string Name { get; set; }

            [XmlAttribute(AttributeName = "Value")]
            public string Value { get; set; }
        }
    }
}
