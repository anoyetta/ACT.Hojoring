using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using FFXIV.Framework.FFXIVHelper;

namespace XIVDBDownloader.Models
{
    public enum ActionCategory
    {
        Nothing = 0,

        TankRole = 10,
        Paladin = 11,
        Warrior = 12,
        DarkKnight = 13,

        HealerRole = 20,
        WhiteMage = 21,
        Scholar = 22,
        Astrologian = 23,

        MeleeDPSRole = 30,
        Monk = 31,
        Dragoon = 32,
        Ninja = 33,
        Samurai = 34,

        RangeDPSRole = 40,
        Bard = 41,
        Machinist = 42,

        MagicDPSRole = 50,
        BlackMage = 51,
        Summoner = 52,
        RedMage = 53,

        PetsEgi = 61,
        PetsFairy = 62,

        Others = 90,
    }

    [DataContract]
    public class ActionData
    {
        [DataMember(Name = "classjob_category", Order = 2)]
        public int ClassJobCategoryID { get; set; }

        [DataMember(Name = "classjob", Order = 1)]
        public int ClassJobID { get; set; }

        [DataMember(Name = "icon", Order = 3)]
        public int IconID { get; set; }

        [DataMember(Name = "id", Order = 4)]
        public int ID { get; set; }

        [DataMember(Name = "name", Order = 5)]
        public string Name { get; set; }

        #region Not DataMember

        public Job Job => Jobs.Find(this.ClassJobID);

        public Roles RawRole
        {
            get
            {
                var role = Roles.Unknown;
                if (Enum.IsDefined(typeof(Roles), this.ClassJobCategoryID))
                {
                    role = (Roles)this.ClassJobCategoryID;
                }

                return role;
            }
        }

        #endregion Not DataMember

        #region Category

        public ActionCategory Category
        {
            get
            {
                switch (this.RawRole)
                {
                    case Roles.Tank:
                        return ActionCategory.TankRole;

                    case Roles.Healer:
                        return ActionCategory.HealerRole;

                    case Roles.MeleeDPS:
                        return ActionCategory.MeleeDPSRole;

                    case Roles.RangeDPS:
                        return ActionCategory.RangeDPSRole;

                    case Roles.MagicDPS:
                        return ActionCategory.MagicDPSRole;

                    case Roles.PetsEgi:
                        return ActionCategory.PetsEgi;

                    case Roles.PetsFairy:
                        return ActionCategory.PetsFairy;

                    case Roles.PhysicalDPS:
                    case Roles.DPS:
                    case Roles.Magic:
                        return ActionCategory.Others;
                }

                switch (this.Job.ID)
                {
                    // タンク
                    case JobIDs.PLD:
                    case JobIDs.GLA:
                        return ActionCategory.Paladin;

                    case JobIDs.WAR:
                    case JobIDs.MRD:
                        return ActionCategory.Warrior;

                    case JobIDs.DRK:
                        return ActionCategory.DarkKnight;

                    // ヒーラー
                    case JobIDs.WHM:
                    case JobIDs.CNJ:
                        return ActionCategory.WhiteMage;

                    case JobIDs.SCH:
                        return ActionCategory.Scholar;

                    case JobIDs.AST:
                        return ActionCategory.Astrologian;

                    // メレー
                    case JobIDs.MNK:
                    case JobIDs.PUG:
                        return ActionCategory.Monk;

                    case JobIDs.DRG:
                    case JobIDs.LNC:
                        return ActionCategory.Dragoon;

                    case JobIDs.NIN:
                    case JobIDs.ROG:
                        return ActionCategory.Ninja;

                    case JobIDs.SAM:
                        return ActionCategory.Samurai;

                    // レンジ
                    case JobIDs.BRD:
                    case JobIDs.ARC:
                        return ActionCategory.Bard;

                    case JobIDs.MCH:
                        return ActionCategory.Machinist;

                    // マジック
                    case JobIDs.BLM:
                    case JobIDs.THM:
                        return ActionCategory.BlackMage;

                    case JobIDs.SMN:
                    case JobIDs.ACN:
                        return ActionCategory.Summoner;

                    case JobIDs.RDM:
                        return ActionCategory.RedMage;
                }

                return ActionCategory.Nothing;
            }
        }

