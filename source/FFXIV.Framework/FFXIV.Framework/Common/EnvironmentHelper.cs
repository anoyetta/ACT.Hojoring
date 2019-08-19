using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using ACT.Hojoring.Activator;
using FFXIV.Framework.XIVHelper;

namespace FFXIV.Framework.Common
{
    public static class EnvironmentHelper
    {
        public static string Pwsh => LazyPwsh.Value;

        private static readonly Lazy<string> LazyPwsh = new Lazy<string>(() => GetPwsh());

        private static string GetPwsh()
        {
            var result = "powershell.exe";

            var path = Environment.GetEnvironmentVariable("Path");
            var values = path.Split(';');

            foreach (var dir in values)
            {
                var pwsh = Path.Combine(dir, "pwsh.exe");
                if (File.Exists(pwsh))
                {
                    result = pwsh;
                    break;
                }
            }

            return result;
        }

        private static volatile bool isGarbaged = false;

        public static void SetTLSProtocol()
        {
            // 同時接続数を増やしておく
            ServicePointManager.DefaultConnectionLimit = 32;

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

        private static string GetPublicKeyToken(
            Assembly assembly)
        {
            var bytes = assembly.GetName().GetPublicKeyToken();
            if (bytes == null || bytes.Length == 0)
            {
                return "None";
            }

            var publicKeyToken = string.Empty;
            for (int i = 0; i < bytes.GetLength(0); i++)
            {
                publicKeyToken += string.Format("{0:x2}", bytes[i]);
            }

            return publicKeyToken;
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

        private static volatile bool isStarted;
        public static readonly string ActivationDenyMessage = "Hojoring is not allowed for you.";

        private static readonly List<Action> ActivationDeniedCallbackList = new List<Action>();

        public static void StartActivator(
            Action callback)
        {
            SetTLSProtocol();

            if (!isStarted)
            {
                ActivationManager.Instance.ActivationDeniedCallback = OnActivationDenied;

                AppLog.DefaultLogger.Trace("Activator started.");
                AppLog.DefaultLogger.Trace(typeof(ActivationManager).Assembly.ToString());
            }

            isStarted = true;

            ActivationDeniedCallbackList.Add(callback);
            ActivationManager.Instance.Start(
                () =>
                    CombatantsManager.Instance.PartyCount >= 4 ||
                    XIVPluginHelper.Instance.InCombat);
        }

        internal static bool TryActivation(
            string name,
            string server,
            string guild)
        {
            if (!IsValidActivationManager())
            {
                return false;
            }

            if (ActivationManager.Instance.ActivationDeniedCallback == null)
            {
                ActivationManager.Instance.ActivationDeniedCallback = OnActivationDenied;
            }
            else
            {
                var info1 = typeof(EnvironmentHelper).GetMethod(
                    nameof(OnActivationDenied),
                    BindingFlags.NonPublic | BindingFlags.Static);

                var info2 = ActivationManager.Instance.ActivationDeniedCallback.Method;

                if (info1 != info2)
                {
                    ActivationManager.Instance.ActivationDeniedCallback = OnActivationDenied;
                }
            }

            return ActivationManager.Instance.TryActivation(name, server, guild);
        }

        private static void OnActivationDenied()
        {
            AppLog.DefaultLogger.Fatal(ActivationDenyMessage);

            WPFHelper.Invoke(() =>
            {
                foreach (var callback in ActivationDeniedCallbackList)
                {
                    callback.Invoke();
                }
            });
        }

        private const string ActivatorPublicKeyToken = "a90fa5bb6d544c25";

        private static bool IsValidActivationManager()
        {
            var asm = typeof(ActivationManager).Assembly;
            if (asm == null)
            {
                return false;
            }

            var token = GetPublicKeyToken(asm);

            return string.Equals(
                token,
                ActivatorPublicKeyToken,
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
