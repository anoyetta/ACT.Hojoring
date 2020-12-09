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
        TimelineBase, ITimelineScript
    {
        #region Logger

        private NLog.Logger AppLogger => AppLog.DefaultLogger;

        #endregion Logger

        public override TimelineElementTypes TimelineType => TimelineElementTypes.Script;

        public override IList<TimelineBase> Children => null;

        [XmlIgnore]
        public string ParentSubRoutine => this.GetRootSubRoutine(this);

        private string GetRootSubRoutine(
            TimelineBase element)
        {
            var parent = element.Parent;

            if (parent == null)
            {
                return string.Empty;
            }

            if (parent is TimelineSubroutineModel sub)
            {
                return sub.Name ?? string.Empty;
            }

            return this.GetRootSubRoutine(parent);
        }

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
            set => this.ScriptingEvent = Enum.TryParse<TimelineScriptEvents>(value, out var v) ? v : null;
        }

        private double? interval = null;

        /// <summary>
        /// 常駐処理を実行する間隔（ミリ秒）
        /// </summary>
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

        [XmlIgnore]
        public DateTime LastExecutedTimestamp
        {
            get;
            private set;
        } = DateTime.MinValue;

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
                Assembly.GetAssembly(typeof(CombatantEx)),
                Assembly.GetAssembly(typeof(XIVLog)));

        private Script<object> script;

        private string currentCompiledCode;

        public static int anonymouseNo = 1;

        public bool Compile()
        {
            var result = true;

            try
            {
                if (string.IsNullOrEmpty(this.Name))
                {
                    this.Name = "AnonymouseScript-" + TimelineScriptingHost.AnonymouseScriptNo;
                    TimelineScriptingHost.AnonymouseScriptNo++;
                }

                this.script = null;

                if (!string.IsNullOrEmpty(this.scriptCode))
                {
                    if (string.IsNullOrEmpty(this.Name))
                    {
                        result = false;

                        this.AppLogger.Error(
                            $"{TimelineConstants.TLXLogSymbol} Script name is nothing, Script name is required.\n<script>\n{this.scriptCode}\n</script>");

                        return result;
                    }

                    if (this.script == null ||
                        this.currentCompiledCode != this.scriptCode)
                    {
                        this.script = CSharpScript.Create(
                            this.scriptCode,
                            TimelineScriptOptions,
                            typeof(TimelineScriptGlobalModel));

                        this.script.Compile();

                        // Test Run
                        this.TestRun();

                        this.currentCompiledCode = this.scriptCode;

                        this.AppLogger.Trace($"{TimelineConstants.TLXLogSymbol} Compile was successful, name=\"{this.Name}\".");
                    }
                    else
                    {
                        this.AppLogger.Trace($"{TimelineConstants.TLXLogSymbol} Is available, name=\"{this.Name}\".");
                    }
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

                var message = $"{TimelineConstants.TLXLogSymbol} Runtime error, name=\"{this.Name}\". {ex.Message}\n{ex.InnerException.ToFormatedString()}\n<script>{this.scriptCode}</script>";
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
                        $"{TimelineConstants.TLXLogSymbol} Unexpected error, name=\"{this.Name}\".\n<script>{this.scriptCode}</script>\n\n{ex.ToFormatedString()}");
                }

                this.script = null;
            }

            return result;
        }

        public object Run()
        {
            if (this.script == null)
            {
                return null;
            }

            var result = default(object);
            var message = string.Empty;

            var globalObject = TimelineScriptGlobalModel.Instance;

            try
            {
                result = this.script?.RunAsync(globalObject).Result?.ReturnValue ?? true;

                if (this.ScriptingEvent != TimelineScriptEvents.Resident)
                {
                    TimelineController.RaiseLog(
                        $"{TimelineConstants.TLXTraceLogSymbol} \"{this.Name}\" executed. ReturnValue=\"{result}\"");
                }
            }
            catch (AggregateException ex)
            {
                result = null;

                message = $"{TimelineConstants.TLXLogSymbol} Runtime error, name=\"{this.Name}\". {ex.Message}\n{ex.InnerException.ToFormatedString()}\n<script>{this.scriptCode}</script>";
                this.AppLogger.Error(message);

                TimelineController.RaiseLog($"{TimelineConstants.TLXTraceLogSymbol} Runtime error, name=\"{this.Name}\". {ex.Message}");
            }
            catch (Exception ex)
            {
                result = null;

                if (ex.InnerException is CompilationErrorException cex)
                {
                    this.DumpCompilationError(cex);
                }
                else
                {
                    this.AppLogger.Error(
                        $"{TimelineConstants.TLXLogSymbol} Unexpected error, name=\"{this.Name}\".\n<script>{this.scriptCode}</script>\n\n{ex.ToFormatedString()}");

                    TimelineController.RaiseLog($"{TimelineConstants.TLXTraceLogSymbol} Unexpected error, name=\"{this.Name}\". {ex.Message}");
                }
            }
            finally
            {
                this.LastExecutedTimestamp = DateTime.Now;
            }

            return result;
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
            message.AppendLine($"{TimelineConstants.TLXLogSymbol} Compilation Error, name=\"{this.Name}\". {errors} errors, {warns} warnings.");

            var i = 1;
            foreach (var diag in cex.Diagnostics)
            {
                message.AppendLine($"{i}. {diag}");
                i++;
            }

            message.AppendLine($"<script>{this.scriptCode}</script>");

            this.AppLogger.Error(message.ToString());
        }

        public override string ToString()
            => $"name={this.Name} parent={this.ParentSubRoutine} event={this.ScriptingEvent} inverval={this.interval:N0}";
    }
}
