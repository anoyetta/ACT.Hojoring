using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Serialization;
using ACT.SpecialSpellTimer.RazorModel;
using FFXIV.Framework.XIVHelper;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace ACT.SpecialSpellTimer.RaidTimeline
{
    public class TimelineScriptModel :
        TimelineBase
    {
        public override TimelineElementTypes TimelineType => TimelineElementTypes.Script;

        public override IList<TimelineBase> Children => null;

        private TimelineScriptEvents? scriptingEvent = null;

        [XmlIgnore]
        public TimelineScriptEvents? ScriptingEvent
        {
            get => this.scriptingEvent;
            set => this.SetProperty(ref this.scriptingEvent, value);
        }

        [XmlAttribute(AttributeName = "event")]
        public string ScriptingEventXML
        {
            get => this.ScriptingEvent?.ToString();
            set => this.ScriptingEvent = Enum.TryParse<TimelineScriptEvents>(value, out var v) ? v : (TimelineScriptEvents?)null;
        }

        private double? interval = null;

        [XmlIgnore]
        public double? Interval
        {
            get => this.interval;
            set => this.SetProperty(ref this.interval, value);
        }

        [XmlAttribute(AttributeName = "interval")]
        public string IntervalXML
        {
            get => this.Interval?.ToString();
            set => this.Interval = double.TryParse(value, out var v) ? v : (double?)null;
        }

        private string scriptCode;

        [XmlText]
        public string ScriptCode
        {
            get => this.scriptCode;
            set => this.SetProperty(ref this.scriptCode, value);
        }

        private static readonly ScriptOptions TimelineScriptOptions = ScriptOptions.Default
            .WithImports(
                "System",
                "System.IO",
                "System.Text",
                "System.Text.RegularExpressions",
                "System.Collections.Generic",
                "System.Collections.Linq",
                "ACT.SpecialSpellTimer.RazorModel",
                "FFXIV.Framework.Common",
                "FFXIV.Framework.Extensions",
                "FFXIV.Framework.XIVHelper")
            .AddReferences(
                Assembly.GetAssembly(typeof(TimelineScriptGlobalModel)),
                Assembly.GetAssembly(typeof(CombatantEx)));

        private Script<object> script;

        public bool Compile()
        {
            var result = true;
            this.script = null;

            try
            {
                if (!string.IsNullOrEmpty(this.scriptCode))
                {
                    this.script = CSharpScript.Create(
                        this.scriptCode,
                        TimelineScriptOptions,
                        typeof(TimelineScriptGlobalModel));

                    this.script.Compile();

                    // Test Run
                    this.Run("00:0000:Hello Script.");
                }
            }
            catch (Exception ex)
            {
                result = false;

                if (ex.InnerException is CompilationErrorException compileError)
                {
                }

                this.script = null;
            }

            return result;
        }

        public bool Run(
            string logLine = null)
            => this.RunAsync(logLine).Result;

        public async Task<bool> RunAsync(
            string logLine = null)
        {
            var result = true;

            if (this.script == null)
            {
                return result;
            }

            var globals = new TimelineScriptGlobalModel()
            {
                LogLine = logLine
            };

            var returnValue = (await this.script?.RunAsync(globals: globals)).ReturnValue;

            if (returnValue is bool b)
            {
                result = b;
            }

            return result;
        }
    }

    public enum TimelineScriptEvents
    {
        /// <summary>
        /// 常駐処理
        /// </summary>
        Resident = 0x00,

        /// <summary>
        /// 判定を拡張する
        /// </summary>
        /// <remarks>
        /// a タグ, t タグ の配下で使用した場合は自動的にこの扱いとなる
        /// </remarks>
        Expression = 0x01,

        /// <summary>
        /// Loglineが発生したとき
        /// </summary>
        OnLogline = 0x02,

        /// <summary>
        /// タイムラインファイルがロードされたとき
        /// </summary>
        OnLoaded = 0x10,

        /// <summary>
        /// 当該サブルーチンが始まったとき
        /// </summary>
        OnSubStarted = 0x11,

        /// <summary>
        /// ワイプしたとき
        /// </summary>
        OnWiped = 0x12,
    }
}