        #endregion Category

        #region Icon

        private const string IconUriBase =
            @"https://secure.xivdb.com/img/game/{0:000000}/{1:000000}.png";

        public string IconUri
        {
            get
            {
                var iconDir = (this.IconID / 1000) * 1000;
                return string.Format(
                    IconUriBase,
                    iconDir,
                    this.IconID);
            }
        }

        #endregion Icon
    }

    public class ActionModel :
        XIVDBApiBase<IList<ActionData>>
    {
        #region Log

        public delegate void WriteLogLineDelegate(string text);

        public WriteLogLineDelegate WriteLogLineAction { get; set; }

        public void WriteLogLine(
            string text)
        {
            this.WriteLogLineAction?.Invoke(text);
        }

        #endregion Log

        public override string Uri =>
            @"https://api.xivdb.com/action?columns=id,name,icon,classjob,classjob_category";

        public void DownloadIcon(
            string directory)
        {
            if (this.ResultList == null ||
                this.ResultList.Count < 1)
            {
                return;
            }

            // ファイル名に使えない文字を取得しておく
            var invalidChars = Path.GetInvalidFileNameChars();

            var iconBaseDirectory = Path.Combine(
                directory,
                "Action icons");

            var actionsByCategory =
                from x in this.ResultList
                where
                x.Category != ActionCategory.Nothing
                orderby
                x.Category,
                x.ID
                group x by
                x.Category;

            using (var wc = new WebClient())
            {
                var isFirstTime = true;
                foreach (var category in actionsByCategory)
                {
                    if (!isFirstTime)
                    {
                        this.WriteLogLine("Please wait..." + Environment.NewLine);
                        Thread.Sleep(TimeSpan.FromSeconds(10));
                    }

                    isFirstTime = false;

                    var key = category.Key;

                    var dirName =
                        $"{((int)key).ToString("00")}_{Enum.GetName(typeof(ActionCategory), key)}";

                    var dir = Path.Combine(iconBaseDirectory, dirName);

                    this.CreateDirectory(dir);

                    this.WriteLogLine($"Download icons. {dirName}");

                    foreach (var skill in category)
                    {
                        Thread.Sleep(TimeSpan.FromMilliseconds(200));

                        var fileName =
                            $"{skill.ID.ToString("0000")}_{skill.Name}.png";

                        // ファイル名に使えない文字を除去する
                        fileName = string.Concat(fileName.Where(c =>
                            !invalidChars.Contains(c)));

                        var file =
                            Path.Combine(dir, fileName);

                        var uri = skill.IconUri;

                        this.WriteLogLine(fileName);
#if DEBUG
                        this.WriteLogLine(uri);
#endif

                        try
                        {
                            wc.DownloadFileTaskAsync(uri, file).Wait();
                            this.WriteLogLine("Done." + Environment.NewLine);
                        }
                        catch (WebException ex)
                        {
                            this.WriteLogLine(ex.ToString());
                        }
                    }
                }
            }
        }

        public void SaveToCSV(
            string file)
        {
            if (this.ResultList == null ||
                this.ResultList.Count < 1)
            {
                return;
            }

            if (!Directory.Exists(Path.GetDirectoryName(file)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(file));
            }

            var buffer = new StringBuilder(5120);

            using (var sw = new StreamWriter(file, false, new UTF8Encoding(false)))
            {
                sw.WriteLine(
                    $"ID,ID_HEX,ClassJob,ClassJobCategory,Name");

                var orderd =
                    from x in this.ResultList
                    orderby
                    x.ClassJobCategoryID,
                    x.ID
                    select
                    x;

                foreach (var action in orderd)
                {
                    buffer.AppendLine(
                        $"{action.ID},{action.ID:X4},{action.ClassJobID},{action.ClassJobCategoryID},{action.Name}");

                    if (buffer.Length >= 5120)
                    {
                        sw.Write(buffer.ToString());
                        buffer.Clear();
                    }
                }

                if (buffer.Length > 0)
                {
                    sw.Write(buffer.ToString());
                }
            }
        }

        private void CreateDirectory(
            string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }
}
