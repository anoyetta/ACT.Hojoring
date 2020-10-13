using System;
using System.Collections.Generic;
using System.Linq;
using FFXIV.Framework.Globalization;

namespace FFXIV.Framework.XIVHelper
{
    public enum JobIDs : int
    {
        Unknown = -1,
        ADV = 0,
        GLA = 1,
        PUG = 2,
        MRD = 3,
        LNC = 4,
        ARC = 5,
        CNJ = 6,
        THM = 7,
        CRP = 8,
        BSM = 9,
        ARM = 10,
        GSM = 11,
        LTW = 12,
        WVR = 13,
        ALC = 14,
        CUL = 15,
        MIN = 16,
        BOT = 17,
        FSH = 18,
        PLD = 19,
        MNK = 20,
        WAR = 21,
        DRG = 22,
        BRD = 23,
        WHM = 24,
        BLM = 25,
        ACN = 26,
        SMN = 27,
        SCH = 28,
        ROG = 29,
        NIN = 30,
        MCH = 31,
        DRK = 32,
        AST = 33,
        SAM = 34,
        RDM = 35,
        BLU = 36,
        GNB = 37,
        DNC = 38,
    }

    public enum Roles
    {
        Unknown = 0,
        Tank = 113,
        Healer = 117,
        MeleeDPS = 114,
        RangeDPS = 115,
        MagicDPS = 116,
        PhysicalDPS = 118,
        DPS = 119,
        Magic = 120,
        Crafter = 210,
        Gatherer = 220,
        PetsEgi = 28,
        PetsFairy = 29,
    }

    public static class RolesExtensions
    {
        public static int ToSortOrder(this Roles role)
            => new Dictionary<Roles, int>()
            {
                { Roles.Unknown, 100 },
                { Roles.Tank, 10 },
                { Roles.Healer, 20 },
                { Roles.MeleeDPS, 50 },
                { Roles.RangeDPS, 60 },
                { Roles.MagicDPS, 70 },
                { Roles.PhysicalDPS, 40 },
                { Roles.DPS, 30 },
                { Roles.Magic, 90 },
                { Roles.Crafter, 100 },
                { Roles.Gatherer, 110 },
                { Roles.PetsEgi, 210 },
                { Roles.PetsFairy, 200 },
            }[role];

        public static string ToText(this Roles role)
            => new Dictionary<Roles, string>()
            {
                { Roles.Unknown, "UNKNOWN" },
                { Roles.Tank, "TANK" },
                { Roles.Healer, "HEALER" },
                { Roles.MeleeDPS, "DPS" },
                { Roles.RangeDPS, "DPS" },
                { Roles.MagicDPS, "DPS" },
                { Roles.PhysicalDPS, "DPS" },
                { Roles.DPS, "DPS" },
                { Roles.Magic, "MAGIC" },
                { Roles.Crafter, "CRAFTER" },
                { Roles.Gatherer, "GATHERER" },
                { Roles.PetsEgi, "EGI" },
                { Roles.PetsFairy, "FAIRY" },
            }[role];
    }

    public class Job
    {
        public JobIDs ID { get; set; } = JobIDs.Unknown;
        public string NameEN { get; set; } = string.Empty;
        public string NameDE { get; set; } = string.Empty;
        public string NameFR { get; set; } = string.Empty;
        public string NameJA { get; set; } = string.Empty;
        public string NameCN { get; set; } = string.Empty;
        public string NameKO { get; set; } = string.Empty;

        public string[] Names => new[]
        {
            this.NameEN,
            this.NameDE,
            this.NameFR,
            this.NameJA,
            this.NameCN,
            this.NameKO,
        };

        public Roles Role { get; set; } = Roles.Unknown;

        /// <summary>
        /// いわゆるレイド等で使用するポピュラーなジョブなのか？
        /// </summary>
        public bool IsPopular { get; set; } = false;

        public string GetName(
            Locales locale)
        {
            switch (locale)
            {
                case Locales.EN: return this.NameEN;
                case Locales.JA: return this.NameJA;
                case Locales.FR: return this.NameFR;
                case Locales.DE: return this.NameDE;
                case Locales.KO: return this.NameKO;

                case Locales.TW:
                case Locales.CN:
                    return this.NameCN;

                default: return this.NameEN;
            }
        }

