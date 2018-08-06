using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Xml.Serialization;
using FFXIV.Framework.Common;
using FFXIV.Framework.Globalization;
using Microsoft.VisualBasic.FileIO;
using NLog;
using Prism.Mvvm;

namespace ACT.UltraScouter.Config
{
    /// <summary>
    /// 方角表示の基準
    /// </summary>
    public enum DirectionOrigin
    {
        /// <summary>
        /// 常に北を基準にする
        /// </summary>
        North = 0,

        /// <summary>
        /// プレイヤーの頭の向きを基準にする
        /// </summary>
        Me,

        /// <summary>
        /// カメラを基準にする
        /// </summary>
        Camera
    }

    [Serializable]
    [DataContract(Namespace = "")]
    public class MobList :
        BindableBase
    {
        #region Logger

        private Logger logger = AppLog.DefaultLogger;

        #endregion Logger

        [XmlIgnore] private bool visible = false;
        [XmlIgnore] private double refreshRateMin = 300;
        [XmlIgnore] private bool testMode = false;
        [XmlIgnore] private bool visibleZ = true;
        [XmlIgnore] private bool visibleMe = true;
        [XmlIgnore] private DirectionOrigin directionOrigin = DirectionOrigin.North;
        [XmlIgnore] private double directionAdjustmentAngle = 0;
        [XmlIgnore] private bool isSimple = false;
        [XmlIgnore] private int displayCount = 10;
        [XmlIgnore] private double nearDistance = 20;
        [XmlIgnore] private bool ttsEnabled = false;
        [XmlIgnore] private bool dumpCombatants = false;

        /// <summary>
        /// 表示？
        /// </summary>
        [DataMember]
        public bool Visible
        {
            get => this.visible;
            set => this.SetProperty(ref this.visible, value);
        }

        /// <summary>
        /// リストのリフレッシュレートの下限値(msec)
        /// </summary>
        [DataMember]
        public double RefreshRateMin
        {
            get => this.refreshRateMin;
            set => this.SetProperty(ref this.refreshRateMin, value);
        }

        /// <summary>
        /// テストモード？
        /// </summary>
        [XmlIgnore]
        public bool TestMode
        {
            get => this.testMode;
            set => this.SetProperty(ref this.testMode, value);
        }

        /// <summary>
        /// Z軸を表示するか？
        /// </summary>
        [DataMember]
        public bool VisibleZ
        {
            get => this.visibleZ;
            set => this.SetProperty(ref this.visibleZ, value);
        }

        /// <summary>
        /// 自分の位置を表示するか？
        /// </summary>
        [DataMember]
        public bool VisibleMe
        {
            get => this.visibleMe;
            set => this.SetProperty(ref this.visibleMe, value);
        }

        /// <summary>
        /// 方向の基準
        /// </summary>
        [DataMember]
        public DirectionOrigin DirectionOrigin
        {
            get => this.directionOrigin;
            set => this.SetProperty(ref this.directionOrigin, value);
        }

        /// <summary>
        /// 方向の補正角度
        /// </summary>
        [DataMember]
        public double DirectionAdjustmentAngle
        {
            get => this.directionAdjustmentAngle;
            set => this.SetProperty(ref this.directionAdjustmentAngle, value);
        }

        /// <summary>
        /// シンプル表示？
        /// </summary>
        [DataMember]
        public bool IsSimple
        {
            get => this.isSimple;
            set
            {
                if (this.SetProperty(ref this.isSimple, value))
                {
                    this.RaisePropertyChanged(nameof(this.IsNotSimple));
                }
            }
        }

        /// <summary>
        /// シンプル表示ではない？
        /// </summary>
        [XmlIgnore]
        public bool IsNotSimple => !this.isSimple;

        /// <summary>
        /// 表示数
        /// </summary>
        [DataMember]
        public int DisplayCount
        {
            get => this.displayCount;
            set => this.SetProperty(ref this.displayCount, value);
        }

        /// <summary>
        /// 近くと認識する距離
        /// </summary>
        [DataMember]
        public double NearDistance
        {
            get => this.nearDistance;
            set => this.SetProperty(ref this.nearDistance, value);
        }

        /// <summary>
        /// TTSを使用するか？
        /// </summary>
        [DataMember]
        public bool TTSEnabled
        {
            get => this.ttsEnabled;
            set => this.SetProperty(ref this.ttsEnabled, value);
        }

        /// <summary>
        /// Combatantsをダンプする
        /// </summary>
        [DataMember]
        public bool DumpCombatants
        {
            get => this.dumpCombatants;
            set => this.SetProperty(ref this.dumpCombatants, value);
        }

        /// <summary>
        /// 場所
        /// </summary>
        [DataMember]
        public Location Location { get; set; } = new Location();

        /// <summary>
        /// Me表示テキスト
        /// </summary>
        [DataMember]
        public DisplayText MeDisplayText { get; set; } = new DisplayText();

        /// <summary>
        /// MobEX表示テキスト
        /// </summary>
        [DataMember]
        public FontInfo MobFont { get; set; } = new FontInfo()
        {
            FontFamily = new FontFamily("Arial"),
            Size = 16,
            Weight = FontWeights.Bold,
        };

        /// <summary>
        /// MobEXのカラー
        /// </summary>
        [DataMember]
        public FontColor MobEXColor { get; set; } = new FontColor()
        {
            Color = Colors.White,
            OutlineColor = Colors.Gold,
        };

        /// <summary>
        /// MobSのカラー
        /// </summary>
        [DataMember]
        public FontColor MobSColor { get; set; } = new FontColor()
        {
            Color = Colors.White,
            OutlineColor = Colors.Gold,
        };

        /// <summary>
        /// MobAのカラー
        /// </summary>
        [DataMember]
        public FontColor MobAColor { get; set; } = new FontColor()
        {
            Color = Colors.White,
            OutlineColor = Colors.Gold,
        };

        /// <summary>
        /// MobBのカラー
        /// </summary>
        [DataMember]
        public FontColor MobBColor { get; set; } = new FontColor()
        {
            Color = Colors.White,
            OutlineColor = Colors.Gold,
        };

        /// <summary>
        /// その他のカラー
        /// </summary>
        [DataMember]
        public FontColor MobOtherColor { get; set; } = new FontColor()
        {
            Color = Colors.White,
            OutlineColor = Colors.Gold,
        };

        public void RaiseMobFontChanged()
        {
            this.RaisePropertyChanged(nameof(this.MobFont));
        }

        #region Target MobList

        private const string MobListFileName = @"MobList.{0}.txt";

        [XmlIgnore]
        private string ResourcesDirectory => DirectoryHelper.FindSubDirectory(@"resources");

        [XmlIgnore]
        public string MobListFile => Path.Combine(
            this.ResourcesDirectory,
            string.Format(MobListFileName, Settings.Instance.FFXIVLocale.ToResourcesName()));

        [XmlIgnore]
        private readonly Dictionary<string, (string Rank, double MaxDistance, bool TTSEnabled)> targetMobList = new Dictionary<string, (string Rank, double MaxDistance, bool TTSEnabled)>();

        /// <summary>
        /// 対象とするモブリスト
        /// </summary>
        [XmlIgnore]
        public IReadOnlyDictionary<string, (string Rank, double MaxDistance, bool TTSEnabled)> TargetMobList => this.targetMobList;

        public void LoadTargetMobList()
        {
            if (!File.Exists(this.MobListFile))
            {
                return;
            }

            using (var sr = new StreamReader(this.MobListFile, new UTF8Encoding(false)))
            using (var tf = new TextFieldParser(sr)
            {
                CommentTokens = new string[] { "#" },
                Delimiters = new string[] { "\t", " " },
                TextFieldType = FieldType.Delimited,
                HasFieldsEnclosedInQuotes = true,
                TrimWhiteSpace = true
            })
            {
                this.targetMobList.Clear();

                while (!tf.EndOfData)
                {
                    var fields = tf.ReadFields()
                        .Where(x => !string.IsNullOrEmpty(x))
                        .ToArray();

                    if (fields.Length <= 0)
                    {
                        continue;
                    }

                    var name = fields.Length > 0 ? fields[0] : string.Empty;
                    var rank = fields.Length > 1 ? fields[1] : string.Empty;

                    // 感知する最大距離
                    var distanceString = fields.Length > 2 ? fields[2] : string.Empty;
                    var distance = double.MaxValue;
                    if (!double.TryParse(distanceString, out distance))
                    {
                        distance = double.MaxValue;
                    }

                    // TTSの有効性
                    var ttsString = fields.Length > 3 ? fields[3] : true.ToString();
                    var tts = true;
                    if (!bool.TryParse(ttsString, out tts))
                    {
                        tts = true;
                    }

                    if (!string.IsNullOrEmpty(name))
                    {
                        this.targetMobList[name] = (rank, distance, tts);
                    }
                }
            }

            this.logger.Info($"MobList loaded. {this.MobListFile}");
        }

        #endregion Target MobList
    }
}
