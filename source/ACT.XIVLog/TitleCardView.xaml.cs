using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using FFXIV.Framework.Common;
using FFXIV.Framework.WPF.Views;

namespace ACT.XIVLog
{
    /// <summary>
    /// TitleCardView.xaml の相互作用ロジック
    /// </summary>
    public partial class TitleCardView :
        Window,
        IOverlay,
        INotifyPropertyChanged
    {
        private static TitleCardView window;

        public static void ShowTitleCard(
            string title,
            int tryCount,
            DateTime recordingTime)
        {
            if (window == null)
            {
                window = new TitleCardView();
            }

            window.VideoTitle = title;
            window.TryCount = tryCount;
            window.RecordingTime = recordingTime;

            window.Show();

            if (!window.Config.IsAlwaysShow)
            {
                WPFHelper.BeginInvoke(async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(2.7));
                    window.Close();
                    window = null;
                });
            }
        }

        public static void CloseTitleCard()
        {
            if (window != null)
            {
                window.Close();
                window = null;
            }
        }

        public TitleCardView()
        {
            this.InitializeComponent();

            this.ToNonActive();
            this.MouseLeftButtonDown += (_, __) => this.DragMove();

            this.VideoTitle = "絶アレキサンダー討滅戦";
            this.TryCount = 1;
            this.RecordingTime = DateTime.Now;
        }

        public Config Config => Config.Instance;

        private bool overlayVisible;

        public bool OverlayVisible
        {
            get => this.overlayVisible;
            set => this.SetOverlayVisible(ref this.overlayVisible, value, 1.0);
        }

        private string videoTitle;

        public string VideoTitle
        {
            get => this.videoTitle;
            set => this.SetProperty(ref this.videoTitle, value);
        }

        private int tryCount;

        public int TryCount
        {
            get => this.tryCount;
            set => this.SetProperty(ref this.tryCount, value);
        }

        private DateTime recordingTime;

        public DateTime RecordingTime
        {
            get => this.recordingTime;
            set => this.SetProperty(ref this.recordingTime, value);
        }

        #region INotifyPropertyChanged

        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(
            [CallerMemberName]string propertyName = null) =>
            this.PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));

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
