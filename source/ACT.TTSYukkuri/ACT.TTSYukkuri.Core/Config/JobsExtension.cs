using FFXIV.Framework.XIVHelper;

namespace ACT.TTSYukkuri.Config
{
    public enum AlertCategories
    {
        Me,

        Paladin,
        Warrior,
        DarkKnight,
        Gunbreaker,

        WhiteMage,
        Scholar,
        Astrologian,
        Sage,

        Monk,
        Dragoon,
        Ninja,
        Samurai,
        Reaper,
        Viper,

        Bard,
        Machinist,
        Dancer,

        BlackMage,
        Summoner,
        RedMage,
        Pictomancer,
        BlueMage,

        CrafterAndGatherer,
    }

    public static class JobIdsExtensions
    {
        private static readonly string[,] JobPhonetics = new string[,]
        {
         // {0-EN,              1-JP,                2-FR,                3-DE,                4-KR                 5,6-CN  }
            { string.Empty,     string.Empty,        string.Empty,        string.Empty,        string.Empty,        string.Empty},
            { "Gladiator",      "けんじゅつし",      "Gladiateur",        "Gladiator",         "검술사",            "剑术师",},
            { "Pugilist",       "かくとうし",        "Pugiliste",         "Faustkämpfer",      "격투사",            "格斗家",},
            { "Marauder",       "ふじゅつし",        "Maraudeur",         "Marodeur",          "도끼술사",          "斧术师",},
            { "Lancer",         "そうじゅつし",      "Maître D'hast",     "Pikenier",          "창술사",            "枪术师",},
            { "Archer",         "きゅうじゅつし",    "Archer",            "Waldläufer",        "궁술사",            "弓箭手",},
            { "Conjurer",       "げんじゅつし",      "élémentaliste",     "Druide",            "환술사",            "幻术师",},
            { "Thaumaturge",    "じゅじゅつし",      "Occultiste",        "Thaumaturg",        "주술사",            "咒术师",},
            { "Carpenter",      "もっこうし",        "Menuisier",         "Zimmerer",          "목수",              "刻木",},
            { "Blacksmith",     "かじし",            "Forgeron",          "Grobschmied",       "대장장이",          "锻铁",},
            { "Armorer",        "かっちゅうし",      "Armurier",          "Plattner",          "갑주제작사",        "铸甲",},
            { "Goldsmith",      "ちょうきんし",      "Orfèvre",           "Goldschmied",       "보석공예가",        "雕金",},
            { "Leatherworker",  "かわざいくし",      "Tanneur",           "Gerber",            "가죽공예가",        "制革",},
            { "Weaver",         "さいほうし",        "Couturier",         "Weber",             "재봉사",            "裁衣",},
            { "Alchemist",      "れんきんじゅつし",  "Alchimiste",        "Alchemist",         "연금술사",          "炼金",},
            { "Culinarian",     "ちょうりし",        "Cuisinier",         "Gourmet",           "요리사",            "厨师",},
            { "Miner",          "さいくつし",        "Mineur",            "Minenarbeiter",     "광부",              "采矿",},
            { "Botanist",       "えんげいし",        "Botaniste",         "Gärtner",           "원예가",            "园艺",},
            { "Fisher",         "りょうし",          "Pêcheur",           "Fischer",           "어부",              "渔夫",},
            { "Paladin",        "ないと",            "Paladin",           "Paladin",           "나이트",            "骑士",},
            { "Monk",           "もんく",            "Moine",             "Mönch",             "몽크",              "武僧",},
            { "Warrior",        "せんし",            "Guerrier",          "Krieger",           "전사",              "战士",},
            { "Dragoon",        "りゅうきし",        "Chevalier Dragon",  "Dragoon",           "용기사",            "龙骑",},
            { "Bard",           "ぎんゆうしじん",    "Barde",             "Barde",             "음유시인",          "诗人",},
            { "White Mage",     "しろまどうし",      "Mage Blanc",        "Weißmagier",        "백마도사",          "白魔",},
            { "Black Mage",     "くろまどうし",      "Mage Noir",         "Schwarzmagier"      "흑마도사",          "黑魔",},
            { "Arcanist",       "はじゅつし",        "Arcaniste",         "Hermetiker",        "비술사",            "秘术师",},
            { "Summoner",       "しょうかんし",      "Invocateur",        "Beschwörer",        "소환사",            "召唤",},
            { "Scholar",        "がくしゃ",          "érudit",            "Gelehrter",         "학자",              "学者",},
            { "Rogue",          "そうけんし",        "Surineur",          "Schurke",           "쌍검사",            "双剑师",},
            { "Ninja",          "にんじゃ",          "Ninja",             "Ninja",             "닌자",              "忍者",},
            { "Machinist",      "きこうし",          "Machiniste",        "Maschinist",        "기공사",            "机工",},
            { "Dark Knight",    "あんこくきし",      "Chevalier Noir",    "Dunkelritter",      "암흑기사",          "黑骑",},
            { "Astrologian",    "せんせいじゅつし",  "Astromancien",      "Astrologe",         "점성술사",          "占星",},
            { "Samurai",        "さむらい",          "Samouraï",          "Samurai",           "사무라이",          "武士",},
            { "Red Mage",       "あかまどうし",      "Mage Rouge",        "Rotmagier",         "적마도사",          "赤魔",},
            { "Blue Mage",      "あおまどうし",      "Mage Bleu",         "Blaumagier",        "청마도사",          "青魔",},
            { "Gunbreaker",     "がんぶれいかー",    "Pistosabreur",      "Revolverklinge",    "건브레이커",        "绝枪",},
            { "Dancer",         "おどりこ",          "Danseur",           "Tänzer",            "무도가",            "舞者",},
            { "Reaper",         "りーぱー",          "Faucheur",          "Schnitter",         "리퍼",              "钐镰",},
            { "Sage",           "けんじゃ",          "Sage",              "Weiser",            "현자",              "贤者",},
            { "Viper",          "う゛ぁいぱー",      "Rôdeur vipèr",      "Viper",             "바이퍼",            "蝰蛇",},
            { "Pictomancer",    "ぴくとまんさー",    "Pictomancien",      "Piktomant",         "픽토맨서",          "画家",},
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
            AlertCategories.Gunbreaker,
            AlertCategories.Dancer,
            AlertCategories.Reaper,
            AlertCategories.Sage,
            AlertCategories.Viper,
            AlertCategories.Pictomancer,
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
            { "Gunbreaker", "ガンブレイカー","Gunbreaker","Gunbreaker","Gunbreaker","Gunbreaker", },
            { "White Mage/Conjurer", "白魔道士・幻術士","White Mage/Conjurer","White Mage/Conjurer","White Mage/Conjurer","白魔", },
            { "Scholar", "学者","Scholar","Scholar","Scholar","学者", },
            { "Astrologian", "占星術師","Astrologian","Astrologian","Astrologian","占星", },
            { "Sage", "賢者", "Sage", "Weiser", "Sage", "Sage", },
            { "Monk/Pugilist", "モンク・拳闘士","Monk/Pugilist","Monk/Pugilist","Monk/Pugilist","武僧", },
            { "Dragoon/Lancer", "竜騎士・槍術士","Dragoon/Lancer","Dragoon/Lancer","Dragoon/Lancer","龙骑", },
            { "Ninja/Rogue", "忍者・双剣士","Ninja/Rogue","Ninja/Rogue","Ninja/Rogue","忍者", },
            { "Samurai", "侍","Samurai","Samurai","Samurai","武士", },
            { "Reaper", "リーパー", "Faucheur", "Schnitter", "Reaper", "Reaper", },
            { "Viper", "ヴァイパー", "Rôdeur vipèr", "Viper", "Viper", "Viper", },
            { "Bard/Archer", "吟遊詩人・弓術士","Bard/Archer","Bard/Archer","Bard/Archer","诗人", },
            { "Machinist", "機工士","Machinist","Machinist","Machinist","机工", },
            { "Dancer", "踊り子","Dancer","Dancer","Dancer","Dancer", },
            { "Black Mage/Thaumaturge", "黒魔道士・呪術師","Black Mage/Thaumaturge","Black Mage/Thaumaturge","Black Mage/Thaumaturge","黑魔", },
            { "Summoner/Arcanist", "召喚士・巴術士","Summoner/Arcanist","Summoner/Arcanist","Summoner/Arcanist","召唤", },
            { "Red Mage", "赤魔道士","Red Mage","Red Mage","Red Mage","赤魔", },
            { "Pictomancer", "ピクトマンサー","Pictomancien","Piktomant","Pictomancer","Pictomancer", },
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
