using System.Windows;
using FFXIV.Framework.Globalization;

namespace ACT.UltraScouter.resources
{
    public static class LocalizeExtensions
    {
        public static void ReloadLocaleDictionary<T>(
            this T element,
            Locales locale) where T : FrameworkElement, ILocalizable =>
            element.Resources.MergedDictionaries.Add(new ResourceDictionary()
            {
                Source = locale.GetUri("Strings.UlSco.{0}.xaml")
            });
    }
}
