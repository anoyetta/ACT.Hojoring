using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Advanced_Combat_Tracker;
using FFXIV.Framework.Bridge;
using FFXIV.Framework.Common;
using FFXIV.Framework.Extensions;
using FFXIV.Framework.WPF.Views;
using FFXIV.Framework.XIVHelper;
using Microsoft.VisualBasic.FileIO;
using NLog;
using NLog.Targets;
using Prism.Commands;
using Prism.Mvvm;

namespace FFXIV.Framework.WPF.ViewModels
{
    public class HelpViewModel :
        BindableBase
    {
        public HelpViewModel()
        {
            AppLog.AppendedLog += (x, y) => this.RaisePropertyChanged(nameof(this.Log));
            this.timer.Tick += this.Timer_Tick;
        }

        private HelpView view;

        public HelpView View
        {
            get => this.view;
            set
            {
                var old = this.view;

                if (this.SetProperty(ref this.view, value))
                {
                    if (old != null)
                    {
                        old.Loaded -= this.View_Loaded;
                        old.Loaded -= this.View_Unloaded;
                    }

                    this.view.Loaded += this.View_Loaded;
                    this.view.Unloaded += this.View_Unloaded;
                }
            }
        }

        private void View_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            this.timer.Start();
            this.UpdateVersionInfo();
        }

        private void View_Unloaded(
            object sender,
            RoutedEventArgs e)
        {
            this.timer.Stop();
        }

        #region VersionInfo

        private string versionInfo;

        public string VersionInfo
        {
            get => this.versionInfo;
            set => this.SetProperty(ref this.versionInfo, value);
        }

        private void UpdateVersionInfo()
        {
            var text = new StringBuilder();

            var ffxivPlugin = ActGlobals.oFormActMain?.ActPlugins?
                .FirstOrDefault(x =>
                    x.pluginFile.Name.ContainsIgnoreCase("FFXIV_ACT_Plugin") &&
                    x.lblPluginStatus.Text.ContainsIgnoreCase("Started"))?
                .pluginFile.FullName;

            if (File.Exists(ffxivPlugin))
            {
                var vi = FileVersionInfo.GetVersionInfo(ffxivPlugin);
                if (vi != null)
                {
                    text.AppendLine($"FFXIV_ACT_Plugin v{vi.FileMajorPart}.{vi.FileMinorPart}.{vi.FileBuildPart}.{vi.FilePrivatePart}");
                }
            }

            var hojorin = DirectoryHelper.FindFile("ACT.Hojoring.Common.dll");
            var spespe = DirectoryHelper.FindFile("ACT.SpecialSpellTimer.dll");
            var ultra = DirectoryHelper.FindFile("ACT.UltraScouter.dll");
            var yukkuri = DirectoryHelper.FindFile("ACT.TTSYukkuri.dll");

            if (File.Exists(hojorin))
            {
                var vi = FileVersionInfo.GetVersionInfo(hojorin);
                if (vi != null)
                {
                    text.AppendLine($"ACT.Hojoring v{vi.FileMajorPart}.{vi.FileMinorPart}.{vi.FilePrivatePart}");
                }
            }

            if (File.Exists(spespe))
            {
                var vi = FileVersionInfo.GetVersionInfo(spespe);
                if (vi != null)
                {
                    text.AppendLine($"ACT.SpecialSpellTimer v{vi.FileMajorPart}.{vi.FileMinorPart}.{vi.FilePrivatePart}");
                }
            }

            if (File.Exists(ultra))
            {
                var vi = FileVersionInfo.GetVersionInfo(ultra);
                if (vi != null)
                {
                    text.AppendLine($"ACT.UltraScouter v{vi.FileMajorPart}.{vi.FileMinorPart}.{vi.FilePrivatePart}");
                }
            }

            if (File.Exists(yukkuri))
            {
                var vi = FileVersionInfo.GetVersionInfo(yukkuri);
                if (vi != null)
                {
                    text.AppendLine($"ACT.TTSYukkuri v{vi.FileMajorPart}.{vi.FileMinorPart}.{vi.FilePrivatePart}");
                }
            }

            this.VersionInfo = text.ToString().TrimEnd('\n', '\r');
        }

        #endregion VersionInfo

        #region Time

        private string utc = string.Empty;

        public string UTC
        {
            get => this.utc;
            set => this.SetProperty(ref this.utc, value);
        }

        private string localTime = string.Empty;

        public string LocalTime
        {
            get => this.localTime;
            set => this.SetProperty(ref this.localTime, value);
        }

        private string eorzeaTime = string.Empty;

        public string EorzeaTime
        {
            get => this.eorzeaTime;
            set => this.SetProperty(ref this.eorzeaTime, value);
        }

        private string zone = string.Empty;

        public string Zone
        {
            get => this.zone;
            set => this.SetProperty(ref this.zone, value);
        }