        public override string ToString() => this.ID.ToString();
    }

    public class Jobs
    {
        private static readonly Job[] jobs = new Job[]
        {
            new Job() {ID = JobIDs.Unknown, Role = Roles.Unknown, NameEN = "Unknown", NameJA = "Unknown", NameFR = "Unknown", NameDE = "Unknown", NameCN = "Unknown", NameKO = "Unknown"},
            new Job() {ID = JobIDs.ADV, Role = Roles.Unknown, NameEN = "Adventurer", NameJA = "冒険者", NameFR = "Aventurier", NameDE = "Abenteurer", NameCN = "冒险者", NameKO = "모험가"},
            new Job() {ID = JobIDs.GLA, Role = Roles.Tank, NameEN = "Gladiator", NameJA = "剣術士", NameFR = "Gladiateur", NameDE = "Gladiator", NameCN = "剑术师", NameKO = "검술사" },
            new Job() {ID = JobIDs.PUG, Role = Roles.MeleeDPS, NameEN = "Pugilist", NameJA = "拳闘士", NameFR = "Pugiliste", NameDE = "Faustk\u00e4mpfer", NameCN = "格斗家", NameKO = "격투사"},
            new Job() {ID = JobIDs.MRD, Role = Roles.Tank, NameEN = "Marauder", NameJA = "斧術士", NameFR = "Maraudeur", NameDE = "Marodeur" , NameCN = "斧术师", NameKO = "도끼술사"},
            new Job() {ID = JobIDs.LNC, Role = Roles.MeleeDPS, NameEN = "Lancer", NameJA = "槍術士", NameFR = "Ma\u00eetre D'hast", NameDE = "Pikenier" , NameCN = "枪术师", NameKO = "창술사"},
            new Job() {ID = JobIDs.ARC, Role = Roles.RangeDPS, NameEN = "Archer", NameJA = "弓術士", NameFR = "Archer", NameDE = "Waldl\u00e4ufer" , NameCN = "弓箭手", NameKO = "궁술사"},
            new Job() {ID = JobIDs.CNJ, Role = Roles.Healer, NameEN = "Conjurer", NameJA = "幻術士", NameFR = "\u00e9l\u00e9mentaliste", NameDE = "Druide" , NameCN = "幻术师", NameKO = "환술사"},
            new Job() {ID = JobIDs.THM, Role = Roles.MagicDPS, NameEN = "Thaumaturge", NameJA = "呪術師", NameFR = "Occultiste", NameDE = "Thaumaturg" , NameCN = "咒术师", NameKO = "주술사"},
            new Job() {ID = JobIDs.CRP, Role = Roles.Crafter, NameEN = "Carpenter", NameJA = "木工師", NameFR = "Menuisier", NameDE = "Zimmerer" , NameCN = "刻木匠", NameKO = "목수"},
            new Job() {ID = JobIDs.BSM, Role = Roles.Crafter, NameEN = "Blacksmith", NameJA = "鍛冶師", NameFR = "Forgeron", NameDE = "Grobschmied" , NameCN = "锻铁师", NameKO = "대장장이"},
            new Job() {ID = JobIDs.ARM, Role = Roles.Crafter, NameEN = "Armorer", NameJA = "甲冑師", NameFR = "Armurier", NameDE = "Plattner" , NameCN = "铸甲匠", NameKO = "갑주제작사"},
            new Job() {ID = JobIDs.GSM, Role = Roles.Crafter, NameEN = "Goldsmith", NameJA = "彫金師", NameFR = "Orf\u00e8vre", NameDE = "Goldschmied" , NameCN = "雕金师", NameKO = "보석공예가"},
            new Job() {ID = JobIDs.LTW, Role = Roles.Crafter, NameEN = "Leatherworker", NameJA = "革細工師", NameFR = "Tanneur", NameDE = "Gerber" , NameCN = "制革匠", NameKO = "가죽공예가"},
            new Job() {ID = JobIDs.WVR, Role = Roles.Crafter, NameEN = "Weaver", NameJA = "裁縫師", NameFR = "Couturier", NameDE = "Weber" , NameCN = "裁衣匠", NameKO = "재봉사"},
            new Job() {ID = JobIDs.ALC, Role = Roles.Crafter, NameEN = "Alchemist", NameJA = "錬金術師", NameFR = "Alchimiste", NameDE = "Alchemist" , NameCN = "炼金术师", NameKO = "연금술사"},
            new Job() {ID = JobIDs.CUL, Role = Roles.Crafter, NameEN = "Culinarian", NameJA = "調理師", NameFR = "Cuisinier", NameDE = "Gourmet" , NameCN = "烹调师", NameKO = "요리사"},
            new Job() {ID = JobIDs.MIN, Role = Roles.Gatherer, NameEN = "Miner", NameJA = "採掘師", NameFR = "Mineur", NameDE = "Minenarbeiter" , NameCN = "采矿工", NameKO = "광부"},
            new Job() {ID = JobIDs.BOT, Role = Roles.Gatherer, NameEN = "Botanist", NameJA = "園芸師", NameFR = "Botaniste", NameDE = "G\u00e4rtner" , NameCN = "园艺师", NameKO = "원예가"},
            new Job() {ID = JobIDs.FSH, Role = Roles.Gatherer, NameEN = "Fisher", NameJA = "漁師", NameFR = "P\u00eacheur", NameDE = "Fischer" , NameCN = "钓鱼人", NameKO = "어부"},
            new Job() {ID = JobIDs.PLD, Role = Roles.Tank, NameEN = "Paladin", NameJA = "ナイト", NameFR = "Paladin", NameDE = "Paladin" , NameCN = "骑士", NameKO = "나이트", IsPopular = true},
            new Job() {ID = JobIDs.MNK, Role = Roles.MeleeDPS, NameEN = "Monk", NameJA = "モンク", NameFR = "Moine", NameDE = "M\u00f6nch" , NameCN = "武僧", NameKO = "몽크", IsPopular = true},
            new Job() {ID = JobIDs.WAR, Role = Roles.Tank, NameEN = "Warrior", NameJA = "戦士", NameFR = "Guerrier", NameDE = "Krieger" , NameCN = "战士", NameKO = "전사", IsPopular = true},
            new Job() {ID = JobIDs.DRG, Role = Roles.MeleeDPS, NameEN = "Dragoon", NameJA = "竜騎士", NameFR = "Chevalier Dragon", NameDE = "Dragoon" , NameCN = "龙骑士", NameKO = "용기사", IsPopular = true},
            new Job() {ID = JobIDs.BRD, Role = Roles.RangeDPS, NameEN = "Bard", NameJA = "吟遊詩人", NameFR = "Barde", NameDE = "Barde" , NameCN = "吟游诗人", NameKO = "음유시인", IsPopular = true},
            new Job() {ID = JobIDs.WHM, Role = Roles.Healer, NameEN = "White Mage", NameJA = "白魔道士", NameFR = "Mage Blanc", NameDE = "Wei\u00dfmagier" , NameCN = "白魔法师", NameKO = "백마도사", IsPopular = true},
            new Job() {ID = JobIDs.BLM, Role = Roles.MagicDPS, NameEN = "Black Mage", NameJA = "黒魔道士", NameFR = "Mage Noir", NameDE = "Schwarzmagier" , NameCN = "黑魔法师", NameKO = "흑마도사", IsPopular = true},
            new Job() {ID = JobIDs.ACN, Role = Roles.MagicDPS, NameEN = "Arcanist", NameJA = "巴術士", NameFR = "Arcaniste", NameDE = "Hermetiker" , NameCN = "秘术师", NameKO = "비술사"},
            new Job() {ID = JobIDs.SMN, Role = Roles.MagicDPS, NameEN = "Summoner", NameJA = "召喚士", NameFR = "Invocateur", NameDE = "Beschw\u00f6rer" , NameCN = "召唤师", NameKO = "소환사", IsPopular = true},
            new Job() {ID = JobIDs.SCH, Role = Roles.Healer, NameEN = "Scholar", NameJA = "学者", NameFR = "\u00e9rudit", NameDE = "Gelehrter" , NameCN = "学者", NameKO = "학자", IsPopular = true},
            new Job() {ID = JobIDs.ROG, Role = Roles.MeleeDPS, NameEN = "Rogue", NameJA = "双剣士", NameFR = "Surineur", NameDE = "Schurke" , NameCN = "双剑师", NameKO = "쌍검사"},
            new Job() {ID = JobIDs.NIN, Role = Roles.MeleeDPS, NameEN = "Ninja", NameJA = "忍者", NameFR = "Ninja", NameDE = "Ninja" , NameCN = "忍者", NameKO = "닌자", IsPopular = true},
            new Job() {ID = JobIDs.MCH, Role = Roles.RangeDPS, NameEN = "Machinist", NameJA = "機工士", NameFR = "Machiniste", NameDE = "Maschinist" , NameCN = "机工士", NameKO = "기공사", IsPopular = true},
            new Job() {ID = JobIDs.DRK, Role = Roles.Tank, NameEN = "Dark Knight", NameJA = "暗黒騎士", NameFR = "Chevalier Noir", NameDE = "Dunkelritter" , NameCN = "暗黑骑士", NameKO = "암흑기사", IsPopular = true},
            new Job() {ID = JobIDs.AST, Role = Roles.Healer, NameEN = "Astrologian", NameJA = "占星術師", NameFR = "Astromancien", NameDE = "Astrologe" , NameCN = "占星术士", NameKO = "점성술사", IsPopular = true},
            new Job() {ID = JobIDs.SAM, Role = Roles.MeleeDPS, NameEN = "Samurai", NameJA = "侍", NameFR = "Samoura\u00ef", NameDE = "Samurai" , NameCN = "武士", NameKO = "사무라이", IsPopular = true},
            new Job() {ID = JobIDs.RDM, Role = Roles.MagicDPS, NameEN = "Red Mage", NameJA = "赤魔道士", NameFR = "Mage Rouge", NameDE = "Rotmagier" , NameCN = "赤魔法师", NameKO = "적마도사", IsPopular = true},
            new Job() {ID = JobIDs.BLU, Role = Roles.MagicDPS, NameEN = "Blue Mage", NameJA = "青魔道士", NameFR = "Mage Bleu", NameDE = "Blaumagier" , NameCN = "青魔法师", NameKO = "청마도사", IsPopular = false},
            new Job() {ID = JobIDs.GNB, Role = Roles.Tank, NameEN = "Gunbreaker", NameJA = "ガンブレイカー", NameFR = "Pistosabreur", NameDE = "Revolverklinge" , NameCN = "绝枪战士", NameKO = "Gunbreaker", IsPopular = true},
            new Job() {ID = JobIDs.DNC, Role = Roles.RangeDPS, NameEN = "Dancer", NameJA = "踊り子", NameFR = "Danseur", NameDE = "Tänzer" , NameCN = "舞者", NameKO = "Dancer", IsPopular = true},
        };

