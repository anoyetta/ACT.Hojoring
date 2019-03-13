using FFXIV.Framework.FFXIVHelper;

namespace ACT.TTSYukkuri.Config
{
    public enum AlertCategories
    {
        Me,

        Paladin,
        Warrior,
        DarkKnight,

        WhiteMage,
        Scholar,
        Astrologian,

        Monk,
        Dragoon,
        Ninja,
        Samurai,

        Bard,
        Machinist,

        BlackMage,
        Summoner,
        RedMage,
        BlueMage,

        CrafterAndGatherer,
    }

    public static class JobIdsExtensions
    {
        private static readonly string[,] JobPhonetics = new string[,]
        {
         // {0-EN,              1-JP,                2-FR,              3-DE,             4-KR                 5,6-CN  }
            { string.Empty,     string.Empty,        string.Empty,      string.Empty,     string.Empty,        string.Empty},
            { "Gladiator",      "けんじゅつし",      "Gladiator",       "Gladiator",      "Gladiator",         "剑术师",},
            { "Pugilist",       "かくとうし",        "Pugilist",        "Pugilist",       "Pugilist",          "格斗家",},
            { "Marauder",       "ふじゅつし",        "Marauder",        "Marauder",       "Marauder",          "斧术师",},
            { "Lancer",         "そうじゅつし",      "Lancer",          "Lancer",         "Lancer",            "枪术师",},
            { "Archer",         "きゅうじゅつし",    "Archer",          "Archer",         "Archer",            "弓箭手",},
            { "Conjurer",       "げんじゅつし",      "Conjurer",        "Conjurer",       "Conjurer",          "幻术师",},
            { "Thaumaturge",    "じゅじゅつし",      "Thaumaturge",     "Thaumaturge",    "Thaumaturge",       "咒术师",},
            { "Carpenter",      "もっこうし",        "Carpenter",       "Carpenter",      "Carpenter",         "刻木",},
            { "Blacksmith",     "かじし",            "Blacksmith",      "Blacksmith",     "Blacksmith",        "锻铁",},
            { "Armorer",        "かっちゅうし",      "Armorer",         "Armorer",        "Armorer",           "铸甲",},
            { "Goldsmith",      "ちょうきんし",      "Goldsmith",       "Goldsmith",      "Goldsmith",         "雕金",},
            { "Leatherworker",  "かわざいくし",      "Leatherworker",   "Leatherworker",  "Leatherworker",     "制革",},
            { "Weaver",         "さいほうし",        "Weaver",          "Weaver",         "Weaver",            "裁衣",},
            { "Alchemist",      "れんきんじゅつし",  "Alchemist",       "Alchemist",      "Alchemist",         "炼金",},
            { "Culinarian",     "ちょうりし",        "Culinarian",      "Culinarian",     "Culinarian",        "厨师",},
            { "Miner",          "さいくつし",        "Miner",           "Miner",          "Miner",             "采矿",},
            { "Botanist",       "えんげいし",        "Botanist",        "Botanist",       "Botanist",          "园艺",},
            { "Fisher",         "りょうし",          "Fisher",          "Fisher",         "Fisher",            "渔夫",},
            { "Paladin",        "ないと",            "Paladin",         "Paladin",        "Paladin",           "骑士",},
            { "Monk",           "もんく",            "Monk",            "Monk",           "Monk",              "武僧",},
            { "Warrior",        "せんし",            "Warrior",         "Warrior",        "Warrior",           "战士",},
            { "Dragoon",        "りゅうきし",        "Dragoon",         "Dragoon",        "Dragoon",           "龙骑",},
            { "Bard",           "ぎんゆうしじん",    "Bard",            "Bard",           "Bard",              "诗人",},
            { "White Mage",     "しろまどうし",      "White Mage",      "White Mage",     "White Mage",        "白魔",},
            { "Black Mage",     "くろまどうし",      "Black Mage",      "Black Mage",     "Black Mage",        "黑魔",},
            { "Arcanist",       "はじゅつし",        "Arcanist",        "Arcanist",       "Arcanist",          "秘术师",},
            { "Summoner",       "しょうかんし",      "Summoner",        "Summoner",       "Summoner",          "召唤",},
            { "Scholar",        "がくしゃ",          "Scholar",         "Scholar",        "Scholar",           "学者",},
            { "Rogue",          "そうけんし",        "Rogue",           "Rogue",          "Rogue",             "双剑师",},
            { "Ninja",          "にんじゃ",          "Ninja",           "Ninja",          "Ninja",             "忍者",},
            { "Machinist",      "きこうし",          "Machinist",       "Machinist",      "Machinist",         "机工",},
            { "Dark Knight",    "あんこくきし",      "Dark Knight",     "Dark Knight",    "Dark Knight",       "黑骑",},
            { "Astrologian",    "せんせいじゅつし",  "Astrologian",     "Astrologian",    "Astrologian",       "占星",},
            { "Samurai",        "さむらい",          "Samurai",         "Samurai",        "Samurai",           "武士",},
            { "Red Mage",       "あかまどうし",      "Red Mage",        "Red Mage",       "Red Mage",          "赤魔",},
            { "Blue Mage",      "あおまどうし",      "Blue Mage",       "Blue Mage",      "Blue Mage",         "Blue Mage",},
        };

