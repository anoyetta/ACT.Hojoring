using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Xml.Serialization;

namespace FFXIV.Framework.Common
{
    [Serializable]
    [DataContract(Name = "Font", Namespace = "")]
    public class FontInfo :
        INotifyPropertyChanged,
        ICloneable
    {
        /// <summary>
        /// デフォルトのフォントファミリー名
        /// </summary>
        private const string DefaultFontFamilyName = "Arial";

        private static readonly object locker = new object();

        [XmlIgnore]
        public static readonly FontInfo DefaultFont = new FontInfo(
            new FontFamily(DefaultFontFamilyName),
            11,
            FontStyles.Normal,
            FontWeights.Normal,
            FontStretches.Normal);

        [XmlIgnore]
        private static readonly Dictionary<string, FontFamily> fontFamilyDictionary = new Dictionary<string, FontFamily>();

        [XmlIgnore]
        private static FontStretchConverter stretchConverter = new FontStretchConverter();

        [XmlIgnore]
        private static FontStyleConverter styleConverter = new FontStyleConverter();

        [XmlIgnore]
        private static FontWeightConverter weightConverter = new FontWeightConverter();

        public FontInfo()
        {
            RaiseOutlineThicknessChangedActions += () =>
            {
                this.RaisePropertyChanged(nameof(this.OutlineThickness));
                this.RaisePropertyChanged(nameof(this.BlurRadius));
            };
        }

        public FontInfo(
            string family) : this()
        {
            this.FontFamily = GetFontFamily(family);
        }

        public FontInfo(
            string family,
            double size) : this()
        {
            this.FontFamily = GetFontFamily(family);
            this.Size = size;
        }

        public FontInfo(
            string family,
            double size,
            string style,
            string weight,
            string stretch) : this()
        {
            this.FontFamily = GetFontFamily(family);
            this.Size = size;
            this.StyleText = style;
            this.WeightText = weight;
            this.StretchText = stretch;
        }

        public FontInfo(
            string family,
            double size,
            FontStyle style,
            FontWeight weight,
            FontStretch stretch) : this()
        {
            this.FontFamily = GetFontFamily(family);
            this.Size = size;
            this.Style = style;
            this.Weight = weight;
            this.Stretch = stretch;
        }

        public FontInfo(
            FontFamily family,
            double size,
            FontStyle style,
            FontWeight weight,
            FontStretch stretch) : this()
        {
            this.FontFamily = family;
            this.Size = size;
            this.Style = style;
            this.Weight = weight;
            this.Stretch = stretch;
        }

        [XmlIgnore]
        private FontFamily fontFamily = new FontFamily(DefaultFontFamilyName);

        [XmlIgnore]
        private double size = 12;

        [XmlIgnore]
        private FontStretch stretch = FontStretches.Normal;

        [XmlIgnore]
        private FontStyle style = FontStyles.Normal;

        [XmlIgnore]
        private FontWeight weight = FontWeights.Normal;

        /// <summary>Font Family</summary>
        [XmlIgnore]
        public FontFamily FontFamily
        {
            get => this.fontFamily;
            set => this.SetProperty(ref this.fontFamily, value);
        }

        [XmlAttribute("FontFamily")]
        [DataMember(Name = "FontFamily")]
        public string FontFamilyText
        {
            get => this.FontFamily?.Source;
            set => this.FontFamily = new FontFamily(value ?? DefaultFontFamilyName);
        }

        /// <summary>Font Size</summary>
        [XmlAttribute]
        [DataMember]
        public double Size
        {
            get => this.size;
            set
            {
                if (this.SetProperty(ref this.size, value))
                {
                    this.RaisePropertyChanged(nameof(this.OutlineThickness));
                }
            }
        }

        /// <summary>Font Stretch</summary>
        [XmlIgnore]
        public FontStretch Stretch
        {
            get => this.stretch;
            set => this.SetProperty(ref this.stretch, value);
        }

        /// <summary>Font Stretch (シリアル化向け)</summary>
        [XmlAttribute(AttributeName = "Stretch")]
        [DataMember(Name = "Stretch")]
        public string StretchText
        {
            get => stretchConverter.ConvertToString(this.Stretch);
            set => this.Stretch = (FontStretch)stretchConverter.ConvertFromString(value);
        }

        /// <summary>Font Style</summary>
        [XmlIgnore]
        public FontStyle Style
        {
            get => this.style;
            set => this.SetProperty(ref this.style, value);
        }

        /// <summary>Font Style (シリアル化向け)</summary>
        [XmlAttribute(AttributeName = "Style")]
        [DataMember(Name = "Style")]
        public string StyleText
        {
            get => styleConverter.ConvertToString(this.Style);
            set => this.Style = (FontStyle)styleConverter.ConvertFromString(value);
        }

        /// <summary>Typeface</summary>
        public FamilyTypeface Typeface => new FamilyTypeface()
        {
            Stretch = this.Stretch,
            Weight = this.Weight,
            Style = this.Style
        };

        /// <summary>Font Weight</summary>
        [XmlIgnore]
        public FontWeight Weight
        {
            get => this.weight;
            set
            {
                if (this.SetProperty(ref this.weight, value))
                {
                    this.RaisePropertyChanged(nameof(this.OutlineThickness));
                }
            }
        }

        /// <summary>Font Weight (シリアル化向け)</summary>
        [XmlAttribute(AttributeName = "Weight")]
        [DataMember(Name = "Weight")]
        public string WeightText
        {
            get => weightConverter.ConvertToString(this.Weight);
            set => this.Weight = (FontWeight)weightConverter.ConvertFromString(value);
        }

        private static Action RaiseOutlineThicknessChangedActions;

        private static void RaiseOutlineThicknessChanged()
        {
            RaiseOutlineThicknessChangedActions?.Invoke();
        }

        private static double outlineThicknessRate = 1.0d;

        public static double OutlineThicknessRate
        {
            get => outlineThicknessRate;
            set
            {
                if (outlineThicknessRate != value)
                {
                    outlineThicknessRate = value;
                    RaiseOutlineThicknessChanged();
                }
            }
        }

        private static double blurRadiusRate = 1.0d;

        public static double BlurRadiusRate
        {
            get => blurRadiusRate;
            set
            {
                if (blurRadiusRate != value)
                {
                    blurRadiusRate = value;
                    RaiseOutlineThicknessChanged();
                }
            }
        }

        /// <summary>
        /// アウトラインの太さ
        /// </summary>
        [XmlIgnore]
        public double OutlineThickness
        {
            get
            {
                // 基準の太さ
                var thickness = 1.1d;

                // フォントサイズを基準に補正をかける
                thickness *= this.Size / 11.0d;

                // ウェイトによる補正をかける
                thickness *=
                    this.Weight.ToOpenTypeWeight() /
                    FontWeights.Normal.ToOpenTypeWeight();

                // 設定によって増幅させる
                thickness *= OutlineThicknessRate;

                if (OutlineThicknessRate != 0)
                {
                    if (thickness < 1)
                    {
                        thickness = 1;
                    }
                }

                return Math.Round(thickness, 2);
            }
        }

        /// <summary>
        /// ブラー半径
        /// </summary>
        [XmlIgnore]
        public double BlurRadius
        {
            get
            {
                // アウトラインの太さを基準にして増幅する
                // 増幅率は一応設定可能とする
                var textBlurGain = 2.0d;
#if DEBUG
                if (WPFHelper.IsDesignMode)
                {
                    return this.OutlineThickness * textBlurGain;
                }
#endif
                textBlurGain = BlurRadiusRate;

                return Math.Round(this.OutlineThickness * textBlurGain, 2);
            }
        }

        public static FontInfo FromString(
            string json)
        {
            var obj = default(FontInfo);

            var serializer = new DataContractJsonSerializer(typeof(FontInfo));
            var data = Encoding.UTF8.GetBytes(json);
            using (var ms = new MemoryStream(data))
            {
                obj = (FontInfo)serializer.ReadObject(ms);
            }

            return obj;
        }

        public override string ToString()
        {
            var json = string.Empty;

            var serializer = new DataContractJsonSerializer(typeof(FontInfo));
            using (var ms = new MemoryStream())
            {
                serializer.WriteObject(ms, this);
                json = Encoding.UTF8.GetString(ms.ToArray());
            }

            return json;
        }

        private static FontFamily GetFontFamily(
            string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                return new FontFamily();
            }

            lock (locker)
            {
                if (!fontFamilyDictionary.ContainsKey(source))
                {
                    fontFamilyDictionary[source] = new FontFamily(source);
                }

                return fontFamilyDictionary[source];
            }
        }

