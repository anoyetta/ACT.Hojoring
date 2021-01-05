using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace ACT.Hojoring.Common
{
    /// <summary>
    /// SplashWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class SplashWindow : Window, INotifyPropertyChanged
    {
#if DEBUG
        private static readonly bool IsDebug = true;
#else
        private static readonly bool IsDebug = false;
#endif

        public string FFXIVVersion => "for patch 5.4x";

        public static Version HojoringVersion => Assembly.GetExecutingAssembly()?.GetName()?.Version;

        private static readonly TimeSpan SplashDuration = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan FadeOutDuration = TimeSpan.FromSeconds(2);
        private DateTime FadeOutStartTime;

        private readonly DoubleAnimation OpacityAnimation = new DoubleAnimation(
            0,
            new Duration(FadeOutDuration));

        private FontFamily reiwaFont;

        public FontFamily ReiwaFont => this.reiwaFont ?? (this.reiwaFont = this.CreateReiwaFont());

        private FontFamily CreateReiwaFont()
            => new FontFamily(
                new Uri("pack://application:,,,/ACT.Hojoring.Common;component/fonts/"),
                "./#HakusyuGyosyo_kk");

        public bool IsSustainFadeOut { get; set; }

        private string message;

        public string Message
        {
            get => this.message;
            set
            {
                if (this.SetProperty(ref this.message, value))
                {
                    this.FadeOutStartTime = DateTime.Now.Add(SplashDuration);
                }
            }
        }

        #region Colors

        private readonly static ColorConverter ColorConverter = new ColorConverter();

        private readonly static string[] MainColors = new[]
        {
            "#745399",
            "#895b8a",
            "#824880",
            "#006e54",
            "#e6b422",
            "#d9a62e",
            "#96514d",
            "#6e7955",
            "#5a544b",
            "#cd5e3c",
            "#6a5d21",
            "#e83929",
            "#e60033",
            "#dccb18",
            "#2a83a2",
            "#2ca9e1",
            "#007bbb",
            "#640125",
            "#839b5c",
            "#f08300",
            "#1e50a2",
            "#0f2350",
            "#4c6cb3",
            "#ea5506",
            "#ffd900",
            "#007b43",
            "#7b7c7d",
            "#524e4d",
            "#6c2735",
            "#72640c",
            "#665a1a",
            "#bf783e",
            "#c5a05a",
            "#d70035",
            "#e95464",
            "#c70067",
            "#7f1184",
            "#eddc44",
            "#f39700",
            "#e60012",
            "#9caeb7",
            "#00a7db",
            "#009944",
            "#d7c447",
            "#9b7cb6",
            "#00ada9",
            "#bb641d",
            "#e85298",
            "#0079c2",
            "#6cbb5a",
            "#b6007a",
            "#e5171f",
            "#522886",
            "#0078ba",
            "#019a66",
            "#e44d93",
            "#814721",
            "#a9cc51",
            "#ee7b1a",
            "#00a0de",
            "#ffd900",
            "#ffec47",
            "#fcc800",
            "#f8b500",
            "#fabf14",
            "#e6b422",
            "#e45e32",
            "#ffd700",
            "#e6b422",
            "#b98c46",
            "#ec6800",
            "#ea5506",
            "#f39800",
            "#f8e58c",
            "#fddea5",
            "#f19072",
            "#df7163",
            "#ddbb99",
            "#e9bc00",
            "#fff352",
            /*
            "XXXXXXX",
            */
        };

        private static readonly Random Random = new Random();

        private static string GetColor() => MainColors[Random.Next(0, MainColors.Length - 1)];

        public Brush MainBrush => new SolidColorBrush((Color)ColorConverter.ConvertFromString(GetColor()));

        #endregion Colors

        public SplashWindow()
        {
            this.InitializeComponent();

            this.ToNonActive();
            this.ToTransparent();

            this.Topmost = IsDebug;

            this.DEBUGLabel.Visibility = IsDebug ? Visibility.Visible : Visibility.Collapsed;

            var ver = HojoringVersion;
            if (ver != null)
            {
                this.VersionLabel.Content = $"v{ver.Major}.{ver.Minor}.{ver.Revision}";
            }

            Timeline.SetDesiredFrameRate(this.OpacityAnimation, 30);

            this.OpacityAnimation.Completed += (x, y) => this.Close();
        }

        public async void StartFadeOut()
        {
            this.FadeOutStartTime = DateTime.Now.Add(SplashDuration);

            await Task.Run(async () =>
            {
                while (DateTime.Now <= this.FadeOutStartTime || this.IsSustainFadeOut)
                {
                    await Task.Delay(200);
                }
            });

            await Application.Current.Dispatcher.InvokeAsync(
                () => this.BeginAnimation(
                    Window.OpacityProperty,
                    this.OpacityAnimation),
                DispatcherPriority.Normal);
        }

        /// <summary>
        /// アクティブにさせない
        /// </summary>
        /// <param name="x">Window</param>
        public void ToNonActive()
        {
            this.SourceInitialized += (s, e) =>
            {
                // Get this window's handle
                var hwnd = new WindowInteropHelper(this).Handle;

                // Change the extended window style to include WS_EX_TRANSPARENT
                var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);

                SetWindowLong(
                    hwnd,
                    GWL_EXSTYLE,
                    extendedStyle | WS_EX_NOACTIVATE);
            };
        }

        /// <summary>
        /// Click透過させる
        /// </summary>
        /// <param name="x">対象のWindow</param>
        public void ToTransparent()
        {
            // Get this window's handle
            var hwnd = new WindowInteropHelper(this).Handle;

            // Change the extended window style to include WS_EX_TRANSPARENT
            var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);

            SetWindowLong(
                hwnd,
                GWL_EXSTYLE,
                extendedStyle | WS_EX_TRANSPARENT);
        }

        #region INotifyPropertyChanged

        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(
            [CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }

        protected virtual bool SetProperty<T>(
            ref T field,
            T value,
            [CallerMemberName] string propertyName = null)
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

        #region Win32 API

        public const int GWL_EXSTYLE = (-20);

        public const int WS_EX_NOACTIVATE = 0x08000000;
        public const int WS_EX_TRANSPARENT = 0x00000020;

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        #endregion Win32 API
    }
}
