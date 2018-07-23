using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using ACT.SpecialSpellTimer.Models;
using ACT.SpecialSpellTimer.resources;
using Advanced_Combat_Tracker;
using FFXIV.Framework.Extensions;
using FFXIV.Framework.FFXIVHelper;
using FFXIV.Framework.Globalization;
using FFXIV.Framework.WPF.Views;
using Prism.Mvvm;
using TamanegiMage.FFXIV_MemoryReader.Model;

namespace ACT.SpecialSpellTimer.Config.Views
{
    /// <summary>
    /// TriggerTesterView.xaml の相互作用ロジック
    /// </summary>
    public partial class TriggerTesterView :
        Window,
        ILocalizable,
        INotifyPropertyChanged
    {
        public TriggerTesterView()
        {
            this.InitializeComponent();
            this.SetLocale(Settings.Default.UILocale);
            this.LoadConfigViewResources();

            // ウィンドウのスタート位置を決める
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            this.MouseLeftButtonDown += (x, y) => this.DragMove();

            this.CloseButton.Click += (x, y) =>
            {
                this.testTimer.Stop();
                this.Close();
            };

            this.Closed += (x, y) =>
            {
                PluginMainWorker.Instance.InSimulation = false;
                this.ClearTestCondition();
            };

            this.testTimer.Tick += this.TestTimer_Tick;

            this.RunButton.Click += async (x, y) =>
            {
                // インスタンススペルを消去する
                SpellTable.Instance.RemoveInstanceSpellsAll();

                await Task.Run(() =>
                {
                    PluginMainWorker.Instance.InSimulation = true;

                    lock (this)
                    {
                        this.testTimer.Stop();
                        this.isPause = false;

                        foreach (var log in this.Logs)
                        {
                            log.IsDone = false;
                        }

                        this.prevTestTimestamp = DateTime.Now;
                        this.TestTime = TimeSpan.Zero;
                        this.testTimer.Start();
                    }
                });
            };

            this.PauseButton.Click += (x, y) =>
            {
                this.isPause = !this.isPause;
                this.PauseButton.Content =
                    this.isPause ? "Resume" : "Pause";
            };

            this.StopButton.Click += async (x, y) =>
            {
                await Task.Run(() =>
                {
                    lock (this)
                    {
                        this.testTimer.Stop();

                        foreach (var log in this.Logs)
                        {
                            log.IsDone = false;
                        }

                        this.prevTestTimestamp = DateTime.MinValue;
                    }

                    PluginMainWorker.Instance.InSimulation = false;
                });

                // インスタンススペルを消去する
                SpellTable.Instance.RemoveInstanceSpellsAll();
            };

            this.OpenButton.Click += (x, y) =>
            {
                var result = this.openFileDialog.ShowDialog(ActGlobals.oFormActMain);
                if (result != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }

                this.LogFile = this.openFileDialog.FileName;
                this.LoadLog();
            };

            this.ApplyButton.Click += async (x, y) =>
            {
                await Task.Run(() =>
                {
                    // インスタンススペルを消去する
                    SpellTable.Instance.RemoveInstanceSpellsAll();

                    // テスト条件を適用する
                    this.ApplyTestCondition();
                });

                ModernMessageBox.ShowDialog(
                    "Test Condition was applied.",
                    "Trigger Simulator");
            };

            this.ClearButton.Click += async (x, y) =>
            {
                await Task.Run(() =>
                {
                    // インスタンススペルを消去する
                    SpellTable.Instance.RemoveInstanceSpellsAll();

                    // テスト条件を解除する
                    this.ClearTestCondition();
                });

                ModernMessageBox.ShowDialog(
                    "Test Condition was cleard.",
                    "Trigger Simulator");
            };
        }

        private System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog()
        {
            RestoreDirectory = true,
            Filter = "CombatLog Files|*.log|All Files|*.*",
            FilterIndex = 0,
            DefaultExt = ".log",
            SupportMultiDottedExtensions = true,
        };

        public string LogFile
        {
            get;
            private set;
        }

        public ObservableCollection<TestLog> Logs
        {
            get;
            private set;
        } = new ObservableCollection<TestLog>();

        private DateTime prevTestTimestamp;
        private TimeSpan testTime;

        private TimeSpan TestTime
        {
            get => this.testTime;
            set
            {
                if (this.SetProperty(ref this.testTime, value))
                {
                    this.RaisePropertyChanged(nameof(this.TestTimeText));
                }
            }
        }

        public string TestTimeText => this.TestTime.ToTLString();

        private DispatcherTimer testTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(0.1),
        };

