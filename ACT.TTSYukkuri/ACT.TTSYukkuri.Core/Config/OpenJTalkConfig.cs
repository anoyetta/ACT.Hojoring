using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using Prism.Mvvm;

namespace ACT.TTSYukkuri.Config
{
    [Serializable]
    public class OpenJTalkConfig :
        BindableBase
    {
        private const string VoiceMeiNormal = "mei_normal.htsvoice";

        private string voice = VoiceMeiNormal;
        private float gain = 1.0f;
        private float volume = 1.0f;
        private float allpass = 0.5f;
        private float postfilter = 0.0f;
        private float rate = 1.0f;
        private float halftone = 0.0f;
        private float unvoice = 0.5f;
        private float accent = 1.0f;
        private float weight = 1.0f;

        public OpenJTalkConfig()
        {
            this.SetRecommend();
        }

        /// <summary>
        /// htvoiceの種類
        /// </summary>
        public string Voice
        {
            get => this.voice;
            set => this.SetProperty(ref this.voice, value);
        }

        /// <summary>
        /// WAVEファイルの増幅率
        /// </summary>
        public float Gain
        {
            get => this.gain;
            set => this.SetProperty(ref this.gain, (float)Math.Round(value, 2));
        }

        /// <summary>
        /// -g 音量 0 ～ 6.0
        /// </summary>
        public float Volume
        {
            get => this.volume;
            set => this.SetProperty(ref this.volume, (float)Math.Round(value, 2));
        }

        /// <summary>
        /// -a オールパス値 0 ～ 1.0
        /// </summary>
        /// <remarks>
        /// 声質
        /// 0.5以上 → 低い声
        /// 0.5以下 → 高い声</remarks>
        public float AllPass
        {
            get => this.allpass;
            set => this.SetProperty(ref this.allpass, (float)Math.Round(value, 2));
        }

        /// <summary>
        /// -b ポストフィルタ係数 0 ～ 1.0
        /// </summary>
        /// <remarks>
        /// 揺らぎ</remarks>
        public float PostFilter
        {
            get => this.postfilter;
            set => this.SetProperty(ref this.postfilter, (float)Math.Round(value, 2));
        }

        /// <summary>
        /// -r スピーチ速度係数 0 ～ 2.0
        /// </summary>
        /// <remarks>
        /// スピーチの速度</remarks>
        public float Rate
        {
            get => this.rate;
            set => this.SetProperty(ref this.rate, (float)Math.Round(value, 2));
        }

        /// <summary>
        /// -fm 追加ハーフトーン -20.0 ～ +20.0
        /// </summary>
        /// <remarks>
        /// 声の高さ</remarks>
        public float HalfTone
        {
            get => this.halftone;
            set => this.SetProperty(ref this.halftone, (float)Math.Round(value, 2));
        }

        /// <summary>
        /// -u 音声/無音声の閾値 0 ～ 1
        /// </summary>
        public float UnVoice
        {
            get => this.unvoice;
            set => this.SetProperty(ref this.unvoice, (float)Math.Round(value, 2));
        }

        /// <summary>
        /// -jm スペクトラム系列内変動の重み 0 ～ 2.0
        /// </summary>
        /// <remarks>
        /// いわゆる抑揚</remarks>
        public float Accent
        {
            get => this.accent;
            set => this.SetProperty(ref this.accent, (float)Math.Round(value, 2));
        }

        /// <summary>
        /// -jf F0系列内変動の重み 0 ～ 2.0
        /// </summary>
        /// <remarks>
        /// 声の大きさに影響する</remarks>
        public float Weight
        {
            get => this.weight;
            set => this.SetProperty(ref this.weight, (float)Math.Round(value, 2));
        }

        /// <summary>
        /// 推奨値を設定する
        /// </summary>
        public void SetRecommend()
        {
            this.Voice = VoiceMeiNormal;
            this.Gain = 1.0f;
            this.Volume = 3.0f;
            this.AllPass = 0.53f;
            this.PostFilter = 0.3f;
            this.Rate = 1.4f;
            this.HalfTone = 1.0f;
            this.UnVoice = 0.0f;
            this.Accent = 0.7f;
            this.Weight = 0.1f;
        }

        [XmlIgnore]
        public string OpenJTalkDirectory
        {
            get
            {
                // ACTのパスを取得する
                var asm = Assembly.GetEntryAssembly();
                if (asm != null)
                {
                    var actDirectory = Path.GetDirectoryName(asm.Location);
                    var resourcesUnderAct = Path.Combine(actDirectory, @"OpenJTalk");

                    if (Directory.Exists(resourcesUnderAct))
                    {
                        return resourcesUnderAct;
                    }
                }

                // 自身の場所を取得する
                var selfDirectory = PluginCore.Instance.PluginDirectory ?? string.Empty;
                var resourcesUnderThis = Path.Combine(selfDirectory, @"OpenJTalk");

                if (Directory.Exists(resourcesUnderThis))
                {
                    return resourcesUnderThis;
                }

                return string.Empty;
            }
        }

        public OpenJTalkVoice[] EnumerateVoice()
        {
            var list = new List<OpenJTalkVoice>();

            var openTalk = this.OpenJTalkDirectory;

            if (string.IsNullOrWhiteSpace(openTalk))
            {
                return list.ToArray();
            }

            var voice = Path.Combine(
                openTalk,
                "voice");

            if (Directory.Exists(voice))
            {
                foreach (var item in Directory.GetFiles(voice, "*.htsvoice")
                    .OrderBy(x => x)
                    .ToArray())
                {
                    list.Add(new OpenJTalkVoice()
                    {
                        File = item
                    });
                }
            }

            return list.ToArray();
        }

        public override string ToString() =>
            $"{nameof(this.Voice)}:{this.Voice}," +
            $"{nameof(this.Volume)}:{this.Volume}," +
            $"{nameof(this.AllPass)}:{this.AllPass}," +
            $"{nameof(this.PostFilter)}:{this.PostFilter}," +
            $"{nameof(this.Rate)}:{this.Rate}," +
            $"{nameof(this.HalfTone)}:{this.HalfTone}," +
            $"{nameof(this.UnVoice)}:{this.UnVoice}," +
            $"{nameof(this.Accent)}:{this.Accent}," +
            $"{nameof(this.Weight)}:{this.Weight}";
    }

    [Serializable]
    public class OpenJTalkVoice
    {
        public string Value => Path.GetFileName(this.File);

        public string Name => Path.GetFileName(this.File);

        public string File { get; set; }

        public override string ToString() => this.Name;
    }
}
