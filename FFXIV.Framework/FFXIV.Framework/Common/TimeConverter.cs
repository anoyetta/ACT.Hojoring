using System;
using System.ComponentModel;
using Prism.Mvvm;

namespace FFXIV.Framework.Common
{
    public enum EorzeaCalendarLocale
    {
        EN = 0,
        JA
    }

    /// <summary>
    /// エオルゼアの月の属性
    /// </summary>
    public enum EorzeaTerm
    {
        Ice = 0,
        Water,
        Wind,
        Lightning,
        Fire,
        Earth,
    }

    /// <summary>
    /// エオルゼアの月（Moon）
    /// </summary>
    public enum EorzeaMoon
    {
        Astral1st = 0,
        Umbral1st,
        Astral2nd,
        Umbral2nd,
        Astral3rd,
        Umbral3rd,
        Astral4th,
        Umbral4th,
        Astral5th,
        Umbral5th,
        Astral6th,
        Umbral6th,
    }

    /// <summary>
    /// エオルゼアの日（Sun）
    /// </summary>
    public enum EorzeaSun
    {
        WindSun = 0,
        LightningSun,
        FireSun,
        EarthSun,
        IceSun,
        WaterSun,
        AstralSun,
        UmbralSun,
    }

    /// <summary>
    /// エオルゼアの刻
    /// </summary>
    public enum EorzeaTimeElement
    {
        IceTime = 0,
        WaterTime,
        WindTime,
        LightningTime,
        FireTime,
        EarthTime
    }

    public static class TimeConverter
    {
        /// <summary>
        /// エオルゼア時間の基点
        /// </summary>
        /// <remarks>
        /// UTC 1970/1/1 0:0:0 を基点とする
        /// </remarks>
        private static readonly DateTimeOffset EorzeaTimeOriginByUTC =
            new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

        /// <summary>
        /// 地球時間:エオルゼア時間比
        /// </summary>
        private static readonly double LTETRatio = 1440d / 70d;

        /// <summary>
        /// 地球時間をエオルゼア時間に変換する
        /// </summary>
        /// <param name="localTime">
        /// 地球の現地時間</param>
        /// <returns>
        /// エオルゼア時間</returns>
        public static EorzeaTime ToEorzeaTime(
            DateTimeOffset localTime)
        {
            // UTCにおける差分を算出する
            var diff = localTime.ToUniversalTime() - EorzeaTimeOriginByUTC;

            var totalSec = diff.TotalSeconds;

            var delta = totalSec * LTETRatio;

            var secound = (int)Math.Truncate(delta % 60d);
            delta -= secound;
            delta /= 60.0d;

            var minute = (int)Math.Truncate(delta % 60d);
            delta -= minute;
            delta /= 60.0d;

            var hour = (int)Math.Truncate(delta % 24d);
            delta -= hour;
            delta /= 24.0d;

            var day = (int)Math.Truncate(delta % 32d);
            delta -= day;
            delta /= 32.0d;

            var month = (int)Math.Truncate(delta % 12d);
            delta -= month;
            delta /= 12.0d;

            var year = (int)Math.Truncate(delta);

            // 年月日は1オリジンなので1を加える
            var et = new EorzeaTime();
            et.Year = (1 + year) % 10000;
            et.Month = 1 + month;
            et.Day = 1 + day;
            et.Hour = hour;
            et.Minute = minute;
            et.Second = 0;

            return et;
        }

        public static DateTimeOffset FromEorzeaTime(
            EorzeaTime eorzeaTime)
        {
            var delta = 0d;

            delta += (eorzeaTime.Year - 1) * 12 * 32 * 24 * 60 * 60;
            delta += (eorzeaTime.Month - 1) * 32 * 24 * 60 * 60;
            delta += (eorzeaTime.Day - 1) * 24 * 60 * 60;
            delta += eorzeaTime.Hour * 60 * 60;
            delta += eorzeaTime.Minute * 60;
            delta += eorzeaTime.Second;

            var totalSec = delta / LTETRatio;

            var utc = EorzeaTimeOriginByUTC.AddSeconds(totalSec);

            return utc.ToLocalTime();
        }