        private async void LoadLog()
        {
            if (!File.Exists(this.LogFile))
            {
                return;
            }

            var list = new List<TestLog>();

            await Task.Run(() =>
            {
                var ignores = LogBuffer.IgnoreLogKeywords;

                var seq = 1L;
                using (var sr = new StreamReader(this.LogFile, new UTF8Encoding(false)))
                {
                    while (!sr.EndOfStream)
                    {
                        var logline = sr.ReadLine();

                        if (string.IsNullOrEmpty(logline) ||
                            logline.StartsWith("#") ||
                            logline.StartsWith("//"))
                        {
                            continue;
                        }

                        if (ignores.Any(x => logline.Contains(x)))
                        {
                            continue;
                        }

                        var log = new TestLog(logline)
                        {
                            Seq = seq++
                        };

                        list.Add(log);
                    }
                }

                if (!list.Any())
                {
                    return;
                }

                // 頭出しをする
                var combatStart = list.FirstOrDefault(x => x.Log.Contains("戦闘開始"));
                if (combatStart != null)
                {
                    list.RemoveRange(0, list.IndexOf(combatStart));
                }

                var first = list.First();

                foreach (var log in list)
                {
                    if (log.Timestamp >= first.Timestamp)
                    {
                        log.Time = log.Timestamp - first.Timestamp;
                    }
                    else
                    {
                        log.Time = log.Timestamp.AddDays(1) - first.Timestamp;
                    }
                }
            });

            this.Logs.Clear();
            this.Logs.AddRange(list);
        }

        private volatile bool isPause = false;

        private void TestTimer_Tick(object sender, EventArgs e)
        {
            lock (this)
            {
                var now = DateTime.Now;

                if (this.isPause)
                {
                    this.prevTestTimestamp = now;
                    return;
                }

                this.TestTime += now - this.prevTestTimestamp;
                this.prevTestTimestamp = now;

                var logs = (
                    from x in this.Logs
                    where
                    x.Time <= this.TestTime &&
                    !x.IsDone
                    orderby
                    x.Seq
                    select
                    x).ToArray();

                foreach (var log in logs)
                {
                    Thread.Yield();

                    log.IsDone = true;

                    var logInfo = new LogLineEventArgs(
                        $"[{DateTime.Now:HH:mm:ss.fff}] {log.Log}",
                        0,
                        DateTime.Now,
                        string.Empty,
                        true);

                    PluginMainWorker.Instance.LogBuffer.LogInfoQueue.Enqueue(logInfo);
                }

                var last = logs.LastOrDefault();
                if (last != null)
                {
                    this.TimelineTestListView.ScrollIntoView(last);
                }
            }
        }

        #region Test Conditions

        public IReadOnlyList<Job> JobList => Jobs.SortedList.Where(x =>
            x.ID != JobIDs.Unknown &&
            x.ID != JobIDs.ADV).ToList();

        public IReadOnlyList<Zone> ZoneList => (
            from x in FFXIVPlugin.Instance?.ZoneList
            orderby
            x.IsAddedByUser ? 0 : 1,
            x.Rank,
            x.ID descending
            select
            x).ToList();

        private string meName = "Paladin Taro";
        private JobIDs meJob = JobIDs.PLD;

        private string party2Name = "Warrior Jiro";
        private JobIDs party2Job = JobIDs.WAR;

        private string party3Name = "White Hanako";
        private JobIDs party3Job = JobIDs.WHM;

        private string party4Name = "Scholar Yoshiko";
        private JobIDs party4Job = JobIDs.SCH;

        private string party5Name = "Monk Saburo";
        private JobIDs party5Job = JobIDs.MNK;

        private string party6Name = "Ryusan InTheSky";
        private JobIDs party6Job = JobIDs.DRG;

        private string party7Name = "Archer Shiro";
        private JobIDs party7Job = JobIDs.BRD;

        private string party8Name = "Black Yoshida";
        private JobIDs party8Job = JobIDs.BLM;

        private int zoneID;

        public int ZoneID
        {
            get => this.zoneID;
            set => this.SetProperty(ref this.zoneID, value);
        }

        public string MeName
        {
            get => this.meName;
            set => this.SetProperty(ref this.meName, value);
        }

        public JobIDs MeJob
        {
            get => this.meJob;
            set => this.SetProperty(ref this.meJob, value);
        }

        public string Party2Name
        {
            get => this.party2Name;
            set => this.SetProperty(ref this.party2Name, value);
        }

        public JobIDs Party2Job
        {
            get => this.party2Job;
            set => this.SetProperty(ref this.party2Job, value);
        }

        public string Party3Name
        {
            get => this.party3Name;
            set => this.SetProperty(ref this.party3Name, value);
        }

        public JobIDs Party3Job
        {
            get => this.party3Job;
            set => this.SetProperty(ref this.party3Job, value);
        }

