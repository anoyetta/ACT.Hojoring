using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using ACT.SpecialSpellTimer.Config;
using ACT.SpecialSpellTimer.Models;
using ACT.SpecialSpellTimer.Utility;
using FFXIV.Framework.FFXIVHelper;
using FFXIV.Framework.Globalization;
using Microsoft.VisualBasic.FileIO;
using Prism.Mvvm;

namespace ACT.SpecialSpellTimer.Sound
{
    public class TTSDictionary
    {
        private const string SourceFileName = @"TTSDictionary.{0}.txt";

        #region Singleton

        private static TTSDictionary instance = new TTSDictionary();

        public static TTSDictionary Instance => instance;

        #endregion Singleton

        public string SourceFile => Path.Combine(
            this.ResourcesDirectory,
            string.Format(SourceFileName, Settings.Default.UILocale.ToText()));

        private readonly object locker = new object();
        private readonly Dictionary<string, string> ttsDictionary = new Dictionary<string, string>();
        private readonly Dictionary<string, Regex> placeholderRegexDictionary = new Dictionary<string, Regex>();

        public ObservableCollection<PCPhonetic> Phonetics { get; private set; } = new ObservableCollection<PCPhonetic>();
        public Dictionary<string, string> Dictionary => this.ttsDictionary;

        private string resourcesDirectory;

        public string ResourcesDirectory
        {
            get
            {
                if (string.IsNullOrEmpty(this.resourcesDirectory))
                {
                    do
                    {
                        // ACTのパスを取得する
                        var asm = Assembly.GetEntryAssembly();
                        if (asm != null)
                        {
                            var actDirectory = Path.GetDirectoryName(asm.Location);
                            var resourcesUnderAct = Path.Combine(actDirectory, @"resources");

                            if (Directory.Exists(resourcesUnderAct))
                            {
                                this.resourcesDirectory = resourcesUnderAct;
                                break;
                            }
                        }

                        // 自身の場所を取得する
                        var selfDirectory = PluginCore.Instance.Location ?? string.Empty;
                        var resourcesUnderThis = Path.Combine(selfDirectory, @"resources");

                        if (Directory.Exists(resourcesUnderThis))
                        {
                            this.resourcesDirectory = resourcesUnderThis;
                            break;
                        }
                    } while (false);
                }

                return this.resourcesDirectory;
            }
        }

        public string ReplaceWordsTTS(
            string textToSpeak)
        {
            if (string.IsNullOrEmpty(textToSpeak))
            {
                return textToSpeak;
            }

            lock (this.locker)
            {
                var placeholderList = TableCompiler.Instance.PlaceholderList;

                var q =
                    from x in this.ttsDictionary
                    orderby
                    !x.Key.Contains("<") && !x.Key.Contains(">") ?
                    0 :
                    1
                    select
                    x;

                foreach (var item in q)
                {
                    if (string.IsNullOrEmpty(item.Key))
                    {
                        continue;
                    }

                    // 通常の置換
                    if (!item.Key.Contains("<") &&
                        !item.Key.Contains(">"))
                    {
                        textToSpeak = textToSpeak.Replace(item.Key, item.Value);
                        continue;
                    }

                    // プレースホルダによる置換
                    var placeholder = placeholderList
                        .FirstOrDefault(x => x.Placeholder == item.Key);
                    if (placeholder == null)
                    {
                        continue;
                    }

                    var beforeRegex = default(Regex);
                    if (this.placeholderRegexDictionary.ContainsKey(placeholder.ReplaceString))
                    {
                        beforeRegex = this.placeholderRegexDictionary[placeholder.ReplaceString];
                    }
                    else
                    {
                        beforeRegex = new Regex(placeholder.ReplaceString, RegexOptions.Compiled);
                        this.placeholderRegexDictionary[placeholder.ReplaceString] = beforeRegex;
                    }

                    // プレースホルダの置換後の値から読み仮名に置換する
                    textToSpeak = beforeRegex.Replace(textToSpeak, item.Value);
                }
            }

            return textToSpeak;
        }

