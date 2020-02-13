using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using ACT.SpecialSpellTimer.RazorModel;
using FFXIV.Framework.Common;
using FFXIV.Framework.Extensions;
using FFXIV.Framework.XIVHelper;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace ACT.SpecialSpellTimer.RaidTimeline
{
    public class TimelineScriptModel :
        TimelineBase
    {
        #region Logger

        private NLog.Logger AppLogger => AppLog.DefaultLogger;

        #endregion Logger

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
                "System.Linq",
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

            try
            {
                this.script = null;

                if (!string.IsNullOrEmpty(this.scriptCode))
                {
                    if (string.IsNullOrEmpty(this.Name))
                    {
                        result = false;

                        this.AppLogger.Error(
                            $"[TL][CSX] Script name is nothing, Script name is required.\n<script>\n{this.scriptCode}\n</script>");

                        return result;
                    }

                    this.script = CSharpScript.Create(
                        this.scriptCode,
                        TimelineScriptOptions,
                        typeof(TimelineScriptGlobalModel));

                    this.script.Compile();

                    // Test Run
                    this.TestRun();

                    this.AppLogger.Trace($"[TL][CSX] Compile was successful, name=\"{this.Name}\".");
                }
            }
            catch (CompilationErrorException cex)
            {
                result = false;
                this.DumpCompilationError(cex);
                this.script = null;
            }
            catch (AggregateException ex)
            {
                result = false;

                var message = $"[TL][CSX] Runtime error, name=\"{this.Name}\". {ex.Message}\n{ex.InnerException.ToFormatedString()}\n<script>{this.scriptCode}</script>";
                this.AppLogger.Error(message);

                this.script = null;
            }
            catch (Exception ex)
            {
                result = false;

                if (ex.InnerException is CompilationErrorException cex)
                {
                    this.DumpCompilationError(cex);
                }
                else
                {
                    this.AppLogger.Error(
                        $"[TL][CSX] Unexpected error, name=\"{this.Name}\".\n<script>{this.scriptCode}</script>\n\n{ex.ToFormatedString()}");
                }

                this.script = null;
            }

            return result;
        }

        public bool Run()
        {
            if (this.script == null)
            {
                return true;
            }
            catch (AggregateException ex)
            {
                result = false;

            var result = true;

            try
            {
                var returnValue = this.script?
                    .RunAsync(globals: TimelineScriptGlobalModel.Instance).Result?.ReturnValue ?? true;

                if (returnValue is bool b)
                {
                    result = b;
                }
            }
            catch (AggregateException ex)
            {
                result = false;

                var message = $"[TL][CSX] Runtime error, name=\"{this.Name}\". {ex.Message}\n{ex.InnerException.ToFormatedString()}\n<script>{this.scriptCode}</script>";
                this.AppLogger.Error(message);
            }
            catch (Exception ex)
            {
                result = false;

                if (ex.InnerException is CompilationErrorException cex)
                {
                    this.DumpCompilationError(cex);
                }
                else
                {
                    this.AppLogger.Error(
                        $"[TL][CSX] Unexpected error, name=\"{this.Name}\".\n<script>{this.scriptCode}</script>\n\n{ex.ToFormatedString()}");
                }
            }

            message.AppendLine($"<script>{this.scriptCode}</script>");

            this.AppLogger.Error(message.ToString());
        }

        private void TestRun()
        {
            this.script?.RunAsync(globals: TimelineScriptGlobalModel.CreateTestInstance()).Wait();
        }

        private void DumpCompilationError(
            CompilationErrorException cex)
        {
            var message = new StringBuilder();

            var errors = cex.Diagnostics.Count(x => x.Severity == DiagnosticSeverity.Error);
            var warns = cex.Diagnostics.Count(x => x.Severity == DiagnosticSeverity.Warning);
            message.AppendLine($"[TL][CSX] Compilation Error, name=\"{this.Name}\". {errors} errors, {warns} warnings.");

            var i = 1;
            foreach (var diag in cex.Diagnostics)
            {
                message.AppendLine($"{i}. {diag}");
                i++;
            }

            message.AppendLine($"<script>{this.scriptCode}</script>");

            this.AppLogger.Error(message.ToString());
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
