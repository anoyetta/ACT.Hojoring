using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;

namespace ACT.Hojoring.Common
{
    public class Hojoring
    {
        private static volatile bool isSplashShown = false;

        public Version Version => Assembly.GetExecutingAssembly().GetName().Version;

        public async void ShowSplash()
        {
            if (isSplashShown)
            {
                return;
            }

            isSplashShown = true;

            var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (Directory.GetFiles(
                dir,
                "*NOSPLASH*",
                SearchOption.TopDirectoryOnly).Length > 0)
            {
                return;
            }

            await Application.Current.Dispatcher.InvokeAsync(
                () =>
                {
                    var window = new SplashWindow();
                    window.Show();
                    window.StartFadeOut();
                },
                DispatcherPriority.Normal);
        }
    }
}
