using System;
using System.IO;
using System.Windows;
using FFXIV.Framework.Common;
using FFXIV.Framework.Globalization;

namespace ACT.SpecialSpellTimer.resources
{
    public static class LocalizeExtensions
    {
        private const string LocaleFileName = @"Strings.SpeSpe.{0}.xaml";

        private const string CommonResourcesDirecotry = @"resources\styles";
        private const string CommonResourcesResources = @"ConfigViewResources.xaml";

        private static readonly object lockObject = new object();
        private static bool isCommonResourcesLoaded;
        private static bool isLocaleLoaded;

        public static void LoadConfigViewResources(
            this ILocalizable target)
        {
            var element = target as FrameworkElement;
            if (element == null)
            {
                return;
            }

            lock (lockObject)
            {
                if (isCommonResourcesLoaded)
                {
                    return;
                }

                var file = Path.Combine(DirectoryHelper.FindSubDirectory(CommonResourcesDirecotry), CommonResourcesResources);
                if (File.Exists(file))
                {
                    Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary()
                    {
                        Source = new Uri(file, UriKind.Absolute)
                    });
                }

                isCommonResourcesLoaded = true;
            }
        }

        public static void ReloadLocaleDictionary<T>(
            this T element,
            Locales locale) where T : FrameworkElement, ILocalizable
        {
            lock (lockObject)
            {
                if (isLocaleLoaded)
                {
                    return;
                }

                Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary()
                {
                    Source = locale.GetUri(LocaleFileName)
                });

                isLocaleLoaded = true;
            }
        }
    }
}
