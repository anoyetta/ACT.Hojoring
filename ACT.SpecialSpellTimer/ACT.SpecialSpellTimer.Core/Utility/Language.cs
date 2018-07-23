using System.Linq;

namespace ACT.SpecialSpellTimer.Utility
{
    internal class Language
    {
        public string FriendlyName { get; set; }
        public string Value { get; set; }
        public string Locale { get; set; }

        public static Language[] GetLanguageList()
        {
            return new Language[]
            {
                new Language { FriendlyName = "English", Value = "EN", Locale = "en-US" },
                new Language { FriendlyName = "日本語", Value = "JP", Locale = "ja-JP" },
                new Language { FriendlyName = "한국어", Value = "KR", Locale = "kr-KR" },
            };
        }

        public override string ToString()
        {
            return FriendlyName;
        }
    }

    public static class LanguageExtentions
    {
        public static string ToLocale(
            this string language) =>
                Language.GetLanguageList()
                .FirstOrDefault(x => x.Value == language)?
                .Locale ?? string.Empty;
    }
}
