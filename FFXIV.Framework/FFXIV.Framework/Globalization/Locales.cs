using System;
using System.Collections.Generic;

namespace FFXIV.Framework.Globalization
{
    public enum Locales
    {
        EN = 0,
        JA,
        FR,
        DE,
        KO,
        TW,
        CN,
    }

    public static class LocalesExtensions
    {
        public static string ToText(
            this Locales locale) =>
            new[]
            {
                "en-US",
                "ja-JP",
                "fr-FR",
                "de-DE",
                "ko-KR",
                "zh-TW",
                "zh-CN",
            }[(int)locale];

        public static IReadOnlyList<ValueAndText> Enums
        {
            get
            {
                var values = Enum.GetValues(typeof(Locales));
                var list = new List<ValueAndText>();
                foreach (Locales value in values)
                {
                    list.Add(new ValueAndText()
                    {
                        Value = value
                    });
                }

                return list;
            }
        }
    }

    public class ValueAndText
    {
        public Locales Value { get; set; }

        public string Text => this.Value.ToText();
    }
}
