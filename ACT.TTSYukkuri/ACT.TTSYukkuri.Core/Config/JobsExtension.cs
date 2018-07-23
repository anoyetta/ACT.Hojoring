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

        CrafterAndGatherer,
    }

    public static class JobIdsExtensions
    {
        private static readonly string[,] JobPhonetics = new string[,]
        {
            { string.Empty, string.Empty },
            { "Gladiator", "けんじゅつし", },
            { "Pugilist", "かくとうし", },
            { "Marauder", "ふじゅつし", },
            { "Lancer", "そうじゅつし", },
            { "Archer", "きゅうじゅつし", },
            { "Conjurer", "げんじゅつし", },
            { "Thaumaturge", "じゅじゅつし", },
            { "Carpenter", "もっこうし", },
            { "Blacksmith", "かじし", },
            { "Armorer", "かっちゅうし", },
            { "Goldsmith", "ちょうきんし", },
            { "Leatherworker", "かわざいくし", },
            { "Weaver", "さいほうし", },
            { "Alchemist", "れんきんじゅつし", },
            { "Culinarian", "ちょうりし", },
            { "Miner", "さいくつし", },
            { "Botanist", "えんげいし", },
            { "Fisher", "りょうし", },
            { "Paladin", "ないと", },
            { "Monk", "もんく", },
            { "Warrior", "せんし", },
            { "Dragoon", "りゅうきし", },
            { "Bard", "ぎんゆうしじん", },
            { "White Mage", "しろまどうし", },
            { "Black Mage", "くろまどうし", },
            { "Arcanist", "はじゅつし", },
            { "Summoner", "しょうかんし", },
            { "Scholar", "がくしゃ", },
            { "Rogue", "そうけんし", },
            { "Ninja", "にんじゃ", },
            { "Machinist", "きこうし", },
            { "Dark Knight", "あんこくきし", },
            { "Astrologian", "せんせいじゅつし", },
            { "Samurai", "さむらい", },
            { "Red Mage", "あかまどうし", },
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
        };

        public static string GetPhonetic(
            this JobIDs id) =>
            JobPhonetics[
                (int)id,
                ((int)Settings.Default.UILocale) > 1 ? 0 : (int)Settings.Default.UILocale];

        public static AlertCategories GetAlertCategory(
            this JobIDs id) =>
            JobAlertCategories[(int)id];
    }

    public static class AlertCategoriesExtensions
    {
        private static readonly string[,] AlertCategoriesTexts = new string[,]
        {
            { "Me", "自分自身" },
            { "Paladin/Gladiator", "ナイト・剣術士", },
            { "Warrior/Marauder", "戦士・斧術士", },
            { "Dark Knight", "暗黒騎士", },
            { "White Mage/Conjurer", "白魔道士・幻術士", },
            { "Scholar", "学者", },
            { "Astrologian", "占星術師", },
            { "Monk/Pugilist", "モンク・拳闘士", },
            { "Dragoon/Lancer", "竜騎士・槍術士", },
            { "Ninja/Rogue", "忍者・双剣士", },
            { "Samurai", "侍", },
            { "Bard/Archer", "吟遊詩人・弓術士", },
            { "Machinist", "機工士", },
            { "Black Mage/Thaumaturge", "黒魔道士・呪術師", },
            { "Summoner/Arcanist", "召喚士・巴術士", },
            { "Red Mage", "赤魔道士", },
            { "Crafter/Gatherer", "クラフター・ギャザラー", },
        };

        public static string GetText(
            this AlertCategories category) =>
            AlertCategoriesTexts[
                (int)category,
                ((int)Settings.Default.UILocale) > 1 ? 0 : (int)Settings.Default.UILocale];
    }
}
