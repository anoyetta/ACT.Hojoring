using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ACT.UltraScouter.Config;
using FFXIV.Framework.Common;
using FFXIV.Framework.Globalization;
using Microsoft.VisualBasic.FileIO;
using NLog;

namespace ACT.UltraScouter.Common
{
    public class TTSDictionary
    {
        private const string SourceFileName = @"TTSDictionary.{0}.txt";

        #region Singleton

        private static TTSDictionary instance = new TTSDictionary();

        public static TTSDictionary Instance => instance;

        #endregion Singleton

        #region Logger

        private Logger AppLogger = AppLog.DefaultLogger;

        #endregion Logger

        private string ResourcesDirectory => DirectoryHelper.FindSubDirectory(@"resources");

        private string SourceFile => Path.Combine(
            this.ResourcesDirectory,
            string.Format(SourceFileName, Settings.Instance.FFXIVLocale.ToResourcesName()));

        private readonly object locker = new object();
        private readonly Dictionary<string, string> ttsDictionary = new Dictionary<string, string>();

        public string ReplaceTTS(
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
            }

            this.AppLogger.Info($"TTSDictionary loaded. {this.SourceFile}");
        }
    }
}
