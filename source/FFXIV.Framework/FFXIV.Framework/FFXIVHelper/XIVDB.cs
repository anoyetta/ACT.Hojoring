using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FFXIV.Framework.Common;
using FFXIV.Framework.Globalization;
using Microsoft.VisualBasic.FileIO;

namespace FFXIV.Framework.FFXIVHelper
{
    public class XIVDB
    {
        #region Logger

        private NLog.Logger AppLogger => AppLog.DefaultLogger;

        #endregion Logger

        #region Singleton

        private static XIVDB instance = new XIVDB();

        public static XIVDB Instance => instance;

        #endregion Singleton

        #region Resources Files

        public string AreaFile => Path.Combine(
            this.ResourcesDirectory + @"\xivdb",
            $@"Instance.{this.FFXIVLocale.ToText()}.csv");

        public string PlacenameFile => Path.Combine(
            this.ResourcesDirectory + @"\xivdb",
            $@"Placename.{this.FFXIVLocale.ToText()}.csv");

        public string SkillFile => Path.Combine(
            this.ResourcesDirectory + @"\xivdb",
            $@"Action.{this.FFXIVLocale.ToText()}.csv");

        public string UserSkillFile => Path.Combine(
            this.ResourcesDirectory,
            $@"Actions.csv");

        #endregion Resources Files

        #region Resources Lists

        private readonly List<Area> areaList = new List<Area>();
        private readonly List<Placename> placenameList = new List<Placename>();
        private readonly Dictionary<int, XIVDBAction> actionList = new Dictionary<int, XIVDBAction>();

        public IReadOnlyList<Area> AreaList => this.areaList;
        public IReadOnlyList<Placename> PlacenameList => this.placenameList;
        public IReadOnlyDictionary<int, XIVDBAction> ActionList => this.actionList;

        #endregion Resources Lists

        public Locales FFXIVLocale
        {
            get;
            set;
        } = Locales.JA;

        private string resourcesDirectory;

        public string ResourcesDirectory =>
            this.resourcesDirectory ?? (this.resourcesDirectory = DirectoryHelper.FindSubDirectory("resources"));

        public XIVDBAction FindAction(
            int ID)
        {
            if (this.actionList.ContainsKey(ID))
            {
                return this.actionList[ID];
            }

            return null;
        }

        public XIVDBAction FindAction(
            string ID)
        {
            try
            {
                var IDAsInt = Convert.ToInt32(ID, 16);
                return this.FindAction(IDAsInt);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public void Load()
        {
            Task.WaitAll(
                Task.Run(() => this.LoadArea()),
                Task.Run(() => this.LoadPlacename()),
                Task.Run(() => this.LoadAction()));
        }

        private void LoadArea()
        {
            if (!File.Exists(this.AreaFile))
            {
                return;
            }

            this.areaList.Clear();

            using (var sr = new StreamReader(this.AreaFile, new UTF8Encoding(false)))
            {
                // ヘッダを飛ばす
                sr.ReadLine();

                while (!sr.EndOfStream)
                {
                    var line = sr.ReadLine();

                    var values = line.Split(',');
                    if (values.Length >= 3)
                    {
                        var entry = new Area()
                        {
                            ID = int.Parse(values[0]),
                            NameEn = values[1],
                            Name = values[2]
                        };

                        this.areaList.Add(entry);
                    }
                }
            }

            this.AppLogger.Trace($"XIVDB Area list loaded. {this.AreaFile}");
        }

        private void LoadPlacename()
        {
            if (!File.Exists(this.PlacenameFile))
            {
                return;
            }

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

            this.AppLogger.Trace($"XIVDB Placement list loaded. {this.PlacenameFile}");
        }

        private void LoadAction()
        {
            if (!File.Exists(this.SkillFile))
            {
                return;
            }

            this.actionList.Clear();

            using (var sr = new StreamReader(this.SkillFile, new UTF8Encoding(false)))
            {
                // ヘッダを飛ばす
                sr.ReadLine();

                while (!sr.EndOfStream)
                {
                    var line = sr.ReadLine();

                    var values = line.Split(',');
                    if (values.Length >= 5)
                    {
                        var entry = new XIVDBAction()
                        {
                            ID = int.Parse(values[0]),
                            Name = values[4]
                        };

                        this.actionList[entry.ID] = entry;
                    }
                }
            }

            this.AppLogger.Trace($"XIVDB Action list loaded. {this.SkillFile}");

            // Userリストの方も読み込む
            this.LoadUserAction();
        }

        private void LoadUserAction()
        {
            var isLoaded = false;

            if (!File.Exists(this.UserSkillFile))
            {
                return;
            }

            using (var sr = new StreamReader(this.UserSkillFile, new UTF8Encoding(false)))
            using (var parser = new TextFieldParser(sr)
            {
                TextFieldType = FieldType.Delimited,
                Delimiters = new[] { "," },
                HasFieldsEnclosedInQuotes = true,
                TrimWhiteSpace = true,
                CommentTokens = new[] { "#" },
            })
            {
                while (!parser.EndOfData)
                {
                    var fields = parser.ReadFields();

                    if (fields == null ||
                        fields.Length < 2)
                    {
                        continue;
                    }

                    int id;
                    if (!int.TryParse(fields[0], out id) ||
                        string.IsNullOrEmpty(fields[1]))
                    {
                        continue;
                    }

                    var entry = new XIVDBAction()
                    {
                        ID = id,
                        Name = fields[1]
                    };

                    this.actionList[entry.ID] = entry;
                    isLoaded = true;
                }
            }

            if (isLoaded)
            {
                this.AppLogger.Trace($"User Action list loaded.");
            }
        }

        #region Sub classes

        /// <summary>
        /// Area ただしXIVDB上の呼称では「Instance」
        /// </summary>
        public class Area
        {
            public int ID { get; set; }
            public string Name { get; set; }
            public string NameEn { get; set; }
        }

        public class Placename
        {
            public int ID { get; set; }
            public string Name { get; set; }
            public string NameEn { get; set; }
        }

        /// <summary>
        /// Skill ただしXIVDB上の呼称では「Action」
        /// </summary>
        public class XIVDBAction
        {
            public int ID { get; set; }
            public string Name { get; set; }
        }

        #endregion Sub classes
    }
}