        public System.Drawing.Font ToFontForWindowsForm()
        {
            System.Drawing.FontStyle style = System.Drawing.FontStyle.Regular;

            if (this.Style == FontStyles.Italic ||
                this.Style == FontStyles.Oblique)
            {
                style |= System.Drawing.FontStyle.Italic;
            }

            if (this.Weight > FontWeights.Normal)
            {
                style |= System.Drawing.FontStyle.Bold;
            }

            System.Drawing.Font f = new System.Drawing.Font(
                this.FontFamily.Source,
                (float)this.Size,
                style);

            return f;
        }

        public string DisplayText
        {
            get
            {
                var t = string.Empty;

                t += this.FontFamilyText + ", ";
                t += this.Size + ", ";
                t += this.StyleText + "-";
                t += this.WeightText + "-";
                t += this.StretchText;

                return t;
            }
        }

        #region INotifyPropertyChanged

        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(
            [CallerMemberName]string propertyName = null)
        {
            this.PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }

        protected virtual bool SetProperty<T>(
            ref T field,
            T value,
            [CallerMemberName]string propertyName = null)
        {
            if (Equals(field, value))
            {
                return false;
            }

            field = value;
            this.PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));

            return true;
        }

        #endregion INotifyPropertyChanged

        public object Clone() => this.MemberwiseClone();
    }
}
