using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml.Serialization;
using ACT.SpecialSpellTimer.Config.Views;
using ACT.SpecialSpellTimer.Models;
using ACT.SpecialSpellTimer.Views;
using Advanced_Combat_Tracker;
using FFXIV.Framework.WPF.Views;
using Prism.Commands;
using Prism.Mvvm;

namespace ACT.SpecialSpellTimer.Config.Models
{
    public enum ItemTypes
    {
        Unknown,
        SpellsRoot,
        TickersRoot,
        TagsRoot,
        Tag,
        SpellPanel,
        Spell,
        Ticker,
    }

    public interface ITreeItem
    {
        ItemTypes ItemType { get; }

        int SortPriority { get; set; }

        string DisplayText { get; }

        /// <summary>
        /// リストボックス等にアイテムを設定して選択状態等を得るための汎用的なBOOL値
        /// </summary>
        bool IsChecked { get; set; }

        bool IsExpanded { get; set; }

        bool Enabled { get; set; }

        bool IsSelected { get; set; }

        bool IsInEditMode { get; set; }

        bool IsInViewMode { get; }

        ICollectionView Children { get; }
    }

    public static class TreeItemExtensions
    {
        public static Guid GetID(
            this ITreeItem item)
        {
            switch (item)
            {
                case SpellPanel p:
                    return p.ID;

                case Spell s:
                    return s.Guid;

                case Ticker t:
                    return t.Guid;

                case Tag tag:
                    return tag.ID;

                default:
                    return Guid.Empty;
            }
        }
    }

