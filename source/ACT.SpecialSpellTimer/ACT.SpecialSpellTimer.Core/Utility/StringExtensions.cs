using System;
using System.Text.RegularExpressions;

using ACT.SpecialSpellTimer.Config;

namespace ACT.SpecialSpellTimer.Utility
{
    public static class StringExtensions
    {
        public static Regex ToRegex(
            this string s)
        {
            Regex regex = null;

            try
            {
                var op = RegexOptions.Compiled;

                if (Settings.Default.EnableMultiLineMaching)
                {
                    op |= RegexOptions.Multiline;
                }

                regex = !string.IsNullOrEmpty(s) ?
                    new Regex(s, op) :
                    null;
            }
            catch (Exception ex)
            {
                regex = null;
                Logger.Write("ToRegex error:", ex);
            }

            return regex;
        }

        public static string ToRegexPattern(
            this string s)
        {
            if (Settings.Default.SimpleRegex)
            {
                return s ?? string.Empty;
            }

            return !string.IsNullOrEmpty(s) ?
                ".*" + s + ".*" :
                string.Empty;
        }
    }
}