        public static IEnumerable<JobIDs> PopularJobIDs =>
            from x in jobs
            where
            x.IsPopular
            orderby
            x.Role.ToSortOrder(),
            x.ID
            select
            x.ID;

        public static IReadOnlyList<Job> List => jobs;

        public static IReadOnlyList<Job> SortedList => (
            from x in jobs
            orderby
            x.Role,
            x.ID
            select
            x).ToList();

        public static Job Find(
            JobIDs id)
        {
            return jobs.Where(x => x.ID == id).FirstOrDefault() ?? new Job();
        }

        public static Job Find(
            int id)
        {
            return Jobs.Find(Jobs.IntToID(id));
        }

        public static Job Find(
            string id)
        {
            int.TryParse(id, out int idAsInt);
            return Jobs.Find(idAsInt);
        }

        public static Job FindFromName(
            string jobName)
            => jobs.FirstOrDefault(job =>
                job.Names.Any(name =>
                    string.Equals(name, jobName, StringComparison.OrdinalIgnoreCase))) ?? new Job();

        public static JobIDs IntToID(
            int id)
        {
            var idAsEnum = JobIDs.Unknown;
            if (Enum.IsDefined(typeof(JobIDs), id))
            {
                idAsEnum = (JobIDs)id;
            }

            return idAsEnum;
        }
    }

    public static class JobIDExtenstions
    {
        public static Job GetInfo(
            this JobIDs id)
            => Jobs.Find(id);
    }
}
