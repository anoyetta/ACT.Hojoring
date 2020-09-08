using System;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Reflection;
using System.Threading.Tasks;
using FFXIV.Framework.Common;

namespace FFXIV.Framework.resources
{
    public class ResourcesDownloader
    {
        private readonly bool IsDebugSkip = EnvironmentHelper.IsDebug ?
            true :
            false;

        #region Lazy Singleton

        private static Lazy<ResourcesDownloader> LazyInstance = new Lazy<ResourcesDownloader>(() => new ResourcesDownloader());

        public static ResourcesDownloader Instance => LazyInstance.Value;

        private ResourcesDownloader()
        {
        }

        #endregion Lazy Singleton

        private static readonly Uri RemoteResourcesListUri =
            EnvironmentHelper.IsDebug ?
            new Uri("https://raw.githubusercontent.com/anoyetta/ACT.Hojoring/develop/resources/_list.txt") :
            new Uri("https://raw.githubusercontent.com/anoyetta/ACT.Hojoring/master/resources/_list.txt");

        public async Task DownloadAsync()
        {
            if (this.IsDebugSkip)
            {
                return;
            }

            UpdateChecker.IsSustainSplash = true;

            var random = new Random();

            var isDownloaded = false;

            using (var wc = new WebClient()
            {
                CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore)
            })
            {
                var temp = GetTempFileName();

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
                        var md5Remote = values.Length >= 3 ? values[2] : string.Empty;
                        var isForceUpdate = false;

                        if (values.Length >= 4)
                        {
                            bool.TryParse(values[3], out isForceUpdate);
                        }

                        if (File.Exists(local))
                        {
                            UpdateChecker.SetMessageToSplash($"Checking... {Path.GetFileName(local)}");

                            if (isForceUpdate)
                            {
                                // NO-OP
                            }
                            else
                            {
                                var md5Local = FileHelper.GetMD5(local);

                                if (IsVerifyHash(md5Local, md5Remote))
                                {
                                    if (!EnvironmentHelper.IsDebug)
                                    {
                                        AppLog.DefaultLogger.Info($"Checking... {local}. It was up-to-date.");
                                        continue;
                                    }
                                }
                            }
                        }

                        FileHelper.CreateDirectory(local);

                        UpdateChecker.SetMessageToSplash($"Downloading... {Path.GetFileName(local)}");

                        temp = GetTempFileName();
                        await wc.DownloadFileTaskAsync(
                            new Uri($"{remote}?random={random.Next()}"),
                            temp);

                        var md5New = FileHelper.GetMD5(temp);

                        if (IsVerifyHash(md5New, md5Remote))
                        {
                            File.Copy(temp, local, true);
                            AppLog.DefaultLogger.Info($"Downloaded... {local}, verify is completed.");
                        }
                        else
                        {
                            if (EnvironmentHelper.IsDebug)
                            {
                                File.Copy(temp, local, true);
                            }

                            AppLog.DefaultLogger.Info($"Downloaded... {local}. Error, it was an inccorrect hash.");
                        }

                        File.Delete(temp);

                        isDownloaded = true;
                        await Task.Delay(TimeSpan.FromSeconds(0.1));
                    }
                }
            }

            UpdateChecker.SetMessageToSplash($"Resources update is now complete.");
            UpdateChecker.IsSustainSplash = false;

            if (isDownloaded)
            {
                await Task.Delay(TimeSpan.FromSeconds(0.3));
            }
        }

        private static string GetPath(
            string path)
            => Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly()?.Location),
                path);

        private static string GetTempFileName()
        {
            var temp = Path.GetTempFileName();
            File.Delete(temp);
            return temp;
        }

        private static bool IsVerifyHash(
            string hash1,
            string hash2)
        {
            if (string.IsNullOrEmpty(hash1) ||
                string.IsNullOrEmpty(hash2))
            {
                return true;
            }

            return string.Equals(hash1, hash2, StringComparison.OrdinalIgnoreCase);
        }
    }
}
