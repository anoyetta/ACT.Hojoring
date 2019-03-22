using System.Collections.Generic;
using System.Text.RegularExpressions;
using FFXIV.Framework.Bridge;
using FFXIV.Framework.Extensions;

namespace ACT.SpecialSpellTimer.RaidTimeline
{
    public static class TimelineTextCommands
    {
        private static readonly string TimelineCommand = "/sst";

        public static void SetSubscribeTextCommands()
        {
            var commandGroups = new[]
            {
                CreateReloadCommands(),
                CreateExpressionsCommands(),
            };

            foreach (var group in commandGroups)
            {
                foreach (var cmd in group)
                {
                    TextCommandBridge.Instance.Subscribe(cmd);
                }
            }
        }

        private static readonly Regex SetVarCommandRegex = new Regex(
            $@"{TimelineCommand}\s+set\s+(?<name>\w+)\s+(?<value>\w]+)\s*(?<option>global|temp)?",
            RegexOptions.Compiled |
            RegexOptions.IgnoreCase);

        private static IEnumerable<TextCommand> CreateExpressionsCommands()
        {
            var setCommand = new TextCommand(
            (string logLine, out Match match) =>
            {
                match = null;

                if (!logLine.ContainsIgnoreCase(TimelineCommand))
                {
                    return false;
                }

                match = SetVarCommandRegex.Match(logLine);
                return match.Success;
            },
            (string logLine, Match match) =>
            {
                if (match == null ||
                    !match.Success)
                {
                    return;
                }

                var name = match.Groups["name"].ToString();
                var value = match.Groups["value"].ToString();

                var option = match.Groups["option"]?.ToString() ?? string.Empty;
                var zone = TimelineController.CurrentController?.CurrentZoneName ?? string.Empty;

                if (option.ContainsIgnoreCase("global"))
                {
                    zone = TimelineModel.GlobalZone;
                }
                else
                {
                    if (option.ContainsIgnoreCase("temp"))
                    {
                        zone = string.Empty;
                    }
                }

                TimelineExpressionsModel.SetVariable(name, value, zone);
            });

            return new[] { setCommand };
        }

        private static readonly Regex ReloadCommandRegex = new Regex(
            $@"{TimelineCommand}\s+reload",
            RegexOptions.Compiled |
            RegexOptions.IgnoreCase);

        private static IEnumerable<TextCommand> CreateReloadCommands()
        {
            var cmd = new TextCommand(
            (string logLine, out Match match) =>
            {
                match = null;

                if (!logLine.ContainsIgnoreCase(TimelineCommand))
                {
                    return false;
                }

                match = ReloadCommandRegex.Match(logLine);
                return match.Success;
            },
            async (string logLine, Match match) =>
            {
                if (match == null ||
                    !match.Success)
                {
                    return;
                }

                if (TimelineController.CurrentController != null)
                {
                    await TimelineController.CurrentController.Model.ExecuteReloadCommandAsync();
                }
            });

            return new[] { cmd };
        }
    }
}
