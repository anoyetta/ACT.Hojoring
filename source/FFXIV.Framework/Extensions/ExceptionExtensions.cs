using System;

namespace FFXIV.Framework.Extensions
{
    public static class ExceptionExtensions
    {
        public static string ToFormatedString(this Exception ex)
        {
            var info = $"{ex.GetType()}\n\n{ex.Message}\n{ex.StackTrace}";

            if (ex.InnerException != null)
            {
                info += $"\n\nInner Exception :\n{ToFormatedString(ex.InnerException)}";
            }

            return info;
        }
    }
}