    public abstract class TreeItemBase :
        BindableBase,
        ITreeItem
    {
        private bool isChecked;
        private bool isSelected;
        private bool isInEditMode;

        public abstract ItemTypes ItemType { get; }

        public abstract int SortPriority { get; set; }

        public abstract string DisplayText { get; }

        public bool IsChecked
        {
            get => this.isChecked;
            set => this.SetProperty(ref this.isChecked, value);
        }

        public abstract bool IsExpanded { get; set; }

        public abstract bool Enabled { get; set; }

        [XmlIgnore]
        public bool IsSelected
        {
            get => this.isSelected;
            set => this.SetProperty(ref this.isSelected, value);
        }

        [XmlIgnore]
        public bool IsInEditMode
        {
            get => this.isInEditMode;
            set
            {
                if (this.SetProperty(ref this.isInEditMode, value))
                {
                    this.RaisePropertyChanged(nameof(this.IsInViewMode));
                }
            }
        }

        [XmlIgnore]
        public bool IsInViewMode => !this.IsInEditMode;

        public abstract ICollectionView Children { get; }

        #region Commands

        private ICommand createNewSpellPanelCommand;

        [XmlIgnore]
        public ICommand CreateNewSpellPanelCommand =>
            this.createNewSpellPanelCommand ?? (this.createNewSpellPanelCommand = new DelegateCommand<ITreeItem>(item =>
            {
                var newPanel = default(SpellPanel);

                switch (item.ItemType)
                {
                    case ItemTypes.SpellsRoot:
                    case ItemTypes.TickersRoot:
                    case ItemTypes.TagsRoot:
                        newPanel = new SpellPanel()
                        {
                            PanelName = "New Panel",
                            SortOrder = SpellOrders.SortRecastTimeASC,
                            Top = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height / 2,
                            Left = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width / 2,
                        };

                        SpellPanelTable.Instance.Table.Add(newPanel);
                        break;

                    case ItemTypes.SpellPanel:
                        var currentPanel = item as SpellPanel;
                        newPanel = currentPanel.MemberwiseClone() as SpellPanel;
                        newPanel.ID = Guid.NewGuid();
                        newPanel.PanelName += " New";
                        newPanel.SortPriority = 0;
                        newPanel.SetupChildrenSource();
                        SpellPanelTable.Instance.Table.Add(newPanel);

                        foreach (var tagID in
                            TagTable.Instance.ItemTags
                                .Where(x => x.ItemID == currentPanel.ID).ToArray()
                                .Select(x => x.TagID)
                                .Distinct())
                        {
                            TagTable.Instance.ItemTags.Add(new ItemTags(newPanel.ID, tagID));
                        }

                        break;

                    case ItemTypes.Tag:
                        var currentTag = item as Tag;

                        newPanel = new SpellPanel()
                        {
                            PanelName = "New Panel",
                            SortOrder = SpellOrders.SortRecastTimeASC,
                            Top = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height / 2,
                            Left = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width / 2,
                        };

                        SpellPanelTable.Instance.Table.Add(newPanel);

                        TagTable.Instance.ItemTags.Add(new ItemTags(newPanel.ID, currentTag.ID));
                        currentTag.IsExpanded = true;
                        break;
                }

                if (newPanel != null)
                {
                    SpellPanelTable.Instance.Save();
                    newPanel.IsSelected = true;
                }
            }));

        private ICommand createNewSpellCommand;

        [XmlIgnore]
        public ICommand CreateNewSpellCommand =>
            this.createNewSpellCommand ?? (this.createNewSpellCommand = new DelegateCommand<ITreeItem>(item =>
            {
                var newSpell = default(Spell);
                var currentSpell = default(Spell);
                var currentPanel = default(SpellPanel);

                switch (item.ItemType)
                {
                    case ItemTypes.SpellsRoot:
                    case ItemTypes.TickersRoot:
                    case ItemTypes.TagsRoot:
                        newSpell = Spell.CreateNew();
                        newSpell.PanelID = SpellPanel.GeneralPanel.ID;
                        break;

                    case ItemTypes.SpellPanel:
                        currentPanel = item as SpellPanel;
                        newSpell = Spell.CreateNew();
                        newSpell.PanelID = currentPanel.ID;
                        currentPanel.IsExpanded = true;
                        break;

                    case ItemTypes.Spell:
                        currentSpell = item as Spell;
                        newSpell = currentSpell.CreateSimilarNew();
                        break;

                    case ItemTypes.Tag:
                        var currentTag = item as Tag;
                        currentPanel = (
                            from x in SpellPanelTable.Instance.Table
                            join y in TagTable.Instance.ItemTags on
                            x.ID equals y.ItemID
                            where
                            y.TagID == currentTag.ID
                            orderby
                            x.PanelName
                            select
                            x).FirstOrDefault();

                        newSpell = Spell.CreateNew();
                        newSpell.PanelID = currentPanel != null ?
                            currentPanel.ID :
                            SpellPanel.GeneralPanel.ID;

                        currentTag.IsExpanded = true;
                        if (currentPanel != null)
                        {
                            currentPanel.IsExpanded = true;
                        }

                        break;
                }

                if (newSpell != null)
                {
                    SpellTable.Instance.Add(newSpell);
                    SpellTable.Instance.Save();

                    // スペルの追加は選択状態にせずパネルを展開状態にするだけにする
                    if (newSpell.Panel != null)
                    {
                        newSpell.Panel.IsExpanded = true;
                    }
                }
            }));

        private ICommand createNewTickerCommand;

        [XmlIgnore]
        public ICommand CreateNewTickerCommand =>
            this.createNewTickerCommand ?? (this.createNewTickerCommand = new DelegateCommand<ITreeItem>(item =>
            {
                var newTicker = default(Ticker);
                var currentTicker = default(Ticker);

                switch (item.ItemType)
                {
                    case ItemTypes.SpellsRoot:
                    case ItemTypes.TickersRoot:
                    case ItemTypes.TagsRoot:
                        newTicker = Ticker.CreateNew();
                        newTicker.Top = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height / 2;
                        newTicker.Left = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width / 2;
                        TickerTable.Instance.Add(newTicker);
                        break;

                    case ItemTypes.Ticker:
                        currentTicker = item as Ticker;
                        newTicker = currentTicker.CreateSimilarNew();
                        TickerTable.Instance.Add(newTicker);

                        foreach (var tagID in
                            TagTable.Instance.ItemTags
                                .Where(x => x.ItemID == currentTicker.Guid).ToArray()
                                .Select(x => x.TagID)
                                .Distinct())
                        {
                            TagTable.Instance.ItemTags.Add(new ItemTags(newTicker.Guid, tagID));
                        }

                        break;

                    case ItemTypes.Tag:
                        var currentTag = item as Tag;

                        newTicker = Ticker.CreateNew();
                        newTicker.Top = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height / 2;
                        newTicker.Left = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width / 2;

                        TickerTable.Instance.Add(newTicker);
                        TagTable.Instance.ItemTags.Add(new ItemTags(newTicker.Guid, currentTag.ID));
                        currentTag.IsExpanded = true;
                        break;
                }

                if (newTicker != null)
                {
                    TickerTable.Instance.Save();
                    newTicker.IsSelected = true;
                }
            }));

        private ICommand createNewTagCommand;

        [XmlIgnore]
        public ICommand CreateNewTagCommand =>
            this.createNewTagCommand ?? (this.createNewTagCommand = new DelegateCommand<ITreeItem>(item =>
            {
                var newItem = default(Tag);

                switch (item.ItemType)
                {
                    case ItemTypes.SpellsRoot:
                    case ItemTypes.TickersRoot:
                    case ItemTypes.TagsRoot:
                    case ItemTypes.Tag:
                        newItem = TagTable.Instance.AddNew("New Tag");
                        TagTable.Instance.Save();
                        newItem.IsSelected = true;
                        break;
                }
            }));

        private ICommand renameCommand;

        [XmlIgnore]
        public ICommand RenameCommand =>
            this.renameCommand ?? (this.renameCommand = new DelegateCommand<ITreeItem>(item =>
            {
                switch (item.ItemType)
                {
                    case ItemTypes.Tag:
                        if ((item as Tag).ID != Tag.ImportsTag.ID)
                        {
                            item.IsInEditMode = true;
                        }

                        break;
                }
            }));

        private ICommand deleteCommand;

        [XmlIgnore]
        public ICommand DeleteCommand =>
            this.deleteCommand ?? (this.deleteCommand = new DelegateCommand<ITreeItem>(item =>
            {
                var result = false;

                switch (item.ItemType)
                {
                    case ItemTypes.Tag:
                        var tag = item as Tag;
                        if (tag.ID == Tag.ImportsTag.ID)
                        {
                            return;
                        }

                        result = ModernMessageBox.ShowDialog(
                            $@"Delete ""{ item.DisplayText }"" tag ?",
                            "Confirm",
                            MessageBoxButton.OKCancel);
                        if (!result)
                        {
                            return;
                        }

                        foreach (var toRemove in
                            TagTable.Instance.ItemTags.Where(x => x.TagID == tag.ID).ToArray())
                        {
                            TagTable.Instance.ItemTags.Remove(toRemove);
                        }

                        TagTable.Instance.Remove(tag);
                        break;

                    case ItemTypes.SpellPanel:
                        var panel = item as SpellPanel;
                        if (panel.ID == SpellPanel.GeneralPanel.ID)
                        {
                            return;
                        }

                        result = ModernMessageBox.ShowDialog(
                            $@"Delete ""{ item.DisplayText }"" panel and spells ?",
                            "Confirm",
                            MessageBoxButton.OKCancel);
                        if (!result)
                        {
                            return;
                        }

                        var targets = SpellTable.Instance.Table.Where(x => x.PanelID == panel.ID).ToArray();
                        foreach (var target in targets)
                        {
                            target.IsDesignMode = false;
                            SpellTable.Instance.Remove(target);
                        }

                        foreach (var toRemove in
                            TagTable.Instance.ItemTags.Where(x => x.ItemID == panel.ID).ToArray())
                        {
                            TagTable.Instance.ItemTags.Remove(toRemove);
                        }

                        panel.PanelWindow?.ToWindow().Hide();
                        SpellPanelTable.Instance.Table.Remove(panel);
                        break;

                    case ItemTypes.Spell:
                        result = ModernMessageBox.ShowDialog(
                            $@"Delete ""{ item.DisplayText }"" ?",
                            "Confirm",
                            MessageBoxButton.OKCancel);
                        if (!result)
                        {
                            return;
                        }

                        var spell = item as Spell;
                        spell.IsDesignMode = false;
                        SpellTable.Instance.Remove(spell);
                        break;

                    case ItemTypes.Ticker:
                        result = ModernMessageBox.ShowDialog(
                            $@"Delete ""{ item.DisplayText }"" ?",
                            "Confirm",
                            MessageBoxButton.OKCancel);
                        if (!result)
                        {
                            return;
                        }

                        var ticker = item as Ticker;

                        foreach (var toRemove in
                            TagTable.Instance.ItemTags.Where(x => x.ItemID == ticker.Guid).ToArray())
                        {
                            TagTable.Instance.ItemTags.Remove(toRemove);
                        }

                        ticker.IsDesignMode = false;
                        TickerTable.Instance.Remove(ticker);
                        break;
                }
            }));

        private ICommand addToTagCommand;

        [XmlIgnore]
        public ICommand AddToTagCommand =>
            this.addToTagCommand ?? (this.addToTagCommand = new DelegateCommand<ITreeItem>(item =>
            {
                switch (item.ItemType)
                {
                    case ItemTypes.Tag:
                        var view = new AddToTagView(item as Tag);
                        view.Show();
                        break;
                }
            }));

        private ICommand designModeCommand;

        [XmlIgnore]
        public ICommand DesignModeCommand =>
            this.designModeCommand ?? (this.designModeCommand = new DelegateCommand<ITreeItem>(async item =>
            {
                if (item == null)
                {
                    return;
                }

                switch (item)
                {
                    case SpellPanel p:
                        p.IsDesignMode = !p.IsDesignMode;
                        break;

                    case Spell s:
                        s.IsDesignMode = !s.IsDesignMode;
                        break;

                    case Ticker t:
                        t.IsDesignMode = !t.IsDesignMode;
                        break;

                    case Tag tag:
                        tag.IsDesignMode = !tag.IsDesignMode;
                        break;
                }

                await Task.Run(() =>
                {
                    TableCompiler.Instance.CompileSpells();
                    TableCompiler.Instance.CompileTickers();
                });

                var showGrid =
                    TableCompiler.Instance.SpellList.Any(x => x.IsDesignMode) ||
                    TableCompiler.Instance.TickerList.Any(x => x.IsDesignMode);

                Settings.Default.VisibleDesignGrid = showGrid;
            }));

        private ICommand changeEnabledCommand;

        [XmlIgnore]
        public ICommand ChangeEnabledCommand =>
            this.changeEnabledCommand ?? (this.changeEnabledCommand = new DelegateCommand<ITreeItem>(item =>
            {
                if (item == null)
                {
                    return;
                }

                switch (item)
                {
                    case SpellPanel s:
                        Task.Run(() => TableCompiler.Instance.CompileSpells());
                        break;

                    case Ticker t:
                        Task.Run(() => TableCompiler.Instance.CompileTickers());
                        break;

                    default:
                        Task.Run(() => TableCompiler.Instance.CompileSpells());
                        Task.Run(() => TableCompiler.Instance.CompileTickers());
                        break;
                }
            }));

        #endregion Commands

        #region Import & Export

        private static readonly System.Windows.Forms.OpenFileDialog OpenFileDialog = new System.Windows.Forms.OpenFileDialog()
        {
            RestoreDirectory = true,
            Filter = "SPESPE Files|*.xml|All Files|*.*",
            FilterIndex = 0,
            DefaultExt = ".xml",
            SupportMultiDottedExtensions = true,
        };

        private static readonly System.Windows.Forms.SaveFileDialog SaveFileDialog = new System.Windows.Forms.SaveFileDialog()
        {
            RestoreDirectory = true,
            Filter = "SPESPE Files|*.xml|All Files|*.*",
            FilterIndex = 0,
            DefaultExt = ".xml",
            SupportMultiDottedExtensions = true,
        };

        private ICommand importCommand;

        [XmlIgnore]
        public ICommand ImportCommand =>
            this.importCommand ?? (this.importCommand = new DelegateCommand<ITreeItem>(item =>
            {
                if (item == null)
                {
                    return;
                }

                try
                {
                    var result = OpenFileDialog.ShowDialog(
                        ActGlobals.oFormActMain);
                    if (result != System.Windows.Forms.DialogResult.OK)
                    {
                        return;
                    }

                    var file = OpenFileDialog.FileName;

                    // 次使うとき用にケアしておく
                    OpenFileDialog.InitialDirectory = Path.GetDirectoryName(file);
                    OpenFileDialog.FileName = Path.GetFileName(file);

                    var data = ExportContainer.LoadFromFile(file);
                    if (data == null)
                    {
                        return;
                    }

                    if (data.Tag == null)
                    {
                        data.Tag = Tag.ImportsTag;
                    }

                    switch (item.ItemType)
                    {
                        case ItemTypes.SpellsRoot:
                            data.Tickers.Clear();
                            break;

                        case ItemTypes.TickersRoot:
                            data.Spells.Clear();
                            data.Panels.Clear();
                            break;

                        case ItemTypes.SpellPanel:
                            var panel = item as SpellPanel;
                            data.Tickers.Clear();
                            data.Panels.Clear();
                            foreach (var spell in data.Spells)
                            {
                                spell.PanelID = panel.ID;
                            }
                            break;

                        case ItemTypes.TagsRoot:
                            break;

                        case ItemTypes.Tag:
                            var tag = item as Tag;
                            data.Tag = tag;
                            break;

                        default:
                            return;
                    }

                    if (data.Panels.Any())
                    {
                        SpellPanelTable.Instance.Table.AddRange(data.Panels);
                    }

                    if (data.Spells.Any())
                    {
                        SpellTable.Instance.AddRange(data.Spells);
                    }

                    if (data.Tickers.Any())
                    {
                        TickerTable.Instance.AddRange(data.Tickers);
                    }

                    if (data.Tag != null)
                    {
                        if (!TagTable.Instance.Tags.Any(x => x.ID == data.Tag.ID))
                        {
                            TagTable.Instance.Tags.Add(data.Tag);
                        }

                        var items = (
                            from x in data.Panels
                            select
                            x.GetID()).Union(
                            from x in data.Tickers
                            select
                            x.GetID());

                        if (!data.Panels.Any())
                        {
                            items = items.Union(
                                from x in data.Spells
                                select
                                x.GetID());
                        }

                        foreach (var x in items)
                        {
                            TagTable.Instance.ItemTags.Add(new ItemTags()
                            {
                                ItemID = x,
                                TagID = data.Tag.ID,
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    ModernMessageBox.ShowDialog(
                        "Import Error!",
                        "ACT.Hojoring",
                        MessageBoxButton.OK,
                        ex);
                    return;
                }

                Task.Run(() =>
                {
                    TableCompiler.Instance.CompileSpells();
                    TableCompiler.Instance.CompileTickers();
                });

                ModernMessageBox.ShowDialog(
                    "Import Completed.",
                    "ACT.Hojoring");
            }));

        private ICommand exportCommand;

        [XmlIgnore]
        public ICommand ExportCommand =>
            this.exportCommand ?? (this.exportCommand = new DelegateCommand<ITreeItem>(item =>
            {
                if (item == null)
                {
                    return;
                }

                var exports = new ExportContainer();
                var name = string.Empty;

                switch (item.ItemType)
                {
                    case ItemTypes.SpellsRoot:
                        SaveFileDialog.FileName = "Spells.xml";
                        exports.Panels.AddRange(SpellPanelTable.Instance.Table);
                        exports.Spells.AddRange(SpellTable.Instance.Table.Where(x => !x.IsInstance));
                        break;

                    case ItemTypes.TickersRoot:
                        SaveFileDialog.FileName = "Tickers.xml";
                        exports.Tickers.AddRange(TickerTable.Instance.Table);
                        break;

                    case ItemTypes.TagsRoot:
                        return;

                    case ItemTypes.SpellPanel:
                        name = (item as SpellPanel).PanelName;
                        SaveFileDialog.FileName = $"{name}.spellpanel.xml";
                        exports.Panels.Add(item as SpellPanel);
                        foreach (Spell spell in (item as SpellPanel).Children)
                        {
                            if (!spell.IsInstance)
                            {
                                exports.Spells.Add(spell);
                            }
                        }

                        break;

                    case ItemTypes.Spell:
                        name = (item as Spell).SpellTitle;
                        SaveFileDialog.FileName = $"{name}.spell.xml";
                        exports.Spells.Add(item as Spell);
                        break;

                    case ItemTypes.Ticker:
                        name = (item as Ticker).Title;
                        SaveFileDialog.FileName = $"{name}.ticker.xml";
                        exports.Tickers.Add(item as Ticker);
                        break;

                    case ItemTypes.Tag:
                        name = (item as Tag).Name;
                        SaveFileDialog.FileName = $"{name}.tag.xml";
                        ExportTag(exports, item as Tag);

                        if (!exports.Panels.Any() &&
                            !exports.Tickers.Any())
                        {
                            return;
                        }
                        break;

                    default:
                        return;
                }

                try
                {
                    var result = SaveFileDialog.ShowDialog(
                        ActGlobals.oFormActMain);
                    if (result != System.Windows.Forms.DialogResult.OK)
                    {
                        return;
                    }

                    var file = SaveFileDialog.FileName;

                    // 次使うとき用にケアしておく
                    SaveFileDialog.InitialDirectory = Path.GetDirectoryName(file);
                    SaveFileDialog.FileName = Path.GetFileName(file);

                    exports.Save(file);
                }
                catch (Exception ex)
                {
                    ModernMessageBox.ShowDialog(
                        "Export Error!",
                        "ACT.Hojoring",
                        MessageBoxButton.OK,
                        ex);
                    return;
                }

                ModernMessageBox.ShowDialog(
                    "Export Completed.",
                    "ACT.Hojoring");
            }));

        private static void ExportTag(
            ExportContainer exports,
            Tag tag)
        {
            exports.Tag = tag;

            foreach (ITreeItem child in tag.Children)
            {
                if (child is SpellPanel panel)
                {
                    exports.Panels.Add(panel);
                    foreach (Spell spell in panel.Children)
                    {
                        if (!spell.IsInstance)
                        {
                            exports.Spells.Add(spell);
                        }
                    }
                }

                if (child is Spell singleSpell)
                {
                    if (!singleSpell.IsInstance)
                    {
                        var parentPanel = singleSpell.Panel;
                        if (!exports.Panels.Any(x => x.ID == parentPanel.ID))
                        {
                            exports.Panels.Add(parentPanel);
                        }

                        exports.Spells.Add(singleSpell);
                    }
                }

                if (child is Ticker ticker)
                {
                    exports.Tickers.Add(ticker);
                }
            }
        }

        #endregion Import & Export
    }
}