        public static EorzeaTerm ToTerm(
            this EorzeaMoon moon)
            => new[]
            {
                EorzeaTerm.Ice,
                EorzeaTerm.Ice,
                EorzeaTerm.Water,
                EorzeaTerm.Water,
                EorzeaTerm.Wind,
                EorzeaTerm.Wind,
                EorzeaTerm.Lightning,
                EorzeaTerm.Lightning,
                EorzeaTerm.Fire,
                EorzeaTerm.Fire,
                EorzeaTerm.Earth,
                EorzeaTerm.Earth,
            }[(int)moon];

        public static EorzeaSun ToSun(
            int day)
            => new[]
            {
                EorzeaSun.WindSun,
                EorzeaSun.LightningSun,
                EorzeaSun.FireSun,
                EorzeaSun.EarthSun,
                EorzeaSun.IceSun,
                EorzeaSun.WaterSun,
                EorzeaSun.AstralSun,
                EorzeaSun.UmbralSun,
            }[(day % 8) - 1];

        public static EorzeaTimeElement ToElement(
            int hour)
            => new[]
            {
                EorzeaTimeElement.IceTime,
                EorzeaTimeElement.WaterTime,
                EorzeaTimeElement.WindTime,
                EorzeaTimeElement.LightningTime,
                EorzeaTimeElement.FireTime,
                EorzeaTimeElement.EarthTime,
            }[hour / 4];

        public static string ToText(
            this EorzeaTerm value,
            EorzeaCalendarLocale locale = EorzeaCalendarLocale.EN)
            => new[,]
            {
                { "氷属月", "Ice" },
                { "水属月", "Water" },
                { "風属月", "Wind" },
                { "雷属月", "Lightning" },
                { "火属月", "Fire" },
                { "土属月", "Earth" },
            }[(int)value, (int)locale];

        public static string ToText(
            this EorzeaMoon value,
            EorzeaCalendarLocale locale = EorzeaCalendarLocale.EN)
            => new[,]
            {
                { "星1月", "1st Astral Moon" },
                { "霊1月", "1st Umbral Moon" },
                { "星2月", "2nd Astral Moon" },
                { "霊2月", "2nd Umbral Moon" },
                { "星3月", "3rd Astral Moon" },
                { "霊3月", "3rd Umbral Moon" },
                { "星4月", "4th Astral Moon" },
                { "霊4月", "4th Umbral Moon" },
                { "星5月", "5th Astral Moon" },
                { "霊5月", "5th Umbral Moon" },
                { "星6月", "6th Astral Moon" },
                { "霊6月", "6th Umbral Moon" },
            }[(int)value, (int)locale];

        public static string ToText(
            this EorzeaSun value,
            EorzeaCalendarLocale locale = EorzeaCalendarLocale.EN)
            => new[,]
            {
                { "風属日", "Wind Sun" },
                { "雷属日", "Lightning Sun" },
                { "火属日", "Fire Sun" },
                { "土属日", "Earth Sun" },
                { "氷属日", "Ice Sun" },
                { "水属日", "Water Sun" },
                { "星極日", "Astral Sun" },
                { "霊極日", "Umbral Sun" },
            }[(int)value, (int)locale];

        public static string ToText(
            this EorzeaTimeElement value,
            EorzeaCalendarLocale locale = EorzeaCalendarLocale.EN)
            => new[,]
            {
                { "氷の刻", "Ice Time" },
                { "水の刻", "Water Time" },
                { "風の刻", "Wind Time" },
                { "雷の刻", "Lightning Time" },
                { "火の刻", "Fire Time" },
                { "土の刻", "Earth Time" },
            }[(int)value, (int)locale];
    }

    public static class DateTimeOffsetEx
    {
        public static EorzeaTime ToEorzeaTime(
            this DateTimeOffset localTime)
            => TimeConverter.ToEorzeaTime(localTime);
    }

