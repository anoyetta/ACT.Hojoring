using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using FFXIV.Framework.Extensions;

namespace ACT.SpecialSpellTimer.Models
{
    /// <summary>
    /// SpellTimerテーブル
    /// </summary>
    public class SpellTable
    {
        #region Singleton

        private static SpellTable instance = new SpellTable();
        public static SpellTable Instance => instance;

        #endregion Singleton

        /// <summary>
        /// SpellTimerデータテーブル
        /// </summary>
        private volatile ObservableCollection<Spell> table = new ObservableCollection<Spell>();

        /// <summary>
        /// デフォルトのファイル
        /// </summary>
        public string DefaultFile => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            @"anoyetta\ACT\ACT.SpecialSpellTimer.Spells.xml");

        /// <summary>
        /// SpellTimerデータテーブル
        /// </summary>
        public ObservableCollection<Spell> Table => this.table;

        public void Add(
            Spell spell)
        {
            lock (this.table)
            {
                this.table.Add(spell);
            }
        }

        public void AddRange(
            IEnumerable<Spell> spells)
        {
            lock (this.table)
            {
                this.table.AddRange(spells);
            }
        }

        public void Remove(
            Spell spell)
        {
            lock (this.table)
            {
                this.table.Remove(spell);
            }
        }

        /// <summary>
        /// カウントをリセットする
        /// </summary>
        public static void ResetCount()
        {
            lock (SpellTable.Instance)
            {
                var toRemove = SpellTable.Instance.instanceSpells
                    .Where(x => !x.Value.IsNotResetAtWipeout)
                    .ToArray();

                foreach (var item in toRemove)
                {
                    SpellTable.Instance.instanceSpells.Remove(item.Key);
                }
            }

            foreach (var row in TableCompiler.Instance.SpellList)
            {
                if (row.IsNotResetAtWipeout)
                {
                    continue;
                }

                row.MatchDateTime = DateTime.MinValue;
                row.UpdateDone = false;
                row.OverDone = false;
                row.BeforeDone = false;
                row.TimeupDone = false;
                row.CompleteScheduledTime = DateTime.MinValue;

                row.StartOverSoundTimer();
                row.StartBeforeSoundTimer();
                row.StartTimeupSoundTimer();
            }
        }

        /// <summary>
        /// スペルの描画済みフラグをクリアする
        /// </summary>
        public void ClearUpdateFlags()
        {
            foreach (var item in this.table)
            {
                item.UpdateDone = false;
            }
        }

        /// <summary>
        /// 指定されたGuidを持つSpellTimerを取得する
        /// </summary>
        /// <param name="guid">Guid</param>
        public Spell GetSpellTimerByGuid(
            Guid guid)
        {
            return this.table
                .AsParallel()
                .Where(x => x.Guid == guid)
                .FirstOrDefault();
        }

        /// <summary>
        /// 読み込む
        /// </summary>
        public void Load()
        {
            this.Load(this.DefaultFile, true);
        }

        /// <summary>
        /// 読み込む
        /// </summary>
        /// <param name="file">ファイルパス</param>
        /// <param name="isClear">消去してからロードする？</param>
        public void Load(
            string file,
            bool isClear)
        {
            try
            {
                // サイズ0のファイルがもしも存在したら消す
                if (File.Exists(file))
                {
                    var fi = new FileInfo(file);
                    if (fi.Length <= 0)
                    {
                        File.Delete(file);
                    }
                }

                if (!File.Exists(file))
                {
                    return;
                }

                using (var sr = new StreamReader(file, new UTF8Encoding(false)))
                {
                    if (sr.BaseStream.Length > 0)
                    {
                        var xs = new XmlSerializer(table.GetType());
                        var data = xs.Deserialize(sr) as IList<Spell>;

                        if (isClear)
                        {
                            this.table.Clear();
                        }

                        foreach (var item in data)
                        {
                            // パネルIDを補完する
                            if (item.PanelID == Guid.Empty)
                            {
                                item.PanelID = SpellPanelTable.Instance.Table
                                    .FirstOrDefault(x => x.PanelName == item.PanelName)?
                                    .ID ?? Guid.Empty;

                                if (item.PanelID == Guid.Empty)
                                {
                                    item.PanelID = SpellPanel.GeneralPanel.ID;
                                }
                            }
                            else
                            {
                                if (!SpellPanelTable.Instance.Table.Any(x =>
                                    x.ID == item.PanelID))
                                {
                                    item.PanelID = SpellPanelTable.Instance.Table
                                        .FirstOrDefault(x => x.PanelName == item.PanelName)?
                                        .ID ?? SpellPanel.GeneralPanel.ID;
                                }
                            }

                            this.table.Add(item);
                        }
                    }
                }
            }
            finally
            {
                if (!this.table.Any())
                {
                    this.table.AddRange(Spell.SampleSpells);
                }

                this.Reset();
            }
        }

