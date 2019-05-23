using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using ACT.SpecialSpellTimer.Config;
using ACT.SpecialSpellTimer.Models;
using FFXIV.Framework.Extensions;
using FFXIV.Framework.WPF.Views;

namespace ACT.SpecialSpellTimer.Views
{
    /// <summary>
    /// SpellTimerList Window
    /// </summary>
    public partial class SpellPanelWindow :
        Window,
        IOverlay,
        ISpellPanelWindow,
        INotifyPropertyChanged
    {
        private Point _originalPoint;
        private bool _isRestorePosition = false;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SpellPanelWindow(
            SpellPanel panel)
        {
            this.Panel = panel;
            this.Panel.PanelWindow = this;

            this._originalPoint = new Point(panel.Left, panel.Top);

            this.InitializeComponent();
            this.ToNonActive();
            this.Opacity = 0;

            this.Loaded += (x, y) => this.SubscribeZOrderCorrector();

            this.MouseLeftButtonDown += (x, y) =>
            {
                if (!this.Panel.Locked)
                {
                    this.DragMove();
                }
            };

            this.Closed += (x, y) =>
            {
                this.activeSpells.Clear();

                if (this.Panel != null)
                {
                    this.Panel.PanelWindow = null;
                    this.Panel = null;
                }
            };

            this.LocationChanged += (sender, args) =>
            {
                if (this._isRestorePosition)
                {
                    return;
                }

                if (this.Panel.Locked)
                {
                    this._isRestorePosition = true;
                    this.Top = this._originalPoint.Y;
                    this.Left = this._originalPoint.X;
                }

                this._isRestorePosition = false;
            };

            this.ActiveSpellViewSource = new CollectionViewSource()
            {
                Source = this.activeSpells,
                IsLiveSortingRequested = true,
            };

            this.ActiveSpellViewSource.SortDescriptions.AddRange(new[]
            {
                new SortDescription(
                    nameof(Spell.ActualSortOrder),
                    ListSortDirection.Ascending)
            });

            this.RaisePropertyChanged(nameof(this.ActiveSpellView));
        }

        private bool overlayVisible;

        public bool OverlayVisible
        {
            get => this.overlayVisible;
            set => this.SetOverlayVisible(ref this.overlayVisible, value, Settings.Default.OpacityToView);
        }

        private bool? isClickthrough = null;

        public bool IsClickthrough
        {
            get => this.isClickthrough ?? false;
            set
            {
                if (this.isClickthrough != value)
                {
                    this.isClickthrough = value;

                    if (this.isClickthrough.Value)
                    {
                        this.ToTransparent();
                    }
                    else
                    {
                        this.ToNotTransparent();
                    }
                }
            }
        }

        public SpellPanel Panel { get; set; }

        public IList<Spell> Spells { get; set; }

        private readonly ObservableCollection<Spell> activeSpells = new ObservableCollection<Spell>();

        public CollectionViewSource ActiveSpellViewSource;
        public ICollectionView ActiveSpellView => this.ActiveSpellViewSource?.View;

        private readonly List<SpellControl> spellControls = new List<SpellControl>();

        private SolidColorBrush backgroundBrush;

        public SolidColorBrush BackgroundBrush
        {
            get => this.backgroundBrush;
            set => this.SetProperty(ref this.backgroundBrush, value);
        }

        private Orientation spellOrientation = Orientation.Vertical;

        public Orientation SpellOrientation
        {
            get => this.spellOrientation;
            set => this.SetProperty(ref this.spellOrientation, value);
        }

        private Thickness spellMargin = new Thickness();

        public Thickness SpellMargin
        {
            get => this.spellMargin;
            set => this.SetProperty(ref this.spellMargin, value);
        }

        /// <summary>
        /// SpellTimerの描画をRefreshする
        /// </summary>
        public void Refresh()
        {
            var now = DateTime.Now;

            // 表示するものがなければ何もしない
            if (this.Spells == null ||
                this.Panel == null)
            {
                this.HideOverlay();
                this.Topmost = false;
                this.activeSpells.Clear();
                this.ClearSpellControls();
                return;
            }

            // 表示対象だけに絞る
            var spells =
                from x in this.Spells
                where
                x.ProgressBarVisible
                select
                x;

            // タイムアップしたものを非表示にする
            foreach (var spell in spells)
            {
                var toHide = false;

                if (Settings.Default.TimeOfHideSpell > 0.0d)
                {
                    if (!spell.DontHide &&
                        !spell.IsDesignMode &&
                        (now - spell.CompleteScheduledTime).TotalSeconds > Settings.Default.TimeOfHideSpell)
                    {
                        toHide = true;
                    }
                }

                if (!toHide)
                {
                    spell.Visibility = Visibility.Visible;
                }
                else
                {
                    spell.Visibility = this.Panel.SortOrder == SpellOrders.Fixed ?
                        Visibility.Hidden :
                        Visibility.Collapsed;
                }
            }

            if (!spells.Any(x => x.Visibility == Visibility.Visible))
            {
                this.HideOverlay();
                this.activeSpells.Clear();
                this.ClearSpellControls();
                return;
            }

            // ソートする
            switch (this.Panel.SortOrder)
            {
                case SpellOrders.None:
                case SpellOrders.SortPriority:
                case SpellOrders.Fixed:
                    spells =
                        from x in spells
                        orderby
                        x.DisplayNo,
                        x.ID
                        select
                        x;
                    break;

                case SpellOrders.SortRecastTimeASC:
                    spells =
                        from x in spells
                        orderby
                        x.CompleteScheduledTime,
                        x.DisplayNo,
                        x.ID
                        select
                        x;
                    break;

                case SpellOrders.SortRecastTimeDESC:
                    spells =
                        from x in spells
                        orderby
                        x.CompleteScheduledTime descending,
                        x.DisplayNo,
                        x.ID
                        select
                        x;
                    break;

                case SpellOrders.SortMatchTime:
                    spells =
                        from x in spells
                        orderby
                        x.MatchDateTime == DateTime.MinValue ?
                            DateTime.MaxValue :
                            x.MatchDateTime,
                        x.DisplayNo,
                        x.ID
                        select
                        x;
                    break;
            }

            // 向きを設定する
            if (!this.Panel.Horizontal)
            {
                this.SpellOrientation = Orientation.Vertical;
                this.SpellMargin = new Thickness(0, 0, 0, this.Panel.Margin);
            }
            else
            {
                this.SpellOrientation = Orientation.Horizontal;
                this.SpellMargin = new Thickness(0, 0, this.Panel.Margin, 0);
            }

            // 背景色を設定する
            var s = spells.FirstOrDefault();
            if (s != null)
            {
                var c = s.BackgroundColor.FromHTMLWPF();
                var backGroundColor = Color.FromArgb(
                    (byte)s.BackgroundAlpha,
                    c.R,
                    c.G,
                    c.B);

                this.BackgroundBrush = this.GetBrush(backGroundColor);
            }

            // 有効なスペルリストを入れ替える
            var toAdd = spells.Where(x => !this.activeSpells.Any(y => y.Guid == x.Guid));
            var toRemove = this.activeSpells.Where(x => !spells.Any(y => y.Guid == x.Guid)).ToArray();

            this.activeSpells.AddRange(toAdd);
            foreach (var spell in toRemove)
            {
                this.activeSpells.Remove(spell);
            }

            // ソート順をセットする
            var order = 1;
            foreach (var spell in spells)
            {
                spell.ActualSortOrder = order++;
            }

            // 表示を更新する
            this.RefreshRender();

            if (this.activeSpells.Any())
            {
                if (this.ShowOverlay())
                {
                    this.Topmost = true;
                    this.SubscribeZOrderCorrector();
                    this.EnsureTopMost();
                }
            }
        }

        private void SpellControl_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            var control = sender as SpellControl;

            lock (this.spellControls)
            {
                if (!this.spellControls.Any(x =>
                    x.Spell.Guid == control.Spell.Guid))
                {
                    control.Spell.UpdateDone = false;
                    this.spellControls.Add(control);
                }
            }
        }