        public string Party4Name
        {
            get => this.party4Name;
            set => this.SetProperty(ref this.party4Name, value);
        }

        public JobIDs Party4Job
        {
            get => this.party4Job;
            set => this.SetProperty(ref this.party4Job, value);
        }

        public string Party5Name
        {
            get => this.party5Name;
            set => this.SetProperty(ref this.party5Name, value);
        }

        public JobIDs Party5Job
        {
            get => this.party5Job;
            set => this.SetProperty(ref this.party5Job, value);
        }

        public string Party6Name
        {
            get => this.party6Name;
            set => this.SetProperty(ref this.party6Name, value);
        }

        public JobIDs Party6Job
        {
            get => this.party6Job;
            set => this.SetProperty(ref this.party6Job, value);
        }

        public string Party7Name
        {
            get => this.party7Name;
            set => this.SetProperty(ref this.party7Name, value);
        }

        public JobIDs Party7Job
        {
            get => this.party7Job;
            set => this.SetProperty(ref this.party7Job, value);
        }

        public string Party8Name
        {
            get => this.party8Name;
            set => this.SetProperty(ref this.party8Name, value);
        }

        public JobIDs Party8Job
        {
            get => this.party8Job;
            set => this.SetProperty(ref this.party8Job, value);
        }

        private void ApplyTestCondition()
        {
            lock (TableCompiler.Instance.SimulationLocker)
            {
                var dummyPartys = new(string Name, JobIDs Job)[]
                {
                    (this.MeName, this.MeJob),
                    (this.Party2Name, this.Party2Job),
                    (this.Party3Name, this.Party3Job),
                    (this.Party4Name, this.Party4Job),
                    (this.Party5Name, this.Party5Job),
                    (this.Party6Name, this.Party6Job),
                    (this.Party7Name, this.Party7Job),
                    (this.Party8Name, this.Party8Job),
                };

                TableCompiler.Instance.SimulationParty.Clear();

                foreach (var dummy in dummyPartys)
                {
                    if (string.IsNullOrEmpty(dummy.Name))
                    {
                        continue;
                    }

                    var combatant = new Combatant();
                    combatant.type = ObjectType.PC;
                    combatant.SetName(dummy.Name);
                    combatant.Job = (byte)dummy.Job;

                    if (!TableCompiler.Instance.SimulationParty.Any())
                    {
                        combatant.ID = 1;
                    }
                    else
                    {
                        combatant.ID = (uint)(8 - TableCompiler.Instance.SimulationParty.Count());
                    }

                    TableCompiler.Instance.SimulationParty.Add(combatant);
                }

                TableCompiler.Instance.SimulationPlayer = TableCompiler.Instance.SimulationParty.FirstOrDefault();
                TableCompiler.Instance.SimulationZoneID = this.ZoneID;
                TableCompiler.Instance.InSimulation = true;
            }
        }

        private void ClearTestCondition()
        {
            lock (TableCompiler.Instance.SimulationLocker)
            {
                TableCompiler.Instance.InSimulation = false;
                TableCompiler.Instance.SimulationPlayer = null;
                TableCompiler.Instance.SimulationParty.Clear();
                TableCompiler.Instance.SimulationZoneID = 0;
            }
        }

        #endregion Test Conditions

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

        public class TestLog :
            BindableBase
        {
            private string logline;

            public TestLog(string logline)
            {
                this.logline = logline;

                this.Log = logline.Remove(0, 15);

                var timestampText = logline.Substring(0, 15).TrimEnd()
                    .Replace("[", string.Empty)
                    .Replace("]", string.Empty);

                DateTime d;
                if (DateTime.TryParse(timestampText, out d))
                {
                    this.Timestamp = d;
                }
            }

            public long Seq { get; set; }

            private bool isDone;

            public bool IsDone
            {
                get => this.isDone;
                set
                {
                    if (this.SetProperty(ref this.isDone, value))
                    {
                        this.RaisePropertyChanged(nameof(this.DoneText));
                    }
                }
            }

            public string DoneText => this.IsDone ? "✔" : string.Empty;

            private DateTime timestamp;

            public DateTime Timestamp
            {
                get => this.timestamp;
                set => this.SetProperty(ref this.timestamp, value);
            }

            private TimeSpan time;

            public TimeSpan Time
            {
                get => this.time;
                set
                {
                    if (this.SetProperty(ref this.time, value))
                    {
                        this.RaisePropertyChanged(nameof(this.TimeText));
                    }
                }
            }

            public string TimeText => this.Time.ToTLString();

            private string log;

            public string Log
            {
                get => this.log;
                set => this.SetProperty(ref this.log, value);
            }

            public override string ToString() => $"[{this.TimeText}] {this.Log}";
        }
    }
}
