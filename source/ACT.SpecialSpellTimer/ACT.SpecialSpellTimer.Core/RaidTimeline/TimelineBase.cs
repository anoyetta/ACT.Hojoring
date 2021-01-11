using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;
using ACT.SpecialSpellTimer.Image;
using ACT.SpecialSpellTimer.Utility;
using Prism.Mvvm;

namespace ACT.SpecialSpellTimer.RaidTimeline
{
    public enum NoticeDevices
    {
        Both = 0,
        Main,
        Sub,
    }

    public enum TimelineElementTypes
    {
        Timeline = 0,
        Default,
        Activity,
        Trigger,
        Subroutine,
        Load,
        PositionSync,
        Combatant,
        HPSync,
        Dump,
        VisualNotice,
        ImageNotice,
        Expressions,
        ExpressionsSet,
        ExpressionsPredicate,
        ExpressionsTable,
        Import,
        Script,
    }

    public static class TimelineElementTypesEx
    {
        public static string ToText(
            this TimelineElementTypes t)
            => new[]
            {
                "timeline",
                "default",
                "activity",
                "trigger",
                "subroutine",
                "load",
                "positionsync",
                "combatant",
                "hpsync",
                "dump",
                "visualnotice",
                "imagenotice",
                "expresions",
                "set",
                "predicate",
                "table",
                "import",
                "script",
            }[(int)t];

        public static TimelineElementTypes FromText(
            string text)
        {
            if (Enum.TryParse<TimelineElementTypes>(text, out TimelineElementTypes e))
            {
                return e;
            }

            return TimelineElementTypes.Timeline;
        }
    }

    [Serializable]
    public abstract class TimelineBase :
        BindableBase
    {
        [XmlIgnore]
        public abstract TimelineElementTypes TimelineType { get; }

        protected Guid id = Guid.NewGuid();

        [XmlIgnore]
        public Guid ID => this.id;

        protected string name = null;

        [XmlAttribute(AttributeName = "name")]
        public virtual string Name
        {
            get => this.name;
            set => this.SetProperty(ref this.name, value);
        }

        private string inherits;

        [XmlAttribute(AttributeName = "inherits")]
        public string Inherits
        {
            get => this.inherits;
            set => this.SetProperty(ref this.inherits, value);
        }

        private bool? enabled = null;

        [XmlIgnore]
        public virtual bool? Enabled
        {
            get => this.enabled;
            set => this.SetProperty(ref this.enabled, value);
        }

        [XmlAttribute(AttributeName = "enabled")]
        public string EnabledXML
        {
            get => this.Enabled?.ToString();
            set => this.Enabled = bool.TryParse(value, out var v) ? v : (bool?)null;
        }

        private TimelineBase parent = null;

        [XmlIgnore]
        public TimelineBase Parent
        {
            get => this.parent;
            set => this.SetProperty(ref this.parent, value);
        }

        public T GetParent<T>() where T : TimelineBase
            => this.parent as T;

        [XmlIgnore]
        public abstract IList<TimelineBase> Children { get; }

        public void Walk(
            Action<TimelineBase> action)
            => this.Walk(x =>
            {
                action.Invoke(x);
                return false;
            });

        public void Walk(
            Func<TimelineBase, bool> action)
        {
            if (action == null)
            {
                return;
            }

            var isBreak = action.Invoke(this);
            if (isBreak)
            {
                return;
            }

            if (this.Children != null)
            {
                foreach (var element in this.Children)
                {
                    element.Walk(action);
                }
            }
        }
    }

    public interface ISynchronizable
    {
        string Name { get; }

        string SyncKeyword { get; set; }

        string SyncKeywordReplaced { get; set; }

        Regex SyncRegex { get; }

        Match SyncMatch { get; set; }

        string Text { get; set; }

        string TextReplaced { get; set; }

        string Notice { get; set; }

        string NoticeReplaced { get; set; }
    }

    public static class ISynchronizableEx
    {
        public static Match TryMatch(
            this ISynchronizable sync,
            string logLine)
        {
            sync.SyncMatch = sync.SyncRegex?.Match(logLine);
            return sync.SyncMatch;
        }

