using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FFXIV.Framework.Common;
using FFXIV.Framework.Globalization;

namespace FFXIV.Framework.XIVHelper
{
    public class XIVApi
    {
        #region Logger

        private NLog.Logger AppLogger => AppLog.DefaultLogger;

        #endregion Logger

        #region Singleton

        private readonly static Lazy<XIVApi> LazyInstance = new Lazy<XIVApi>(() => new XIVApi());

        public static XIVApi Instance => LazyInstance.Value;

        private XIVApi()
        {
        }

        #endregion Singleton

        #region Resources Files

        public string TerritoryFile => Path.Combine(
            this.ResourcesDirectory + @"\xivdb",
            $@"TerritoryType.{this.FFXIVLocale.ToResourcesName()}.csv");

        public string TerritoryENFile => Path.Combine(
            this.ResourcesDirectory + @"\xivdb",
            $@"TerritoryType.en-US.csv");

        public string AreaFile => Path.Combine(
            this.ResourcesDirectory + @"\xivdb",
            $@"Instance.{this.FFXIVLocale.ToResourcesName()}.csv");

        public string AreaENFile => Path.Combine(
            this.ResourcesDirectory + @"\xivdb",
            $@"Instance.en-US.csv");

        public string PlacenameFile => Path.Combine(
            this.ResourcesDirectory + @"\xivdb",
            $@"Placename.{this.FFXIVLocale.ToResourcesName()}.csv");

        public string SkillFile => Path.Combine(
            this.ResourcesDirectory + @"\xivdb",
            $@"Action.{this.FFXIVLocale.ToResourcesName()}.csv");

        public string UserSkillFile => Path.Combine(
            this.ResourcesDirectory,
            $@"Actions.csv");

        public string BuffFile => Path.Combine(
            this.ResourcesDirectory + @"\xivdb",
            $@"Status.{this.FFXIVLocale.ToResourcesName()}.csv");

        #endregion Resources Files

        #region Resources Lists

        private readonly List<Area> territoryList = new List<Area>(2048);
        private readonly List<Area> areaList = new List<Area>(65535);
        private readonly Dictionary<uint, XIVApiAction> actionList = new Dictionary<uint, XIVApiAction>(20480);
        private readonly Dictionary<uint, Buff> buffList = new Dictionary<uint, Buff>(2560);

        public IReadOnlyList<Area> TerritoryList => this.territoryList;
        public IReadOnlyList<Area> AreaList => this.areaList;
        public IReadOnlyDictionary<uint, XIVApiAction> ActionList => this.actionList;
        public IReadOnlyDictionary<uint, Buff> BuffList => this.buffList;

        #endregion Resources Lists

        public Locales FFXIVLocale
        {
            get;
            set;
        } = Locales.JA;

        private string resourcesDirectory;

        public string ResourcesDirectory =>
            this.resourcesDirectory ?? (this.resourcesDirectory = DirectoryHelper.FindSubDirectory("resources"));

        public XIVApiAction FindAction(
            uint ID)
        {
            if (this.actionList.ContainsKey(ID))
            {
                return this.actionList[ID];
            }

            return null;
        }

