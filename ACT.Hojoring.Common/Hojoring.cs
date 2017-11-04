using System;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;

namespace ACT.Hojoring.Common
{
    public class Hojoring
    {
        private static volatile bool isSplashShown = false;

        public string LastestReleaseUrl => @"https://github.com/anoyetta/ACT.Hojoring/releases/latest";

        public Version Version => Assembly.GetExecutingAssembly().GetName().Version;

        public void ShowSplash()
        {
            if (isSplashShown)
            {
                return;
            }

            isSplashShown = true;

            var window = default(SplashWindow);

            Application.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new Action(() =>
                {
                    window = new SplashWindow();
                    window.Show();
                }));
        }
    }
}