        private static readonly Regex DetectVariableRegex = new Regex(
            @"(TABLE|VAR)\['(?<name>[^']+?)'\]",
            RegexOptions.Compiled);

        public static void InitRegex(
            this ISynchronizable sync)
        {
            if (string.IsNullOrEmpty(sync.SyncKeyword))
            {
                sync.SyncKeywordReplaced = string.Empty;
                return;
            }

            var replacedKeyword = sync.SyncKeyword;

            var matches = DetectVariableRegex.Matches(
                replacedKeyword);

            if (matches.Count > 0)
            {
                var variableList = new List<string>();

                foreach (Match m in matches)
                {
                    var name = m.Groups["name"].Value;

                    if (!string.IsNullOrEmpty(name))
                    {
                        if (variableList.Contains(name))
                        {
                            continue;
                        }

                        var action = new Action(() =>
                        {
                            var sw = Stopwatch.StartNew();

                            try
                            {
                                sync.RecompileRegex();
                            }
                            finally
                            {
                                sw.Stop();
                            }

                            var label = !string.IsNullOrEmpty(sync.Name) ?
                                $"name={sync.Name}" :
                                $"text={sync.Text}";

                            var log = $"{TimelineConstants.LogSymbol} Refered trigger was recompiled. {label} regex=\"{sync.SyncRegex}\" {sw.ElapsedMilliseconds}ms";
                            var simpleLog = $"{TimelineConstants.LogSymbol} Refered trigger was recompiled. {label} {sw.ElapsedMilliseconds}ms";
                            TimelineController.RaiseLog(simpleLog);
                            Logger.Write(log);
                        });

                        if (!TimelineExpressionsModel.ReferedTriggerRecompileDelegates.ContainsKey(name))
                        {
                            TimelineExpressionsModel.ReferedTriggerRecompileDelegates[name] = action;
                        }
                        else
                        {
                            TimelineExpressionsModel.ReferedTriggerRecompileDelegates[name] += action;
                        }

                        variableList.Add(name);
                    }
                }
            }

            // プレースホルダを置換する
            // VAR['hoge'] 変数を置換する
            replacedKeyword = TimelineExpressionsModel.ReplaceText(replacedKeyword);

            // EVAL(expressions) 関数を置換する
            replacedKeyword = TimelineExpressionsModel.ReplaceEval(replacedKeyword);

            sync.SyncKeywordReplaced = replacedKeyword;
        }

        public static void RecompileRegex(
            this ISynchronizable sync)
        {
            if (string.IsNullOrEmpty(sync.SyncKeyword))
            {
                sync.SyncKeywordReplaced = string.Empty;
                return;
            }

#if DEBUG
            if (sync.Name?.Contains("DEBUG") ?? false)
            {
                Debug.WriteLine("RecompileRegex");
            }
#endif

            var replacedKeyword = sync.SyncKeyword;

            // プレースホルダを置換する
            // VAR['hoge'] 変数を置換する
            replacedKeyword = TimelineExpressionsModel.ReplaceText(replacedKeyword);

            // EVAL(expressions) 関数を置換する
            replacedKeyword = TimelineExpressionsModel.ReplaceEval(replacedKeyword);

            sync.SyncKeywordReplaced = replacedKeyword;
        }
    }

    public interface IStylable
    {
        string Style { get; set; }

        TimelineStyle StyleModel { get; set; }

        string Icon { get; set; }

        bool ExistsIcon { get; }

        BitmapSource IconImage { get; }

        BitmapSource ThisIconImage { get; }
    }

    public static class IStylableEx
    {
        public static bool GetExistsIcon(
            this IStylable element) =>
            !string.IsNullOrEmpty(element.Icon) ||
            !string.IsNullOrEmpty(element.StyleModel?.Icon);

        public static BitmapSource GetIconImage(
            this IStylable element) =>
            element.ThisIconImage ?? element.StyleModel?.IconImage;

        public static BitmapSource GetThisIconImage(
            this IStylable element) =>
            string.IsNullOrEmpty(element.Icon) ?
            null :
            IconController.Instance.GetIconFile(element.Icon)?.CreateBitmapImage();
    }
}
