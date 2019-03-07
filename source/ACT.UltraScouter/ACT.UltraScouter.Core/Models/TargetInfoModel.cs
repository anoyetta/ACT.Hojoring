using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using ACT.UltraScouter.Config;
using ACT.UltraScouter.Models.Enmity;
using ACT.UltraScouter.Models.FFLogs;
using FFXIV.Framework.Bridge;
using FFXIV.Framework.Common;
using FFXIV.Framework.Extensions;
using FFXIV.Framework.FFXIVHelper;
using NLog;
using Prism.Mvvm;
using Sharlayan.Core.Enums;

namespace ACT.UltraScouter.Models
{
    public class TargetInfoModel :
        BindableBase
    {
        #region Singleton

        private readonly static TargetInfoModel DesigntimeTargetInfo = new TargetInfoModel()
        {
            Name = "Naoki Yoshida",
            IsCasting = true,
            CastSkillName = "とても強い攻撃とても強い攻撃とても強い攻撃",
            CastDurationMax = 100,
            castDurationCurrent = 74,
        };

        private static TargetInfoModel instance = WPFHelper.IsDesignMode ?
            DesigntimeTargetInfo :
            new TargetInfoModel();

        public static TargetInfoModel Instance => instance;

        #endregion Singleton

        #region Logger

        private Logger logger = AppLog.DefaultLogger;

        #endregion Logger

        public delegate void CastingEventHandler(object sender, CastingEventArgs args);

        public event CastingEventHandler Casting;

        protected bool isCasting;
        protected float castDurationCurrent;
        protected float castDurationMax;
        protected string castSkillName;
        protected int castSkillID;

        protected double currentHP;
        protected double currentHPRate;
        protected double distance;
        protected bool isEffectiveDistance;
        protected double maxHP;
        protected string name;

        public TargetInfoModel()
        {
            this.CreateEnmityViewSource();
        }

        public bool IsCasting
        {
            get => this.isCasting;
            set
            {
                if (value &&
                    this.isCasting != value)
                {
                    this.Casting?.Invoke(this, new CastingEventArgs()
                    {
                        Source = this,
                        Actor = this.Name,
                        CastingDateTime = DateTime.Now,
                        CastSkillID = this.CastSkillID,
                        CastSkillName = this.CastSkillName,
                        CastDurationMax = this.CastDurationMax,
                        CastDurationCurrent = this.CastDurationCurrent,
                    });
                }

                this.SetProperty(ref this.isCasting, value);
            }
        }

        public float CastDurationCurrent
        {
            get => this.castDurationCurrent;
            set
            {
                if (this.SetProperty(ref this.castDurationCurrent, value))
                {
                    this.RaisePropertyChanged(nameof(this.CastingProgressRate));
                    this.RaisePropertyChanged(nameof(this.CastingRemain));
                }
            }
        }

        public float CastDurationMax
        {
            get => this.castDurationMax;
            set
            {
                if (this.SetProperty(ref this.castDurationMax, value))
                {
                    this.RaisePropertyChanged(nameof(this.CastingProgressRate));
                    this.RaisePropertyChanged(nameof(this.CastingRemain));
                }
            }
        }

        public string CastSkillName
        {
            get => this.castSkillName;
            set => this.SetProperty(ref this.castSkillName, value);
        }

        public int CastSkillID
        {
            get => this.castSkillID;
            set => this.SetProperty(ref this.castSkillID, value);
        }

        public double CastingRemain =>
            this.CastDurationMax != 0 ?
                this.CastDurationMax - this.CastDurationCurrent :
                0;

        public double CastingProgressRate =>
            this.CastDurationMax != 0 ?
                this.CastDurationCurrent / this.CastDurationMax :
                0;

        public double CurrentHP
        {
            get => this.currentHP;
            set => this.SetProperty(ref this.currentHP, value);
        }

        public double CurrentHPRate
        {
            get => this.currentHPRate;
            set => this.SetProperty(ref this.currentHPRate, value);
        }

        public double MaxHP
        {
            get => this.maxHP;
            set => this.SetProperty(ref this.maxHP, value);
        }

        public string Name
        {
            get => this.name;
            set
            {
                if (this.SetProperty(ref this.name, value))
                {
                    this.SetInitialName();
                    this.RaisePropertyChanged(nameof(this.DistanceText));
                    this.UpdateDictanceIndicator();
                }
            }
        }

        private Actor.Type objectType = Actor.Type.Unknown;

        public Actor.Type ObjectType
        {
            get => this.objectType;
            set => this.SetProperty(ref this.objectType, value);
        }

        private JobIDs job = JobIDs.Unknown;

        public JobIDs Job
        {
            get => this.job;
            set => this.SetProperty(ref this.job, value);
        }

        private int worldID;

        public int WorldID
        {
            get => this.worldID;
            set => this.SetProperty(ref this.worldID, value);
        }

        private string worldName;

        public string WorldName
        {
            get => this.worldName;
            set => this.SetProperty(ref this.worldName, value);
        }

        public string NameFI { get; protected set; } = string.Empty;
        public string NameIF { get; protected set; } = string.Empty;
        public string NameII { get; protected set; } = string.Empty;

        protected void SetInitialName()
        {
            if (string.IsNullOrEmpty(this.Name))
            {
                this.NameFI = string.Empty;
                this.NameIF = string.Empty;
                this.NameII = string.Empty;
                return;
            }

            var parts = this.Name.Split(' ');
            if (parts.Length >= 2)
            {
                this.NameFI =
                    parts[0] + " " + parts[1].Substring(0, 1) + ".";

                this.NameIF =
                    parts[0].Substring(0, 1) + ". " + parts[1];

                this.NameII =
                    parts[0].Substring(0, 1) + ". " + parts[1].Substring(0, 1) + ".";
            }
        }

        #region Distance

        public FontFamily FontAwesome => Arrows.FontAwesome;

        public double Distance
        {
            get => this.distance;
            set
            {
                if (this.SetProperty(ref this.distance, value))
                {
                    this.RaisePropertyChanged(nameof(this.DistanceText));
                    this.UpdateDictanceIndicator();
                }
            }
        }

        public bool IsEffectiveDistance
        {
            get => this.isEffectiveDistance;
            set
            {
                if (this.SetProperty(ref this.isEffectiveDistance, value))
                {
                    this.RaisePropertyChanged(nameof(this.DistanceText));
                    this.UpdateDictanceIndicator();
                }
            }
        }

        public string DistanceText
        {
            get
            {
                if (string.IsNullOrEmpty(this.Name))
                {
                    return string.Empty;
                }

                var format = this.IsEffectiveDistance ?
                    "N0" : "N1";

                return this.Distance.ToString(format);
            }
        }

        public enum DistanceCategory
        {
            Unkown = 0,
            InHeal,         // 単体ヒールの射程内
            InRange,        // レンジ攻撃の射程内
            InRangePlus,    // レンジAAの射程内
            InPBAoE,        // 自分中心AOE(広)の範囲内
            InPBAoEPlus,    // 自分終身AOE(狭)の範囲内
            InCross,        // メレーの射程内
            InCrossPlus     // メレーAAの射程内
        }

        public static readonly IList<(string symbol, Color color)> DistanceIndicators = new (string symbol, Color color)[]
        {
            /*
            (null, Colors.Transparent),           // Unknown
            ("\xf041", Colors.LightSkyBlue),      // InHeal
            ("\xf062", Colors.LimeGreen),         // InRange
            ("\xf0aa", Colors.LimeGreen),         // InRangePlus
            ("\xf140", Colors.PeachPuff),         // InPBAoE
            ("\xf192", Colors.Gold),              // InPBAoEPlus
            ("\xf067", Colors.OrangeRed),         // InCross
            ("\xf055", Colors.OrangeRed),         // InCrossPlus
            */

            (null, Colors.Transparent),             // Unknown
            ("<<<<", Colors.LightSkyBlue),          // InHeal
            ("<<<-", Colors.GreenYellow),           // InRange
            ("<<<", Colors.GreenYellow),            // InRangePlus
            ("<<-", Colors.Violet),                 // InPBAoE
            ("<<+", Colors.DarkOrange),             // InPBAoEPlus
            ("<-", Color.FromRgb(0xFF, 0x1E, 0x1E)),    // InCross
            ("<", Color.FromRgb(0xFF, 0x1E, 0x1E)),     // InCrossPlus
        };

        public void UpdateDictanceIndicator()
        {
            if (!this.IsEffectiveDistance)
            {
                this.DistanceIndicator = DistanceCategory.Unkown;
                return;
            }

            if (string.IsNullOrEmpty(this.Name))
            {
                this.DistanceIndicator = DistanceCategory.Unkown;
                return;
            }

            var cat = DistanceCategory.Unkown;
            var d = this.Distance + 1;

            /*
            <= 30 : 段階1  （回復魔法の射程に入った）
            <= 25 : 段階2  （攻撃魔法の射程に入った）
            <= 24 : 段階2+ （レンジのAA射程に入った）
            <= 20 : 段階3  （メディカラの範囲に入った）
            <= 15 : 段階4  （メディカの範囲に入った。ロブの射程に入った）
            <= 3  : 段階5  （メレーのWSの射程に入った）
            <= 2  : 段階5+ （メレーのAAの射程に入った）
            */

            if (d > 30)
            {
                cat = DistanceCategory.Unkown;
            }
            else if (d > 25 && d <= 30)
            {
                cat = DistanceCategory.InHeal;
            }
            else if (d > 24 && d <= 25)
            {
                cat = DistanceCategory.InRange;
            }
            else if (d > 20 && d <= 24)
            {
                cat = DistanceCategory.InRangePlus;
            }
            else if (d > 15 && d <= 20)
            {
                cat = DistanceCategory.InPBAoE;
            }
            else if (d > 3 && d <= 15)
            {
                cat = DistanceCategory.InPBAoEPlus;
            }
            else if (d > 2 && d <= 3)
            {
                cat = DistanceCategory.InCross;
            }
            else if (d > 0 && d <= 2)
            {
                cat = DistanceCategory.InCrossPlus;
            }
            else if (d <= 0 && !string.IsNullOrEmpty(this.Name))
            {
                cat = DistanceCategory.InCrossPlus;
            }

            this.DistanceIndicator = cat;
        }

        private DistanceCategory distanceIndicator = DistanceCategory.Unkown;

        public DistanceCategory DistanceIndicator
        {
            get => this.distanceIndicator;
            private set
            {
                if (this.SetProperty(ref this.distanceIndicator, value))
                {
                    this.RaisePropertyChanged(nameof(this.DistanceIndicatorText));
                    this.RaisePropertyChanged(nameof(this.DistanceIndicatorBrush));
                }
            }
        }

        public string DistanceIndicatorText
            => DistanceIndicators[(int)this.DistanceIndicator].symbol;

        public Brush DistanceIndicatorBrush
            => DistanceIndicators[(int)this.DistanceIndicator].color.ToBrush();

        #endregion Distance

        #region FFLogs

        private static ParseTotalModel designtimeParseTotal;
        private static ParseTotalModel designtimeParseTotalNoHistogram;

        private static ParseTotalModel DesigntimeParseTotal =>
            designtimeParseTotal ?? (designtimeParseTotal = CreateDesigntimeParseTotal());

        private static ParseTotalModel DesigntimeParseTotalNoHistogram =>
            designtimeParseTotalNoHistogram ?? (designtimeParseTotalNoHistogram = CreateDesigntimeParseTotal(true));

        private static ParseTotalModel CreateDesigntimeParseTotal(
            bool noHistogram = false)
        {
            var model = new ParseTotalModel()
            {
                CharacterNameFull = "Naoki Yoshida",
                Server = "Chocobo",
                Region = FFLogsRegions.JP,
                Job = Jobs.Find(JobIDs.BLM),
                BestJobName = "Black Mage",
                Histogram = !noHistogram ?
                    HistogramsModel.DesigntimeModel :
                    new HistogramsModel(),
            };

            model.AddRangeParse(new[]
            {
                new ParseModel()
                {
                    EncounterID = 1,
                    Spec = "Black Mage",
                    EncounterName = "BOSS 1",
                    Percentile = 100.0f,
                    Total = 6451f,
                },
                new ParseModel()
                {
                    EncounterID = 2,
                    Spec = "Black Mage",
                    EncounterName = "BOSS 2",
                    Percentile = 95.0f,
                    Total = 5234f,
                },
                new ParseModel()
                {
                    EncounterID = 3,
                    Spec = "Black Mage",
                    EncounterName = "BOSS 3",
                    Percentile = 75.0f,
                    Total = 5912f,
                },
                new ParseModel()
                {
                    EncounterID = 4,
                    Spec = "Black Mage",
                    EncounterName = "BOSS 4",
                    Percentile = 50.0f,
                    Total = 4987f,
                },
                new ParseModel()
                {
                    EncounterID = 5,
                    Spec = "Black Mage",
                    EncounterName = "BOSS 5",
                    Percentile = 25.0f,
                    Total = 6166f,
                },
                new ParseModel()
                {
                    EncounterID = 6,
                    Spec = "Black Mage",
                    EncounterName = "BOSS 6",
                    Percentile = 14.0f,
                    Total = 3167f,
                },
            });

            return model;
        }

        public static bool IsAvailableParseTotalTextCommand { get; private set; } = false;

        private static ParseTotalModel TextCommandParseTotal { get; } = new ParseTotalModel();

        public static ParseTotalModel APITestResultParseTotal { get; set; } = null;

        private ParseTotalModel parseTotal = WPFHelper.IsDesignMode ? DesigntimeParseTotal : new ParseTotalModel();

        public ParseTotalModel ParseTotal
        {
            get => this.parseTotal;
            private set
            {
                if (this.SetProperty(ref this.parseTotal, value))
                {
                    this.parseTotal.RaiseAllPropertiesChanged();
                }
            }
        }

        private static readonly Dictionary<string, ParseTotalModel> ParseTotalDictionary = new Dictionary<string, ParseTotalModel>();

        public void RefreshFFLogsInfo()
        {
            var config = Settings.Instance.FFLogs;
            if (!config.Visible)
            {
                return;
            }

            if (config.IsDesignMode)
            {
                if (APITestResultParseTotal != null)
                {
                    this.ParseTotal = APITestResultParseTotal;
                }
                else
                {
                    this.ParseTotal = config.VisibleHistogram ?
                        DesigntimeParseTotal :
                        DesigntimeParseTotalNoHistogram;

                    this.ParseTotal.RefreshCharacterName();
                }

                return;
            }

            if (string.IsNullOrEmpty(config.ApiKey))
            {
                return;
            }

            if (IsAvailableParseTotalTextCommand)
            {
                this.ParseTotal = TextCommandParseTotal;
                return;
            }

            var job = Jobs.Find(this.job);

            lock (ParseTotalDictionary)
            {
                var model = default(ParseTotalModel);
                var key = ParseTotalModel.CreateDataKey(this.name, this.worldName, config.ServerRegion, job);
                if (!ParseTotalDictionary.ContainsKey(key))
                {
                    model = new ParseTotalModel();
                    ParseTotalDictionary[key] = model;
                }
                else
                {
                    model = ParseTotalDictionary[key];
                }

                this.ParseTotal = model;
            }

            Task.Run(async () =>
            {
                await this.ParseTotal.GetParseAsync(
                    this.Name,
                    this.WorldName,
                    config.ServerRegion,
                    job);
            });

            this.GarbageParseTotalDictionary();
        }

        private DateTime lastGarbageTimestamp = DateTime.MinValue;

        private void GarbageParseTotalDictionary()
        {
            if ((DateTime.Now - this.lastGarbageTimestamp).TotalMinutes < 2.0)
            {
                return;
            }

            var ttl = Settings.Instance.FFLogs.RefreshInterval * 1.5d * -1;

            lock (ParseTotalDictionary)
            {
                var targets = ParseTotalDictionary
                    .Where(x => x.Value.Timestamp < DateTime.Now.AddMinutes(ttl))
                    .ToArray();

                foreach (var item in targets)
                {
                    ParseTotalDictionary.Remove(item.Key);
                }
            }

            this.lastGarbageTimestamp = DateTime.Now;
        }

        private static System.Timers.Timer textCommandTTLTimer;

        private static System.Timers.Timer TextCommandTTLTimer =>
            textCommandTTLTimer ?? (textCommandTTLTimer = CreateTextCommandTTLTimer());

        private static System.Timers.Timer CreateTextCommandTTLTimer()
        {
            var timer = new System.Timers.Timer();

            timer.AutoReset = false;
            timer.Interval = Settings.Instance.FFLogs.FromCommandTTL * 1000d;
            timer.Elapsed += (x, y) =>
            {
                IsAvailableParseTotalTextCommand = false;
            };

            return timer;
        }

        public static void GetFFLogsInfoFromTextCommand(
            string characterName,
            string serverName)
        {
            try
            {
                TextCommandTTLTimer.Stop();
                IsAvailableParseTotalTextCommand = true;

                if (string.IsNullOrEmpty(characterName) ||
                    string.IsNullOrEmpty(serverName))
                {
                    var player = FFXIVPlugin.Instance.GetPlayer();

                    if (string.IsNullOrEmpty(characterName))
                    {
                        characterName = player.Name;
                    }

                    if (string.IsNullOrEmpty(serverName))
                    {
                        serverName = player.WorldName;
                    }
                }

                var ti = CultureInfo.CurrentCulture.TextInfo;
                characterName = ti.ToTitleCase(characterName);
                serverName = ti.ToTitleCase(serverName);

                Task.Run(async () =>
                {
                    await TextCommandParseTotal.GetParseAsync(
                        characterName,
                        serverName,
                        Settings.Instance.FFLogs.ServerRegion,
                        null);
                });
            }
            finally
            {
                TextCommandTTLTimer.Interval = Settings.Instance.FFLogs.FromCommandTTL * 1000d;
                TextCommandTTLTimer.Start();
            }
        }

        #endregion FFLogs

        #region Enmity

        private volatile bool isEnmityDesignMode = false;
        private volatile int previousMaxCountOfDisplay = 0;

        private static ObservableCollection<EnmityModel> designtimeEnmityList;

        private static ObservableCollection<EnmityModel> DesigntimeEnmityList => designtimeEnmityList ?? (designtimeEnmityList = new ObservableCollection<EnmityModel>()
        {
            new EnmityModel()
            {
                Index = 1,
                ID = 1,
                Name = "Taro Yamada",
                JobID = JobIDs.WAR,
                HateRate = 1.0f,
                Enmity = 3214405,
            },
            new EnmityModel()
            {
                Index = 2,
                ID = 2,
                Name = "Jiro Suzuki",
                JobID = JobIDs.PLD,
                HateRate = 0.85f,
                Enmity = 3214405 * 0.85f,
            },
            new EnmityModel()
            {
                Index = 3,
                ID = 3,
                Name = "Hanako Hime",
                JobID = JobIDs.WHM,
                HateRate = 0.52f,
                Enmity = 3214405 * 0.52f,
            },
            new EnmityModel()
            {
                Index = 4,
                ID = 4,
                Name = "Cookie Cream",
                JobID = JobIDs.SCH,
                HateRate = 0.48f,
                Enmity = 3214405 * 0.48f,
                IsMe = true,
            },
            new EnmityModel()
            {
                Index = 5,
                ID = 5,
                Name = "Ryusan Sky",
                JobID = JobIDs.DRG,
                HateRate = 0.32f,
                Enmity = 3214405 * 0.32f,
            },
            new EnmityModel()
            {
                Index = 6,
                ID = 6,
                Name = "Utako Song",
                JobID = JobIDs.BRD,
                HateRate = 0.31f,
                Enmity = 3214405 * 0.31f,
            },
            new EnmityModel()
            {
                Index = 7,
                ID = 7,
                Name = "Red Yoshida",
                JobID = JobIDs.RDM,
                HateRate = 0.29f,
                Enmity = 3214405 * 0.29f,
            },
            new EnmityModel()
            {
                Index = 8,
                ID = 8,
                Name = "Ridea Numako",
                JobID = JobIDs.SMN,
                HateRate = 0.10f,
                Enmity = 3214405 * 0.10f,
            },
        });

        private bool isExistsEnmityList = false;

        public bool IsExistsEnmityList
        {
            get => this.isExistsEnmityList;
            set => this.SetProperty(ref this.isExistsEnmityList, value);
        }

        private ObservableCollection<EnmityModel> enmityList = WPFHelper.IsDesignMode ?
            DesigntimeEnmityList :
            new ObservableCollection<EnmityModel>();

        private CollectionViewSource EnmityViewSource { get; set; }

        public ICollectionView EnmityView => this.EnmityViewSource?.View;

        private async void CreateEnmityViewSource() => await WPFHelper.InvokeAsync(() =>
        {
            var source = new CollectionViewSource()
            {
                Source = this.enmityList,
            };

            source.SortDescriptions.AddRange(new[]
            {
                new SortDescription(nameof(EnmityModel.Index), ListSortDirection.Ascending),
            });

            source.LiveFilteringProperties.AddRange(new[]
            {
                nameof(EnmityModel.Index),
            });

            source.Filter += (_, e) =>
            {
                var enmity = e.Item as EnmityModel;
                e.Accepted = (enmity?.Index ?? int.MaxValue) <= Settings.Instance.Enmity.MaxCountOfDisplay;
            };

            this.EnmityViewSource = source;
            this.EnmityView.Refresh();
            this.RaisePropertyChanged(nameof(this.EnmityView));
            this.IsExistsEnmityList = this.enmityList.Any();

            this.enmityList.Walk(x => x.RaiseAllPropertiesChanged());
        });

        private volatile bool isEnmityRefreshing = false;

        public void RefreshEnmityList(
            IEnumerable<EnmityEntry> enmityEntryList)
        {
            if (this.isEnmityRefreshing)
            {
                return;
            }

            try
            {
                this.isEnmityRefreshing = true;

                var config = Settings.Instance.Enmity;
                if (!config.Visible)
                {
                    this.enmityList.Clear();
                    this.IsExistsEnmityList = false;
                    return;
                }

                if (!config.IsDesignMode &&
                    this.ObjectType != Actor.Type.Monster)
                {
                    this.enmityList.Clear();
                    this.IsExistsEnmityList = false;
                    return;
                }

                if (config.IsDesignMode)
                {
                    if (!this.isEnmityDesignMode)
                    {
                        this.enmityList.Clear();
                        this.enmityList.AddRange(DesigntimeEnmityList);
                        this.EnmityView?.Refresh();
                    }

                    this.isEnmityDesignMode = true;

                    foreach (var x in this.enmityList)
                    {
                        x.Name = Combatant.NameToInitial(x.Name, ConfigBridge.Instance.PCNameStyle);
                    }

                    if (this.previousMaxCountOfDisplay != config.MaxCountOfDisplay)
                    {
                        this.EnmityView?.Refresh();
                    }

                    this.IsExistsEnmityList = true;
                    this.RefreshEnmtiyHateRateBarWidth();
                    this.previousMaxCountOfDisplay = config.MaxCountOfDisplay;
                    return;
                }

                this.isEnmityDesignMode = false;

                if (config.HideInNotCombat &&
                    !FFXIVPlugin.Instance.InCombat)
                {
                    this.enmityList.Clear();
                    this.IsExistsEnmityList = false;
                    return;
                }

                var partyCount = FFXIVPlugin.Instance.PartyMemberCount;
                if (config.HideInSolo)
                {
                    if (partyCount <= 1)
                    {
                        this.enmityList.Clear();
                        this.IsExistsEnmityList = false;
                        return;
                    }
                }

                if (enmityEntryList == null ||
                    enmityEntryList.Count() < 1)
                {
                    this.enmityList.Clear();
                    this.IsExistsEnmityList = false;
                    return;
                }

                var index = 1;
                var newEnmityList = (
                    from x in enmityEntryList
                    orderby
                    x.Enmity descending
                    select new EnmityModel()
                    {
                        Index = index++,
                        ID = x.ID,
                        Name = config.IsSelfDisplayYou && x.IsMe ?
                            "YOU" :
                            Combatant.NameToInitial(x.Name, ConfigBridge.Instance.PCNameStyle),
                        JobID = (JobIDs)x.Job,
                        Enmity = (double)x.Enmity,
                        HateRate = x.HateRate / 100f,
                        IsMe = x.IsMe,
                        IsPet = x.IsPet,
                    }).ToArray();

                var needsRefresh = false;
                foreach (var src in newEnmityList)
                {
                    var dest = this.enmityList.FirstOrDefault(x => x.ID == src.ID);
                    if (dest == null)
                    {
                        this.enmityList.Add(src);
                        needsRefresh = true;
                        continue;
                    }

                    if (dest.Index != src.Index)
                    {
                        dest.Index = src.Index;
                        needsRefresh = true;
                    }

                    dest.Name = src.Name;
                    dest.JobID = src.JobID;
                    dest.Enmity = src.Enmity;
                    dest.HateRate = src.HateRate;
                    dest.IsMe = src.IsMe;
                    dest.IsPet = src.IsPet;
                }

                var toRemoves = this.enmityList.Where(x => !newEnmityList.Any(y => y.ID == x.ID)).ToArray();
                foreach (var enmity in toRemoves)
                {
                    this.enmityList.Remove(enmity);
                }

                if (this.previousMaxCountOfDisplay != config.MaxCountOfDisplay ||
                    needsRefresh ||
                    toRemoves.Any())
                {
                    this.EnmityView?.Refresh();
                }

                this.IsExistsEnmityList = this.enmityList.Any();
                this.RefreshEnmtiyHateRateBarWidth();
                this.previousMaxCountOfDisplay = config.MaxCountOfDisplay;
            }
            finally
            {
                this.isEnmityRefreshing = false;
            }
        }

        private double previousBarWidthMax = 0d;

        private void RefreshEnmtiyHateRateBarWidth()
        {
            if (!this.enmityList.Any())
            {
                return;
            }

            var max = Settings.Instance.Enmity.BarWidth;

            if (this.previousBarWidthMax != max)
            {
                this.previousBarWidthMax = max;
                this.enmityList.Walk(x => x.RefreshBarWidth());
            }
        }

        #endregion Enmity
    }
}
