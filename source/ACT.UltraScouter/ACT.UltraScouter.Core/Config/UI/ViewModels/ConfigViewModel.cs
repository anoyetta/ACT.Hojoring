using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using ACT.UltraScouter.Common;
using ACT.UltraScouter.Config.UI.Views;
using ACT.UltraScouter.Workers;
using FFXIV.Framework.Common;
using FFXIV.Framework.WPF.Views;
using Prism.Commands;
using Prism.Mvvm;

namespace ACT.UltraScouter.Config.UI.ViewModels
{
    public class ConfigViewModel :
        BindableBase
    {
        #region Menu

        private readonly object locker = new object();

        private ObservableCollection<TreeSource> menuTreeViewItems;

        public ObservableCollection<TreeSource> MenuTreeViewItems
        {
            get
            {
                lock (locker)
                {
                    return this.menuTreeViewItems ?? this.InitializeMenuTreeViewItems();
                }
            }
        }

        private ObservableCollection<TreeSource> InitializeMenuTreeViewItems()
        {
            TreeSource createTargetSubset(
                string parentText,
                object viewModel = null,
                Page parentView = null,
                IEnumerable<TreeSource> additionalChildren = null)
            {
                var general = parentView != null ?
                    parentView :
                    new BlankView();

                var name = new TargetNameConfigView();
                var hp = new TargetHPConfigView();
                var action = new TargetActionConfigView();
                var distance = new TargetDistanceConfigView();

                if (viewModel != null)
                {
                    general.DataContext = new ConfigViewModel();

                    name.DataContext = viewModel;
                    hp.DataContext = viewModel;
                    action.DataContext = viewModel;
                    distance.DataContext = viewModel;

                    if (additionalChildren != null)
                    {
                        foreach (var c in additionalChildren)
                        {
                            c.Content.DataContext = viewModel;
                        }
                    }
                }

                var parent = new TreeSource(parentText)
                {
                    Content = general
                };

                var children = new ObservableCollection<TreeSource>()
                {
                    new TreeSource("Name", parent)
                    {
                        Content = name
                    },

                    new TreeSource("HP", parent)
                    {
                        Content = hp
                    },

                    new TreeSource("Action", parent)
                    {
                        Content = action
                    },

                    new TreeSource("Distance", parent)
                    {
                        Content = distance
                    },
                };

                if (additionalChildren != null)
                {
                    foreach (var c in additionalChildren)
                    {
                        c.Parent = parent;
                    }

                    children.AddRange(additionalChildren);
                }

                parent.Child = children;

                return parent;
            }

            var enmityView = new TreeSource("Enmity")
            {
                Content = new EnmityConfigView()
            };

            var ffLogsView = new TreeSource("FFLogs")
            {
                Content = new FFLogsConfigView()
            };

            menuTreeViewItems = new ObservableCollection<TreeSource>()
            {
                new TreeSource("General")
                {
                    Content = new GeneralConfigView()
                    {
                        DataContext = new ConfigViewModel()
                    },
                    IsSelected = true,
                },

                createTargetSubset("Target", new TargetConfigViewModel(), new TargetGeneralConfigView(), new[] { enmityView, ffLogsView }),
                createTargetSubset("Focus Target", new FTConfigViewModel()),
                createTargetSubset("Target of Target", new ToTConfigViewModel()),
                createTargetSubset("BOSS", new BossConfigViewModel(), new BossGeneralConfigView()),

                new TreeSource("ME")
                {
                    Content = new BlankView()
                    {
                        DataContext = new ConfigViewModel()
                    },

                    Child = new ObservableCollection<TreeSource>()
                    {
                        new TreeSource("Action")
                        {
                            Content = new TargetActionConfigView()
                            {
                                DataContext = new MeConfigViewModel()
                            }
                        },

                        new TreeSource("HP")
                        {
                            Content = new MyStatusConfigView()
                            {
                                DataContext = new MyHPConfigViewModel()
                            }
                        },

                        new TreeSource("MP")
                        {
                            Content = new MyStatusConfigView()
                            {
                                DataContext = new MyMPConfigViewModel()
                            }
                        },

                        new TreeSource("3s Ticker")
                        {
                            Content = new MPTickerConfigView()
                            {
                                DataContext = new MPTickerConfigViewModel()
                            }
                        },

                        new TreeSource("My Marker")
                        {
                            Content = new MyMarkerConfigView()
                            {
                                DataContext = new MyMarkerConfigViewModel()
                            }
                        },
                    },
                },

                new TreeSource("Mob")
                {
                    Content = new MobListConfigView()
                    {
                        DataContext = new MobListConfigViewModel()
                    },

                    Child = new ObservableCollection<TreeSource>()
                    {
                        new TreeSource("Enemy List")
                        {
                            Content = new EnemyHPConfigView()
                            {
                                DataContext = new EnemyHPConfigViewModel()
                            }
                        },

                        new TreeSource("Combatants")
                        {
                            Content = new CombatantsView()
                            {
                                DataContext = new CombatantsViewModel()
                            }
                        }
                    }
                },

                new TreeSource("Tactical Radar")
                {
                    Content = new TacticalRadarConfigView()
                    {
                        DataContext = new TacticalRadarConfigViewModel()
                    },
                }
            };

            return menuTreeViewItems;
        }

        #endregion Menu

        public FFXIV.Framework.Config FrameworkConfig => FFXIV.Framework.Config.Instance;

        public Settings Config => Settings.Instance;

        public static IEnumerable<ThreadPriority> ThreadPriorities
            => Enumerator.GetThreadPriorities();

        public static IEnumerable<DispatcherPriority> DispatcherPriorities
            => Enumerator.GetDispatcherPriorities();

        public static IReadOnlyList<ValueAndText> TTSDeviceList
            => new List<ValueAndText>()
            {
                new ValueAndText() { Value = TTSDevices.Normal },
                new ValueAndText() { Value = TTSDevices.OnlyMain },
                new ValueAndText() { Value = TTSDevices.OnlySub },
            };

        private ICommand restartThreadsCommand;

        public ICommand RestartThreadsCommand =>
            this.restartThreadsCommand ?? (this.restartThreadsCommand = new DelegateCommand(async () =>
            {
                await WPFHelper.InvokeAsync(() =>
                {
                    MainWorker.Instance.RestartScanMemoryWorker();
                    MainWorker.Instance.RestartRefreshViewWorker();
                });

                ModernMessageBox.ShowDialog(
                    "\"ScanMemory\" and \"RefreshView\" restarted.\n" +
                    "Applied new settings.",
                    "ACT.UltraScouter");
            }));

        public class ValueAndText
        {
            public TTSDevices Value { get; set; }
            public string Text => this.Value.ToText();
        }
    }
}
