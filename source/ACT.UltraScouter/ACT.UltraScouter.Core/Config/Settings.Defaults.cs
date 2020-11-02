using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using ACT.UltraScouter.Common;
using ACT.UltraScouter.Models.FFLogs;
using FFXIV.Framework.Common;
using FFXIV.Framework.XIVHelper;

namespace ACT.UltraScouter.Config
{
    public partial class Settings :
        INotifyPropertyChanged
    {
        /// <summary>
        /// 初期カラー（背景）
        /// </summary>
        public static readonly Color DefaultColorFill = Colors.White;

        /// <summary>
        /// 初期カラー（アウトライン）
        /// </summary>
        public static readonly Color DefaultColorStroke = Colors.DodgerBlue;

        /// <summary>
        /// 初期フォント
        /// </summary>
        public static readonly FontInfo DefaultFont = new FontInfo()
        {
            FontFamily = new FontFamily("Arial"),
            Size = 13,
            Style = FontStyles.Normal,
            Weight = FontWeights.Bold,
            Stretch = FontStretches.Normal,
        };

        /// <summary>
        /// 初期フォントL
        /// </summary>
        public static readonly FontInfo DefaultFontL = new FontInfo()
        {
            FontFamily = new FontFamily("Arial"),
            Size = 20,
            Style = FontStyles.Normal,
            Weight = FontWeights.Bold,
            Stretch = FontStretches.Normal,
        };

        /// <summary>
        /// 初期プログレスバー高さ
        /// </summary>
        public static readonly double DefaultProgressBarHeight = 18;

        /// <summary>
        /// 初期プログレスバー幅
        /// </summary>
        public static readonly double DefaultProgressBarWidth = 200;

        /// <summary>
        /// 初期値
        /// </summary>
        public static readonly Dictionary<string, object> DefaultValues = new Dictionary<string, object>()
        {
            { nameof(Settings.LastUpdateDateTime), DateTime.MinValue },
            { nameof(Settings.Opacity), 1.0 },
            { nameof(Settings.TextOutlineThicknessGain), 1.0d },
            { nameof(Settings.TextBlurGain), 2.0d },
            { nameof(Settings.ClickThrough), false },
            { nameof(Settings.PollingRate), 30 },
            { nameof(Settings.OverlayRefreshRate), 60 },
            { nameof(Settings.ScanMemoryThreadPriority), ThreadPriority.Normal },
            { nameof(Settings.UIThreadPriority), DispatcherPriority.Background },
            { nameof(Settings.AnimationMaxFPS), 30 },

            #region Sounds

            { nameof(Settings.UseNAudio), true },
            { nameof(Settings.WaveVolume), 100f },
            { nameof(Settings.TTSDevice), TTSDevices.Normal },

            #endregion Sounds

            #region Constants

            { nameof(Settings.IdleInterval), 1.0 },
            { nameof(Settings.HPBarAnimationInterval), 100 },
            { nameof(Settings.ActionCounterFontSizeRatio), 0.7 },
            { nameof(Settings.ActionCounterSingleFontSizeRatio), 0.85 },
            { nameof(Settings.ProgressBarDarkRatio), 0.35 },
            { nameof(Settings.ProgressBarEffectRatio), 1.2 },
            { nameof(Settings.HPVisible), true },
            { nameof(Settings.HPRateVisible), true },
            { nameof(Settings.HoverLifeLimit), 3.0 },
            { nameof(Settings.CircleBackOpacity), 0.8 },
            { nameof(Settings.CircleBackBrightnessRate), 0.3 },
            { nameof(Settings.CircleBlurRadius), 14.0 },

            #endregion Constants

            #region Target

            { nameof(Settings.UseHoverTarget), false },

            { nameof(Settings.TargetName), new TargetName()
            {
                Visible = true,
                Location = new Location() { X = 0, Y = 0 },
                DisplayText = new DisplayText()
                {
                    Font = DefaultFont,
                    Color = DefaultColorFill,
                    OutlineColor = DefaultColorStroke,
                },
            }},

            { nameof(Settings.TargetHP), new TargetHP()
            {
                Visible = true,
                TextLocation = new Location() { X = 0, Y = 0 },
                BarLocation = new Location() { X = 0, Y = 20 },
                DisplayText = new DisplayText()
                {
                    Font = DefaultFont,

                    // ここは色を反転させておく
                    Color = DefaultColorStroke,
                    OutlineColor = DefaultColorFill,
                },
                ProgressBar = new ProgressBar()
                {
                    Height = 10,
                    Width = 500,
                    ColorRange = new ObservableCollection<ProgressBarColorRange>()
                    {
                        new ProgressBarColorRange() { Min = 0, Max = 10, Color = Colors.Red },
                        new ProgressBarColorRange() { Min = 10, Max = 20, Color = Colors.OrangeRed },
                        new ProgressBarColorRange() { Min = 20, Max = 50, Color = Colors.DarkOrange },
                        new ProgressBarColorRange() { Min = 50, Max = 75, Color = Colors.LightSeaGreen },
                        new ProgressBarColorRange() { Min = 75, Max = 100, Color = Colors.RoyalBlue },
                    },
                    LinkOutlineColor = true,
                    OutlineColor = DefaultColorStroke,
                },
                HPBarVisible = true,
                IsHPValueOnHPBar = false,
                LinkFontColorToBarColor = true,
                LinkFontOutlineColorToBarColor = false,
            }},

            { nameof(Settings.TargetDistance), new TargetDistance()
            {
                Visible = true,
                Location = new Location() { X = 0, Y = 0 },
                DisplayText = new DisplayText()
                {
                    Font = DefaultFont,
                    Color = DefaultColorFill,
                    OutlineColor = DefaultColorStroke
                },
            }},

            { nameof(Settings.TargetAction), new TargetAction()
            {
                Visible = true,
                Location = new Location() { X = 0, Y = 0 },
                CastingActionNameVisible = true,
                CastingRateVisible = true,
                CastingRemainVisible = false,
                CastingRemainInInteger = true,
                WaveSoundEnabled = false,
                WaveFile = string.Empty,
                TTSEnabled = false,
                UseCircle = false,
                IsCircleReverse = false,
                CircleBlurRadius = 14,
                DisplayText = new DisplayText()
                {
                    Font = DefaultFont,
                    Color = Colors.Gold,
                    OutlineColor = Colors.LemonChiffon
                },
                ProgressBar = new ProgressBar()
                {
                    Height = 10,
                    Width = 300,
                    ColorRange = new ObservableCollection<ProgressBarColorRange>()
                    {
                        new ProgressBarColorRange() { Min = 0, Max = 68, Color = Colors.Gold },
                        new ProgressBarColorRange() { Min = 68, Max = 100, Color = Colors.OrangeRed },
                    },
                    LinkOutlineColor = true,
                    OutlineColor = DefaultColorStroke,
                }
            }},

            { nameof(Settings.Enmity), new Enmity()
            {
                Visible = false,
                HideInNotCombat = true,
                HideInSolo = false,
                IsSelfDisplayYou = true,
                IsDenomi = false,
                IsVisibleIcon = true,
                IsVisibleName = true,
                IconScale = 1.0d,
                ScaningRate = 250d,
                MaxCountOfDisplay = 8,
                IsDesignMode = false,
                Location = new Location() { X = 0, Y = 0 },
                Scale = 1.0d,
                BarWidth = 250d,
                DisplayText = new DisplayText()
                {
                    Font = DefaultFontL,
                    Color = Colors.White,
                    OutlineColor = Color.FromRgb(0x11, 0x13, 0x2b),
                },
            }},

            { nameof(Settings.FFLogs), new FFLogs()
            {
                Visible = false,
                VisibleHistogram = true,
                HideInCombat = true,
                Location = new Location() { X = 0, Y = 0 },
                Scale = 1.0d,
                IsDesignMode = false,
                Background = Color.FromArgb((byte)(255 * 0.6), 0, 0, 0),
                DisplayText = new DisplayText()
                {
                    Font = DefaultFontL,
                    Color = Colors.White,
                    OutlineColor = Color.FromRgb(0x11, 0x13, 0x2b),
                },
                RefreshInterval = 8.0d,
                FromCommandTTL = 14.0d,
                CategoryColors = FFLogs.DefaultCategoryColors,
                Partition = FFLogsPartitions.Standard,
            }},

            #endregion Target

            #region Focus Target

            { nameof(Settings.FTName), new TargetName()
            {
                Visible = false,
                Location = new Location() { X = 0, Y = 0 },
                DisplayText = new DisplayText()
                {
                    Font = DefaultFont,
                    Color = DefaultColorFill,
                    OutlineColor = DefaultColorStroke,
                },
            }},

            { nameof(Settings.FTHP), new TargetHP()
            {
                Visible = false,
                TextLocation = new Location() { X = 0, Y = 0 },
                BarLocation = new Location() { X = 0, Y = 20 },
                DisplayText = new DisplayText()
                {
                    Font = DefaultFont,

                    // ここは色を反転させておく
                    Color = DefaultColorStroke,
                    OutlineColor = DefaultColorFill,
                },
                ProgressBar = new ProgressBar()
                {
                    Height = 10,
                    Width = 500,
                    ColorRange = new ObservableCollection<ProgressBarColorRange>()
                    {
                        new ProgressBarColorRange() { Min = 0, Max = 10, Color = Colors.Red },
                        new ProgressBarColorRange() { Min = 10, Max = 20, Color = Colors.OrangeRed },
                        new ProgressBarColorRange() { Min = 20, Max = 50, Color = Colors.DarkOrange },
                        new ProgressBarColorRange() { Min = 50, Max = 75, Color = Colors.LightSeaGreen },
                        new ProgressBarColorRange() { Min = 75, Max = 100, Color = Colors.RoyalBlue },
                    },
                    LinkOutlineColor = true,
                    OutlineColor = DefaultColorStroke,
                },
                HPBarVisible = true,
                IsHPValueOnHPBar = false,
                LinkFontColorToBarColor = true,
                LinkFontOutlineColorToBarColor = false,
            }},

            { nameof(Settings.FTDistance), new TargetDistance()
            {
                Visible = false,
                Location = new Location() { X = 0, Y = 0 },
                DisplayText = new DisplayText()
                {
                    Font = DefaultFont,
                    Color = DefaultColorFill,
                    OutlineColor = DefaultColorStroke
                },
            }},

            { nameof(Settings.FTAction), new TargetAction()
            {
                Visible = false,
                Location = new Location() { X = 0, Y = 0 },
                CastingActionNameVisible = true,
                CastingRateVisible = true,
                CastingRemainVisible = false,
                CastingRemainInInteger = true,
                WaveSoundEnabled = false,
                WaveFile = string.Empty,
                TTSEnabled = false,
                UseCircle = false,
                IsCircleReverse = false,
                CircleBlurRadius = 14,
                DisplayText = new DisplayText()
                {
                    Font = DefaultFont,
                    Color = Colors.Gold,
                    OutlineColor = Colors.LemonChiffon
                },
                ProgressBar = new ProgressBar()
                {
                    Height = 10,
                    Width = 300,
                    ColorRange = new ObservableCollection<ProgressBarColorRange>()
                    {
                        new ProgressBarColorRange() { Min = 0, Max = 68, Color = Colors.Gold },
                        new ProgressBarColorRange() { Min = 68, Max = 100, Color = Colors.OrangeRed },
                    },
                    LinkOutlineColor = true,
                    OutlineColor = DefaultColorStroke,
                }
            }},

            #endregion Focus Target

            #region Target of Target

            { nameof(Settings.ToTName), new TargetName()
            {
                Visible = false,
                Location = new Location() { X = 0, Y = 0 },
                DisplayText = new DisplayText()
                {
                    Font = DefaultFont,
                    Color = DefaultColorFill,
                    OutlineColor = DefaultColorStroke,
                },
            }},

            { nameof(Settings.ToTHP), new TargetHP()
            {
                Visible = false,
                TextLocation = new Location() { X = 0, Y = 0 },
                BarLocation = new Location() { X = 0, Y = 20 },
                DisplayText = new DisplayText()
                {
                    Font = DefaultFont,

                    // ここは色を反転させておく
                    Color = DefaultColorStroke,
                    OutlineColor = DefaultColorFill,
                },
                ProgressBar = new ProgressBar()
                {
                    Height = 10,
                    Width = 500,
                    ColorRange = new ObservableCollection<ProgressBarColorRange>()
                    {
                        new ProgressBarColorRange() { Min = 0, Max = 10, Color = Colors.Red },
                        new ProgressBarColorRange() { Min = 10, Max = 20, Color = Colors.OrangeRed },
                        new ProgressBarColorRange() { Min = 20, Max = 50, Color = Colors.DarkOrange },
                        new ProgressBarColorRange() { Min = 50, Max = 75, Color = Colors.LightSeaGreen },
                        new ProgressBarColorRange() { Min = 75, Max = 100, Color = Colors.RoyalBlue },
                    },
                    LinkOutlineColor = true,
                    OutlineColor = DefaultColorStroke,
                },
                HPBarVisible = true,
                IsHPValueOnHPBar = false,
                LinkFontColorToBarColor = true,
                LinkFontOutlineColorToBarColor = false,
            }},

            { nameof(Settings.ToTDistance), new TargetDistance()
            {
                Visible = false,
                Location = new Location() { X = 0, Y = 0 },
                DisplayText = new DisplayText()
                {
                    Font = DefaultFont,
                    Color = DefaultColorFill,
                    OutlineColor = DefaultColorStroke
                },
            }},

            { nameof(Settings.ToTAction), new TargetAction()
            {
                Visible = false,
                Location = new Location() { X = 0, Y = 0 },
                CastingActionNameVisible = true,
                CastingRateVisible = true,
                CastingRemainVisible = false,
                CastingRemainInInteger = true,
                WaveSoundEnabled = false,
                WaveFile = string.Empty,
                TTSEnabled = false,
                UseCircle = false,
                IsCircleReverse = false,
                CircleBlurRadius = 14,
                DisplayText = new DisplayText()
                {
                    Font = DefaultFont,
                    Color = Colors.Gold,
                    OutlineColor = Colors.LemonChiffon
                },
                ProgressBar = new ProgressBar()
                {
                    Height = 10,
                    Width = 300,
                    ColorRange = new ObservableCollection<ProgressBarColorRange>()
                    {
                        new ProgressBarColorRange() { Min = 0, Max = 68, Color = Colors.Gold },
                        new ProgressBarColorRange() { Min = 68, Max = 100, Color = Colors.OrangeRed },
                    },
                    LinkOutlineColor = true,
                    OutlineColor = DefaultColorStroke,
                }
            }},

            #endregion Target of Target

            #region BOSS

            { nameof(Settings.BossName), new TargetName()
            {
                Visible = false,
                Location = new Location() { X = 0, Y = 0 },
                DisplayText = new DisplayText()
                {
                    Font = DefaultFont,
                    Color = DefaultColorFill,
                    OutlineColor = DefaultColorStroke,
                },
            }},

            { nameof(Settings.BossHP), new TargetHP()
            {
                Visible = false,
                TextLocation = new Location() { X = 0, Y = 0 },
                BarLocation = new Location() { X = 0, Y = 20 },
                DisplayText = new DisplayText()
                {
                    Font = DefaultFont,

                    // ここは色を反転させておく
                    Color = DefaultColorStroke,
                    OutlineColor = DefaultColorFill,
                },
                ProgressBar = new ProgressBar()
                {
                    Height = 10,
                    Width = 500,
                    ColorRange = new ObservableCollection<ProgressBarColorRange>()
                    {
                        new ProgressBarColorRange() { Min = 0, Max = 10, Color = Colors.Red },
                        new ProgressBarColorRange() { Min = 10, Max = 20, Color = Colors.OrangeRed },
                        new ProgressBarColorRange() { Min = 20, Max = 50, Color = Colors.DarkOrange },
                        new ProgressBarColorRange() { Min = 50, Max = 75, Color = Colors.LightSeaGreen },
                        new ProgressBarColorRange() { Min = 75, Max = 100, Color = Colors.RoyalBlue },
                    },
                    LinkOutlineColor = true,
                    OutlineColor = DefaultColorStroke,
                },
                HPBarVisible = true,
                IsHPValueOnHPBar = false,
                LinkFontColorToBarColor = true,
                LinkFontOutlineColorToBarColor = false,
            }},

            { nameof(Settings.BossDistance), new TargetDistance()
            {
                Visible = false,
                Location = new Location() { X = 0, Y = 0 },
                DisplayText = new DisplayText()
                {
                    Font = DefaultFont,
                    Color = DefaultColorFill,
                    OutlineColor = DefaultColorStroke
                },
            }},

            { nameof(Settings.BossAction), new TargetAction()
            {
                Visible = false,
                Location = new Location() { X = 0, Y = 0 },
                CastingActionNameVisible = true,
                CastingRateVisible = true,
                CastingRemainVisible = false,
                CastingRemainInInteger = true,
                WaveSoundEnabled = false,
                WaveFile = string.Empty,
                TTSEnabled = false,
                UseCircle = false,
                IsCircleReverse = false,
                CircleBlurRadius = 14,
                DisplayText = new DisplayText()
                {
                    Font = DefaultFont,
                    Color = Colors.Gold,
                    OutlineColor = Colors.LemonChiffon
                },
                ProgressBar = new ProgressBar()
                {
                    Height = 10,
                    Width = 300,
                    ColorRange = new ObservableCollection<ProgressBarColorRange>()
                    {
                        new ProgressBarColorRange() { Min = 0, Max = 68, Color = Colors.Gold },
                        new ProgressBarColorRange() { Min = 68, Max = 100, Color = Colors.OrangeRed },
                    },
                    LinkOutlineColor = true,
                    OutlineColor = DefaultColorStroke,
                }
            }},

            { nameof(Settings.BossHPThreshold), 140.0 },
            { nameof(Settings.BossVSTargetHideBoss), true },
            { nameof(Settings.BossVSFTHideBoss), true },
            { nameof(Settings.BossVSToTHideBoss), true },

            #endregion BOSS

            #region Me

            { nameof(Settings.MeAction), new TargetAction()
            {
                Visible = false,
                Location = new Location() { X = 0, Y = 0 },
                CastingActionNameVisible = true,
                CastingRateVisible = true,
                CastingRemainVisible = false,
                CastingRemainInInteger = true,
                WaveSoundEnabled = false,
                WaveFile = string.Empty,
                TTSEnabled = false,
                UseCircle = false,
                IsCircleReverse = false,
                CircleBlurRadius = 14,
                DisplayText = new DisplayText()
                {
                    Font = DefaultFont,
                    Color = Colors.White,
                    OutlineColor = Colors.Gold
                },
                ProgressBar = new ProgressBar()
                {
                    Height = 8,
                    Width = 250,
                    ColorRange = new ObservableCollection<ProgressBarColorRange>()
                    {
                        new ProgressBarColorRange() { Min = 0, Max = 74, Color = Colors.DarkOrange },
                        new ProgressBarColorRange() { Min = 74, Max = 100, Color = Colors.Gold },
                    },
                    LinkOutlineColor = true,
                    OutlineColor = DefaultColorStroke,
                }
            }},

            { nameof(Settings.MyHP), new MyStatus()
            {
                Visible = false,
                Location = new Location() { X = 200, Y = 200 },
                HideInNotCombat = true,
                Size = new BindableSize() { W = 250, H = 50 },
                Scale = 1.0d,
                VisibleText = true,
                VisibleBar = true,
                LinkFontColorToBarColor = false,
                LinkFontOutlineColorToBarColor = true,
                TextLocation = new Location { X = 10, Y = 0 },
                TextHorizontalAlignment = HorizontalAlignment.Right,
                BarLocation = new Location { X = 10, Y = 20 },
                BarStyle = StatusStyles.Horizontal,
                DisplayText = new DisplayText()
                {
                    Font = DefaultFont,
                    Color = Colors.White,
                    OutlineColor = Colors.Navy,
                },
                ProgressBar = new ProgressBar()
                {
                    Height = 10,
                    Width = 200,
                    ColorRange = new ObservableCollection<ProgressBarColorRange>()
                    {
                        new ProgressBarColorRange() { Min = 0, Max = 10, Color = Colors.Red },
                        new ProgressBarColorRange() { Min = 10, Max = 20, Color = Colors.OrangeRed },
                        new ProgressBarColorRange() { Min = 20, Max = 50, Color = Colors.DarkOrange },
                        new ProgressBarColorRange() { Min = 50, Max = 75, Color = Colors.LightSeaGreen },
                        new ProgressBarColorRange() { Min = 75, Max = 100, Color = Colors.RoyalBlue },
                    },
                    LinkOutlineColor = true,
                    OutlineColor = DefaultColorStroke,
                },
            }},

            { nameof(Settings.MyMP), new MyStatus()
            {
                Visible = false,
                Location = new Location() { X = 200, Y = 230 },
                HideInNotCombat = true,
                Size = new BindableSize() { W = 250, H = 50 },
                Scale = 1.0d,
                VisibleText = true,
                VisibleBar = true,
                LinkFontColorToBarColor = false,
                LinkFontOutlineColorToBarColor = true,
                TextLocation = new Location { X = 10, Y = 0 },
                TextHorizontalAlignment = HorizontalAlignment.Right,
                BarLocation = new Location { X = 10, Y = 20 },
                BarStyle = StatusStyles.Horizontal,
                DisplayText = new DisplayText()
                {
                    Font = DefaultFont,
                    Color = Colors.White,
                    OutlineColor = Colors.Navy,
                },
                ProgressBar = new ProgressBar()
                {
                    Height = 10,
                    Width = 200,
                    ColorRange = new ObservableCollection<ProgressBarColorRange>()
                    {
                        new ProgressBarColorRange() { Min = 0, Max = 30, Color = Colors.Red },
                        new ProgressBarColorRange() { Min = 30, Max = 60, Color = Colors.OrangeRed },
                        new ProgressBarColorRange() { Min = 60, Max = 90, Color = Colors.LightSeaGreen },
                        new ProgressBarColorRange() { Min = 90, Max = 100, Color = Colors.RoyalBlue },
                    },
                    LinkOutlineColor = true,
                    OutlineColor = DefaultColorStroke,
                },
                TargetJobs = DefaultMPOverlayTargetJobs,
            }},

            { nameof(Settings.MPTicker), new MPTicker()
            {
                Visible = false,
                CounterVisible = true,
                Location = new Location() { X = 0, Y = 0 },
                TargetJobs = DefaultMPTickerTargetJobs,
                ExplationTimeForDisplay = 60,
                UseCircle = false,
                IsCircleReverse = false,
                SwapBarAndText = false,
                DisplayText = new DisplayText()
                {
                    Font = DefaultFont,
                    Color = Colors.White,
                    OutlineColor = Colors.Gold
                },
                ProgressBar = new ProgressBar()
                {
                    Height = 8,
                    Width = 100,
                    ColorRange = new ObservableCollection<ProgressBarColorRange>()
                    {
                        new ProgressBarColorRange() { Min = 1, Max = 3, Color = Colors.DarkOrange },
                        new ProgressBarColorRange() { Min = 0, Max = 1, Color = Colors.Gold },
                    },
                    LinkOutlineColor = true,
                    OutlineColor = DefaultColorStroke,
                }
            } },

            { nameof(Settings.MyMarker), new MyMarker()
            {
                Visible = false,
                Location = new Location() { X = 0, Y = 0 },
                Scale = 1.0d,
                DisplayText = new DisplayText()
                {
                    Font = DefaultFont,
                    Color = Colors.White,
                    OutlineColor = Colors.Gold
                },
                MarkerType = MyMarkerTypes.ArrowUp,
            }},

            { nameof(Settings.MyUtility), new MyUtility()
            {
                DelayFromWipeout = 3,

                ExtendMealEffect = new ExtendMealEffectSendKeyConfig()
                {
                    RemainingTimeThreshold = 30
                },
                RestoreTankStance = new MyUtilitySendKeyConfig(),
                SummonFairy = new MyUtilitySendKeyConfig(),
                DrawCard = new MyUtilitySendKeyConfig(),
                SummonEgi = new MyUtilitySendKeyConfig(),
            } },

            #endregion Me

            #region MobList

            { nameof(Settings.MobList), new MobList()
            {
                Visible = false,
                Scale = 1.0d,
                RefreshRateMin = 300,
                VisibleZ = true,
                VisibleMe = true,
                IsScanNPC = true,
                DisplayCount = 10,
                NearDistance = 20,
                TTSEnabled = true,
                Location = new Location() { X = 0, Y = 0 },

                MeDisplayText = new DisplayText()
                {
                    Font = DefaultFont,
                    Color = Colors.White,
                    OutlineColor = Colors.Navy
                },

                MobFont = DefaultFont,

                MobEXColor = new FontColor()
                {
                    Color = Colors.White,
                    OutlineColor = Colors.Gold
                },

                MobSColor = new FontColor()
                {
                    Color = Colors.White,
                    OutlineColor = Colors.DarkOrange
                },

                MobAColor = new FontColor()
                {
                    Color = Colors.White,
                    OutlineColor = Colors.DarkSeaGreen
                },

                MobBColor = new FontColor()
                {
                    Color = Colors.White,
                    OutlineColor = Colors.DeepSkyBlue
                },

                MobOtherColor = new FontColor()
                {
                    Color = Colors.White,
                    OutlineColor = Colors.Black
                },
            } },

            #endregion MobList

            #region Enemy

            { nameof(Settings.EnemyHP), new EnemyHP()
            {
                Visible = false,
                Location = new Location() { X = 0, Y = 0 },
                Size = new BindableSize() { W = 400, H = 300 },
                Scale = 1.0,
                HideInNotCombat = true,
                Background = new Color() { A = 128, R = 0, G = 0, B = 0},
                DisplayText = new DisplayText()
                {
                    Font = DefaultFont,
                    Color = DefaultColorFill,
                    OutlineColor = Colors.Navy,
                },
                ProgressBar = new ProgressBar()
                {
                    Height = 3,
                    ColorRange = new ObservableCollection<ProgressBarColorRange>()
                    {
                        new ProgressBarColorRange() { Min = 0, Max = 10, Color = Colors.Red },
                        new ProgressBarColorRange() { Min = 10, Max = 20, Color = Colors.OrangeRed },
                        new ProgressBarColorRange() { Min = 20, Max = 50, Color = Colors.DarkOrange },
                        new ProgressBarColorRange() { Min = 50, Max = 75, Color = Colors.LightSeaGreen },
                        new ProgressBarColorRange() { Min = 75, Max = 100, Color = Colors.RoyalBlue },
                    },
                    LinkOutlineColor = true,
                    OutlineColor = DefaultColorStroke,
                },
            }},

            #endregion Enemy

            #region TacticalRadar

            { nameof(Settings.TacticalRadar), new TacticalRadar()
            {
                Visible = false,
                Scale = 1.0d,
                Location = new Location() { X = 100, Y = 200 },
                DirectionOrigin = DirectionOrigin.Camera,

                DisplayText = new DisplayText()
                {
                    Font = DefaultFont,
                    Color = Colors.White,
                    OutlineColor = Colors.Navy
                },
            } },

            #endregion TacticalRadar
        };

        private static ObservableCollection<JobAvailablity> DefaultMPTickerTargetJobs
        {
            get
            {
                var jobs = new List<JobAvailablity>();
                foreach (var job in Jobs.SortedList
                    .Where(x => x.IsPopular)
                    .Where(x =>
                        x.Role == Roles.Tank ||
                        x.Role == Roles.Healer ||
                        x.Role == Roles.MeleeDPS ||
                        x.Role == Roles.RangeDPS ||
                        x.Role == Roles.MagicDPS))
                {
                    var entry = new JobAvailablity()
                    {
                        Job = job.ID,
                        Available =
                            job.ID == JobIDs.THM ||
                            job.ID == JobIDs.BLM,
                    };

                    jobs.Add(entry);
                }

                return new ObservableCollection<JobAvailablity>(jobs);
            }
        }

        internal static ObservableCollection<JobAvailablity> DefaultMPOverlayTargetJobs
        {
            get
            {
                var jobs = new List<JobAvailablity>();
                foreach (var job in Jobs.SortedList
                    .Where(x => x.IsPopular)
                    .Where(x =>
                        x.Role == Roles.Tank ||
                        x.Role == Roles.Healer ||
                        x.Role == Roles.MeleeDPS ||
                        x.Role == Roles.RangeDPS ||
                        x.Role == Roles.MagicDPS))
                {
                    var entry = new JobAvailablity()
                    {
                        Job = job.ID,
                    };

                    jobs.Add(entry);
                }

                return new ObservableCollection<JobAvailablity>(jobs);
            }
        }

        /// <summary>
        /// Clone
        /// </summary>
        /// <returns>
        /// このオブジェクトのクローン</returns>
        public Settings Clone() => (Settings)this.MemberwiseClone();

        /// <summary>
        /// 初期値に戻す
        /// </summary>
        public void Reset()
        {
            lock (locker)
            {
                var pis = this.GetType().GetProperties();
                foreach (var pi in pis)
                {
                    try
                    {
                        var defaultValue =
                            DefaultValues.ContainsKey(pi.Name) ?
                            DefaultValues[pi.Name] :
                            null;

                        if (defaultValue != null)
                        {
                            pi.SetValue(this, defaultValue);
                        }
                    }
                    catch
                    {
                        Debug.WriteLine($"Settings Reset Error: {pi.Name}");
                    }
                }
            }
        }
    }
}
