using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using ACT.UltraScouter.Common;
using ACT.UltraScouter.Config.UI.Views;
using FFXIV.Framework.Common;
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
                object viewModell = null,
                Page parentView = null)
            {
                var general = parentView != null ?
                    parentView :
                    new BlankView();

                var name = new TargetNameConfigView();
                var hp = new TargetHPConfigView();
                var action = new TargetActionConfigView();
                var distance = new TargetDistanceConfigView();

                if (viewModell != null)
                {
                    general.DataContext = new ConfigViewModel();

                    name.DataContext = viewModell;
                    hp.DataContext = viewModell;
                    action.DataContext = viewModell;
                    distance.DataContext = viewModell;
                }

                var parent = new TreeSource(parentText)
                {
                    Content = general
                };

                parent.Child = new ObservableCollection<TreeSource>()
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

                return parent;
            }

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

                createTargetSubset("Target", new TargetConfigViewModel(), new TargetGeneralConfigView()),
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

                        new TreeSource("MP Ticker")
                        {
                            Content = new MPTickerConfigView()
                            {
                                DataContext = new MPTickerConfigViewModel()
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
                        new TreeSource("Combatants")
                        {
                            Content = new CombatantsView()
                            {
                                DataContext = new CombatantsViewModel()
                            }
                        }
                    }
                }
            };

            return menuTreeViewItems;
        }

        #endregion Menu

        public Settings Config => Settings.Instance;

        public static IReadOnlyList<ValueAndText> TTSDeviceList
            => new List<ValueAndText>()
            {
                new ValueAndText() { Value = TTSDevices.Normal },
                new ValueAndText() { Value = TTSDevices.OnlyMain },
                new ValueAndText() { Value = TTSDevices.OnlySub },
            };

        public class ValueAndText
        {
            public TTSDevices Value { get; set; }
            public string Text => this.Value.ToText();
        }
    }
}