        private DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.ContextIdle)
        {
            Interval = TimeSpan.FromSeconds(0.25),
        };

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (this.View?.IsLoaded ?? false)
            {
                var now = DateTimeOffset.Now;

                this.UTC = $"(UTC) {now.ToUniversalTime():yyyy/MM/dd HH:mm:ss}";
                this.LocalTime = $" (LT) {now:yyyy/MM/dd HH:mm:ss K}";
                this.EorzeaTime = $" (ET) {now.ToEorzeaTime()}";

                var zoneID = XIVPluginHelper.Instance?.GetCurrentZoneID();
                var zoneName = ActGlobals.oFormActMain?.CurrentZone;
                this.Zone = $"ZONE: {zoneID}\n{zoneName}";
            }
        }

        #endregion Time

        private Action reloadConfigAction;

        public Action ReloadConfigAction
        {
            get => this.reloadConfigAction;
            set
            {
                if (this.SetProperty(ref this.reloadConfigAction, value))
                {
                    this.RaisePropertyChanged(nameof(this.AvailableReloadConfigAction));
                }
            }
        }

        public bool AvailableReloadConfigAction => this.ReloadConfigAction != null;

        public string Log => AppLog.Log.ToString();

        #region Wiki

        private ICommand wikiCommand;

        public ICommand WikiCommand =>
            this.wikiCommand ?? (this.wikiCommand = new DelegateCommand(() =>
                Process.Start(new ProcessStartInfo("https://github.com/anoyetta/ACT.Hojoring/wiki"))));

        #endregion Wiki

        #region LastestRelease

        private ICommand lastestReleaseCommand;

        public ICommand LastestReleaseCommand =>
            this.lastestReleaseCommand ?? (this.lastestReleaseCommand = new DelegateCommand(() =>
                Process.Start(new ProcessStartInfo("https://github.com/anoyetta/ACT.Hojoring/releases"))));

        #endregion LastestRelease

        #region Issues

        private ICommand issuesCommand;

        public ICommand IssuesCommand =>
            this.issuesCommand ?? (this.issuesCommand = new DelegateCommand(() =>
                Process.Start(new ProcessStartInfo("https://github.com/anoyetta/ACT.Hojoring/issues"))));

        #endregion Issues

        #region Update

        private bool usePreRelease = false;

        public bool UsePreRelease
        {
            get => this.usePreRelease;
            set => this.SetProperty(ref this.usePreRelease, value);
        }

        private ICommand updateCommand;

        public ICommand UpdateCommand =>
            this.updateCommand ?? (this.updateCommand = new DelegateCommand(() =>
            {
                UpdateChecker.StartUpdateScript(this.usePreRelease);
            }));

        #endregion Update

        #region OpenConfig

        private ICommand openConfigCommand;

        public ICommand OpenConfigCommand =>
            this.openConfigCommand ?? (this.openConfigCommand = new DelegateCommand(() =>
            {
                var folder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    @"anoyetta\ACT");

                if (Directory.Exists(folder))
                {
                    Process.Start(folder);
                }
            }));

        #endregion OpenConfig

        #region ReloadConfig

        private ICommand reloadConfigCommand;

        public ICommand ReloadConfigCommand =>
            this.reloadConfigCommand ?? (this.reloadConfigCommand = new DelegateCommand(async () =>
            {
                await Task.Run(() =>
                {
                    if (this.ReloadConfigAction != null)
                    {
                        this.ReloadConfigAction.Invoke();
                        MessageBox.Show(
                            "Configuration Reloaded.",
                            "ACT.Hojoring",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                });
            }));

        #endregion ReloadConfig

        #region OpenPluginLog

        private ICommand openPluginLogCommand;

        public ICommand OpenPluginLogCommand =>
            this.openPluginLogCommand ?? (this.openPluginLogCommand = new DelegateCommand(() =>
            {
                var fts = LogManager.Configuration.AllTargets.Where(x => x is FileTarget);
                foreach (FileTarget ft in fts)
                {
                    var file = Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        ft.FileName.Render(
                            new LogEventInfo
                            {
                                Level = LogLevel.Info
                            }));

                    if (File.Exists(file))
                    {
                        Process.Start(file);
                    }
                }
            }));

        #endregion OpenPluginLog

        #region OpenACTLog

        private ICommand openACTLogCommand;

        public ICommand OpenACTLogCommand =>
            this.openACTLogCommand ?? (this.openACTLogCommand = new DelegateCommand(() =>
            {
                var file = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    @"Advanced Combat Tracker\Advanced Combat Tracker.log");
                if (File.Exists(file))
                {
                    Process.Start(file);
                }
            }));

        #endregion OpenACTLog

        #region ConstactDiscord

        private ICommand contactDiscordCommand;

        public ICommand ContactDiscordCommand =>
            this.contactDiscordCommand ?? (this.contactDiscordCommand = new DelegateCommand(() =>
                Process.Start(new ProcessStartInfo("https://discord.gg/n6Mut3F"))));

        #endregion ConstactDiscord

        #region ConstactTwitter

        private ICommand contactTwitterCommand;

        public ICommand ContactTwitterCommand =>
            this.contactTwitterCommand ?? (this.contactTwitterCommand = new DelegateCommand(() =>
                Process.Start(new ProcessStartInfo("https://twitter.com/anoyetta"))));

        #endregion ConstactTwitter

        #region SaveSupportInfo

        private ICommand saveSupportInfoCommand;

        public ICommand SaveSupportInfoCommand =>
            this.saveSupportInfoCommand ?? (this.saveSupportInfoCommand = new DelegateCommand(() =>
                this.SaveSupportInfo()));

        #endregion SaveSupportInfo

        #region Show Debug Window

        private ICommand showDebugWindowCommand;

        public ICommand ShowDebugWindowCommand =>
            this.showDebugWindowCommand ?? (this.showDebugWindowCommand = new DelegateCommand(() =>
            {
                var view = new SandboxView();
                view.Show();
            }));

        #endregion Show Debug Window

        private bool wait;

        public bool Wait
        {
            get => this.wait;
            set => this.SetProperty(ref this.wait, value);
        }

        private System.Windows.Forms.SaveFileDialog saveFileDialog = new System.Windows.Forms.SaveFileDialog()
        {
            RestoreDirectory = true,
            FileName = $"ACT.Hojoring.HelpMe.{DateTime.Now:yyyy-MM-dd}.zip",
            DefaultExt = "zip",
            Filter = "Zip archives (*.zip)|*.txt|All files (*.*)|*.*",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            SupportMultiDottedExtensions = true,
        };

        private async void SaveSupportInfo()
        {
            try
            {
                this.Wait = true;

                var result = this.saveFileDialog.ShowDialog();
                if (result != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }

                await Task.Run(() =>
                    this.SaveSupportInfoCore(this.saveFileDialog.FileName));

                ModernMessageBox.ShowDialog(
                    "SupportInfo Saved.",
                    "ACT.Hojoring");
            }
            catch (Exception ex)
            {
                ModernMessageBox.ShowDialog(
                    "Fatal Error.",
                    "ACT.Hojoring",
                    MessageBoxButton.OK,
                    ex);
            }
            finally
            {
                this.Wait = false;
            }
        }

        private void SaveSupportInfoCore(
            string file)
        {
            // 一時フォルダを生成する
            var temp = Path.GetTempFileName();
            File.Delete(temp);
            Directory.CreateDirectory(temp);

            // ACT用Directoryを作る
            var actDest = Path.Combine(temp, "Advanced Combat Tracker");
            Directory.CreateDirectory(actDest);

            // ACTの情報を集める
            var actSrc = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Advanced Combat Tracker");

            File.Copy(
                Path.Combine(actSrc, "Advanced Combat Tracker.log"),
                Path.Combine(actDest, "Advanced Combat Tracker.log"),
                true);

            FileSystem.CopyDirectory(
                Path.Combine(actSrc, "Config"),
                Path.Combine(actDest, "Config"),
                true);

            // anoyetta用Directoryを作る
            var anyDest = Path.Combine(temp, "anoyetta");
            Directory.CreateDirectory(anyDest);

            // ACTの情報を集める
            var anySrc = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "anoyetta");

            FileSystem.CopyDirectory(
                anySrc,
                anyDest,
                true);

            // Hojoringのツリーを保存する
            var pluginDirectory = Path.Combine(temp, "ACT.Hojoring");
            if (!Directory.Exists(pluginDirectory))
            {
                Directory.CreateDirectory(pluginDirectory);
            }

            using (var p = new Process())
            {
                var here = Path.Combine(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    "..");
                p.StartInfo.FileName = "powershell.exe";
                p.StartInfo.Arguments =
                    $@"-nologo -command ""Get-ChildItem '{here}' -Recurse | ?{{$_.FullName -notmatch 'backup'}} | Format-Table -AutoSize | Out-File -Encoding utf8 '{temp}\file_list_Hojoring.txt'";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.Start();
                p.WaitForExit();
            }

            // ACT本体のツリーを保存する
            using (var p = new Process())
            {
                var here = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                p.StartInfo.FileName = "powershell.exe";
                p.StartInfo.Arguments = $@"-nologo -command ""Get-ChildItem '{here}' -Recurse | Out-File -Encoding utf8 '{temp}\file_list_ACT.txt'";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.Start();
                p.WaitForExit();
            }

            // APPDATAのツリーを保存する
            using (var p = new Process())
            {
                p.StartInfo.FileName = "powershell.exe";
                p.StartInfo.Arguments = $@"-nologo -command ""Get-ChildItem '{anySrc}' -Recurse | Out-File -Encoding utf8 '{temp}\file_list_APPDATA.txt'";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.Start();
                p.WaitForExit();
            }

            // 追加バックアップを行う
            HelpBridge.Instance.BackupCallback?.Invoke(temp);

            if (File.Exists(file))
            {
                File.Delete(file);
            }

            ZipFile.CreateFromDirectory(
                temp,
                file,
                CompressionLevel.Optimal,
                false);

            Directory.Delete(temp, true);
        }
    }
}
