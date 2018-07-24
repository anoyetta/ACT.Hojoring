using System;
using System.Collections.Generic;
using System.IO;
using FFXIV.Framework.Common;

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

        public static string ToResourcesName(
            this Locales locale)
        {
            var name = locale.ToText();
            if (locale == Locales.TW)
            {
                name = Locales.CN.ToText();
            }

            return name;
        }

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

        public static Uri GetUri(
            this Locales locale,
            string baseFileName)
        {
            const string Direcotry = @"resources\strings";

            var uri = default(Uri);
            var localeName = locale.ToResourcesName();

            var fileName = string.Format(baseFileName, localeName);

            var file = Path.Combine(DirectoryHelper.FindSubDirectory(Direcotry), fileName);
            if (!File.Exists(file))
            {
                // 言語リソースが存在しない場合はENを適用する
                fileName = string.Format(baseFileName, Locales.EN.ToText());
                file = Path.Combine(DirectoryHelper.FindSubDirectory(Direcotry), fileName);
            }

            if (File.Exists(file))
            {
                uri = new Uri(file, UriKind.Absolute);
            }

            return uri;
        }
    }

    public class ValueAndText
    {
        public Locales Value { get; set; }

        public string Text => this.Value.ToText();
    }
}
