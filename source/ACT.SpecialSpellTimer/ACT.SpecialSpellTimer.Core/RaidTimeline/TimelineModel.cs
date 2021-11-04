using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Serialization;
using ACT.SpecialSpellTimer.Config;
using ACT.SpecialSpellTimer.RazorModel;
using Advanced_Combat_Tracker;
using FFXIV.Framework.Common;
using FFXIV.Framework.Extensions;
using FFXIV.Framework.Globalization;
using FFXIV.Framework.WPF.Views;
using FFXIV.Framework.XIVHelper;
using Prism.Commands;
using RazorEngine;
using RazorEngine.Configuration;
using RazorEngine.Templating;
using RazorEngine.Text;

namespace ACT.SpecialSpellTimer.RaidTimeline
{
    [XmlType(TypeName = "timeline")]
    [XmlInclude(typeof(TimelineDefaultModel))]
    [XmlInclude(typeof(TimelineActivityModel))]
    [XmlInclude(typeof(TimelineTriggerModel))]
    [XmlInclude(typeof(TimelineSubroutineModel))]
    public partial class TimelineModel :
        TimelineBase
    {
        public override TimelineElementTypes TimelineType => TimelineElementTypes.Timeline;

        public override IList<TimelineBase> Children => this.elements;

        [XmlElement(ElementName = "name")]
        public string TimelineName
        {
            get => this.name;
            set
            {
                if (this.SetProperty(ref this.name, value))
                {
                    this.RaisePropertyChanged(nameof(this.DisplayName));
                }
            }
        }

        private string revision = null;

        [XmlElement(ElementName = "rev")]
        public string Revision
        {
            get => this.revision;
            set => this.SetProperty(ref this.revision, value);
        }

        private string description = null;

        [XmlElement(ElementName = "description")]
        public string Description
        {
            get => this.description;
            set
            {
                var text = string.Empty;

                if (!string.IsNullOrEmpty(value))
                {
                    using (var sr = new StringReader(value))
                    {
                        while (true)
                        {
                            var line = sr.ReadLine();
                            if (line == null)
                            {
                                break;
                            }

                            line = line.Trim();
                            if (string.IsNullOrEmpty(line))
                            {
                                continue;
                            }

                            text += !string.IsNullOrEmpty(text) ?
                                Environment.NewLine + line :
                                line;
                        }
                    }
                }

                this.SetProperty(ref this.description, text);
                this.RaisePropertyChanged(nameof(this.DescriptionForDisplay));
            }
        }

        private string author = null;

        [XmlElement(ElementName = "author")]
        public string Author
        {
            get => this.author;
            set
            {
                var text = string.Empty;

                if (!string.IsNullOrEmpty(value))
                {
                    using (var sr = new StringReader(value))
                    {
                        while (true)
                        {
                            var line = sr.ReadLine();
                            if (line == null)
                            {
                                break;
                            }

                            line = line.Trim();
                            if (string.IsNullOrEmpty(line))
                            {
                                continue;
                            }

                            text += !string.IsNullOrEmpty(text) ?
                                Environment.NewLine + line :
                                line;
                        }
                    }
                }

                this.SetProperty(ref this.author, text);
                this.RaisePropertyChanged(nameof(this.AuthorForDisplay));
                this.RaisePropertyChanged(nameof(this.IsExistsAuthor));
                this.RaisePropertyChanged(nameof(this.DescriptionForDisplay));
            }
        }

        /// <summary>
        /// CC BY-SA ライセンス表記
        /// </summary>
        public const string CC_BY_SALicense = "CC BY-SA";

        /// <summary>
        /// Public Domain ライセンス表記
        /// </summary>
        public const string PublicDomainLicense = "Public Domain";

        private string license = null;

        [XmlElement(ElementName = "license")]
        public string License
        {
            get => this.license;
            set
            {
                if (this.SetProperty(ref this.license, value))
                {
                    this.RaisePropertyChanged(nameof(this.LicenseForDisplay));
                    this.RaisePropertyChanged(nameof(this.IsExistsLicense));
                    this.RaisePropertyChanged(nameof(this.DescriptionForDisplay));
                }
            }
        }

        [XmlIgnore]
        public bool IsEnabled
        {
            get => TimelineSettings.Instance.TimelineFiles
                .FirstOrDefault(x => x.Key == this.SourceFile)?
                .Value ?? true;
            set
            {
                var settings = TimelineSettings.Instance.TimelineFiles
                    .FirstOrDefault(x => x.Key == this.SourceFile);

                if (settings != null &&
                    settings.Value != value)
                {
                    settings.Value = value;
                    this.RaisePropertyChanged();
                    this.RaisePropertyChanged(nameof(IsDisabled));
                }
            }
        }

        [XmlIgnore]
        public bool IsDisabled
        {
            get => !this.IsEnabled;
            set => this.IsEnabled = !value;
        }

        [XmlIgnore]
        public bool IsExistsAuthor => !string.IsNullOrEmpty(this.Author);

        [XmlIgnore]
        public bool IsExistsLicense => !string.IsNullOrEmpty(this.License);

        [XmlIgnore]
        public string AuthorForDisplay => string.IsNullOrEmpty(this.Author) ?
            null :
            $"Author : {this.Author.Replace(Environment.NewLine, ", ")}";

        [XmlIgnore]
        public string LicenseForDisplay => string.IsNullOrEmpty(this.License) ?
            null :
            $"License: {this.License}";

        [XmlIgnore]
        public string DescriptionForDisplay
        {
            get
            {
                var result = this.Description;

#if false
                var b = new List<string>(); ;
                if (this.IsExistsAuthor)
                {
                    b.Add(this.AuthorForDisplay);
                }

                if (this.IsExistsLicense)
                {
                    b.Add(this.LicenseForDisplay);
                }

                if (b.Any())
                {
                    if (!string.IsNullOrEmpty(result))
                    {
                        result += $"\n\n{string.Join(Environment.NewLine, b)}";
                    }
                    else
                    {
                        result = string.Join(Environment.NewLine, b);
                    }
                }
#endif
                if (string.IsNullOrEmpty(result))
                {
                    result = "no description";
                }

                return result;
            }
        }

        public const string GlobalZone = "{GLOBAL}";
        public const string ReferenceZone = "{REFERENCE}";

        private string zone = string.Empty;

        [XmlElement(ElementName = "zone")]
        public string Zone
        {
            get => this.zone;
            set
            {
                if (this.SetProperty(ref this.zone, value))
                {
                    this.RaisePropertyChanged(nameof(this.DisplayName));
                    this.RaisePropertyChanged(nameof(this.IsGlobalZone));
                }
            }
        }

        [XmlIgnore]
        public bool IsGlobalZone => string.Equals(this.Zone, GlobalZone, StringComparison.OrdinalIgnoreCase);

        [XmlIgnore]
        public bool IsReference => string.Equals(this.Zone, ReferenceZone, StringComparison.OrdinalIgnoreCase);

        private Locales locale = Locales.JA;

        [XmlElement(ElementName = "locale")]
        public Locales Locale
        {
            get => this.locale;
            set => this.SetProperty(ref this.locale, value);
        }

        [XmlIgnore]
        public string LocaleText => this.Locale.ToText();

        private string entry = null;

        [XmlElement(ElementName = "entry")]
        public string Entry
        {
            get => this.entry;
            set => this.SetProperty(ref this.entry, value);
        }

        private string startTrigger = null;

        [XmlElement(ElementName = "start")]
        public string StartTrigger
        {
            get => this.startTrigger;
            set
            {
                if (this.SetProperty(ref this.startTrigger, value))
                {
                    if (string.IsNullOrEmpty(this.startTrigger))
                    {
                        this.StartTriggerRegex = null;
                    }
                    else
                    {
                        this.StartTriggerRegex = new Regex(
                            this.startTrigger,
                            RegexOptions.Compiled |
                            RegexOptions.ExplicitCapture);
                    }
                }
            }
        }

        private Regex startTriggerRegex = null;

        [XmlIgnore]
        public Regex StartTriggerRegex
        {
            get => this.startTriggerRegex;
            private set => this.SetProperty(ref this.startTriggerRegex, value);
        }

        private string endTrigger = null;

        [XmlElement(ElementName = "end")]
        public string EndTrigger
        {
            get => this.endTrigger;
            set
            {
                if (this.SetProperty(ref this.endTrigger, value))
                {
                    if (string.IsNullOrEmpty(this.endTrigger))
                    {
                        this.EndTriggerRegex = null;
                    }
                    else
                    {
                        this.EndTriggerRegex = new Regex(
                            this.endTrigger,
                            RegexOptions.Compiled |
                            RegexOptions.ExplicitCapture);
                    }
                }
            }
        }

        private Regex endTriggerRegex = null;

        [XmlIgnore]
        public Regex EndTriggerRegex
        {
            get => this.endTriggerRegex;
            private set => this.SetProperty(ref this.endTriggerRegex, value);
        }

        private string sourceFile = string.Empty;

        [XmlIgnore]
        public string SourceFile
        {
            get => this.sourceFile;
            private set
            {
                if (this.SetProperty(ref this.sourceFile, value))
                {
                    this.RaisePropertyChanged(nameof(this.SourceFileName));
                }
            }
        }

        private bool isActive = false;

        [XmlIgnore]
        public bool IsActive
        {
            get => this.isActive;
            set => this.SetProperty(ref this.isActive, value);
        }

        [XmlIgnore]
        public string SourceFileName => Path.GetFileName(this.SourceFile);

        private List<TimelineBase> elements = new List<TimelineBase>();

        [XmlIgnore]
        public IReadOnlyList<TimelineBase> Elements => this.elements;

        [XmlElement(ElementName = "default")]
        public TimelineDefaultModel[] Defaults
        {
            get => this.Elements.Where(x => x.TimelineType == TimelineElementTypes.Default).Cast<TimelineDefaultModel>().ToArray();
            set => this.AddRange(value);
        }

        [XmlElement(ElementName = "a")]
        public TimelineActivityModel[] Activities
        {
            get => this.Elements.Where(x => x.TimelineType == TimelineElementTypes.Activity).Cast<TimelineActivityModel>().OrderBy(x => x.Time).ToArray();
            set => this.AddRange(value);
        }

        /// <summary>
        /// Triggers
        /// </summary>
        /// <remarks>
        /// 必ずNoでソートされる</remarks>
        [XmlElement(ElementName = "t")]
        public TimelineTriggerModel[] Triggers
        {
            get => this.Elements
                .Where(x => x.TimelineType == TimelineElementTypes.Trigger)
                .Cast<TimelineTriggerModel>()
                .OrderBy(x => x.No.GetValueOrDefault())
                .ToArray();
            set => this.AddRange(value);
        }

        [XmlElement(ElementName = "s")]
        public TimelineSubroutineModel[] Subroutines
        {
            get => this.Elements.Where(x => x.TimelineType == TimelineElementTypes.Subroutine).Cast<TimelineSubroutineModel>().ToArray();
            set => this.AddRange(value);
        }

        /// <summary>
        /// Script
        /// </summary>
        [XmlElement(ElementName = "script")]
        public TimelineScriptModel[] Scripts
        {
            get => this.Elements
                .Where(x => x.TimelineType == TimelineElementTypes.Script)
                .Cast<TimelineScriptModel>()
                .ToArray();
            set => this.AddRange(value);
        }

        private TimelineController controller;

        /// <summary>
        /// タイムラインの実行を制御するオブジェクト
        /// </summary>
        [XmlIgnore]
        public TimelineController Controller =>
            this.controller = (this.controller ?? new TimelineController(this));

        /// <summary>
        /// Compile後のテキスト
        /// </summary>
        [XmlIgnore]
        public string CompiledText
        {
            get;
            private set;
        }

        private bool hasError;

        [XmlIgnore]
        public bool HasError
        {
            get => this.hasError;
            set
            {
                if (this.SetProperty(ref this.hasError, value))
                {
                    this.RaisePropertyChanged(nameof(this.HasNotError));
                }
            }
        }

        [XmlIgnore]
        public bool HasNotError => !this.hasError;

        private string errorText;

        [XmlIgnore]
        public string ErrorText
        {
            get => this.errorText;
            set => this.SetProperty(ref this.errorText, value);
        }

        #region Methods

        public bool ExistsActivities() =>
            this.Activities.Any(x => x.Enabled.GetValueOrDefault()) ||
            this.Subroutines.Any(x => x.Enabled.GetValueOrDefault());

        public void Add(TimelineBase timeline)
        {
            timeline.Parent = this;
            this.elements.Add(timeline);
        }

        public void AddRange(IEnumerable<TimelineBase> timelines)
        {
            if (timelines != null)
            {
                foreach (var tl in timelines)
                {
                    this.Add(tl);
                }
            }
        }

        public static string RazorDumpFile
            => Path.Combine(
                Path.GetTempPath(),
                "dump.xml");

        public static void ShowRazorDumpFile()
        {
            try
            {
                if (File.Exists(RazorDumpFile))
                {
                    Process.Start(RazorDumpFile);
                }
            }
            catch (Exception)
            {
            }
        }

        public static TimelineModel Load(
            string file)
        {
            const string IgnoreLoadKeyword = "#ignore";

            if (!File.Exists(file))
            {
                return null;
            }

            // ファイルサイズ0？
            var fi = new FileInfo(file);
            if (fi.Length <= 0)
            {
                return null;
            }

            // 読込無効キーワード #ignore がある？
            var text = File.ReadAllText(file, new UTF8Encoding(false));
            if (text.ContainsIgnoreCase(IgnoreLoadKeyword))
            {
                return null;
            }

            var tl = default(TimelineModel);
            var sb = default(StringBuilder);

            try
            {
                // Razorエンジンで読み込む
                sb = CompileRazor(file);

                if (File.Exists(RazorDumpFile))
                {
                    File.Delete(RazorDumpFile);
                }

                if (sb.Length > 0)
                {
                    using (var sr = new StringReader(sb.ToString()))
                    {
                        var xs = new XmlSerializer(typeof(TimelineModel));
                        var data = xs.Deserialize(sr) as TimelineModel;
                        if (data != null)
                        {
                            tl = data;
                            tl.SourceFile = file;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                tl = new TimelineModel();
                tl.SourceFile = file;
                tl.Name = tl.SourceFileName;
                tl.HasError = true;
                tl.Controller.Status = TimelineStatus.Error;

                var msg = new StringBuilder();
                msg.AppendLine($"Timeline load error.");
                msg.AppendLine($"{tl.SourceFileName}");
                msg.AppendLine();
                msg.AppendLine($"{ex.Message}");

                if (ex.InnerException != null)
                {
                    msg.AppendLine($"{ex.InnerException.Message}");
                }

                tl.ErrorText = msg.ToString();

                if (sb != null)
                {
                    File.WriteAllText(
                        RazorDumpFile,
                        sb.ToString(),
                        new UTF8Encoding(false));

                    tl.CompiledText = sb.ToString();
                }

                return tl;
            }

            if (tl != null)
            {
                // 既定値を適用する
                tl.SetDefaultValues();
            }

            // Compile後のテキストを保存する
            tl.CompiledText = tl != null ?
                sb.ToString() :
                string.Empty;

            return tl;
        }

        /// <summary>
        /// テキストを RazorEngine でCompile(パース)する
        /// </summary>
        /// <param name="source">
        /// 元のテキスト</param>
        /// <returns>
        /// パース後のテキスト</returns>
        private static StringBuilder CompileRazor(
            string file)
        {
            // Razorモデルに対象のファイルパスを設定する
            TimelineRazorModel.Instance.UpdateCurrentTimelineFile(file);

            var sb = new StringBuilder();

            using (var sw = new StringWriter(sb))
            using (var engine = CreateTimelineEngine())
            {
                engine.RunCompile(
                    file,
                    sw,
                    typeof(TimelineRazorModel),
                    TimelineRazorModel.Instance);
            }

            return sb;
        }

        /// <summary>
        /// タイムライン定義ファイル用の RazorEngine を生成する
        /// </summary>
        /// <returns>RazorEngine</returns>
        private static IRazorEngineService CreateTimelineEngine()
        {
            var config = new TemplateServiceConfiguration();

            config.Language = Language.CSharp;

            config.ReferenceResolver = new RazorReferenceResolver();

            config.Namespaces.Add("System.Runtime");
            config.Namespaces.Add("System.IO");
            config.Namespaces.Add("System.Linq");
            config.Namespaces.Add("System.Text");
            config.Namespaces.Add("System.Text.RegularExpressions");
            config.Namespaces.Add("FFXIV.Framework.Common");
            config.Namespaces.Add("FFXIV.Framework.Extensions");
            config.Namespaces.Add("ACT.SpecialSpellTimer.RazorModel");

            config.EncodedStringFactory = new RawStringFactory();

            config.TemplateManager = new DelegateTemplateManager((name) =>
            {
                if (File.Exists(name))
                {
                    return File.ReadAllText(name, new UTF8Encoding(false));
                }

                var path = Path.Combine(
                    TimelineRazorModel.Instance.TimelineDirectory,
                    name);

                if (File.Exists(path))
                {
                    return File.ReadAllText(path, new UTF8Encoding(false));
                }

                return name;
            });

            return RazorEngineService.Create(config);
        }

        /// <summary>
        /// Razorパーサに渡すモデルを更新する
        /// </summary>
        public static void RefreshRazorModel()
        {
            var model = TimelineRazorModel.Instance;

            TimelineRazorVariable.GetVarDelegate ??= () => TimelineExpressionsModel.GetVariables();
            TimelineRazorVariable.SetVarDelegate ??= (name, value, zone) => TimelineExpressionsModel.SetVariable(name, value, zone);
            TimelineTables.GetTableDelegate ??= (tableName) => TimelineExpressionsModel.GetTable(tableName);

            model.BaseDirectory = TimelineManager.Instance.TimelineDirectory;

            var et = EorzeaTime.Now;
            model.ET = $"{et.Hour:00}:{et.Minute:00}";
            model.ZoneID = XIVPluginHelper.Instance.GetCurrentZoneID();
            model.Zone = ActGlobals.oFormActMain.CurrentZone;
            model.Locale = Settings.Default.FFXIVLocale.ToString();

            var party = CombatantsManager.Instance.GetPartyList() as CombatantEx[];
            var player = CombatantsManager.Instance.Player;

            if (player == null)
            {
                model.Player = new TimelineRazorPlayer();
                model.Party = new[]
                {
                    new TimelineRazorPlayer(),
                    new TimelineRazorPlayer(),
                    new TimelineRazorPlayer(),
                    new TimelineRazorPlayer(),
                    new TimelineRazorPlayer(),
                    new TimelineRazorPlayer(),
                    new TimelineRazorPlayer(),
                    new TimelineRazorPlayer(),
                };

                return;
            }

            model.Player = new TimelineRazorPlayer()
            {
                Number = 0,
                Name = player.Name,
                Job = player.JobID.ToString(),
                Role = player.Role.ToString(),
            };

            var combatants = new List<TimelineRazorPlayer>();

            for (int i = 0; i < 8; i++)
            {
                var data = i < party.Length ?
                    party[i] :
                    null;

                var member = new TimelineRazorPlayer()
                {
                    Number = i + 1,
                    Name = data?.Name ?? string.Empty,
                    Job = data?.JobID.ToString() ?? string.Empty,
                    Role = data?.Role.ToString() ?? string.Empty,
                };

                combatants.Add(member);
            }

            model.Party = combatants.ToArray();
        }

        private static readonly Regex TimelineTagRegex = new Regex(
            "<timeline.*>",
            RegexOptions.Compiled);

        public void Save(
            string file)
        {
            lock (this)
            {
                FileHelper.CreateDirectory(file);

                var ns = new XmlSerializerNamespaces();
                ns.Add(string.Empty, string.Empty);

                var sb = new StringBuilder();
                using (var sw = new StringWriter(sb))
                {
                    var xs = new XmlSerializer(this.GetType());
                    xs.Serialize(sw, this, ns);
                }

                sb.Replace("utf-16", "utf-8");
                sb.Replace("True", "true");
                sb.Replace("False", "false");

                var text = sb.ToString();
                text = TimelineTagRegex.Replace(text, "<timeline>");

                File.WriteAllText(
                    file,
                    text + Environment.NewLine,
                    new UTF8Encoding(false));
            }
        }

        #endregion Methods

        #region To View

        private CollectionViewSource activitySource;

        private CollectionViewSource ActivitySource =>
            this.activitySource ?? (this.activitySource = this.CreateActivityView());

        public ICollectionView ActivityView => this.ActivitySource?.View;

        private string subName = string.Empty;

        [XmlIgnore]
        public string SubName
        {
            get => this.subName;
            set => this.SetProperty(ref this.subName, value);
        }

        public string DisplayName =>
            !string.IsNullOrEmpty(this.TimelineName) ?
            this.TimelineName :
            this.Zone;

        private bool isActivitiesVisible = true;

        [XmlIgnore]
        public bool IsActivitiesVisible
        {
            get => this.isActivitiesVisible;
            set => this.SetProperty(ref this.isActivitiesVisible, value);
        }

        public void StopLive()
        {
            this.IsActivitiesVisible = false;
        }

        public void ResumeLive()
        {
            if (!this.IsActivitiesVisible)
            {
                this.RefreshActivitiesView();
                this.IsActivitiesVisible = true;
            }
        }

        private CollectionViewSource CreateActivityView()
        {
            var cvs = new CollectionViewSource()
            {
                Source = this.Controller.ActivityLine,
                IsLiveSortingRequested = TimelineSettings.Instance.IsTimelineLiveUpdate,
                IsLiveFilteringRequested = TimelineSettings.Instance.IsTimelineLiveUpdate,
            };

            cvs.Filter += (x, y) =>
                y.Accepted = !(y.Item as TimelineActivityModel).IsDone;

            cvs.LiveFilteringProperties.Add(nameof(TimelineActivityModel.IsDone));

            cvs.SortDescriptions.AddRange(new[]
            {
                new SortDescription()
                {
                    PropertyName = nameof(TimelineActivityModel.Seq),
                    Direction = ListSortDirection.Ascending,
                }
            });

            return cvs;
        }

        public void RefreshActivitiesView()
        {
            if (!TimelineSettings.Instance.IsTimelineLiveUpdate)
            {
                WPFHelper.BeginInvoke(() =>
                {
                    this.ActivityView?.Refresh();
                },
                DispatcherPriority.Background);
            }
        }

        #endregion To View

        #region Commands

        private ICommand changeActiveCommand;

        public ICommand ChangeActiveCommand =>
            this.changeActiveCommand ?? (this.changeActiveCommand = new DelegateCommand<bool?>((isChecked) =>
            {
                if (this.HasError)
                {
                    return;
                }

                if (!isChecked.HasValue)
                {
                    return;
                }

                if (isChecked.Value)
                {
                    var old = TimelineManager.Instance.TimelineModels.FirstOrDefault(x =>
                        x != this &&
                        x.IsActive);

                    if (old != null)
                    {
                        old.IsActive = false;
                        old.Controller.Unload();
                    }

                    this.Controller.Load();
                }
                else
                {
                    this.Controller.Unload();
                }
            }));

        private ICommand showCommand;

        public ICommand ShowCommand =>
            this.showCommand ?? (this.showCommand = new DelegateCommand(() =>
            {
                if (string.IsNullOrEmpty(this.CompiledText))
                {
                    return;
                }

                var temp = Path.Combine(
                    Path.GetTempPath(),
                    Path.GetFileName(this.SourceFile
                        .Replace(".xml", ".compiled.xml")
                        .Replace(".cshtml", ".compiled.xml")));

                FileHelper.DeleteForce(temp);

                System.IO.File.WriteAllText(
                    temp,
                    this.CompiledText,
                    new UTF8Encoding(false));

                FileHelper.SetReadOnly(temp);

                Process.Start(temp);
            }));

        private ICommand editCommand;

        public ICommand EditCommand =>
            this.editCommand ?? (this.editCommand = new DelegateCommand(() =>
            {
                if (System.IO.File.Exists(this.SourceFile))
                {
                    Process.Start(this.SourceFile);
                }
            }));

        private ICommand reloadCommand;

        public ICommand ReloadCommand =>
            this.reloadCommand ?? (this.reloadCommand = new DelegateCommand(async () =>
            {
                if (!File.Exists(this.SourceFile))
                {
                    return;
                }

                try
                {
                    await this.ExecuteReloadCommandAsync();

                    if (!this.HasError)
                    {
                        ModernMessageBox.ShowDialog(
                            "Timeline reloading has completed.",
                            "Timeline Manager");
                    }
                    else
                    {
                        ModernMessageBox.ShowDialog(
                            "An error has occurred in reloading the timeline.",
                            "Timeline Manager");

                        this.ShowErrorDetailsCommand.Execute(null);
                    }
                }
                catch (Exception ex)
                {
                    TimelineModel.ShowRazorDumpFile();

                    ModernMessageBox.ShowDialog(
                        "An unexpected error has occurred in reloading the timeline.",
                        "Timeline Manager",
                        MessageBoxButton.OK,
                        ex);
                }
            }));

        private ICommand showErrorDetailsCommand;

        public ICommand ShowErrorDetailsCommand =>
            this.showErrorDetailsCommand ?? (this.showErrorDetailsCommand = new DelegateCommand(async () =>
            {
                this.ShowCommand.Execute(null);
                await Task.Delay(500);

                var temp = Path.Combine(
                    Path.GetTempPath(),
                    "error_details.txt");

                FileHelper.DeleteForce(temp);

                File.WriteAllText(
                    temp,
                    this.ErrorText,
                    new UTF8Encoding(false));

                FileHelper.SetReadOnly(temp);

                Process.Start(temp);
            }));

        public async Task ExecuteReloadCommandAsync()
        {
            await Task.Run(() => this.Reload());

            if (this.IsActive)
            {
                this.Controller.Unload();
                this.Controller.Load();
            }
        }

        public void Reload()
        {
            // Razorの引数モデルを更新する
            RefreshRazorModel();

            var tl = TimelineModel.Load(this.SourceFile);
            if (tl == null)
            {
                return;
            }

            this.elements.Clear();
            this.AddRange(tl.Elements);

            this.SourceFile = tl.SourceFile;
            this.TimelineName = tl.TimelineName;
            this.Revision = tl.Revision;
            this.Description = tl.Description;
            this.Zone = tl.Zone;
            this.Locale = tl.Locale;
            this.Entry = tl.Entry;
            this.EndTrigger = tl.EndTrigger;
            this.CompiledText = tl.CompiledText;
            this.HasError = tl.HasError;
            this.ErrorText = tl.ErrorText;

            if (this.IsGlobalZone)
            {
                TimelineManager.Instance.ReloadGlobalTriggers(this);
            }
        }

        #endregion Commands

        #region Dummy Timeline

        private static TimelineModel dummyTimeline;

        public static TimelineModel DummyTimeline =>
            dummyTimeline ?? (dummyTimeline = CreateDummyTimeline());

        public static TimelineModel CreateDummyTimeline(
            TimelineStyle testStyle = null)
        {
            var tl = new TimelineModel();

            tl.TimelineName = "サンプルタイムライン";
            tl.Zone = "Hojoring Zone v1.0 (Ultimate)";

            if (testStyle == null)
            {
                testStyle = TimelineStyle.SuperDefaultStyle;
                if (!WPFHelper.IsDesignMode)
                {
                    testStyle = TimelineSettings.Instance.DefaultStyle;
                }
            }

            var act1 = new TimelineActivityModel()
            {
                Enabled = true,
                Seq = 1,
                Text = "デスセンテンス",
                Time = TimeSpan.FromSeconds(10.1),
                Parent = new TimelineSubroutineModel()
                {
                    Name = "PHASE-1"
                },
                StyleModel = testStyle,
            };

            var act2 = new TimelineActivityModel()
            {
                Enabled = true,
                Seq = 2,
                Text = "ツイスター",
                Time = TimeSpan.FromSeconds(16.1),
                StyleModel = testStyle,
            };

            var act3 = new TimelineActivityModel()
            {
                Enabled = true,
                Seq = 3,
                Text = "メガフレア",
                Time = TimeSpan.FromSeconds(20.1),
                CallTarget = "PHASE-2",
                StyleModel = testStyle,
            };

            tl.Controller.ActivityLine.Add(act1);
            tl.Controller.ActivityLine.Add(act2);
            tl.Controller.ActivityLine.Add(act3);

            for (int i = 1; i <= 13; i++)
            {
                var a = new TimelineActivityModel()
                {
                    Enabled = true,
                    Seq = act3.Seq + i,
                    Text = "アクション" + i,
                    Time = TimeSpan.FromSeconds(30 + i),
                    StyleModel = testStyle,
                };

                tl.Controller.ActivityLine.Add(a);
            }

            return tl;
        }

        #endregion Dummy Timeline
    }
}
