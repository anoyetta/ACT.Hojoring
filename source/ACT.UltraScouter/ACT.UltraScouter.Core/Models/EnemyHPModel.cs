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
            set
            {
                if (this.SetProperty(ref this.deltaHP, value))
                {
                    this.RaisePropertyChanged(nameof(IsExistsDelta));
                    this.RaisePropertyChanged(nameof(DeltaHPRate));
                }
            }
        }

        public bool IsExistsDelta => this.deltaHP != 0;

        public double DeltaHPRate => this.CurrentHP != 0 ? ((double)this.DeltaHP / (double)(this.CurrentHP + this.DeltaHP)) : 0;

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
}
