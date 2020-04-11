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
using System.Windows.Shapes;
using ACT.SpecialSpellTimer.Config;
using ACT.SpecialSpellTimer.Models;
using FFXIV.Framework.WPF.Views;

namespace ACT.SpecialSpellTimer.Views
{
    public interface ISpellPanelWindow :
        IOverlay
    {
        bool IsClickthrough { get; set; }

        SpellPanel Panel { get; }

        IList<Spell> Spells { get; set; }

        void Refresh();
    }

    public static class SpellPanelWindowExtensions
    {
        public static Window ToWindow(this ISpellPanelWindow s) => s as Window;
    }

    /// <summary>
    /// AdvancedSpellPanelWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class AdvancedSpellPanelWindow :
        Window,
        ISpellPanelWindow,
        INotifyPropertyChanged
    {
        private Point _originalPoint;
        private bool _isRestorePosition = false;

        public static ISpellPanelWindow GetWindow(
            SpellPanel panel)
        {
            if (panel.EnabledAdvancedLayout)
            {
                return new AdvancedSpellPanelWindow(panel);
            }
            else
            {
                return new SpellPanelWindow(panel);
            }
        }

        public AdvancedSpellPanelWindow() :
            this(SpellPanel.GeneralPanel)
        {
        }

        public AdvancedSpellPanelWindow(
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

            for (int r = 0; r < this.GuidRulerGrid.RowDefinitions.Count; r++)
            {
                for (int c = 0; c < this.GuidRulerGrid.ColumnDefinitions.Count; c++)
                {
                    var rect = new Rectangle()
                    {
                        Stroke = Brushes.LemonChiffon,
                        StrokeThickness = 0.2,
                    };

                    Grid.SetRow(rect, r);
                    Grid.SetColumn(rect, c);
                    this.GuidRulerGrid.Children.Insert(0, rect);
                }
            }

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

        private SpellPanel panel;

        public SpellPanel Panel
        {
            get => this.panel;
            private set => this.SetProperty(ref this.panel, value);
        }

        private IList<Spell> spells;

        public IList<Spell> Spells
        {
            get => this.spells;
            set => this.SetProperty(ref this.spells, value);
        }

        private readonly ObservableCollection<Spell> activeSpells = new ObservableCollection<Spell>();

        public CollectionViewSource ActiveSpellViewSource;
        public ICollectionView ActiveSpellView => this.ActiveSpellViewSource?.View;

        private readonly List<SpellControl> spellControls = new List<SpellControl>();
        private volatile bool isStackLayout;

        public void Refresh()
        {
            var now = DateTime.Now;

            // 表示するものがなければ何もしない
            if (this.Spells == null)
            {
                this.Topmost = false;
                this.HideOverlay();
                this.activeSpells.Clear();
                this.ClearSpellControls();
                return;
            }

            // 表示対象だけに絞る
            var spells =
                from x in this.Spells
                where
                x.ProgressBarVisible
                orderby
                x.DisplayNo
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

                spell.Visibility = !toHide ?
                    Visibility.Visible :
                    Visibility.Collapsed;
            }

            if (!spells.Any(x => x.Visibility == Visibility.Visible))
            {
                this.HideOverlay();
                this.activeSpells.Clear();
                this.ClearSpellControls();
                return;
            }

            if (this.isStackLayout != this.panel.IsStackLayout)
            {
                lock (this)
                {
                    if (this.isStackLayout != this.panel.IsStackLayout)
                    {
                        this.isStackLayout = this.panel.IsStackLayout;
                        this.activeSpells.Clear();
                    }
                }
            }

            // 有効なスペルリストを入れ替える
            var toAdd = spells.Where(x => !this.activeSpells.Any(y => y.Guid == x.Guid));
            var toRemove = this.activeSpells.Where(x => !spells.Any(y => y.Guid == x.Guid)).ToArray();

            this.activeSpells.AddRange(toAdd);
            foreach (var spell in toRemove)
            {
                this.activeSpells.Remove(spell);
            }

            // 表示を更新する
            if (this.RefreshRender())
            {
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
            else
            {
                this.HideOverlay();
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

        private bool RefreshRender()
        {
            var controls = default(SpellControl[]);

            lock (this.spellControls)
            {
                this.spellControls.RemoveAll(x =>
                    !this.activeSpells.Any(y => y.Guid == x.Spell.Guid));

                controls = this.spellControls.ToArray();
            }

            var isActive = false;
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
                isActive |= control.IsActive;

                // 最初は非表示（Opacity=0）にしているので表示する
                control.Opacity = 1.0;
            }

            return isActive;
        }

        #region Drag & Drop

#if false

        private bool isDrag;
        private Point dragOffset;

        private void DragOnMouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            var el = sender as UIElement;
            if (el != null)
            {
                this.isDrag = true;
                this.dragOffset = e.GetPosition(el);
                el.CaptureMouse();
            }
        }

        private void DragOnMouseLeftButtonUp(
            object sender,
            MouseButtonEventArgs e)
        {
            if (this.isDrag)
            {
                var el = sender as UIElement;
                el.ReleaseMouseCapture();
                this.isDrag = false;
            }
        }

        private void DragOnMouseMove(
            object sender,
            MouseEventArgs e)
        {
            if (this.isDrag)
            {
                var pt = Mouse.GetPosition(this.SpellsCanvas);
                var el = sender as UIElement;
                Canvas.SetLeft(el, pt.X - this.dragOffset.X);
                Canvas.SetTop(el, pt.Y - this.dragOffset.Y);
            }
        }
#endif

        #endregion Drag & Drop

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
