using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using FFXIV.Framework.Common;
using FFXIV.Framework.WPF.Views;

namespace ACT.SpecialSpellTimer.RaidTimeline.Views
{
    /// <summary>
    /// TimelineOverlay.xaml の相互作用ロジック
    /// </summary>
    public partial class TimelineOverlay :
        Window,
        IOverlay,
        INotifyPropertyChanged
    {
        #region Design View

        private static TimelineOverlay designOverlay;

        private static TimelineModel dummyTimeline;

        public static TimelineModel BindingDummyTimeline => dummyTimeline;

        public static void ShowDesignOverlay(
            TimelineStyle testStyle = null)
        {
            if (designOverlay == null)
            {
                dummyTimeline = TimelineModel.CreateDummyTimeline(testStyle);
                dummyTimeline.Controller.RefreshActivityLineVisibility();

                designOverlay = CreateDesignOverlay();
                designOverlay.Model = dummyTimeline;
            }

            // 本番ビューを隠す
            if (TimelineView != null &&
                TimelineView.OverlayVisible)
            {
                TimelineView.OverlayVisible = false;
            }

            designOverlay.Show();
            designOverlay.OverlayVisible = true;
        }

        public static void HideDesignOverlay(
            bool restoreTimelineView = true)
        {
            if (designOverlay != null)
            {
                designOverlay.OverlayVisible = false;
                designOverlay.Hide();
                designOverlay.Close();
                designOverlay = null;

                // 本番ビューを復帰させるか？
                if (restoreTimelineView)
                {
                    if (TimelineSettings.Instance.OverlayVisible)
                    {
                        if (TimelineView != null &&
                            !TimelineView.OverlayVisible &&
                            TimelineView.Model != null)
                        {
                            TimelineView.OverlayVisible = true;
                        }
                    }
                }
            }
        }

        private static TimelineOverlay CreateDesignOverlay()
        {
            var overlay = new TimelineOverlay()
            {
                DummyMode = true
            };

            return overlay;
        }

        #endregion Design View

        #region View

        private static TimelineOverlay TimelineView { get; set; }

        public static void ShowTimeline(
            TimelineModel timelineModel)
        {
            if (!TimelineSettings.Instance.Enabled ||
                !TimelineSettings.Instance.OverlayVisible)
            {
                return;
            }

            // 有効な表示データが含まれていない？
            if (!timelineModel.ExistsActivities())
            {
                return;
            }

            WPFHelper.Invoke(() =>
            {
                if (TimelineView == null)
                {
                    TimelineView = new TimelineOverlay();
                    TimelineView.Show();
                }

                TimelineView.Model = timelineModel;

                ChangeClickthrough(TimelineSettings.Instance.Clickthrough);

                TimelineView.OverlayVisible = true;
            });
        }

        public static void ChangeClickthrough(
            bool isClickthrough)
        {
            if (TimelineView != null)
            {
                TimelineView.IsClickthrough = isClickthrough;
            }

            if (designOverlay != null)
            {
                designOverlay.IsClickthrough = isClickthrough;
            }
        }

        public static void CloseTimeline()
        {
            WPFHelper.Invoke(() =>
            {
                if (TimelineView != null)
                {
                    TimelineView.Model = null;
                    TimelineView.DataContext = null;
                    TimelineView.Close();
                    TimelineView = null;
                }
            });
        }

        #endregion View

        public TimelineOverlay()
        {
            if (WPFHelper.IsDesignMode)
            {
                this.Model = TimelineModel.DummyTimeline;
            }

            this.InitializeComponent();
            this.LoadResourcesDictionary();

            this.ToNonActive();

            this.MouseLeftButtonDown += (x, y) => this.DragMove();

            this.Opacity = 0;
            this.Topmost = false;

            this.Loaded += (x, y) =>
            {
                this.IsClickthrough = this.Config.Clickthrough;
                this.SubscribeZOrderCorrector();
            };
        }

        public bool DummyMode { get; set; } = false;

        private TimelineModel model;

        public TimelineModel Model
        {
            get => this.model;
            set => this.SetProperty(ref this.model, value);
        }

        public TimelineSettings Config => TimelineSettings.Instance;

        #region Resources Dictionary

        private void LoadResourcesDictionary()
        {
            const string Direcotry = @"Resources\Styles";
            const string Resources = @"TimelineOverlayResources.xaml";
            var file = Path.Combine(DirectoryHelper.FindSubDirectory(Direcotry), Resources);
            if (File.Exists(file))
            {
                this.Resources.MergedDictionaries.Add(new ResourceDictionary()
                {
                    Source = new Uri(file, UriKind.Absolute)
                });
            }
        }

        #endregion Resources Dictionary

        #region IOverlay

        private bool overlayVisible;

        public bool OverlayVisible
        {
            get => this.overlayVisible;
            set => this.SetOverlayVisible(ref this.overlayVisible, value, this.Config.OverlayOpacity);
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
                        this.ResizeMode = ResizeMode.NoResize;
                    }
                    else
                    {
                        this.ToNotTransparent();
                        this.ResizeMode = ResizeMode.CanResizeWithGrip;
                    }
                }
            }
        }

        #endregion IOverlay

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
