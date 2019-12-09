using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using FFXIV.Framework.Common;
using FFXIV.Framework.WPF.Views;

namespace ACT.SpecialSpellTimer.RaidTimeline.Views
{
    /// <summary>
    /// TimelineNoticeOverlay.xaml の相互作用ロジック
    /// </summary>
    public partial class TimelineNoticeOverlay :
        Window,
        IOverlay,
        INotifyPropertyChanged
    {
        #region Design View

        private static TimelineNoticeOverlay designOverlay;

        private static IList<TimelineVisualNoticeModel> dummyNoticeList;

        private static IList<TimelineVisualNoticeModel> BindingDummyNoticeList => dummyNoticeList;

        public static void ShowDesignOverlay(
            TimelineStyle testStyle = null)
        {
            if (designOverlay == null)
            {
                dummyNoticeList = TimelineVisualNoticeModel.CreateDummyNotices(testStyle);

                designOverlay = CreateDesignOverlay();
            }

            designOverlay.Show();
            designOverlay.OverlayVisible = true;

            foreach (var notice in dummyNoticeList)
            {
                designOverlay.AddNotice(notice, true);
            }
        }

        public static void HideDesignOverlay()
        {
            if (designOverlay != null)
            {
                designOverlay.OverlayVisible = false;
                designOverlay.Hide();
                designOverlay.Close();
                designOverlay = null;
            }
        }

        private static TimelineNoticeOverlay CreateDesignOverlay()
        {
            var overlay = new TimelineNoticeOverlay()
            {
                DummyMode = true
            };

            return overlay;
        }

        #endregion Design View

        #region View

        public static TimelineNoticeOverlay NoticeView { get; private set; }

        public static void ShowNotice()
        {
            if (!TimelineSettings.Instance.Enabled ||
                !TimelineSettings.Instance.OverlayVisible)
            {
                return;
            }

            WPFHelper.Invoke(() =>
            {
                if (NoticeView == null)
                {
                    NoticeView = new TimelineNoticeOverlay();
                    NoticeView.Show();
                }

                ChangeClickthrough(TimelineSettings.Instance.Clickthrough);
            });
        }

        public static void ChangeClickthrough(
            bool isClickthrough)
        {
            if (NoticeView != null)
            {
                NoticeView.IsClickthrough = isClickthrough;
            }

            if (designOverlay != null)
            {
                designOverlay.IsClickthrough = isClickthrough;
            }
        }

        public static void CloseNotice()
        {
            WPFHelper.Invoke(() =>
            {
                if (NoticeView != null)
                {
                    NoticeView.DataContext = null;
                    NoticeView.Close();
                    NoticeView = null;
                }
            });
        }

        #endregion View

        private readonly TimelineVisualNoticeModel FirstNotice = new TimelineVisualNoticeModel()
        {
            Text = "Loading...",
            TextToDisplay = "Loading...",
            Duration = 0.0,
            DurationVisible = false,
            StyleModel = TimelineSettings.Instance.DefaultNoticeStyle,
        };

        public TimelineNoticeOverlay()
        {
            this.InitializeComponent();

            this.ToNonActive();

            this.MouseLeftButtonDown += (x, y) => this.DragMove();

            this.Opacity = 0;
            this.Topmost = false;
            this.OverlayVisible = false;

            this.Loaded += (x, y) =>
            {
                this.IsClickthrough = this.Config.Clickthrough;
                this.SubscribeZOrderCorrector();

                // 描画用に最初の通知を装填する
                // ダミーのときは見えてしまうためフォントサイズを小さくしておく
                var style = TimelineSettings.Instance.DefaultNoticeStyle.Clone();
                style.Font.Size /= 2;
                this.FirstNotice.StyleModel = style;
                this.AddNotice(this.FirstNotice, false, true);
            };

            this.SetupNoticesSource();
        }

        public bool DummyMode { get; set; } = false;

        private readonly ObservableCollection<TimelineVisualNoticeModel> noticeList =
            new ObservableCollection<TimelineVisualNoticeModel>(
                new List<TimelineVisualNoticeModel>(32));

        public void AddNotice(
            TimelineVisualNoticeModel notice,
            bool dummyMode = false,
            bool init = false)
        {
            lock (this)
            {
                var same = this.noticeList.FirstOrDefault(x =>
                    x.IsVisible &&
                    x.TextToDisplay == notice.TextToDisplay);
                if (same != null)
                {
                    lock (same)
                    {
                        same.Timestamp = notice.Timestamp;
                        same.IncrementStack();
                    }

                    return;
                }

                notice.StartNotice(
                    (toRemove) =>
                    {
                        lock (this)
                        {
                            toRemove.ClearStack();
                            this.noticeList.Remove(toRemove);
                            this.OverlayVisible = this.noticeList.Any(x => x.IsVisible);
                        }
                    },
                    dummyMode);

                notice.IncrementStack();
                this.noticeList.Add(notice);
                this.EnsureTopMost();

                if (!init)
                {
                    this.OverlayVisible = true;
                }
            }
        }

        public void ClearNotice()
        {
            this.noticeList.Clear();
        }

        private CollectionViewSource noticesSource;

        public ICollectionView NoticeList => this.noticesSource?.View;

        public bool IsExistsNotice => !this.NoticeList?.IsEmpty ?? false;

        public TimelineSettings Config => TimelineSettings.Instance;

        private void SetupNoticesSource()
        {
            this.noticesSource = new CollectionViewSource()
            {
                Source = this.noticeList,
                IsLiveFilteringRequested = true,
                IsLiveSortingRequested = true,
            };

            this.noticesSource.Filter += (x, y) =>
            {
                y.Accepted = (y.Item as TimelineVisualNoticeModel).IsVisible;
            };

            this.noticesSource.LiveFilteringProperties.Add(nameof(TimelineVisualNoticeModel.IsVisible));

            this.noticesSource.SortDescriptions.AddRange(new[]
            {
                new SortDescription()
                {
                    PropertyName = nameof(TimelineVisualNoticeModel.Order),
                    Direction = ListSortDirection.Ascending,
                },
                new SortDescription()
                {
                    PropertyName = nameof(TimelineVisualNoticeModel.LogSeq),
                    Direction = ListSortDirection.Ascending,
                },
                new SortDescription()
                {
                    PropertyName = nameof(TimelineVisualNoticeModel.Timestamp),
                    Direction = ListSortDirection.Ascending,
                }
            });

            this.noticeList.CollectionChanged += (_, __) => this.RaisePropertyChanged(nameof(this.IsExistsNotice));

            this.RaisePropertyChanged(nameof(this.NoticeList));
            this.RaisePropertyChanged(nameof(this.IsExistsNotice));
        }

        public void RefreshNotices()
        {
            if (!this.Config.IsTimelineLiveUpdate)
            {
                WPFHelper.BeginInvoke(() =>
                {
                    this.noticesSource?.View?.Refresh();
                },
                DispatcherPriority.Background);
            }
        }

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
