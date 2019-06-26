using System.Collections.ObjectModel;
using FFXIV.Framework.Common;
using FFXIV.Framework.XIVHelper;

namespace ACT.UltraScouter.Models
{
    public class MobListModel :
        TargetInfoModel
    {
        #region Singleton

        private static MobListModel instance = new MobListModel();
        public new static MobListModel Instance => instance;

        public MobListModel()
        {
#if DEBUG
            if (WPFHelper.IsDesignMode)
            {
                this.mobList.Clear();

                var mob1 = new MobInfo()
                {
                    Name = "テストモブ1",
                    Direction = Direction.N,
                    DirectionText = Arrows.N,
                    DirectionAngle = -90,
                    Distance = 120.0,
                    Rank = "S",
                    X = 10.0,
                    Y = 20.0,
                    Z = 30.0,
                };

                var mob2 = new MobInfo()
                {
                    Name = "テストテストモブ2軍曹",
                    Direction = Direction.N,
                    DirectionText = Arrows.NE,
                    DirectionAngle = -45,
                    Distance = 8.0,
                    Rank = "EX",
                    X = 10.0,
                    Y = 20.0,
                    Z = 30.0,
                };

                var mob3 = new MobInfo()
                {
                    Name = "テストテストモブ3軍曹",
                    Direction = Direction.N,
                    DirectionText = Arrows.SE,
                    DirectionAngle = 45,
                    Distance = 10.0,
                    Rank = "EX",
                    X = 10.0,
                    Y = 20.0,
                    Z = 30.0,
                };

                var mob4 = new MobInfo()
                {
                    Name = "テストテストモブ4軍曹",
                    Direction = Direction.N,
                    DirectionText = Arrows.E,
                    DirectionAngle = 0,
                    Distance = 10.0,
                    Rank = "EX",
                    X = 10.0,
                    Y = 20.0,
                    Z = 30.0,
                };

                this.mobList.Add(mob1);
                this.mobList.Add(mob2);
                this.mobList.Add(mob3);
                this.mobList.Add(mob4);
            }
#endif
        }

        #endregion Singleton

        private ObservableCollection<MobInfo> mobList = new ObservableCollection<MobInfo>();

        public ObservableCollection<MobInfo> MobList => this.mobList;

        private int mobListCount = 0;

        public int MobListCount
        {
            get => this.mobListCount;
            set => this.SetProperty(ref this.mobListCount, value);
        }

        private double meX;
        private double meY;
        private double meZ;

        public double MeX
        {
            get => this.meX;
            set
            {
                if (this.SetProperty(ref this.meX, value))
                {
                    this.RaisePropertyChanged(nameof(this.MeXonMap));
                }
            }
        }

        public double MeY
        {
            get => this.meY;
            set
            {
                if (this.SetProperty(ref this.meY, value))
                {
                    this.RaisePropertyChanged(nameof(this.MeYonMap));
                }
            }
        }

        public double MeZ
        {
            get => this.meZ;
            set
            {
                if (this.SetProperty(ref this.meZ, value))
                {
                    this.RaisePropertyChanged(nameof(this.MeZonMap));
                }
            }
        }

        public double MeXonMap => CombatantEx.ToHorizontalMapPosition(this.MeX);
        public double MeYonMap => CombatantEx.ToHorizontalMapPosition(this.MeY);
        public double MeZonMap => CombatantEx.ToVerticalMapPosition(this.MeZ);

        public void ClearMobList()
        {
            lock (this.MobList)
            {
                this.MobList.Clear();
            }
        }

        public void RaiseAllPropertiesChanged()
        {
            foreach (var pi in this.GetType().GetProperties())
            {
                this.RaisePropertyChanged(pi.Name);
            }

            foreach (var mob in this.MobList)
            {
                mob.RaiseAllPropertiesChanged();
            }
        }
    }
}
