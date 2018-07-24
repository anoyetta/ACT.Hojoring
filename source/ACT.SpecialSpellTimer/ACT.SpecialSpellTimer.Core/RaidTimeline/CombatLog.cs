using System;
using System.Windows;
using System.Windows.Media;
using ACT.SpecialSpellTimer.Config;
using FFXIV.Framework.Extensions;
using Prism.Mvvm;

namespace ACT.SpecialSpellTimer.RaidTimeline
{
    /// <summary>
    /// 戦闘ログ
    /// </summary>
    public class CombatLog :
        BindableBase
    {
        private long no;

        public long No
        {
            get => this.no;
            set => this.SetProperty(ref this.no, value);
        }

        /// <summary>
        /// 一意な連番
        /// </summary>
        public long ID { get; set; } = 0;

        private bool isOrigin;

        /// <summary>
        /// 起点？
        /// </summary>
        public bool IsOrigin
        {
            get => this.isOrigin;
            set
            {
                if (this.SetProperty(ref this.isOrigin, value))
                {
                    this.RaisePropertyChanged(nameof(this.FontWeight));
                    this.RaisePropertyChanged(nameof(this.Foreground));
                    this.RaisePropertyChanged(nameof(this.Background));
                }
            }
        }

        /// <summary>
        /// ログのタイムスタンプ
        /// </summary>
        public DateTime TimeStamp { get; set; } = DateTime.MinValue;

        private TimeSpan timeStampElapted = TimeSpan.Zero;

        /// <summary>
        /// 経過時間
        /// </summary>
        public TimeSpan TimeStampElapted
        {
            get => this.timeStampElapted;
            set
            {
                if (this.SetProperty(ref this.timeStampElapted, value))
                {
                    this.RaisePropertyChanged(nameof(this.TimeStampElaptedString));
                }
            }
        }

        /// <summary>
        /// 経過時間
        /// </summary>
        public string TimeStampElaptedString =>
            Settings.Default.TimelineTotalSecoundsFormat ?
            this.TimeStampElapted.ToSecondString() :
            this.TimeStampElapted.ToTLString();

        private LogTypes logType = LogTypes.Unknown;

        /// <summary>
        /// ログの種類
        /// </summary>
        public LogTypes LogType
        {
            get => this.logType;
            set => this.SetProperty(ref this.logType, value);
        }

        /// <summary>
        /// ログの種類
        /// </summary>
        public string LogTypeName => this.LogType.ToText();

        /// <summary>
        /// 関連したスキル
        /// </summary>
        public string Skill { get; set; } = string.Empty;

        /// <summary>
        /// 発生したActivity
        /// </summary>
        public string Activity { get; set; } = string.Empty;

        /// <summary>
        /// Actor
        /// </summary>
        public string Actor { get; set; } = string.Empty;

        /// <summary>
        /// Actorの残HP率
        /// </summary>
        public decimal HPRate { get; set; }

        /// <summary>
        /// Actorの残HP率
        /// </summary>
        public string HPRateText =>
            this.HPRate != 0 ?
            $"{this.HPRate:N0}%" :
            string.Empty;

        /// <summary>
        /// ナマのログ
        /// </summary>
        public string Raw { get; set; } = string.Empty;

        /// <summary>
        /// ナマのログからタイムスタンプを除去した部分
        /// </summary>
        public string RawWithoutTimestamp => this.Raw.Substring(15);

        public string Zone { get; set; } = string.Empty;

        public string Text { get; set; } = null;

        public string SyncKeyword { get; set; } = null;

        public FontWeight FontWeight =>
            this.IsOrigin ? FontWeights.Black : FontWeights.Normal;

        public SolidColorBrush Foreground =>
            new SolidColorBrush(
                this.LogType.ToForegroundColor());

        public SolidColorBrush Background =>
            new SolidColorBrush(
                this.LogType.ToBackgroundColor());

        public SolidColorBrush BackgroundLine =>
            (
                this.LogType == LogTypes.CombatStart ||
                this.LogType == LogTypes.CombatEnd ||
                this.LogType == LogTypes.Dialog
            ) ?
            new SolidColorBrush(this.LogType.ToBackgroundColor()) :
            Brushes.Transparent;
    }
}