        public XIVApiAction FindAction(
            string ID)
        {
            try
            {
                var IDAsInt = Convert.ToUInt32(ID, 16);
                return this.FindAction(IDAsInt);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public void Load()
        {
            var sw = Stopwatch.StartNew();

            Task.WaitAll(
                Task.Run(() => this.LoadTerritory()),
                Task.Run(() => this.LoadArea()),
                Task.Run(() => this.LoadAction()),
                Task.Run(() => this.LoadBuff()));

            sw.Stop();
            _ = sw.Elapsed.TotalSeconds;
        }

        private static readonly int AddtionalTerritoryStartID = 0;

        private void LoadTerritory()
        {
            var en = this.LoadTerritoryCore(this.TerritoryENFile);
            var la = this.LoadTerritoryCore(this.TerritoryFile);

            if (en == null ||
                la == null)
            {
                return;
            }

            var dic = en.ToDictionary(x => x.ID);

            foreach (var item in la)
            {
                if (dic.ContainsKey(item.ID))
                {
                    item.NameEn = dic[item.ID].Name;
                }
            }

            this.territoryList.Clear();
            this.territoryList.AddRange(la);
        }

        private List<Area> LoadTerritoryCore(string file)
        {
            if (!File.Exists(file))
            {
                return null;
            }

            var list = new List<Area>(2048);

            var lines = CSVParser.LoadFromPath(file, encoding: new UTF8Encoding(true));

            Parallel.ForEach(lines, (fields) =>
            {
                if (fields.Count < 5)
                {
                    return;
                }

                if (!int.TryParse(fields[0], out int id) ||
                    string.IsNullOrEmpty(fields[6]) ||
                    !int.TryParse(fields[10], out int indented) ||
                    !int.TryParse(fields[11], out int order))
                {
                    return;
                }

                if (id < AddtionalTerritoryStartID)
                {
                    return;
                }

                var entry = new Area()
                {
                    ID = id,
                    Name = fields[6],
                    IntendedUse = indented,
                    Order = order,
                };

                lock (list)
                {
                    list.Add(entry);
                }
            });

            return list;
        }

        private void LoadArea()
        {
            if (!File.Exists(this.AreaFile))
            {
                return;
            }

            // 英語辞書として英語版リストをロードしておく
            var en = this.AreaENDictionary;

            var current = new List<Area>(256);

            var lines = CSVParser.LoadFromPath(this.AreaFile, encoding: new UTF8Encoding(true));

            Parallel.ForEach(lines, (fields) =>
            {
                if (fields.Count < 5)
                {
                    return;
                }

                if (!int.TryParse(fields[0], out int id) ||
                    string.IsNullOrEmpty(fields[4]))
                {
                    return;
                }

                var entry = new Area()
                {
                    ID = id,
                    NameEn = en[id]?.Name ?? string.Empty,
                    Name = fields[4]
                };

                lock (current)
                {
                    current.Add(entry);
                }
            });

            lock (this.areaList)
            {
                this.areaList.Clear();
                this.areaList.AddRange(current);
            }

            this.AppLogger.Trace($"xivapi area list loaded. {this.AreaFile}");
        }

        private Dictionary<int, Area> _areaENDictionary;

        private Dictionary<int, Area> AreaENDictionary => this._areaENDictionary ??= this.LoadAreaEN();

        private Dictionary<int, Area> LoadAreaEN()
        {
            var dic = new Dictionary<int, Area>()
            {
                { 0, new Area() {ID = 0, NameEn = "dummy", Name = "dummy"} }
            };

            if (!File.Exists(this.AreaENFile))
            {
                return dic;
            }

            var lines = CSVParser.LoadFromPath(this.AreaENFile, encoding: new UTF8Encoding(true));

            Parallel.ForEach(lines, (fields) =>
            {
                if (fields.Count < 5)
                {
                    return;
                }

                if (!int.TryParse(fields[0], out int id) ||
                    string.IsNullOrEmpty(fields[4]))
                {
                    return;
                }

                var entry = new Area()
                {
                    ID = id,
                    NameEn = fields[4],
                    Name = fields[4]
                };

                lock (dic)
                {
                    dic[entry.ID] = entry;
                }
            });

            this.AppLogger.Trace($"xivapi area list loaded. {this.AreaENFile}");

            return dic;
        }

#if false
        private void LoadPlacename()
        {
            if (!File.Exists(this.PlacenameFile))
            {
                return;
            }

            lock (this.placenameList)
            {
                this.placenameList.Clear();

                using (var sr = new StreamReader(this.PlacenameFile, new UTF8Encoding(false)))
                {
                    // ヘッダを飛ばす
                    sr.ReadLine();

                    while (!sr.EndOfStream)
                    {
                        var line = sr.ReadLine();

                        var values = line.Split(',');
                        if (values.Length >= 3)
                        {
                            var entry = new Placename()
                            {
                                ID = int.Parse(values[0]),
                                NameEn = values[1],
                                Name = values[2]
                            };

                            this.placenameList.Add(entry);
                        }
                    }
                }
            }

            this.AppLogger.Trace($"xivapi placement list loaded. {this.PlacenameFile}");
        }
#endif

        private void LoadAction()
        {
            if (!File.Exists(this.SkillFile))
            {
                return;
            }

            var sw = Stopwatch.StartNew();

            var userList = this.UserActionList;

            var obj = new object();
            lock (this.actionList)
            {
                this.actionList.Clear();

                var lines = CSVParser.LoadFromPath(this.SkillFile, encoding: new UTF8Encoding(true));

                Parallel.ForEach(lines, (fields) =>
                {
                    if (fields.Count < 2)
                    {
                        return;
                    }

                    if (!uint.TryParse(fields[0], out uint id) ||
                        string.IsNullOrEmpty(fields[1]))
                    {
                        return;
                    }

                    var entry = new XIVApiAction()
                    {
                        ID = id,
                        Name = fields[1],
                        AttackTypeName = fields[XIVApiAction.AttackTypeIndex]
                    };

                    entry.SetAttackTypeEnum();

                    if (userList.ContainsKey(entry.ID))
                    {
                        entry.Name = userList[entry.ID].Name;
                    }

                    lock (obj)
                    {
                        this.actionList[entry.ID] = entry;
                    }
                });
            }

            sw.Stop();
            this.AppLogger.Trace($"xivapi action list loaded. {sw.Elapsed.TotalSeconds:N0}s {this.SkillFile}");
        }

        private Dictionary<uint, XIVApiAction> _userActionList;
        private Dictionary<uint, XIVApiAction> UserActionList => this._userActionList ??= this.LoadUserAction();

        private Dictionary<uint, XIVApiAction> LoadUserAction()
        {
            var list = new Dictionary<uint, XIVApiAction>(256);

            if (!File.Exists(this.UserSkillFile))
            {
                return list;
            }

            var lines = CSVParser.LoadFromPath(this.UserSkillFile, encoding: new UTF8Encoding(false));

            Parallel.ForEach(lines, (fields) =>
            {
                if (fields.Count < 2)
                {
                    return;
                }

                if (!uint.TryParse(fields[0], out uint id) ||
                    string.IsNullOrEmpty(fields[1]))
                {
                    return;
                }

                var entry = new XIVApiAction()
                {
                    ID = id,
                    Name = fields[1],
                };

                lock (list)
                {
                    list[entry.ID] = entry;
                }
            });

            this.AppLogger.Trace($"user action list loaded.");

            return list;
        }

        private void LoadBuff()
        {
            if (!File.Exists(this.BuffFile))
            {
                return;
            }

            var lines = CSVParser.LoadFromPath(this.BuffFile, encoding: new UTF8Encoding(true));

            var obj = new object();
            lock (this.buffList)
            {
                this.buffList.Clear();

                Parallel.ForEach(lines, (fields) =>
                {
                    if (fields.Count < 2)
                    {
                        return;
                    }

                    uint id;
                    if (!uint.TryParse(fields[0], out id) ||
                        string.IsNullOrEmpty(fields[1]))
                    {
                        return;
                    }

                    var entry = new Buff()
                    {
                        ID = id,
                        Name = fields[1]
                    };

                    lock (obj)
                    {
                        this.buffList[entry.ID] = entry;
                    }
                });
            }

            this.AppLogger.Trace($"xivapi status list loaded. {this.BuffFile}");
        }

        #region Sub classes

        /// <summary>
        /// Area ただしXIVApi上の呼称では「Instance」
        /// </summary>
        public class Area
        {
            public int ID { get; set; }
            public string Name { get; set; }
            public string NameEn { get; set; }
            public int Order { get; set; }
            public int IntendedUse { get; set; }
        }

        public class Placename
        {
            public int ID { get; set; }
            public string Name { get; set; }
            public string NameEn { get; set; }
        }

        /// <summary>
        /// Skill ただしXIVApi上の呼称では「Action」
        /// </summary>
        public class XIVApiAction
        {
            public static readonly int AttackTypeIndex = 43;

            public uint ID { get; set; }
            public string Name { get; set; }
            public string AttackTypeName { get; set; }
            public AttackTypes AttackType { get; private set; }

            public void SetAttackTypeEnum()
                => this.AttackType = ((AttackTypes[])Enum.GetValues(typeof(AttackTypes)))
                    .FirstOrDefault(x => x.ToDisplay() == this.AttackTypeName);

            public override string ToString()
                => $"0x{this.ID:X4}({this.ID}) {this.Name} {this.AttackTypeName}";
        }

        #endregion Sub classes
    }

    public enum AttackTypes
    {
        Unknown = 0,
        Slash = 1,
        Pierce = 2,
        Impact = 3,
        Shoot = 4,
        Magic = 5,
        Breath = 6,
        Sound = 7,
        LimitBreak = 8
    }

    public static class AttackTypesExtensions
    {
        private static readonly string[] DisplayTexts = new[]
        {
            string.Empty,
            "斬",
            "突",
            "打",
            "射",
            "魔法",
            "ブレス",
            "音波",
            "リミットブレイク"
        };

        public static string ToDisplay(
            this AttackTypes value)
            => DisplayTexts[(int)value];
    }
}
