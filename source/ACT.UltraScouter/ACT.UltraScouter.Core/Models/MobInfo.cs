using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ACT.UltraScouter.Common;
using ACT.UltraScouter.Config;
using ACT.UltraScouter.ViewModels;
using FFXIV.Framework.Common;
using FFXIV.Framework.XIVHelper;
using Prism.Commands;
using Prism.Mvvm;
using Sharlayan.Core.Enums;

namespace ACT.UltraScouter.Models
{
    public class MobInfo :
        BindableBase
    {
        public MobInfo()
        {
        }

        public MobInfo Clone() =>
            (MobInfo)this.MemberwiseClone();

        public MobInfo Clone(
            Action<MobInfo> overrideAction)
        {
            var clone = this.Clone();
            if (overrideAction != null)
            {
                overrideAction.Invoke(clone);
            }

            return clone;
        }

        public MobList Config => Settings.Instance.MobList;

        public MobListViewModel ParentViewModel => MobListViewModel.Current;

        private static readonly Dictionary<string, int> RankTable = new Dictionary<string, int>()
        {
            { "EX", 10 },
            { "S", 20 },
            { "A", 30 },
            { "B", 40 },
        };

        private static readonly DisplayText mobEXText = new DisplayText()
        {
            Font = Settings.Instance.MobList.MobFont,
            Color = Settings.Instance.MobList.MobEXColor.Color,
            OutlineColor = Settings.Instance.MobList.MobEXColor.OutlineColor,
        };

        private static readonly DisplayText mobSText = new DisplayText()
        {
            Font = Settings.Instance.MobList.MobFont,
            Color = Settings.Instance.MobList.MobSColor.Color,
            OutlineColor = Settings.Instance.MobList.MobSColor.OutlineColor,
        };

        private static readonly DisplayText mobAText = new DisplayText()
        {
            Font = Settings.Instance.MobList.MobFont,
            Color = Settings.Instance.MobList.MobAColor.Color,
            OutlineColor = Settings.Instance.MobList.MobAColor.OutlineColor,
        };

        private static readonly DisplayText mobBText = new DisplayText()
        {
            Font = Settings.Instance.MobList.MobFont,
            Color = Settings.Instance.MobList.MobBColor.Color,
            OutlineColor = Settings.Instance.MobList.MobBColor.OutlineColor,
        };

        private static readonly DisplayText mobOtherText = new DisplayText()
        {
            Font = Settings.Instance.MobList.MobFont,
            Color = Settings.Instance.MobList.MobOtherColor.Color,
            OutlineColor = Settings.Instance.MobList.MobOtherColor.OutlineColor,
        };

        public static DisplayText MobEXText => mobEXText;
        public static DisplayText MobSText => mobSText;
        public static DisplayText MobAText => mobAText;
        public static DisplayText MobBText => mobBText;
        public static DisplayText MobOtherText => mobOtherText;

        public static void UpdateFont(
            FontInfo font)
        {
            var mobFont = Settings.Instance.MobList.MobFont;
            mobFont.FontFamily = font.FontFamily;
            mobFont.Size = font.Size;
            mobFont.Weight = font.Weight;
            mobFont.Style = font.Style;
            mobFont.Stretch = font.Stretch;

            Settings.Instance.MobList.RaiseMobFontChanged();
        }

        private int index;
        private CombatantEx combatant;
        private string name;
        private string rank = "DUMMY";
        private bool ttsEnabled = true;
        private int duplicateCount = 0;
        private int rankSortKey;
        private double directionAngle;
        private Direction direction = Direction.Unknown;
        private string directionText;
        private double distance = -1;
        private double maxDistance = double.MaxValue;
        private double x;
        private double y;
        private double z;
        private string targetHeight;
        private bool isNear;

        private DisplayText displayText = new DisplayText()
        {
            Font = Settings.Instance.MobList.MobFont,
            Color = Settings.Instance.MobList.MobOtherColor.Color,
            OutlineColor = Settings.Instance.MobList.MobOtherColor.OutlineColor,
        };

#if DEBUG
        private bool visible = true;
#else
        private bool visible = false;
#endif

        public int Index
        {
            get => this.index;
            set => this.SetProperty(ref this.index, value);
        }

        public CombatantEx Combatant
        {
            get => this.combatant;
            set
            {
                if (!this.SetProperty(ref this.combatant, value))
                {
                    return;
                }

                this.RefreshDistance();
            }
        }

        public void RefreshDistance()
        {
            this.Name = this.combatant.NameForDisplay;
            this.X = this.combatant.PosX;
            this.Y = this.combatant.PosY;
            this.Z = this.combatant.PosZ;

            var x1 = this.combatant.Player?.PosX ?? 0;
            var y1 = this.combatant.Player?.PosY ?? 0;
            var x2 = this.X;
            var y2 = this.Y;

            // プレイヤーとの角度を求める
            var rad = Math.Atan2(
                y2 - y1,
                x2 - x1);

            // 単純な計算角度を算出する
            this.DirectionAngle = rad * 180.0 / Math.PI;
            this.RaisePropertyChanged(nameof(this.DirectionAngle));

            // 0 - 360 に補正する
            var deg = this.DirectionAngle % 360.0;
            if (deg < 0.0)
            {
                deg = 360.0 + deg;
            }

            // 8方位に変換する
            var dir = (int)((deg + 22.5) / 45.0);
            dir %= 8;

            if (Enum.IsDefined(typeof(Direction), dir))
            {
                this.Direction = (Direction)dir;
            }

            // 距離は最後にセットする
            this.Distance = Math.Round(this.combatant.HorizontalDistanceByPlayer, 1);

            this.RaisePropertyChanged(nameof(this.IsPC));
            this.RaisePropertyChanged(nameof(this.JobIcon));
        }

        public bool IsPC => this.combatant?.ActorType == Actor.Type.PC;

        public BitmapSource JobIcon => JobIconDictionary.Instance.GetIcon(this.combatant?.JobID ?? JobIDs.Unknown);

        public string Name
        {
            get => this.name;
            set => this.SetProperty(ref this.name, value);
        }

        public string Rank
        {
            get => this.rank;
            set
            {
                if (this.SetProperty(ref this.rank, value))
                {
                    this.RankSortKey = this.GetRankSortKey(this.rank);

                    switch (this.rank)
                    {
                        case "EX": this.DisplayText = mobEXText; break;
                        case "S": this.DisplayText = MobSText; break;
                        case "A": this.DisplayText = mobAText; break;
                        case "B": this.DisplayText = mobBText; break;
                        default: this.DisplayText = mobOtherText; break;
                    }
                }
            }
        }

        public bool TTSEnabled
        {
            get => this.ttsEnabled;
            set => this.SetProperty(ref this.ttsEnabled, value);
        }

        public int DuplicateCount
        {
            get => this.duplicateCount;
            set
            {
                if (this.SetProperty(ref this.duplicateCount, value))
                {
                    this.RaisePropertyChanged(nameof(this.DuplicateCountText));
                }
            }
        }

        public string DuplicateCountText =>
            this.duplicateCount > 1 ? $"x {this.duplicateCount}" : string.Empty;

        private int GetRankSortKey(
            string rank)
        {
            var dic = MobInfo.RankTable;

            lock (dic)
            {
                if (dic.ContainsKey(rank))
                {
                    return dic[rank];
                }

                var newSortKey = dic.Values.Max() + 1;
                dic[rank] = newSortKey;

                return newSortKey;
            }
        }

        public int RankSortKey
        {
            get => this.rankSortKey;
            set => this.SetProperty(ref this.rankSortKey, value);
        }

        public double DirectionAngle
        {
            get => this.directionAngle;
            set => this.SetProperty(ref this.directionAngle, value);
        }

        public Direction Direction
        {
            get => this.direction;
            set
            {
                if (this.SetProperty(ref this.direction, value))
                {
                    this.DirectionText = this.direction.ToArrow();
                }
            }
        }

        public FontFamily FontAwesome => Arrows.FontAwesome;
        public string ArrowByWingding => Arrows.Arrow0ByWingdings;
        public string ArrowByFontAwesome => Arrows.Arrow0ByFontAwesome;
        public string Asterisk => "\xf069";
        public string High => "\xf102";
        public string Low => "\xf103";

        public string DirectionText
        {
            get => this.directionText;
            set => this.SetProperty(ref this.directionText, value);
        }

        public double Distance
        {
            get => this.distance;
            set
            {
                if (this.SetProperty(ref this.distance, value))
                {
                    this.IsNear =
                        (this.Combatant?.HorizontalDistanceByPlayer ?? 9999) <=
                        Settings.Instance.MobList.NearDistance;
                }
            }
        }

        public double MaxDistance
        {
            get => this.maxDistance;
            set => this.SetProperty(ref this.maxDistance, value);
        }

        public double X
        {
            get => this.x;
            set
            {
                if (this.SetProperty(ref this.x, value))
                {
                    this.RaisePropertyChanged(nameof(this.XonMap));
                }
            }
        }

        public double Y
        {
            get => this.y;
            set
            {
                if (this.SetProperty(ref this.y, value))
                {
                    this.RaisePropertyChanged(nameof(this.YonMap));
                }
            }
        }

        public double Z
        {
            get => this.z;
            set
            {
                if (this.SetProperty(ref this.z, value))
                {
                    this.RaisePropertyChanged(nameof(this.ZonMap));
                }

                var playerZ = this.Combatant?.Player?.PosZ ?? 0;
                var deltaZ = this.z - playerZ;
                if (deltaZ >= 10)
                {
                    // fa angle-doubleup
                    this.TargetHeight = this.High;
                }
                else if (deltaZ <= -10)
                {
                    // fa angle-doubledown
                    this.TargetHeight = this.Low;
                }
                else
                {
                    this.TargetHeight = string.Empty;
                }
            }
        }

        public string TargetHeight
        {
            get => this.targetHeight;
            set => this.SetProperty(ref this.targetHeight, value);
        }

        public double XonMap => CombatantEx.ToHorizontalMapPosition(this.X);
        public double YonMap => CombatantEx.ToHorizontalMapPosition(this.Y);
        public double ZonMap => CombatantEx.ToVerticalMapPosition(this.Z);

        public bool IsNear
        {
            get => this.isNear;
            set => this.SetProperty(ref this.isNear, value);
        }

        public DisplayText DisplayText
        {
            get => this.displayText;
            private set => this.SetProperty(ref this.displayText, value);
        }

        public bool Visible
        {
            get => this.visible;
            set => this.SetProperty(ref this.visible, value);
        }

        private static string beforeTTS;
        private static DateTime beforeTTSTimestamp;

        /// <summary>
        /// TTSで通知する
        /// </summary>
        public void NotifyByTTS()
        {
            if (!Settings.Instance.MobList.TTSEnabled)
            {
                return;
            }

            if (!this.TTSEnabled)
            {
                return;
            }

            if ((DateTime.Now - MobInfo.beforeTTSTimestamp).TotalSeconds >= 3.0)
            {
                MobInfo.beforeTTS = string.Empty;
            }

            // モブ名を辞書で置換する
            var mobName = TTSDictionary.Instance.ReplaceTTS(this.Name);

            if (!string.IsNullOrEmpty(mobName))
            {
                var tts = $"{mobName} {this.Rank}";

                if (MobInfo.beforeTTS != tts)
                {
                    TTSWrapper.Speak(tts);
                }

                MobInfo.beforeTTS = tts;
                MobInfo.beforeTTSTimestamp = DateTime.Now;
            }
        }

        public override string ToString()
            => $"Name={this.Name}, Rank={this.Rank}";

        /// <summary>
        /// お知らせ用の文字列に変換する
        /// </summary>
        /// <returns>お知らせ用文字列</returns>
        public string ToNoticeString()
            => $"{this.Name} {this.Rank} X:{this.X:N1}, Y:{this.Y:N1}";

        /// <summary>
        /// Clipboardにコピーするコマンド
        /// </summary>
        public ICommand copyPositionCommand;

        /// <summary>
        /// Clipboardにコピーするコマンド
        /// </summary>
        public ICommand CopyPositionCommand =>
            this.copyPositionCommand ?? (this.copyPositionCommand = new DelegateCommand<MobInfo>((mob) =>
            {
                if (mob == null)
                {
                    return;
                }

                var text = mob.ToNoticeString();
                Clipboard.SetDataObject(text);
            }));

        public void RaiseAllPropertiesChanged()
        {
            foreach (var pi in this.GetType().GetProperties())
            {
                this.RaisePropertyChanged(pi.Name);
            }
        }
    }
}
