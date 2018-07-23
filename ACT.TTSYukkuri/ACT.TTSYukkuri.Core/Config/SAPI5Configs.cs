using ACT.TTSYukkuri.SAPI5;
using Prism.Mvvm;

namespace ACT.TTSYukkuri.Config
{
    public class SAPI5Configs :
        BindableBase
    {
        private string voiceID;
        private int rate;
        private int volume = 100;
        private Pitches pitch = Pitches.Default;

        public string VoiceID
        {
            get => this.voiceID;
            set => this.SetProperty(ref this.voiceID, value);
        }

        /// <summary>
        /// 読み上げ速さ -10 ～ 10
        /// </summary>
        public int Rate
        {
            get => this.rate;
            set => this.SetProperty(ref this.rate, value);
        }

        /// <summary>
        /// ボリューム 0 ～ 100
        /// </summary>
        public int Volume
        {
            get => this.volume;
            set => this.SetProperty(ref this.volume, value);
        }

        /// <summary>
        /// ピッチ
        /// </summary>
        public Pitches Pitch
        {
            get => this.pitch;
            set => this.SetProperty(ref this.pitch, value);
        }

        public override string ToString() =>
            $"{nameof(this.VoiceID)}:{this.VoiceID}," +
            $"{nameof(this.Rate)}:{this.Rate}," +
            $"{nameof(this.Volume)}:{this.Volume}," +
            $"{nameof(this.Pitch)}:{this.Pitch}";
    }
}
