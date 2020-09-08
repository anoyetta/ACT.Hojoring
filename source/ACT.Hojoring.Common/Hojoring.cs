using System;
using System.Reflection;
using System.Windows;

namespace ACT.Hojoring.Common
{
    public class Hojoring
    {
        private static volatile bool isSplashShown = false;

        public Version Version => Assembly.GetExecutingAssembly().GetName().Version;

        private SplashWindow splash;

        public void ShowSplash(
            string message = "")
        {
            lock (this)
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

                this.splash = new SplashWindow();
                this.splash.Loaded += (_, __) => this.splash.StartFadeOut();
                this.splash.Show();
                this.splash.Activate();
                this.splash.Message = message;
            }
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
