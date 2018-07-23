using System;
using System.IO;
using System.Windows;
using FFXIV.Framework.Common;
using FFXIV.Framework.Globalization;

namespace ACT.SpecialSpellTimer.resources
{
    public static class LocalizeExtensions
    {
        public static void LoadConfigViewResources(
            this ILocalizable target)
        {
            const string Direcotry = @"resources\styles";
            const string Resources = @"ConfigViewResources.xaml";

            var element = target as FrameworkElement;
            if (element == null)
            {
                return;
            }

            var file = Path.Combine(DirectoryHelper.FindSubDirectory(Direcotry), Resources);
            if (File.Exists(file))
            {
                element.Resources.MergedDictionaries.Add(new ResourceDictionary()
                {
                    Source = new Uri(file, UriKind.Absolute)
                });
            }
        }

        public static void ReloadLocaleDictionary<T>(
            this T element,
            Locales locale) where T : FrameworkElement, ILocalizable
        {
            const string Direcotry = @"resources\strings";
            var Resources = $"Strings.SpeSpe.{locale.ToText()}.xaml";

            var file = Path.Combine(DirectoryHelper.FindSubDirectory(Direcotry), Resources);
            if (File.Exists(file))
            {
                element.Resources.MergedDictionaries.Add(new ResourceDictionary()
                {
                    Source = new Uri(file, UriKind.Absolute)
                });
            }
        }
    }
}