        private string ReplaceTTS(
            string textToSpeak)
        {
            lock (this.locker)
            {
                if (this.ttsDictionary.ContainsKey(textToSpeak))
                {
                    textToSpeak = this.ttsDictionary[textToSpeak];
                }

                return textToSpeak;
            }
        }

        public void Load()
        {
            if (!File.Exists(this.SourceFile))
            {
                return;
            }

            using (var sr = new StreamReader(this.SourceFile, new UTF8Encoding(false)))
            using (var tf = new TextFieldParser(sr)
            {
                CommentTokens = new string[] { "#" },
                Delimiters = new string[] { "\t", " " },
                TextFieldType = FieldType.Delimited,
                HasFieldsEnclosedInQuotes = true,
                TrimWhiteSpace = true
            })
            {
                lock (this.locker)
                {
                    this.ttsDictionary.Clear();
                }

                while (!tf.EndOfData)
                {
                    var fields = tf.ReadFields()
                        .Where(x => !string.IsNullOrEmpty(x))
                        .ToArray();

                    if (fields.Length <= 0)
                    {
                        continue;
                    }

                    var key = fields.Length > 0 ? fields[0] : string.Empty;
                    var value = fields.Length > 1 ? fields[1] : string.Empty;

                    if (!string.IsNullOrEmpty(key))
                    {
                        lock (this.locker)
                        {
                            this.ttsDictionary[key] = value;
                        }
                    }
                }

                lock (this.locker)
                {
                    foreach (var item in this.Phonetics)
                    {
                        this.ttsDictionary[item.Name] = item.Phonetic;
                        this.ttsDictionary[item.NameFI] = item.Phonetic;
                        this.ttsDictionary[item.NameIF] = item.Phonetic;
                        this.ttsDictionary[item.NameII] = item.Phonetic;
                    }
                }
            }

            Logger.Write($"TTSDictionary loaded. {this.SourceFile}");
        }

        public class PCPhonetic :
            BindableBase
        {
            private uint id;
            private string name;
            private string nameFI;
            private string nameIF;
            private string nameII;
            private string phonetic;
            private JobIDs jobID;

            public uint ID
            {
                get => this.id;
                set => this.SetProperty(ref this.id, value);
            }

            public string Name
            {
                get => this.name;
                set
                {
                    if (this.SetProperty(ref this.name, value))
                    {
                        if (TTSDictionary.Instance.ttsDictionary.ContainsKey(this.name))
                        {
                            this.Phonetic = TTSDictionary.Instance.ttsDictionary[this.name];
                        }
                    }
                }
            }

            public string NameFI
            {
                get => this.nameFI;
                set => this.SetProperty(ref this.nameFI, value);
            }

            public string NameIF
            {
                get => this.nameIF;
                set => this.SetProperty(ref this.nameIF, value);
            }

            public string NameII
            {
                get => this.nameII;
                set => this.SetProperty(ref this.nameII, value);
            }

            public string Phonetic
            {
                get => this.phonetic;
                set
                {
                    if (this.SetProperty(ref this.phonetic, value))
                    {
                        TTSDictionary.Instance.ttsDictionary[this.Name] = value;
                        TTSDictionary.Instance.ttsDictionary[this.NameFI] = value;
                        TTSDictionary.Instance.ttsDictionary[this.NameIF] = value;
                        TTSDictionary.Instance.ttsDictionary[this.NameII] = value;
                    }
                }
            }

            public JobIDs JobID
            {
                get => this.jobID;
                set => this.SetProperty(ref this.jobID, value);
            }

            public int SortOrder
            {
                get
                {
                    if ((TableCompiler.Instance?.Player?.ID ?? 0) == this.ID)
                    {
                        return 0;
                    }

                    var job = Jobs.Find(this.JobID);
                    if (job == null)
                    {
                        return (int)this.JobID;
                    }

                    return ((int)job.Role * 100) + (int)this.JobID;
                }
            }
        }
    }
}
