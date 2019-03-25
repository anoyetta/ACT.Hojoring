using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FFXIV.Framework.Bridge
{
    public class TextCommandBridge
    {
        #region Singleton

        private static TextCommandBridge instance;

        public static TextCommandBridge Instance => instance ?? (instance = new TextCommandBridge());

        private TextCommandBridge()
        {
        }

        #endregion Singleton

        private readonly List<TextCommand> TextCommands = new List<TextCommand>();

        public void Subscribe(
            TextCommand command)
            => this.TextCommands.Add(command);

        public bool TryExecute(
            string logLine)
        {
            if (string.IsNullOrEmpty(logLine))
            {
                return false;
            }

            var logLineWithoutCode = logLine.Substring(8);
            if (logLineWithoutCode.TrimStart().StartsWith("#") ||
                logLineWithoutCode.TrimStart().StartsWith("//"))
            {
                return false;
            }

            var executed = false;

            foreach (var command in this.TextCommands)
            {
                executed |= command.TryExecute(logLine);
            }

            return executed;
        }

        public bool TryExecute(
            IEnumerable<string> logLines)
        {
            var executed = false;

            foreach (var logLine in logLines)
            {
                executed |= this.TryExecute(logLine);
            }

            return executed;
        }
    }

    public class TextCommand
    {
        public TextCommand(
            CanExecuteCallback canExecute,
            ExecuteCallback execute)
        {
            this.CanExecute = canExecute;
            this.Execute = execute;
        }

        public delegate bool CanExecuteCallback(string logLine, out Match match);

        public delegate void ExecuteCallback(string logLine, Match match = null);

        public CanExecuteCallback CanExecute { get; private set; }

        public ExecuteCallback Execute { get; private set; }

        public bool TryExecute(
            string logLine)
        {
            if (this.CanExecute == null ||
                this.Execute == null)
            {
                return false;
            }

            if (this.CanExecute(logLine, out Match match))
            {
                this.Execute(logLine, match);
                return true;
            }

            return false;
        }
    }
}
