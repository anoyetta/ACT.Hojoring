using System.Windows;
using FFXIV.Framework.Globalization;

namespace ACT.TTSYukkuri.resources
{
    public static class LocalizeExtensions
    {
        private const string LocaleFileName = @"Strings.Yukkuri.{0}.xaml";

        private static readonly object lockObject = new object();
        private static bool isLocaleLoaded;

        public static void ReloadLocaleDictionary<T>(
            this T element,
            Locales locale) where T : FrameworkElement
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
