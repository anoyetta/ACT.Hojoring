using Prism.Mvvm;

namespace FFXIV.Framework.XIVHelper
{
    public class Zone :
        BindableBase
    {
        private int id = 0;
        private int idonDB = 0;
        private bool isAddedByUser = false;
        private string name = string.Empty;
        private int rank;

        public int ID
        {
            get => this.id;
            set => this.SetProperty(ref this.id, value);
        }

        public int IDonDB
        {
            get => this.idonDB;
            set => this.SetProperty(ref this.idonDB, value);
        }

        public bool IsAddedByUser
        {
            get => this.isAddedByUser;
            set => this.SetProperty(ref this.isAddedByUser, value);
        }

        public string Name
        {
            get => this.name;
            set => this.SetProperty(ref this.name, value);
        }

        public int Rank
        {
            get => this.rank;
            set => this.SetProperty(ref this.rank, value);
        }

        public override string ToString() => this.Name;

        public static int ToRank(
            int intendedUse,
            string name)
        {
            var rank = 0;

            // レイド
            if (intendedUse == (int)TerritoryIntendedUse.Raid8)
            {
                if (name.Contains("絶") ||
                    name.Contains("Ultimate") ||
                    name.Contains("fatal"))
                {
                    rank = 10;
                }
                else
                {
                    if (name.Contains("零式") ||
                        name.Contains("Savage") ||
                        name.Contains("sadique") ||
                        name.Contains("episch"))
                    {
                        rank = 11;
                    }
                    else
                    {
                        rank = 12;
                    }
                }
            }

            // 討滅戦
            if (intendedUse == (int)TerritoryIntendedUse.Trial)
            {
                rank = 20;
            }

            // 24人レイド
            if (intendedUse == (int)TerritoryIntendedUse.Raid24)
            {
                rank = 50;
            }

            // ディープダンジョン
            if (intendedUse == (int)TerritoryIntendedUse.DeepDungeon)
            {
                rank = 60;
            }

            // PvP
            if (intendedUse == (int)TerritoryIntendedUse.PvP1 ||
                intendedUse == (int)TerritoryIntendedUse.PvP2 ||
                intendedUse == (int)TerritoryIntendedUse.PvP3)
            {
                rank = 90;
            }

            return rank;
        }
    }

    public enum TerritoryIntendedUse
    {
        Dungeon = 3,
        Raid24 = 8,
        Raid8 = 17,
        Trial = 10,
        PvP1 = 18,
        PvP2 = 39,
        PvP3 = 42,
        DeepDungeon = 31,
    }
}
