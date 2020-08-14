using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using FFXIV.Framework.Common;

namespace FFXIV.Framework.resources
{
    public class ResourcesDownloader
    {
#if DEBUG
        private readonly bool IsDebugSkip = true;
#else
        private readonly bool IsDebugSkip = false;
#endif

        #region Lazy Singleton

        private static Lazy<ResourcesDownloader> LazyInstance = new Lazy<ResourcesDownloader>(() => new ResourcesDownloader());

        public static ResourcesDownloader Instance => LazyInstance.Value;

        private ResourcesDownloader()
        {
        }

        #endregion Lazy Singleton

        private static readonly Uri RemoteResourcesListUri =
            new Uri("https://raw.githubusercontent.com/anoyetta/ACT.Hojoring/master/resources/_list.txt");

        public async Task DownloadAsync()
        {
            if (this.IsDebugSkip)
            {
                return;
            }

            var splash = new ResourcesDownloaderView();
            splash.Show();

            try
            {
                var isDownloaded = false;

                using (var wc = new WebClient())
                {
                    var temp = Path.GetTempFileName();
                    File.Delete(temp);

                    await wc.DownloadFileTaskAsync(RemoteResourcesListUri, temp);
                    var list = File.ReadAllText(temp);
                    File.Delete(temp);

                    using (var sr = new StringReader(list))
                    {
                        while (sr.Peek() > -1)
                        {
                            var line = sr.ReadLine().Trim();

                            if (string.IsNullOrEmpty(line) ||
                                line.StartsWith("#"))
                            {
                                continue;
                            }

                            var values = line.Split(' ');
                            if (values.Length < 2)
                            {
                                continue;
                            }

                            var local = GetPath(values[0]);
                            var remote = values[1];
                            var isForceUpdate = false;

                            if (values.Length > 2)
                            {
                                bool.TryParse(values[2], out isForceUpdate);
                            }

                            if (File.Exists(local))
                            {
#if !DEBUG
                                if (!isForceUpdate)
                                {
                                    continue;
                                }
#endif
                                File.Delete(local);
                            }

                            AppLog.DefaultLogger.Info($"Download... {local}");

                            splash.CurrentResources = values[0];
                            splash.Activate();

                            FileHelper.CreateDirectory(local);

                            await wc.DownloadFileTaskAsync(
                                new Uri(remote),
                                local);

                            isDownloaded = true;
                            await Task.Delay(TimeSpan.FromSeconds(0.1));
                        }
                    }
                }

                splash.CurrentResources = "Completed!";
                splash.Activate();

                if (isDownloaded)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
            }
            finally
            {
                splash.Close();
            }
        }

        private static string GetPath(
            string path)
            => Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly()?.Location),
                path);
    }
}
