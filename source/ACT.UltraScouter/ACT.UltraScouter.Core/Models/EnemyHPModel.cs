using System;
using System.Collections.Generic;
using System.Windows.Media;
using ACT.UltraScouter.Config;
using Prism.Mvvm;

namespace ACT.UltraScouter.Models
{
    public class EnemyHPModel : BindableBase
    {
        public EnemyHP Config => Settings.Instance.EnemyHP;

        private uint id;

        public uint ID
        {
            get => this.id;
            set => this.SetProperty(ref this.id, value);
        }

        private bool isCurrentTarget;

        public bool IsCurrentTarget
        {
            get => this.isCurrentTarget;
            set
            {
                if (this.SetProperty(ref this.isCurrentTarget, value))
                {
                    this.RaisePropertyChanged(nameof(this.BorderBrush));
                }
            }
        }

        public SolidColorBrush BorderBrush => this.isCurrentTarget ? Brushes.WhiteSmoke : Brushes.Transparent;

        public Color BorderBrushColor => this.BorderBrush.Color;

        private double distance;

        public double Distance
        {
            get => this.distance;
            set => this.SetProperty(ref this.distance, value);
        }

        private string name;

        public string Name
        {
            get => this.name;
            set => this.SetProperty(ref this.name, value);
        }

        private uint currentHP;

        public uint CurrentHP
        {
            get => this.currentHP;
            set
            {
                if (this.SetProperty(ref this.currentHP, value))
                {
                    this.RaisePropertyChanged(nameof(CurrentHPRate));
                    this.RaisePropertyChanged(nameof(DeltaHPRate));
                    this.RaisePropertyChanged(nameof(HPColor));
                }
            }
        }

        private uint maxHP;

        public uint MaxHP
        {
            get => this.maxHP;
            set
            {
                if (this.SetProperty(ref this.maxHP, value))
                {
                    this.RaisePropertyChanged(nameof(CurrentHPRate));
                    this.RaisePropertyChanged(nameof(HPColor));
                }
            }
        }

        public double CurrentHPRate => this.MaxHP != 0 ? ((double)this.CurrentHP / (double)this.MaxHP) : 0;

        private uint deltaHP;

        public uint DeltaHP
        {
            get => this.deltaHP;
            set => this.SetProperty(ref this.deltaHP, value);
        }

        private double deltaHPRate;

        public double DeltaHPRate
        {
            get => this.deltaHPRate;
            set
            {
                if (this.SetProperty(ref this.deltaHPRate, value))
                {
                    this.RaisePropertyChanged(nameof(this.DeltaHPRateAbs));
                    this.RaisePropertyChanged(nameof(this.DeltaHPSign));
                }
            }
        }

        public double DeltaHPRateAbs => Math.Abs(this.deltaHPRate);

        public DeltaHPSign DeltaHPSign => this.deltaHPRate switch
        {
            var x when x > 0 => DeltaHPSign.Positive,
            var x when x < 0 => DeltaHPSign.Negative,
            _ => DeltaHPSign.Zero,
        };

        private bool isExistsDelta;

        public bool IsExistsDelta
        {
            get => this.isExistsDelta;
            set => this.SetProperty(ref this.isExistsDelta, value);
        }

        public SolidColorBrush HPColor => GetBrush(Settings.Instance.EnemyHP.ProgressBar.AvailableColor(this.CurrentHPRate * 100));

        private static readonly Dictionary<Color, SolidColorBrush> CachedBrushes = new Dictionary<Color, SolidColorBrush>(16);

        public static SolidColorBrush GetBrush(
            Color color)
        {
            lock (CachedBrushes)
            {
                if (CachedBrushes.ContainsKey(color))
                {
                    return CachedBrushes[color];
                }

                var brush = new SolidColorBrush(color);
                brush.Freeze();
                CachedBrushes[color] = brush;

                return brush;
            }
        }
    }

    public enum DeltaHPSign
    {
        Zero,
        Positive,
        Negative,
    }
}
