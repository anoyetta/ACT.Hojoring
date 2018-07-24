using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using ACT.SpecialSpellTimer.RaidTimeline;
using ACT.SpecialSpellTimer.resources;
using Advanced_Combat_Tracker;
using FFXIV.Framework.Common;
using FFXIV.Framework.Extensions;
using FFXIV.Framework.Globalization;
using FFXIV.Framework.WPF.Views;
using Prism.Commands;

namespace ACT.SpecialSpellTimer.Config.Views
{
    /// <summary>
    /// TimelineAnalyzerView.xaml の相互作用ロジック
    /// </summary>
    public partial class TimelineAnalyzerView :
        UserControl,
        ILocalizable,
        INotifyPropertyChanged
    {
        public TimelineAnalyzerView()
        {
            this.InitializeComponent();
            this.SetLocale(Settings.Default.UILocale);
            this.LoadConfigViewResources();

            this.CombatLogs.CollectionChanged += (x, y) =>
            {
                this.RaisePropertyChanged(nameof(this.Zone));
            };

            this.CombatLogDataGrid.CopyingRowClipboardContent += (x, y) =>
            {
                var grid = x as DataGrid;
                var currentCell = y.ClipboardRowContent[grid.CurrentCell.Column.DisplayIndex];
                y.ClipboardRowContent.Clear();
                y.ClipboardRowContent.Add(currentCell);
            };

            this.combatLogSource.LiveFilteringProperties.Add(nameof(CombatLog.LogType));
            this.combatLogSource.Filter += (x, y) =>
            {
                y.Accepted = false;
                if (y.Item is CombatLog log)
                {
                    y.Accepted = log.LogType != LogTypes.Unknown;
                }
            };
        }

        private CollectionViewSource combatLogSource = new CollectionViewSource()
        {
            Source = CombatAnalyzer.Instance.CurrentCombatLogList,
            IsLiveFilteringRequested = true,
            IsLiveSortingRequested = true,
            IsLiveGroupingRequested = true,
        };

        public Settings RootConfig => Settings.Default;

        public ICollectionView CombatLogs => this.combatLogSource.View;

        public string Zone => this.CombatLogs?.Cast<CombatLog>().FirstOrDefault()?.Zone;

        private void AutoCombatLogAnalyze_Checked(
            object sender,
            RoutedEventArgs e)
        {
            CombatAnalyzer.Instance.Start();
        }

        private void AutoCombatLogAnalyzex_Unchecked(
            object sender,
            RoutedEventArgs e)
        {
            CombatAnalyzer.Instance.End();
        }

        private System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog()
        {
            Description = "分析結果の保存先を選択してください。",
            ShowNewFolderButton = true,
        };

        private ICommand browseLogDirectoryCommand;

        public ICommand BrowseLogDirectoryCommand =>
            this.browseLogDirectoryCommand ?? (this.browseLogDirectoryCommand = new DelegateCommand(() =>
            {
                this.dialog.SelectedPath = this.RootConfig.CombatLogSaveDirectory;
                if (this.dialog.ShowDialog(ActGlobals.oFormActMain) ==
                    System.Windows.Forms.DialogResult.OK)
                {
                    this.RootConfig.CombatLogSaveDirectory = this.dialog.SelectedPath;
                }
            }));

        private ICommand openLogCommand;

        public ICommand OpenLogCommand =>
            this.openLogCommand ?? (this.openLogCommand = new DelegateCommand(() =>
            {
                var dir = this.RootConfig.CombatLogSaveDirectory;
                if (Directory.Exists(dir))
                {
                    Process.Start(dir);
                }
            }));

        private System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog()
        {
            RestoreDirectory = true,
            Filter = "CombatLog Files|*.log|All Files|*.*",
            FilterIndex = 0,
            DefaultExt = ".log",
            SupportMultiDottedExtensions = true,
        };

        private const string SpreadFilter = "Spreadsheet Files|*.xlsx|All Files|*.*";
        private const string SpreadExt = ".xlsx";
        private const string TimelineFilter = "Timeline Files|*.xml|All Files|*.*";
        private const string TimelineExt = ".xml";
        private const string LogFilter = "Log Files|*.log|All Files|*.*";
        private const string LogExt = ".log";

        private System.Windows.Forms.SaveFileDialog saveFileDialog = new System.Windows.Forms.SaveFileDialog()
        {
            RestoreDirectory = true,
            FilterIndex = 0,
            SupportMultiDottedExtensions = true,
        };

        private ICommand setOriginCommand;

        public ICommand SetOriginCommand =>
            this.setOriginCommand ?? (this.setOriginCommand = new DelegateCommand(async () =>
            {
                var originLog = this.CombatLogDataGrid.SelectedItem as CombatLog;

                if (originLog == null)
                {
                    return;
                }

                await WPFHelper.BeginInvoke(() =>
                    CombatAnalyzer.Instance.SetOrigin(
                        this.CombatLogs.Cast<CombatLog>(),
                        originLog));
            }));

        private ICommand changeSecondsFormatCommand;

        public ICommand ChangeSecondsFormatCommand =>
            this.changeSecondsFormatCommand ?? (this.changeSecondsFormatCommand = new DelegateCommand(async () =>
            {
                if (this.CombatLogs == null)
                {
                    return;
                }

                await WPFHelper.BeginInvoke(() =>
                    this.CombatLogs.Refresh());
            }));

        private ICommand copyCommand;

        public ICommand CopyCommand =>
            this.copyCommand ?? (this.copyCommand = new DelegateCommand(() =>
            {
                var currentCell = this.CombatLogDataGrid.CurrentCell;
                var log = currentCell.Item as CombatLog;

                if (currentCell != null &&
                    log != null)
                {
                    var text = string.Empty;
                    switch (currentCell.Column.Header)
                    {
                        case "No":
                            text = log.No.ToString();
                            break;

                        case "Time":
                            text = log.TimeStampElapted.ToString();
                            break;

                        case "Activity Type":
                            text = log.LogTypeName.ToString();
                            break;

                        case "Actor":
                            text = log.Actor.ToString();
                            break;

                        case "HP":
                            text = log.HPRate.ToString();
                            break;

                        case "Activity":
                            text = log.Activity.ToString();
                            break;

                        case "Log":
                            text = log.RawWithoutTimestamp.ToString();
                            break;
                    }

                    Clipboard.SetText(text);
                }
            }));

        private ICommand importCombatLogCommand;

        public ICommand ImportCombatLogCommand =>
            this.importCombatLogCommand ?? (this.importCombatLogCommand = new DelegateCommand(async () =>
            {
                if (string.IsNullOrEmpty(this.openFileDialog.InitialDirectory))
                {
                    this.openFileDialog.InitialDirectory = Settings.Default.CombatLogSaveDirectory;
                    this.openFileDialog.FileName = string.Empty;
                }

                if (this.openFileDialog.ShowDialog(ActGlobals.oFormActMain)
                    != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }

                var file = this.openFileDialog.FileName;

                // NETWORK.log？
                if (file.ContainsIgnoreCase("NETWORK"))
                {
                    ModernMessageBox.ShowDialog(
                        $"Invalid Log Format.\n\"NETWORK.log\" can not be analyzed.",
                        "Timeline Analyzer",
                        MessageBoxButton.OK);

                    return;
                }

                try
                {
                    await Task.Run(() =>
                    {
                        var lines = File.ReadAllLines(file, new UTF8Encoding(false));
                        CombatAnalyzer.Instance.ImportLogLines(lines.ToList());
                    });

                    ModernMessageBox.ShowDialog(
                        $"CombatLog Imported",
                        "Timeline Analyzer");
                }
                catch (Exception ex)
                {
                    ModernMessageBox.ShowDialog(
                        $"Import CombatLog Error.",
                        "Timeline Analyzer",
                        MessageBoxButton.OK,
                        ex);
                }
            }));

        private ICommand saveToSpreadsheetCommand;

        public ICommand SaveToSpreadsheetCommand =>
            this.saveToSpreadsheetCommand ?? (this.saveToSpreadsheetCommand = new DelegateCommand(async () =>
            {
                var logs = this.CombatLogs.Cast<CombatLog>()?.ToList();
                if (logs == null ||
                    !logs.Any())
                {
                    return;
                }

                var timestamp = logs.Last().TimeStamp;
                var zone = logs.First().Zone;
                zone = zone.Replace(" ", "_");
                foreach (var c in Path.GetInvalidFileNameChars())
                {
                    zone = zone.Replace(c, '_');
                }

                this.saveFileDialog.Filter = SpreadFilter;
                this.saveFileDialog.DefaultExt = SpreadExt;
                this.saveFileDialog.FileName =
                    $"{timestamp.ToString("yyyy-MM-dd")}.{zone}.xlsx";

                if (this.saveFileDialog.ShowDialog(ActGlobals.oFormActMain)
                    == System.Windows.Forms.DialogResult.OK)
                {
                    var file = this.saveFileDialog.FileName;
                    this.saveFileDialog.FileName = Path.GetFileName(file);

                    try
                    {
                        await Task.Run(() => CombatAnalyzer.Instance.SaveToSpreadsheet(file, logs));

                        ModernMessageBox.ShowDialog(
                            $"CombatLog Saved.\n\n\"{Path.GetFileName(file)}\"",
                            "Timeline Analyzer");
                    }
                    catch (Exception ex)
                    {
                        ModernMessageBox.ShowDialog(
                            $"Save CombatLog Error.",
                            "Timeline Analyzer",
                            MessageBoxButton.OK,
                            ex);
                    }
                }
            }));

        private ICommand saveDraftTimelineCommand;

        public ICommand SaveDraftTimelineCommand =>
            this.saveDraftTimelineCommand ?? (this.saveDraftTimelineCommand = new DelegateCommand(async () =>
            {
                var logs = this.CombatLogs.Cast<CombatLog>()?.ToList();
                if (logs == null ||
                    !logs.Any())
                {
                    return;
                }

                var zone = logs.First().Zone;
                zone = zone.Replace(" ", "_");

                this.saveFileDialog.Filter = TimelineFilter;
                this.saveFileDialog.DefaultExt = TimelineExt;
                this.saveFileDialog.FileName =
                    $"{zone}.xml";

                if (this.saveFileDialog.ShowDialog(ActGlobals.oFormActMain)
                    == System.Windows.Forms.DialogResult.OK)
                {
                    var file = this.saveFileDialog.FileName;
                    this.saveFileDialog.FileName = Path.GetFileName(file);

                    try
                    {
                        await Task.Run(() => CombatAnalyzer.Instance.SaveToDraftTimeline(file, logs));

                        ModernMessageBox.ShowDialog(
                            $"Draft Timeline Saved.\n\n\"{Path.GetFileName(file)}\"",
                            "Timeline Analyzer");
                    }
                    catch (Exception ex)
                    {
                        ModernMessageBox.ShowDialog(
                            $"Save Timeline Error.",
                            "Timeline Analyzer",
                            MessageBoxButton.OK,
                            ex);
                    }
                }
            }));

        private ICommand saveTestLogCommand;

        public ICommand SaveTestLogCommand =>
            this.saveTestLogCommand ?? (this.saveTestLogCommand = new DelegateCommand(async () =>
            {
                var logs = this.CombatLogs.Cast<CombatLog>()?.ToList();
                if (logs == null ||
                    !logs.Any())
                {
                    return;
                }

                var zone = logs.First().Zone;
                zone = zone.Replace(" ", "_");

                this.saveFileDialog.Filter = LogFilter;
                this.saveFileDialog.DefaultExt = LogExt;
                this.saveFileDialog.FileName =
                    $"{zone}.test.log";

                if (this.saveFileDialog.ShowDialog(ActGlobals.oFormActMain)
                    == System.Windows.Forms.DialogResult.OK)
                {
                    var file = this.saveFileDialog.FileName;
                    this.saveFileDialog.FileName = Path.GetFileName(file);

                    try
                    {
                        await Task.Run(() => CombatAnalyzer.Instance.SaveToTestLog(file, logs));

                        ModernMessageBox.ShowDialog(
                            $"Test Log Saved.\n\n\"{Path.GetFileName(file)}\"",
                            "Timeline Analyzer");
                    }
                    catch (Exception ex)
                    {
                        ModernMessageBox.ShowDialog(
                            $"Save Test Log Error.",
                            "Timeline Analyzer",
                            MessageBoxButton.OK,
                            ex);
                    }
                }
            }));

        #region TextBox Utility

        private void TextBoxSelect(
            object sender,
            MouseButtonEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                if (!textBox.IsKeyboardFocusWithin)
                {
                    textBox.Focus();
                    e.Handled = true;
                }
            }
        }

        private void TextBoxOnGotFocus(
            object sender,
            RoutedEventArgs e)
        {
            (sender as TextBox)?.SelectAll();
        }

        #endregion TextBox Utility

        #region ILocalizebale

        public void SetLocale(Locales locale) => this.ReloadLocaleDictionary(locale);

        #endregion ILocalizebale

        #region INotifyPropertyChanged

        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(
            [CallerMemberName]string propertyName = null)
        {
            this.PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }

        protected virtual bool SetProperty<T>(
            ref T field,
            T value,
            [CallerMemberName]string propertyName = null)
        {
            if (Equals(field, value))
            {
                return false;
            }

            field = value;
            this.PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));

            return true;
        }

        #endregion INotifyPropertyChanged
    }
}
