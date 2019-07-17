using System.Text.RegularExpressions;

namespace FFXIV.Framework.Extensions
{
    public static class RegexExtensions
    {
        public static bool TryGetDuration(
            Match match,
            out double duration)
        {
            duration = 0;

            if (!match.Success)
            {
                return false;
            }

            var text = string.Empty;

            text = match.Groups["duration"].Value;

            if (double.TryParse(text, out duration) &&
                duration < 9999d)
            {
                return true;
            }

            text = match.Groups["_duration"].Value;

            if (double.TryParse(text, out duration) &&
                duration < 9999d)
            {
                return true;
            }

            duration = 0;
            return false;
        }
    }
}
