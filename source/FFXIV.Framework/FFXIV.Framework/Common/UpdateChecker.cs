using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Threading;
using Advanced_Combat_Tracker;
using FFXIV.Framework.Extensions;
using FFXIV.Framework.WPF.Views;
using Microsoft.Win32;
using NLog;
using Octokit;

namespace FFXIV.Framework.Common
{
    /// <summary>
    /// Update Checker
    /// </summary>7
    public static class UpdateChecker
    {
        #region Logger

        private static Logger Logger => AppLog.DefaultLogger;

        #endregion Logger

        private static readonly object Locker = new object();

        private static dynamic hojoringInstance;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static dynamic GetHojoring()
        {
            const string HojoringTypeName = "ACT.Hojoring.Common.Hojoring";

            var obj = default(object);

            lock (Locker)
            {
                if (hojoringInstance != null)
                {
                    return hojoringInstance;
                }

                try
                {
#if DEBUG
                    // DEBUGビルド時に依存関係でDLLを配置させるためにタイプを参照する
                    new ACT.Hojoring.Common.Hojoring();
#endif
                    var t = Type.GetType(HojoringTypeName);

                    if (t == null)
                    {
                        var cd = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                        var hojoring = Path.Combine(cd, "ACT.Hojoring.Common.dll");
                        if (File.Exists(hojoring))
                        {
                            var asm = Assembly.LoadFrom(hojoring);
                            t = asm?.GetType(HojoringTypeName);
                        }
                    }

                    if (t != null)
                    {
                        obj = Activator.CreateInstance(t);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                    obj = null;
                }

                hojoringInstance = obj;
                return obj;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Version GetHojoringVersion()
        {
            var ver = default(Version);

            try
            {
                var hojoring = GetHojoring();
                if (hojoring != null)
                {
                    ver = hojoring.Version;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                ver = null;
            }

            return ver;
        }

        private static volatile bool isShowen = false;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ShowSplash()
        {
            if (isShowen)
            {
                return;
            }

            isShowen = true;

            try
            {
                // エントリアセンブリのパスを出力する
                var entry = Assembly.GetEntryAssembly().Location;
                Logger.Trace($"Entry {entry}");

                // ついでにFFXIV_ACT_Pluginのバージョンを出力する
                var ffxivPlugin = ActGlobals.oFormActMain?.ActPlugins?
                    .FirstOrDefault(
                        x => x.pluginFile.Name.ContainsIgnoreCase("FFXIV_ACT_Plugin"))?
                    .pluginFile.FullName;

                if (File.Exists(ffxivPlugin))
                {
                    var vi = FileVersionInfo.GetVersionInfo(ffxivPlugin);
                    if (vi != null)
                    {
                        Logger.Trace($"FFXIV_ACT_Plugin v{vi.FileMajorPart}.{vi.FileMinorPart}.{vi.FileBuildPart}.{vi.FilePrivatePart}");
                    }
                }

                // Hojoringのバージョンを出力しつつSPLASHを表示する
                var hojoring = GetHojoring();
                if (hojoring != null)
                {
                    var ver = hojoring.Version as Version;
                    if (ver != null)
                    {
                        Logger.Trace($"Hojoring v{ver.Major}.{ver.Minor}.{ver.Revision}");
                    }

                    hojoring.ShowSplash("initializing...");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void SetMessageToSplash(
            string message)
        {
            var hojoring = GetHojoring();
            if (hojoring != null)
            {
                hojoring.Message = message;
            }
        }

        public static bool IsSustainSplash
        {
            get => GetHojoring()?.IsSustainFadeOut ?? false;
            set
            {
                var hojoring = GetHojoring();
                if (hojoring != null)
                {
                    hojoring.IsSustainFadeOut = value;
                }
            }
        }

        /// <summary>
        /// チェック済み辞書
        /// </summary>
        private static readonly Dictionary<string, bool> checkedDictinary = new Dictionary<string, bool>();

        private static readonly Regex ReleaseVersionTitleRegex = new Regex(
            @"v(?<major>\d+)\.(?<minor>\d+)\.(?<revision>\d+)-?(?<build>[\d\.]*)",
            RegexOptions.Compiled);

        /// <summary>
        /// アップデートを行う
        /// </summary>
        /// <returns>メッセージ</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string Update(
            string repository,
            Assembly currentAssembly)
        {
            var r = string.Empty;

            try
            {
                SetMessageToSplash("Checking for update...");

                // TLSプロトコルを設定する
                EnvironmentHelper.SetTLSProtocol();

                var html = string.Empty;

                var client = new GitHubClient(new ProductHeaderValue("ACT.Hojoring"));

                if (GetHojoring() != null)
                {
                    repository = "ACT.Hojoring";
                }

                var releases = client.Repository.Release.GetAll("anoyetta", repository).Result;

                var lastest = releases.FirstOrDefault(x => !x.Prerelease);
                if (lastest == null)
                {
                    return r;
                }

                var lastestReleaseVersion = lastest.Name;

                // 現在のバージョンを取得する
                var currentVersion = currentAssembly.GetName().Version;

                // Hororingのバージョンに置き換える？
                var hojoringVer = GetHojoringVersion();
                if (hojoringVer != null)
                {
                    currentVersion = hojoringVer;
                }

                // バージョンを比較する
                if (!lastestReleaseVersion.ContainsIgnoreCase("FINAL"))
                {
                    var match = ReleaseVersionTitleRegex.Match(lastestReleaseVersion);
                    if (!match.Success)
                    {
                        Logger.Trace($"Update checker. Unknown release version.");
                        return r;
                    }

                    var values = lastestReleaseVersion.Replace("v", string.Empty).Split('.');
                    var remoteVersion = new Version(
                        int.Parse(match.Groups["major"].Value),
                        int.Parse(match.Groups["minor"].Value),
                        0,
                        int.Parse(match.Groups["revision"].Value));

                    if (remoteVersion <= currentVersion)
                    {
                        Logger.Trace($"Update checker. up to date.");
                        return r;
                    }
                }

                // チェック済み？
                if (checkedDictinary.ContainsKey(repository) &&
                    checkedDictinary[repository])
                {
                    return r;
                }

                // このURLはチェック済みにする
                checkedDictinary[repository] = true;

                var prompt = string.Empty;
                prompt += $"{repository} new version released !" + Environment.NewLine;
                prompt += "now: " + "v" + currentVersion.Major.ToString() + "." + currentVersion.Minor.ToString() + "." + currentVersion.Revision.ToString() + Environment.NewLine;
                prompt += "new: " + lastestReleaseVersion + Environment.NewLine;
                prompt += Environment.NewLine;
                prompt += "Do you want to Update ?";

                if (MessageBox.Show(prompt, repository, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) !=
                    DialogResult.Yes)
                {
                    return r;
                }

                UpdateChecker.StartUpdateScript();
            }
            catch (Exception ex)
            {
                r = $"Update Checker Error !\n\n{ex.ToString()}";
            }

            return r;
        }

        private static readonly string UpdateScriptUrl = "https://raw.githubusercontent.com/anoyetta/ACT.Hojoring/master/source/ACT.Hojoring.Updater/update_hojoring.ps1";

        /// <summary>
        /// アップデートスクリプトを起動する
        /// </summary>
        /// <param name="usePreRelease">
        /// プレリリースを取得するか？</param>
        public static async void StartUpdateScript(
            bool usePreRelease = false)
        {
            var cd = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var script = Path.Combine(cd, "update_hojoring.ps1");

            using (var web = new WebClient())
            {
                var temp = Path.GetTempFileName();
                File.Delete(temp);

                await web.DownloadFileTaskAsync(
                    UpdateScriptUrl,
                    temp);

                Thread.Sleep(10);
                File.Copy(temp, script, true);
            }

            if (File.Exists(script))
            {
                var args = $"-NoLog  -NoProfile -ExecutionPolicy Unrestricted -File \"{script}\" {usePreRelease}";

                Process.Start("powershell.exe", args);
            }
        }

        #region .NET Framework Version

        /// <summary>
        /// .NET Framework 4.7.1 のリリース番号
        /// </summary>
        private const int DoTNet471ReleaseNo = 461308;

        /// <summary>
        /// 最後にチェックした結果
        /// </summary>
        private static bool? lastResult;

        /// <summary>
        /// .NET Framework が有効か？
        /// </summary>
        /// <returns>bool</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool IsAvailableDotNet()
        {
            lock (Locker)
            {
                if (lastResult.HasValue)
                {
                    return lastResult.Value;
                }

                // 環境情報をダンプする
                DumpEnvironment();

                var result = IsDotNet471();
                lastResult = result;

                Logger.Info(
                    result ?
                    $".NET Framework is Available." :
                    $".NET Framework is OLD.");

                if (!result)
                {
                    var prompt = new StringBuilder();

                    prompt.AppendLine(".NET Framework not available.");
                    prompt.AppendLine("ACT.Hojoring requires .NET Framework 4.7.1 or later.");
                    prompt.AppendLine(string.Empty);
                    prompt.AppendLine("You can install newest .NET Framework from this site.");

                    MessageBox.Show(
                        prompt.ToString(),
                        "ACT.Hojoring",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation);

                    Process.Start("https://www.microsoft.com/net/download/windows");
                }

#if !DEBUG
                if (result)
                {
                    var location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    var references = Path.Combine(
                        location,
                        "bin");

                    if (!Directory.Exists(references))
                    {
                        result = false;
                        lastResult = false;

                        var prompt = new StringBuilder();

                        prompt.AppendLine("\"bin\" folder not found.");
                        prompt.AppendLine("Your setup is not complete.");
                        prompt.AppendLine(string.Empty);
                        prompt.AppendLine("Please check deployment of plugin.");

                        MessageBox.Show(
                            prompt.ToString(),
                            "ACT.Hojoring",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Exclamation);
                    }
                }
#endif

                return result;
            }
        }

        /// <summary>
        /// .NET Framework 4.7.1 がインストールされているか？
        /// </summary>
        /// <remarks>
        /// MSDN - 方法: インストールされている .NET Framework バージョンを確認する
        /// https://docs.microsoft.com/ja-jp/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed
        /// の方法で判定している。
        /// </remarks>
        /// <returns>bool</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool IsDotNet471() =>
            GetDotNetVersion() >= DoTNet471ReleaseNo;

        /// <summary>
        /// レジストリから.NET Frameworkのバージョンを取得する
        /// </summary>
        /// <returns>リリース番号</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int GetDotNetVersion()
        {
            const string subkey = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\";

            using (var ndpKey = RegistryKey
                .OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32)
                .OpenSubKey(subkey))
            {
                if (ndpKey != null &&
                    ndpKey.GetValue("Release") != null)
                {
                    var releaseNo = (int)ndpKey.GetValue("Release");
                    return releaseNo;
                }
            }

            return 0;
        }

        private static string GetRegistryValue(
            string keyname,
            string valuename)
            => Registry.GetValue(keyname, valuename, string.Empty).ToString();

        private const int Windows10BuildNo = 10240;
        private const int Windows81BuildNo = 9600;
        private static int osBuildNo;

        private static void DumpEnvironment()
        {
            var productName = GetRegistryValue(
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion",
                "ProductName");

            var releaseId = GetRegistryValue(
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion",
                "ReleaseId");

            var buildNo = GetRegistryValue(
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion",
                "CurrentBuild");

            var dotNetVersion = GetRegistryValue(
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full",
                "Version");

            var dotNetReleaseID = GetRegistryValue(
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full",
                "Release");

            int i;
            if (int.TryParse(buildNo, out i))
            {
                osBuildNo = i;
            }

            Logger.Info($"{productName} v{releaseId}, build {buildNo}");
            Logger.Info($".NET Framework v{dotNetVersion}, release {dotNetReleaseID}");
        }

        public static bool IsWindowsNewer =>
            osBuildNo >= Windows81BuildNo;

        private static volatile bool shownWindowsIsOld = false;

        public static bool IsAvailableWindows()
        {
            const string prompt1 = "Unsupported Operating System.";
            const string prompt2 = "Windows 10 or Later is Required.";

            var result = false;

            if (IsWindowsNewer)
            {
                result = true;
            }
            else
            {
                if (Config.Instance.SupportWin7)
                {
                    result = true;

                    if (!shownWindowsIsOld)
                    {
                        shownWindowsIsOld = true;
                        Logger.Warn($"{prompt1} {prompt2}");
                        Logger.Warn($"Support Win7 manualy, but you'd better update to Windows 10. https://www.microsoft.com/software-download/windows10");
                    }
                }
                else
                {
                    if (!shownWindowsIsOld)
                    {
                        shownWindowsIsOld = true;
                        Logger.Error($"{prompt1} {prompt2}");
                        WPFHelper.BeginInvoke(
                            () => ModernMessageBox.ShowDialog(
                                $"{prompt1}\n{prompt2}",
                                "ACT.Hojoring"),
                            DispatcherPriority.Normal);
                    }
                }
            }

            return result;
        }

        #endregion .NET Framework Version
    }
}
