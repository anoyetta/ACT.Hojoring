using System;
using Prism.Mvvm;
using VoiceTextWebAPI.Client;

namespace ACT.TTSYukkuri.Config
{
    [Serializable]
    public class HOYAConfig :
        BindableBase
    {
        private string apiKey;
        private Speaker speaker = Speaker.Hikari;
        private Emotion emotion = Emotion.Default;
        private EmotionLevel emotionLevel = EmotionLevel.Default;
        private int volume = 100;
        private int speed = 100;
        private int pitch = 100;

        public void SetDefault()
        {
            var defaultConfig = new HOYAConfig();
            this.Speaker = defaultConfig.Speaker;
            this.Emotion = defaultConfig.Emotion;
            this.EmotionLevel = defaultConfig.EmotionLevel;
            this.Volume = defaultConfig.Volume;
            this.Speed = defaultConfig.Speed;
            this.Pitch = defaultConfig.Pitch;
        }

        public string APIKey
        {
            get => this.apiKey;
            set => this.SetProperty(ref this.apiKey, value);
        }

        public Speaker Speaker
        {
            get => this.speaker;
            set => this.SetProperty(ref this.speaker, value);
        }

        public Emotion Emotion
        {
            get => this.emotion;
            set => this.SetProperty(ref this.emotion, value);
        }

        public EmotionLevel EmotionLevel
        {
            get => this.emotionLevel;
            set => this.SetProperty(ref this.emotionLevel, value);
        }

        // 50-200
        public int Volume
        {
            get => this.volume;
            set => this.SetProperty(ref this.volume, value);
        }

        // 50-400
        public int Speed
        {
            get => this.speed;
            set => this.SetProperty(ref this.speed, value);
        }

        // 50-200
        public int Pitch
        {
            get => this.pitch;
            set => this.SetProperty(ref this.pitch, value);
        }

        public override string ToString() =>
            $"{nameof(this.Speaker)}:{this.Speaker}," +
            $"{nameof(this.Emotion)}:{this.Emotion}," +
            $"{nameof(this.EmotionLevel)}:{this.EmotionLevel}," +
            $"{nameof(this.Volume)}:{this.Volume}," +
            $"{nameof(this.Speed)}:{this.Speed}," +
            $"{nameof(this.Pitch)}:{this.Pitch}";
    }
}