        public static readonly AlertCategories[] JobAlertCategories = new AlertCategories[]
        {
            AlertCategories.Me,
            AlertCategories.Paladin,
            AlertCategories.Monk,
            AlertCategories.Warrior,
            AlertCategories.Dragoon,
            AlertCategories.Bard,
            AlertCategories.WhiteMage,
            AlertCategories.BlackMage,
            AlertCategories.CrafterAndGatherer,
            AlertCategories.CrafterAndGatherer,
            AlertCategories.CrafterAndGatherer,
            AlertCategories.CrafterAndGatherer,
            AlertCategories.CrafterAndGatherer,
            AlertCategories.CrafterAndGatherer,
            AlertCategories.CrafterAndGatherer,
            AlertCategories.CrafterAndGatherer,
            AlertCategories.CrafterAndGatherer,
            AlertCategories.CrafterAndGatherer,
            AlertCategories.CrafterAndGatherer,
            AlertCategories.Paladin,
            AlertCategories.Monk,
            AlertCategories.Warrior,
            AlertCategories.Dragoon,
            AlertCategories.Bard,
            AlertCategories.WhiteMage,
            AlertCategories.BlackMage,
            AlertCategories.Summoner,
            AlertCategories.Summoner,
            AlertCategories.Scholar,
            AlertCategories.Ninja,
            AlertCategories.Ninja,
            AlertCategories.Machinist,
            AlertCategories.DarkKnight,
            AlertCategories.Astrologian,
            AlertCategories.Samurai,
            AlertCategories.RedMage,
            AlertCategories.BlueMage,
        };

        public static string GetPhonetic(
            this JobIDs id) => id == JobIDs.Unknown ? string.Empty :
            JobPhonetics[
                (int)id,
                ((int)Settings.Default.UILocale) > 5 ? 5 : (int)Settings.Default.UILocale];

        public static AlertCategories GetAlertCategory(
            this JobIDs id) =>
            JobAlertCategories[(int)id];
    }

    public static class AlertCategoriesExtensions
    {
        private static readonly string[,] AlertCategoriesTexts = new string[,]
        {
            // {0-EN,              1-JP,              2-FR,              3-DE,             4-KR                 5,6-CN  }
            { "Me","自分自身","Me","Me","Me","我", },
            { "Paladin/Gladiator", "ナイト・剣術士","Paladin/Gladiator","Paladin/Gladiator","Paladin/Gladiator","骑士", },
            { "Warrior/Marauder", "戦士・斧術士","Warrior/Marauder","Warrior/Marauder","Warrior/Marauder","战士", },
            { "Dark Knight", "暗黒騎士","Dark Knight","Dark Knight","Dark Knight","黑骑", },
            { "White Mage/Conjurer", "白魔道士・幻術士","White Mage/Conjurer","White Mage/Conjurer","White Mage/Conjurer","白魔", },
            { "Scholar", "学者","Scholar","Scholar","Scholar","学者", },
            { "Astrologian", "占星術師","Astrologian","Astrologian","Astrologian","占星", },
            { "Monk/Pugilist", "モンク・拳闘士","Monk/Pugilist","Monk/Pugilist","Monk/Pugilist","武僧", },
            { "Dragoon/Lancer", "竜騎士・槍術士","Dragoon/Lancer","Dragoon/Lancer","Dragoon/Lancer","龙骑", },
            { "Ninja/Rogue", "忍者・双剣士","Ninja/Rogue","Ninja/Rogue","Ninja/Rogue","忍者", },
            { "Samurai", "侍","Samurai","Samurai","Samurai","武士", },
            { "Bard/Archer", "吟遊詩人・弓術士","Bard/Archer","Bard/Archer","Bard/Archer","诗人", },
            { "Machinist", "機工士","Machinist","Machinist","Machinist","机工", },
            { "Black Mage/Thaumaturge", "黒魔道士・呪術師","Black Mage/Thaumaturge","Black Mage/Thaumaturge","Black Mage/Thaumaturge","黑魔", },
            { "Summoner/Arcanist", "召喚士・巴術士","Summoner/Arcanist","Summoner/Arcanist","Summoner/Arcanist","召唤", },
            { "Red Mage", "赤魔道士","Red Mage","Red Mage","Red Mage","赤魔", },
            { "Blue Mage", "青魔道士","Blue Mage","Blue Mage","Blue Mage","Blue Mage", },
            { "Crafter/Gatherer", "クラフター・ギャザラー","Crafter/Gatherer","Crafter/Gatherer","Crafter/Gatherer","采集/制造", },
        };

        public static string GetText(
            this AlertCategories category) =>
            AlertCategoriesTexts[
                (int)category,
                ((int)Settings.Default.UILocale) > 5 ? 5 : (int)Settings.Default.UILocale];
    }
}
