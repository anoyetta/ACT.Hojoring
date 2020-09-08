using System;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;

namespace ACT.Hojoring.Common
{
    public class Hojoring
    {
        private static volatile bool isSplashShown = false;

        public Version Version => Assembly.GetExecutingAssembly().GetName().Version;

        private SplashWindow splash;

        public async void ShowSplash(
            string message = "")
        {
            if (isSplashShown)
            {
                return;
            }

            isSplashShown = true;

#if false
            var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (Directory.GetFiles(
                dir,
                "*NOSPLASH*",
                SearchOption.TopDirectoryOnly).Length > 0)
            {
                return;
            }
#endif

            await Application.Current.Dispatcher.InvokeAsync(
                () =>
                {
                    this.splash = new SplashWindow();
                    this.splash.Loaded += (_, __) => this.splash.StartFadeOut();
                    this.splash.Show();
                    this.splash.Activate();
                    this.splash.Message = message;
                },
                DispatcherPriority.Normal);
        }

        public string Message
        {
            get => this.splash?.Message;
            set
            {
                if (this.splash != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        this.splash.Message = value;
                    });
                }
            }
        }

        public bool IsSustainFadeOut
        {
            get => this.splash?.IsSustainFadeOut ?? false;
            set
            {
                if (this.splash != null)
                {
                    this.splash.IsSustainFadeOut = value;
                }
            }
        }
    }
}
