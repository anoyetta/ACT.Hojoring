using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Forms.Integration;
using System.Windows.Media;
using System.Windows.Threading;
using ACT.SpecialSpellTimer.resources;
using FFXIV.Framework.Common;
using FFXIV.Framework.Globalization;
using FFXIV.Framework.WPF.Views;

namespace ACT.SpecialSpellTimer.RaidTimeline.Views
{
    /// <summary>
    /// TimelineImageNoticeOverlay.xaml の相互作用ロジック
    /// </summary>
    public partial class TimelineImageNoticeOverlay :
        Window,
        ILocalizable,
        IOverlay,
        INotifyPropertyChanged
    {
        #region Design View

        private static TimelineImageNoticeOverlay designOverlay;

        public static void ShowDesignOverlay()
        {
            if (designOverlay == null)
            {
                designOverlay = CreateDesignOverlay();
            }

            designOverlay.Show();
            designOverlay.OverlayVisible = true;
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

        private static TimelineImageNoticeOverlay CreateDesignOverlay()
        {
            var overlay = new TimelineImageNoticeOverlay()
            {
                DummyMode = true,
                Model = TimelineImageNoticeModel.DummyNotice,
            };

            overlay.WindowStartupLocation = overlay.Model.StartupLocation;

            // これがないとTextBoxに半角が入力できない
            ElementHost.EnableModelessKeyboardInterop(overlay);

            return overlay;
        }

        #endregion Design View

        #region View

        public void ShowNotice()
        {
            if (!TimelineSettings.Instance.Enabled ||
                !TimelineSettings.Instance.OverlayVisible)
            {
                return;
            }

            if (!this.OverlayVisible)
            {
                WPFHelper.Invoke(() =>
                {
                    this.IsClickthrough = this.Config.Clickthrough;
                    this.OverlayVisible = true;
                },
                DispatcherPriority.Normal);
            }
        }

        public static void ChangeClickthrough(
            bool isClickthrough)
        {
            if (designOverlay != null)
            {
                designOverlay.IsClickthrough = isClickthrough;
            }
        }

        public void CloseNotice()
        {
            WPFHelper.Invoke(() =>
            {
                this.Model = null;
                this.Close();
            });
        }

        #endregion View

        public TimelineImageNoticeOverlay()
        {
            this.InitializeComponent();
            this.LoadConfigViewResources();

            this.ToNonActive();

            this.MouseLeftButtonDown += (x, y) => this.DragMove();

            this.Opacity = 0;
            this.Topmost = false;
            this.OverlayVisible = false;

            this.Loaded += (x, y) =>
            {
                this.IsClickthrough = this.Config.Clickthrough;
                this.SubscribeZOrderCorrector();
                this.EnsureTopMost();
            };

#if DEBUG
            if (WPFHelper.IsDesignMode)
            {
                this.Model = TimelineImageNoticeModel.DummyNotice;
                this.DummyMode = true;
            }
#endif
        }

        private bool dummyMode = false;

        public bool DummyMode
        {
            get => this.dummyMode;
            set
            {
                if (this.SetProperty(ref this.dummyMode, value))
                {
                    this.RaisePropertyChanged(nameof(this.ImageBorderBrush));
                }
            }
        }

        public Brush ImageBorderBrush
            => this.DummyMode ?
                Brushes.Beige :
                Brushes.Transparent;

        public TimelineSettings Config => TimelineSettings.Instance;

        private TimelineImageNoticeModel model;

        public TimelineImageNoticeModel Model
        {
            get => this.model;
            set => this.SetProperty(ref this.model, value);
        }

        #region ILocalizable not implement

        public void SetLocale(Locales locale)
        {
            throw new NotImplementedException();
        }

        #endregion ILocalizable not implement

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
                    }
                    else
                    {
                        this.ToNotTransparent();
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