        private void ClearSpellControls()
        {
            lock (this.spellControls)
            {
                this.spellControls.Clear();
            }
        }

        private void RefreshRender()
        {
            var controls = default(SpellControl[]);

            lock (this.spellControls)
            {
                this.spellControls.RemoveAll(x =>
                    !this.activeSpells.Any(y => y.Guid == x.Spell.Guid));

                controls = this.spellControls.ToArray();
            }

            foreach (var control in controls)
            {
                var spell = control.Spell;

                // Designモードならば必ず再描画する
                if (spell.IsDesignMode)
                {
                    if (spell.MatchDateTime == DateTime.MinValue)
                    {
                        control.Update();
                        control.StartBarAnimation();
                    }
                    else
                    {
                        if ((DateTime.Now - spell.CompleteScheduledTime).TotalSeconds > 1.0d)
                        {
                            spell.MatchDateTime = DateTime.MinValue;
                        }
                    }
                }

                // 一度もログにマッチしていない時はバーを初期化する
                if (spell.MatchDateTime == DateTime.MinValue &&
                    !spell.UpdateDone)
                {
                    control.Progress = 1.0d;
                    control.RecastTime = 0;
                }
                else
                {
                    control.RecastTime = (spell.CompleteScheduledTime - DateTime.Now).TotalSeconds;
                    if (control.RecastTime < 0)
                    {
                        control.RecastTime = 0;
                    }

                    var totalRecastTime = (spell.CompleteScheduledTime - spell.MatchDateTime).TotalSeconds;
                    control.Progress = totalRecastTime != 0 ?
                        (totalRecastTime - control.RecastTime) / totalRecastTime :
                        1.0d;
                    if (control.Progress > 1.0d)
                    {
                        control.Progress = 1.0d;
                    }
                }

                if (!spell.UpdateDone)
                {
                    control.Update();
                    control.StartBarAnimation();
                    spell.UpdateDone = true;
                }

                control.Refresh();

                // 最初は非表示（Opacity=0）にしているので表示する
                control.Opacity = 1.0;
            }
        }

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