    public class EorzeaTime :
        BindableBase
    {
        public EorzeaTime()
        {
            this.PropertyChanged += this.EorzeaTime_PropertyChanged;
        }

        public static EorzeaCalendarLocale DefaultLocale
        {
            get;
            set;
        } = EorzeaCalendarLocale.EN;

        private void EorzeaTime_PropertyChanged(
            object sender,
            PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(this.Locale):
                    this.RaisePropertyChanged(nameof(this.TermText));
                    this.RaisePropertyChanged(nameof(this.MoonText));
                    this.RaisePropertyChanged(nameof(this.SunText));
                    this.RaisePropertyChanged(nameof(this.ElementOfTimeText));
                    this.RaisePropertyChanged(nameof(this.NextElementOfTimeText));
                    break;

                case nameof(this.Month):
                    this.RaisePropertyChanged(nameof(this.Term));
                    this.RaisePropertyChanged(nameof(this.Moon));
                    this.RaisePropertyChanged(nameof(this.TermText));
                    this.RaisePropertyChanged(nameof(this.MoonText));
                    break;

                case nameof(this.Day):
                    this.RaisePropertyChanged(nameof(this.Sun));
                    this.RaisePropertyChanged(nameof(this.SunText));
                    break;

                case nameof(this.Hour):
                    this.RaisePropertyChanged(nameof(this.ElementOfTime));
                    this.RaisePropertyChanged(nameof(this.ElementOfTimeText));
                    this.RaisePropertyChanged(nameof(this.NextElementOfTime));
                    this.RaisePropertyChanged(nameof(this.NextElementOfTimeText));
                    break;
            }
        }

        public static EorzeaTime Now => DateTimeOffset.Now.ToEorzeaTime();

        private EorzeaCalendarLocale locale = DefaultLocale;

        public EorzeaCalendarLocale Locale
        {
            get => this.locale;
            set => this.SetProperty(ref this.locale, value);
        }

        private int year;

        public int Year
        {
            get => this.year;
            set => this.SetProperty(ref this.year, value);
        }

        private int month;

        public int Month
        {
            get => this.month;
            set => this.SetProperty(ref this.month, value);
        }

        private int day;

        public int Day
        {
            get => this.day;
            set => this.SetProperty(ref this.day, value);
        }

        private int hour;

        public int Hour
        {
            get => this.hour;
            set => this.SetProperty(ref this.hour, value);
        }

        private int minute;

        public int Minute
        {
            get => this.minute;
            set => this.SetProperty(ref this.minute, value);
        }

        private int secound;

        public int Second
        {
            get => this.secound;
            set => this.SetProperty(ref this.secound, value);
        }

        public EorzeaTerm Term => this.Moon.ToTerm();

        public string TermText => this.Term.ToText(this.Locale);

        public EorzeaMoon Moon => (EorzeaMoon)(this.Month - 1);

        public string MoonText => this.Moon.ToText(this.Locale);

        public EorzeaSun Sun => TimeConverter.ToSun(this.Day);

        public string SunText => this.Sun.ToText(this.Locale);

        public EorzeaTimeElement ElementOfTime => TimeConverter.ToElement(this.Hour);

        public EorzeaTimeElement NextElementOfTime
        {
            get
            {
                var next = (int)this.ElementOfTime + 1;
                if (next > (int)EorzeaTimeElement.EarthTime)
                {
                    next = 0;
                }

                return (EorzeaTimeElement)next;
            }
        }

        public TimeSpan NextElementOfTimeRemainSeconds
        {
            get
            {
                var elementStartTime = new EorzeaTime()
                {
                    Year = this.Year,
                    Month = this.Month,
                    Day = this.Day,
                    Hour = this.Hour / 4,
                    Minute = 0,
                    Second = 0,
                };

                var elementDurationSeconds =
                    (this.ToLocalTime() - elementStartTime.ToLocalTime()).TotalSeconds;

                var remain = 700 - (int)elementDurationSeconds;

                return TimeSpan.FromSeconds(remain);
            }
        }

        public string ElementOfTimeText => this.ElementOfTime.ToText(this.Locale);

        public string NextElementOfTimeText => this.NextElementOfTime.ToText(this.Locale);

        public DateTimeOffset ToLocalTime() => TimeConverter.FromEorzeaTime(this);

        public override string ToString()
            => $"{this.Year:0000}/{this.Month:00}/{this.Day:00} {this.Hour:00}:{this.Minute:00}:{this.Second:00}";

        public string ToStringFlat()
            => $"{this.Year:0000}{this.Month:00}{this.Day:00}{this.Hour:00}{this.Minute:00}{this.Second:00}";

        public long ToLong()
            => long.Parse(this.ToStringFlat());

        public EorzeaTime Clone() => this.MemberwiseClone() as EorzeaTime;
    }
}
