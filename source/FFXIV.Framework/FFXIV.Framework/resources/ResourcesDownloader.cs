using System;
using System.IO;
using System.Linq;
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

        private static readonly (string Local, string Uri)[] RemoteResourcesFiles = new (string Local, string Uri)[]
        {
            (Local : @"bin\openJTalk\dic\sys.dic", Uri : "https://drive.google.com/uc?id=1DmyTtLe5yAL4oL2-dE53zz_TJNQoq6Oo"),
            (Local : @"bin\openJTalk\voice\man_m001.htsvoice", Uri : "https://drive.google.com/uc?id=1gGnWc8J-AVXt4zgsvnl067xmvYkAQlGN"),
            (Local : @"bin\openJTalk\voice\mei_angry.htsvoice", Uri : "https://drive.google.com/uc?id=1O8nWOCc52BirB1z5UhSzaYp13CABqs23"),
            (Local : @"bin\openJTalk\voice\mei_bashful.htsvoice", Uri : "https://drive.google.com/uc?id=1YHtb3Ekbcsu886y1PdpT9vPmqSWo-ILG"),
            (Local : @"bin\openJTalk\voice\mei_happy.htsvoice", Uri : "https://drive.google.com/uc?id=1feBrBX3TMRRhpaMSxiWyXa5rAyUhjOpK"),
            (Local : @"bin\openJTalk\voice\mei_normal.htsvoice", Uri : "https://drive.google.com/uc?id=1QvV_UvrN2bk74O7F3FhMVgOsRA2AMtN_"),
            (Local : @"bin\openJTalk\voice\mei_sad.htsvoice", Uri : "https://drive.google.com/uc?id=11fbO9WEDliq78uFwyI06DTuxOkUxQWeb"),
            (Local : @"bin\openJTalk\voice\tohoku-f01-angry.htsvoice", Uri : "https://drive.google.com/uc?id=1tiRgFC-V9mOqJEl6AIaOuH-GWGpvezaM"),
            (Local : @"bin\openJTalk\voice\tohoku-f01-happy.htsvoice", Uri : "https://drive.google.com/uc?id=1sUHx5JxVrzHqMazuIzKHBI3DXU3Nl4sK"),
            (Local : @"bin\openJTalk\voice\tohoku-f01-neutral.htsvoice", Uri : "https://drive.google.com/uc?id=15YWIIVnHF-PO9wxa65si75Mt2WW3SXVV"),
            (Local : @"bin\openJTalk\voice\tohoku-f01-sad.htsvoice", Uri : "https://drive.google.com/uc?id=1sRBGxA48c3UqKgMZNGnuANLP6uXUGY7X"),
            (Local : @"bin\openJTalk\voice\type-A.htsvoice", Uri : "https://drive.google.com/uc?id=1SpAuVpfWxwZiD8qlkryGVacGySNBD6E2"),
            (Local : @"bin\openJTalk\voice\type-B.htsvoice", Uri : "https://drive.google.com/uc?id=1L4Z1Q3OPyYZaesh8-jeuDdF-76hZqi5U"),
            (Local : @"bin\openJTalk\voice\type-G.htsvoice", Uri : "https://drive.google.com/uc?id=1CQgynTdOOkPxiNbdsHTn4aAwS66ObeOR"),
            (Local : @"bin\openJTalk\voice\type-T.htsvoice", Uri : "https://drive.google.com/uc?id=1EyxeLxMwlg4I87HywnAA9JGEXssb-GYV"),
            (Local : @"bin\yukkuri\aq_dic\aqdic.bin", Uri : "https://drive.google.com/uc?id=1LSiXo-C88QhFVW0Wc9aRNWQm_Pz6I2qr"),
            (Local : @"bin\lib\grpc_csharp_ext.x64.dll", Uri : "https://drive.google.com/uc?id=1-qVlIokg-XocmLTQPWmr4KSjz3Yc45Am"),
            (Local : @"bin\lib\grpc_csharp_ext.x86.dll", Uri : "https://drive.google.com/uc?id=1PQjQtDiSA0K0qAQTg0Q3gpbY6GFGUUch"),
            (Local : @"bin\lib\libopus.dll", Uri : "https://drive.google.com/uc?id=1mNx5YnoNwz_sktXv891TYbBZi6f4XNMh"),
            (Local : @"bin\lib\libsodium.dll", Uri : "https://drive.google.com/uc?id=16GRv_xOIl1opXnC8gL1MHssBUQiVUX2i"),
        };

        public bool IsReady() => EnvironmentHelper.IsDebug ?
            false :
            RemoteResourcesFiles.All(x => File.Exists(GetPath(x.Local)));

        public async Task DownloadAsync()
        {
            if (this.IsDebugSkip)
            {
                return;
            }

            if (this.IsReady())
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
                    foreach (var resources in RemoteResourcesFiles)
                    {
                        var local = GetPath(resources.Local);

                        if (File.Exists(local))
                        {
#if DEBUG
                            File.Delete(local);
#else
                            continue;
#endif
                        }

                        AppLog.DefaultLogger.Info($"Download... {resources.Local}");

                        splash.CurrentResources = resources.Local;
                        splash.Activate();

                        FileHelper.CreateDirectory(local);

                        await wc.DownloadFileTaskAsync(
                            new Uri(resources.Uri),
                            local);

                        isDownloaded = true;
                        await Task.Delay(TimeSpan.FromSeconds(0.1));
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
