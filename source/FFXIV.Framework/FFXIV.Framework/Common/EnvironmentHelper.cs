using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace FFXIV.Framework.Common
{
    public static class EnvironmentHelper
    {
        private static volatile bool isGarbaged = false;

        public static void SetTLSProtocol()
        {
            // TLS1.0, 1.1 を無効化する
            ServicePointManager.SecurityProtocol &= ~SecurityProtocolType.Tls;
            ServicePointManager.SecurityProtocol &= ~SecurityProtocolType.Tls11;

            // TLS1.2を有効にする
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        }

        public static async void GarbageLogs() => await Task.Run(() =>
        {
            if (isGarbaged)
            {
                return;
            }

            isGarbaged = true;

            var appdata = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                @"anoyetta\ACT");

            if (!Directory.Exists(appdata))
            {
                return;
            }

            Directory.GetFiles(appdata, "*.bak", SearchOption.TopDirectoryOnly)
                .Walk((file) =>
                {
                    File.Delete(file);
                });

            Directory.GetFiles(appdata, "*.log", SearchOption.TopDirectoryOnly)
                .Walk((file) =>
                {
                    File.Delete(file);
                });

            var logs = Path.Combine(appdata, "logs");
            if (Directory.Exists(logs))
            {
                Directory.GetFiles(logs, "*.log", SearchOption.TopDirectoryOnly)
                    .Walk((file) =>
                    {
                        var timestamp = File.GetCreationTime(file);
                        if ((DateTime.Now - timestamp).TotalDays > 30)
                        {
                            File.Delete(file);
                        }
                    });
            }

            var archives = Path.Combine(logs, "archives");
            if (Directory.Exists(archives))
            {
                Directory.Delete(archives, true);
            }
        });

        public static string GetAppDataPath()
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                GetCompanyName() + "\\" + GetProductName());

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            return path;
        }

        public static string GetProductName()
        {
            var atr = (AssemblyProductAttribute)Attribute.GetCustomAttribute(
                Assembly.GetEntryAssembly(),
                typeof(AssemblyProductAttribute));

            return atr != null ? atr.Product : "UNKNOWN";
        }

        public static string GetCompanyName()
        {
            var atr = (AssemblyCompanyAttribute)Attribute.GetCustomAttribute(
                Assembly.GetEntryAssembly(),
                typeof(AssemblyCompanyAttribute));

            return atr != null ? atr.Company : "UNKNOWN";
        }

        public static Version GetVersion()
        {
            return Assembly.GetEntryAssembly().GetName().Version;
        }

        public static string ToStringShort(
            this Version version)
        {
            var v =
                "v" +
                version.Major.ToString() + "." +
                version.Minor.ToString() + "." +
                version.Revision.ToString();

            return v;
        }
    }
}
