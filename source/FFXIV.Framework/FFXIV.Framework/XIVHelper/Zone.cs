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
            get
            {
                var rank = 255;

                // レイド
                if (this.IDonDB >= 30000 && this.IDonDB < 40000)
                {
                    rank = 119;

                    if (this.Name.Contains("絶") ||
                        this.Name.Contains("Ultimate") ||
                        this.Name.Contains("fatal"))
                    {
                        rank = 109;
                    }

                    if (this.Name.Contains("零式") ||
                        this.Name.Contains("Savage") ||
                        this.Name.Contains("sadique") ||
                        this.Name.Contains("episch"))
                    {
                        rank = 110;
                    }
                }

                // 討滅戦
                if (this.IDonDB >= 20000 && this.IDonDB < 30000)
                {
                    rank = 129;
                }

                // PvP
                if (this.IDonDB >= 40000 && this.IDonDB < 55000)
                {
                    rank = 139;
                }

                // misc
                if (rank == 255)
                {
                    if (this.Name.Contains("Hard") ||
                        this.Name.Contains("brutal"))
                    {
                        rank = 210;
                    }
                }

                return rank;
            }
        }

        public override string ToString() => this.Name;
    }
}
