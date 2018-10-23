using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using FFXIV.Framework.Common;
using Prism.Mvvm;

namespace ACT.SpecialSpellTimer.Models
{
    [Serializable]
    public class TagTable :
        BindableBase
    {
        #region Singleton

        private static TagTable instance = new TagTable();

        public static TagTable Instance => instance;

        #endregion Singleton

        private ObservableCollection<Tag> tags = new ObservableCollection<Tag>();
        private ObservableCollection<ItemTags> itemTags = new ObservableCollection<ItemTags>();

        public ObservableCollection<Tag> Tags
        {
            get => this.tags;
            set => this.SetProperty(ref this.tags, value);
        }

        public ObservableCollection<ItemTags> ItemTags
        {
            get => this.itemTags;
            set => this.SetProperty(ref this.itemTags, value);
        }

        [XmlIgnore]
        public string DefaultFile =>
            Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            @"anoyetta\ACT\ACT.SpecialSpellTimer.Tags.xml");

        public void Add(
            Tag tag)
            => this.Tags.Add(tag);

        public Tag AddNew(
            string newTagName)
        {
            var tag = new Tag()
            {
                Name = newTagName
            };

            this.Add(tag);

            return tag;
        }

        public void Remove(
            Tag tag)
        {
            var targets = this.itemTags.Where(x => x.TagID == tag.ID).ToArray();
            foreach (var item in targets)
            {
                this.itemTags.Remove(item);
            }

            this.Tags.Remove(tag);
        }

        public void Load()
        {
            var file = this.DefaultFile;

            try
            {
                if (!File.Exists(file))
                {
                    return;
                }

                using (var sr = new StreamReader(file, new UTF8Encoding(false)))
                {
                    if (sr.BaseStream.Length > 0)
                    {
                        var xs = new XmlSerializer(this.GetType());
                        var data = xs.Deserialize(sr) as TagTable;

                        this.Tags.Clear();
                        this.ItemTags.Clear();

                        this.ItemTags.AddRange(data.ItemTags);
                        this.Tags.AddRange(data.Tags);
                    }
                }
            }
            finally
            {
                // インポートタグを追加する
                var importsTag = this.Tags.FirstOrDefault(x => x.Name == Tag.ImportsTag.Name);
                if (importsTag == null)
                {
                    this.Tags.Add(Tag.ImportsTag);
                }
                else
                {
                    Tag.SetImportTag(importsTag);
                }
            }
        }

        public void Save()
        {
            lock (this)
            {
                var file = this.DefaultFile;

                FileHelper.CreateDirectory(file);

                var ns = new XmlSerializerNamespaces();
                ns.Add(string.Empty, string.Empty);

                var sb = new StringBuilder();
                using (var sw = new StringWriter(sb))
                {
                    var xs = new XmlSerializer(this.GetType());
                    xs.Serialize(sw, this, ns);
                }

                sb.Replace("utf-16", "utf-8");

                File.WriteAllText(
                    file,
                    sb.ToString(),
                    new UTF8Encoding(false));
            }
        }

        public void Backup()
        {
            var file = this.DefaultFile;

            if (File.Exists(file))
            {
                var backupFile = Path.Combine(
                    Path.Combine(Path.GetDirectoryName(file), "backup"),
                    Path.GetFileNameWithoutExtension(file) + "." + DateTime.Now.ToString("yyyy-MM-dd") + ".bak");

                FileHelper.CreateDirectory(backupFile);

                File.Copy(
                    file,
                    backupFile,
                    true);

                // 古いバックアップを消す
                foreach (var bak in
                    Directory.GetFiles(Path.GetDirectoryName(backupFile), "*.bak"))
                {
                    var timeStamp = File.GetCreationTime(bak);
                    if ((DateTime.Now - timeStamp).TotalDays >= 3.0d)
                    {
                        File.Delete(bak);
                    }
                }
            }
        }
    }
}