        /// <summary>
        /// スペルテーブルを初期化する
        /// </summary>
        public void Reset()
        {
            var id = 0L;
            foreach (var row in this.table)
            {
                id++;
                row.ID = id;
                if (row.Guid == Guid.Empty)
                {
                    row.Guid = Guid.NewGuid();
                }

                row.MatchDateTime = DateTime.MinValue;
                row.Regex = null;
                row.RegexPattern = string.Empty;
                row.KeywordReplaced = string.Empty;
                row.RegexForExtend1 = null;
                row.RegexForExtendPattern1 = string.Empty;
                row.KeywordForExtendReplaced1 = string.Empty;
                row.RegexForExtend2 = null;
                row.RegexForExtendPattern2 = string.Empty;
                row.KeywordForExtendReplaced2 = string.Empty;
                row.RegexForExtend3 = null;
                row.RegexForExtendPattern3 = string.Empty;
                row.KeywordForExtendReplaced3 = string.Empty;

                if (string.IsNullOrWhiteSpace(row.BackgroundColor))
                {
                    row.BackgroundColor = Color.Transparent.ToHTML();
                }
            }
        }

        /// <summary>
        /// 保存する
        /// </summary>
        public void Save(
            bool force = false)
        {
            this.Save(this.DefaultFile, force);
        }

        /// <summary>
        /// 保存する
        /// </summary>
        /// <param name="file">ファイルパス</param>
        public void Save(
            string file,
            bool force,
            string panelName = "")
        {
            if (this.table == null)
            {
                return;
            }

            if (!force)
            {
                if (this.table.Count <= 0)
                {
                    return;
                }
            }

            var work = this.table.Where(x =>
                !x.IsInstance &&
                (
                    string.IsNullOrEmpty(panelName) ||
                    x.PanelName == panelName
                )).ToList();

            this.Save(file, work);
        }

        private static readonly Encoding DefaultEncoding = new UTF8Encoding(false);

        public void Save(
            string file,
            List<Spell> list)
        {
            lock (this)
            {
                var dir = Path.GetDirectoryName(file);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var ns = new XmlSerializerNamespaces();
                ns.Add(string.Empty, string.Empty);

                var sb = new StringBuilder();
                using (var sw = new StringWriter(sb))
                {
                    var xs = new XmlSerializer(list.GetType());
                    xs.Serialize(sw, list, ns);
                }

                sb.Replace("utf-16", "utf-8");

                File.WriteAllText(
                    file,
                    sb.ToString() + Environment.NewLine,
                    DefaultEncoding);
            }
        }

        public IList<Spell> LoadFromFile(
            string file)
        {
            var data = default(IList<Spell>);

            if (!File.Exists(file))
            {
                return data;
            }

            using (var sr = new StreamReader(file, new UTF8Encoding(false)))
            {
                if (sr.BaseStream.Length > 0)
                {
                    var xs = new XmlSerializer(table.GetType());
                    data = xs.Deserialize(sr) as IList<Spell>;

                    // IDは振り直す
                    if (data != null)
                    {
                        var id = this.table.Any() ?
                            this.table.Max(x => x.ID) + 1 :
                            1;
                        foreach (var item in data)
                        {
                            item.ID = id++;
                            item.Guid = Guid.NewGuid();
                        }
                    }
                }
            }

            return data;
        }

        #region To Instance spells

        /// <summary>
        /// インスタンス化されたスペルの辞書 key : スペルの表示名
        /// </summary>
        private readonly Dictionary<string, Spell> instanceSpells =
            new Dictionary<string, Spell>(32);

        /// <summary>
        /// インスタンススペルを取得して返す
        /// </summary>
        /// <returns>
        /// インスタンススペルのリスト</returns>
        public IReadOnlyList<Spell> GetInstanceSpells()
        {
            lock (this.instanceSpells)
            {
                return this.instanceSpells.Values.ToList();
            }
        }

        /// <summary>
        /// 同じスペル表示名のインスタンスを取得するか新たに作成する
        /// </summary>
        /// <param name="spellTitle">スペル表示名</param>
        /// <param name="sourceSpell">インスタンスの元となるスペル</param>
        /// <returns>インスタンススペル</returns>
        public Spell GetOrAddInstance(
            string spellTitle,
            Spell sourceSpell)
        {
            var key = $"{sourceSpell.Guid}+{spellTitle}";

            var instance = default(Spell);

            lock (this.instanceSpells)
            {
                if (this.instanceSpells.ContainsKey(key))
                {
                    instance = this.instanceSpells[key];
                }
                else
                {
                    instance = sourceSpell.CreateInstanceNew(spellTitle);
                    this.instanceSpells[key] = instance;
                }
            }

            instance.SpellTitleReplaced = spellTitle;
            instance.CompleteScheduledTime = DateTime.MinValue;

            return instance;
        }

        /// <summary>
        /// インスタンス化されたスペルをすべて削除する
        /// </summary>
        public void RemoveInstanceSpellsAll()
        {
            lock (this.instanceSpells)
            {
                this.instanceSpells.Clear();
            }
        }

        #endregion To Instance spells
    }
}
