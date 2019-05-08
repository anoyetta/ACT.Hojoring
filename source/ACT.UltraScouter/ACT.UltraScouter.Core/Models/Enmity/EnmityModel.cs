using System.Windows.Media;
using System.Windows.Media.Imaging;
using ACT.UltraScouter.Config;
using FFXIV.Framework.Common;
using FFXIV.Framework.FFXIVHelper;
using Prism.Mvvm;

namespace ACT.UltraScouter.Models.Enmity
{
    public class EnmityModel :
        BindableBase
    {
        public static readonly GenericEqualityComparer<EnmityModel> EnmityModelComparer = new GenericEqualityComparer<EnmityModel>(
            (x, y) => x.ID == y.ID,
            (obj) => obj.ID.GetHashCode());

        public UltraScouter.Config.Enmity Config => Settings.Instance.Enmity;

        private int index;

        public int Index
        {
            get => this.index;
            set => this.SetProperty(ref this.index, value);
        }

        private uint id;

        public uint ID
        {
            get => this.id;
            set => this.SetProperty(ref this.id, value);
        }

        private string name;

        public string Name
        {
            get => this.name;
            set => this.SetProperty(ref this.name, value);
        }

        private JobIDs jobID = JobIDs.Unknown;

        public JobIDs JobID
        {
            get => this.jobID;
            set
            {
                if (this.SetProperty(ref this.jobID, value))
                {
                    this.RaisePropertyChanged(nameof(this.JobIcon));
                    this.RaisePropertyChanged(nameof(this.BarColorBrush));
                }
            }
        }

        public BitmapSource JobIcon => JobIconDictionary.Instance.GetIcon(this.jobID);

        private static SolidColorBrush MeBrush;
        private static SolidColorBrush TankBrush;
        private static SolidColorBrush HealerBrush;
        private static SolidColorBrush DPSBrush;

        public static void CreateBrushes()
        {
            if (MeBrush != null)
            {
                return;
            }

            /*
            彩度調整版
            MeBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#e6b422"));
            TankBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00a1e9"));
            HealerBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#82ae46"));
            DPSBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#e60033"));
            */

            // デフォルトカラー
            MeBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#e6b422"));
            TankBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3849A1"));
            HealerBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#467737"));
            DPSBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#AF3748"));
        }

        public SolidColorBrush BarColorBrush
        {
            get
            {
                if (this.isMe)
                {
                    return MeBrush;
                }

                var role = Jobs.Find(this.jobID)?.Role ?? Roles.Unknown;
                switch (role)
                {
                    case Roles.Tank:
                        return TankBrush;

                    case Roles.Healer:
                        return HealerBrush;

                    case Roles.MeleeDPS:
                    case Roles.RangeDPS:
                    case Roles.MagicDPS:
                    case Roles.PhysicalDPS:
                    case Roles.DPS:
                        return DPSBrush;

                    default:
                        return Brushes.Gray;
                }
            }
        }

        private bool isTop;

        public bool IsTop
        {
            get => this.isTop;
            set => this.SetProperty(ref this.isTop, value);
        }

        private bool isMe;

        public bool IsMe
        {
            get => this.isMe;
            set
            {
                if (this.SetProperty(ref this.isMe, value))
                {
                    this.RaisePropertyChanged(nameof(this.BarColorBrush));
                }
            }
        }

        private bool isPet;

        public bool IsPet
        {
            get => this.isPet;
            set => this.SetProperty(ref this.isPet, value);
        }

        private double enmity;

        public double Enmity
        {
            get => this.enmity;
            set => this.SetProperty(ref this.enmity, value);
        }

        private double enmityDifference;

        public double EnmityDifference
        {
            get => this.enmityDifference;
            set => this.SetProperty(ref this.enmityDifference, value);
        }

        private float hateRate;

        public float HateRate
        {
            get => this.hateRate;
            set
            {
                if (this.SetProperty(ref this.hateRate, value))
                {
                    this.RaisePropertyChanged(nameof(this.BarWidth));
                }
            }
        }

        public DisplayText DisplayText => Settings.Instance.Enmity.DisplayText;

        public double BarWidth => Settings.Instance.Enmity.BarWidth * this.HateRate;

        public double BarWidthMax => Settings.Instance.Enmity.BarWidth;

        public void RefreshBarWidth()
        {
            this.RaisePropertyChanged(nameof(this.BarWidthMax));
            this.RaisePropertyChanged(nameof(this.BarWidth));
        }

        /// <summary>
        /// すべてのPropertiesの変更通知を発生させる
        /// </summary>
        public void RaiseAllPropertiesChanged()
        {
            foreach (var pi in this.GetType().GetProperties())
            {
                this.RaisePropertyChanged(pi.Name);
            }
        }

        public override string ToString()
            => $"ID={this.ID}, Name={this.Name}, Enmity={this.Enmity}";
    }
}
