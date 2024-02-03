using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
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

        public static TagTable Instance { get; } = new TagTable();

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

        private bool isLoaded = false;

        public void Load()
        {
            lock (this)
            {
                var file = this.DefaultFile;

                try
                {
                    this.isLoaded = true;

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
                            var xs = new XmlSerializer(this.GetType());
                            var data = xs.Deserialize(sr) as TagTable;

                            this.Tags.Clear();
                            this.ItemTags.Clear();

                            this.ItemTags.AddRange(data.ItemTags);
                            this.Tags.AddRange(data.Tags);
                        }
                    }
                } catch (Exception ex)
                {
                    var info = ex.GetType().ToString() + Environment.NewLine + Environment.NewLine;
                    info += ex.Message + Environment.NewLine;
                    info += ex.StackTrace.ToString();

                    if (ex.InnerException != null)
                    {
                        info += Environment.NewLine + Environment.NewLine;
                        info += "Inner Exception :" + Environment.NewLine;
                        info += ex.InnerException.GetType().ToString() + Environment.NewLine + Environment.NewLine;
                        info += ex.InnerException.Message + Environment.NewLine;
                        info += ex.InnerException.StackTrace.ToString();
                    }

                    var result = MessageBox.Show("faild config load\n\n" + DefaultFile + "\n" + info + "\n\ntry to load backup?", "error!", MessageBoxButton.YesNo);
                    if (result == MessageBoxResult.Yes)
                    {
                        if (EnvironmentHelper.RestoreFile(DefaultFile))
                        {
                            Load();
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
        }

        private static readonly Encoding DefaultEncoding = new UTF8Encoding(false);

        public void Save()
        {
            lock (this)
            {
                if (!this.isLoaded)
                {
                    return;
                }

                var file = this.DefaultFile;

                FileHelper.CreateDirectory(file);

                var ns = new XmlSerializerNamespaces();
                ns.Add(string.Empty, string.Empty);

                using (var sw = new StreamWriter(file, false, DefaultEncoding))
                {
                    var xs = new XmlSerializer(this.GetType());
                    xs.Serialize(sw, this, ns);
                    sw.Close();
                }
            }
        }
    }
}
