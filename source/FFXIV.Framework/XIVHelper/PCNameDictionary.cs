using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using FFXIV.Framework.Common;

namespace FFXIV.Framework.XIVHelper
{
    public class PCNameDictionary
    {
        #region Singleton

        private static PCNameDictionary instance;

        public static PCNameDictionary Instance => instance ?? (instance = new PCNameDictionary());

        private PCNameDictionary()
        {
        }

        public static void Free() => instance = null;

        #endregion Singleton

        public IReadOnlyList<(string Name, DateTime Timestamp)> PCNameList => this.pcNameList;

        private readonly List<(string Name, DateTime Timestamp)> pcNameList = new List<(string Name, DateTime Timestamp)>();

        private static readonly string FileName = Path.Combine(
            DirectoryHelper.FindSubDirectory("resources"),
            "PCNameHistory.txt");

        public void Add(
            string name)
        {
            this.pcNameList.RemoveAll(x => x.Name == name);
            this.pcNameList.Add((name, DateTime.Now));
        }

        public string[] GetNames()
        {
            var list = new List<string>();
            foreach (var entry in this.pcNameList)
            {
                list.AddRange(CombatantEx.GetNames(entry.Name));
            }

            return list.ToArray();
        }

        public void Load()
        {
            if (!File.Exists(FileName))
            {
                return;
            }

            var regex = new Regex(
                @"""(?<name>.+)""\s+(?<timestamp>.+)$",
                RegexOptions.Compiled |
                RegexOptions.ExplicitCapture);

            this.pcNameList.Clear();

            using (var sr = new StreamReader(FileName, new UTF8Encoding(false)))
            {
                while (!sr.EndOfStream)
                {
                    var line = sr.ReadLine().Trim();
                    if (string.IsNullOrEmpty(line))
                    {
                        continue;
                    }

                    if (line.StartsWith("#"))
                    {
                        continue;
                    }

                    var match = regex.Match(line);
                    if (!match.Success)
                    {
                        continue;
                    }

                    var name = match.Groups["name"].Value;
                    var timestampAsString = match.Groups["timestamp"].Value;

                    DateTime timestamp;
                    if (!DateTime.TryParse(timestampAsString, out timestamp))
                    {
                        timestamp = DateTime.Now;
                    }

                    this.pcNameList.Add((name, timestamp));
                }
            }
        }

        public void Save()
        {
            if (!this.pcNameList.Any())
            {
                return;
            }

            var now = DateTime.Now;

            var sb = new StringBuilder();
            foreach (var entry in this.pcNameList
                .Where(x => (now - x.Timestamp).TotalDays <= 60))
            {
                sb.AppendLine($"\"{entry.Name}\"\t{entry.Timestamp.ToString("yyyy-MM-dd")}");
            }

            if (sb.Length > 0)
            {
                FileHelper.CreateDirectory(FileName);
                File.WriteAllText(FileName, sb.ToString(), new UTF8Encoding(false));
            }
        }
    }
}
