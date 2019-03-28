using System;
using System.ComponentModel.DataAnnotations;

namespace FFXIV.Framework.Extensions
{
    public static class EnumExtensions
    {
        public static string DisplayName<T>(this T value) where T : Enum
        {
            var fieldInfo = value.GetType().GetField(value.ToString());
            var descriptionAttributes = fieldInfo.GetCustomAttributes(
                typeof(DisplayAttribute), false) as DisplayAttribute[];

            if (descriptionAttributes != null && descriptionAttributes.Length > 0)
            {
                return descriptionAttributes[0].Name;
            }
            else
            {
                return value.ToString();
            }
        }
    }
}
