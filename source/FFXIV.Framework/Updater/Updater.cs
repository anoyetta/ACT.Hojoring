using Advanced_Combat_Tracker;
using FFXIV.Framework.Common;
using Markdig;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FFXIV.Framework.Updater
{
    /// <summary>
    /// FFXIV.Framework および ACT.Hojoring のアップデート（ダウンロード・展開・配置準備）を管理するクラスです。
    /// .7z形式の展開には外部ツール(7za.exe)を使用します。
    /// </summary>
    public class FFXIVFrameworkUpdater : IDisposable
    {
        #region Singleton
        private static FFXIVFrameworkUpdater instance;
        private static readonly object lockObj = new object();

        public static FFXIVFrameworkUpdater Instance
        {
            get
            {
                lock (lockObj)
                {
                    return instance ?? (instance = new FFXIVFrameworkUpdater());
                }
            }
        }

        private FFXIVFrameworkUpdater() { }
        #endregion

        #region Constants & Fields
        private const string GitHubRepo = "anoyetta/ACT.Hojoring";
        private const string UserAgent = "FFXIV-Framework-Updater";

        public const string NewFileSuffix = ".new";
        public const string OldFileSuffix = ".old";

        private bool disposed = false;

        /// <summary>
        /// アップデート（ファイル置換）の直前に実行されるアクション。
        /// </summary>
        public Action OnBeforeUpdate { get; set; }
        #endregion
        public Action<string, Exception> Logger { get; set; }

        private void Log(string message, Exception ex = null)
        {
            this.Logger?.Invoke(message, ex);
        }

        #region Win32 API
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteFile(string lpFileName);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool MoveFileEx(string lpExistingFileName, string lpNewFileName, uint dwFlags);

        private const uint MOVEFILE_DELAY_UNTIL_REBOOT = 0x00000004;
        #endregion

        #region Data Models
        public class UpdateTarget
        {
            public string AssetKeyword { get; set; }
            public string DisplayName { get; set; }
            public string PluginDirectory { get; set; }
            public Version CurrentVersion { get; set; }
            public bool IsFullPackage { get; set; } = false;
            public int StrippedDirs { get; set; } = 0;
            public string Repo { get; set; } = GitHubRepo;
        }

        public class GitHubRelease
        {
            [JsonProperty("tag_name")] public string TagName { get; set; }
            [JsonProperty("name")] public string Name { get; set; }
            [JsonProperty("html_url")] public string HtmlUrl { get; set; }
            [JsonProperty("body")] public string Body { get; set; }
            [JsonProperty("prerelease")] public bool Prerelease { get; set; }
            [JsonProperty("assets")] public List<GitHubAsset> Assets { get; set; }
        }

        public class GitHubAsset
        {
            [JsonProperty("name")] public string Name { get; set; }
            [JsonProperty("browser_download_url")] public string DownloadUrl { get; set; }
        }
        #endregion

        public string TempDir { get; private set; }
        private string _destDir;

        public void InitializePaths(string dest, string tmpName)
        {
            _destDir = dest;
            string parent = Path.GetDirectoryName(dest);
            if (string.IsNullOrEmpty(parent)) parent = AppDomain.CurrentDomain.BaseDirectory;
            TempDir = Path.Combine(parent, tmpName);
        }

        public static void DeleteOldFiles(string dir)
        {
            if (!Directory.Exists(dir)) return;
            try
            {
                var oldFiles = Directory.GetFiles(dir, "*" + OldFileSuffix, SearchOption.AllDirectories);
                foreach (var old in oldFiles)
                {
                    try
                    {
                        File.SetAttributes(old, FileAttributes.Normal);
                        File.Delete(old);
                    }
                    catch { }
                }
            }
            catch { }
        }

        #region Progress Dialog
        public class ProgressDialog : Form
        {
            private ProgressBar progressBar;
            private RichTextBox logBox;
            private WebBrowser releaseNoteBox;
            private Button closeButton;
            private TaskCompletionSource<bool> _dialogCloseTask = new TaskCompletionSource<bool>();
            public Task WaitForClose => _dialogCloseTask.Task;

            public ProgressDialog(string title)
            {
                this.Text = $"{title} - Update Progress";
                this.Size = new Size(750, 600);
                this.StartPosition = FormStartPosition.CenterScreen;
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.TopMost = true;

                progressBar = new ProgressBar { Dock = DockStyle.Top, Height = 25, Minimum = 0, Maximum = 100 };
                logBox = new RichTextBox { Dock = DockStyle.Bottom, Height = 180, ReadOnly = true, BackColor = Color.Black, ForeColor = Color.White };

                Panel bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 45, Padding = new Padding(5) };
                closeButton = new Button { Text = "Close", Dock = DockStyle.Right, Width = 100, Enabled = false };
                closeButton.Click += (s, e) => this.Close();
                bottomPanel.Controls.Add(closeButton);

                releaseNoteBox = new WebBrowser { Dock = DockStyle.Fill };

                this.Controls.Add(releaseNoteBox);
                this.Controls.Add(progressBar);
                this.Controls.Add(logBox);
                this.Controls.Add(bottomPanel);
                this.FormClosed += (s, e) => _dialogCloseTask.TrySetResult(true);
            }

            public void UpdateProgress(int value) => this.SafeInvoke(() => progressBar.Value = Math.Min(100, Math.Max(0, value)));
            public void Log(string message) => this.SafeInvoke(() => { logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n"); logBox.ScrollToCaret(); });
            public void EnableCloseButton() => this.SafeInvoke(() => closeButton.Enabled = true);

            public void SetReleaseNotes(string tagName, string markdown)
            {
                // 改行コードを正規化し、Markdownとして適切に処理されるようにする
                // GitHubのBodyは \r\n だったり \n だったりするため、一度 \n に統一してから
                // Markdig のパイプラインオプション（Configure）で改行をそのままHTMLに反映させる設定を使用する
                var rawMarkdown = (markdown ?? "").Replace("\r\n", "\n").Replace("\r", "\n");

                // Markdigの設定でソフト改行をハード改行として扱う（GitHubに近い挙動）
                var pipeline = new MarkdownPipelineBuilder()
                    .UseAdvancedExtensions()
                    .UseSoftlineBreakAsHardlineBreak()
                    .Build();

                var htmlContent = Markdown.ToHtml(rawMarkdown, pipeline);
                var fullHtml = $@"
<html>
<head>
<meta http-equiv='X-UA-Compatible' content='IE=edge' />
<style>
    body {{ font-family: 'Segoe UI', 'Meiryo', sans-serif; font-size: 10pt; line-height: 1.6; padding: 15px; color: #333; }}
    h1.release-tag {{ color: #0056b3; border-bottom: 2px solid #0056b3; padding-bottom: 10px; margin-top: 0; font-size: 18pt; }}
    h2, h3 {{ border-bottom: 1px solid #ddd; padding-bottom: 5px; color: #444; margin-top: 20px; }}
    code {{ background-color: #f0f0f0; padding: 2px 4px; border-radius: 3px; font-family: 'Consolas', monospace; }}
    pre {{ background-color: #f8f8f8; padding: 10px; border-radius: 5px; overflow-x: auto; border: 1px solid #eee; }}
    ul, ol {{ padding-left: 25px; }}
    li {{ margin-bottom: 4px; }}
</style>
</head>
<body>
    <h1 class='release-tag'>{tagName}</h1>
    {htmlContent}
</body>
</html>";
                this.SafeInvoke(() => releaseNoteBox.DocumentText = fullHtml);
            }
            private void SafeInvoke(Action action) { if (!this.IsDisposed && this.IsHandleCreated) { if (this.InvokeRequired) this.Invoke(action); else action(); } }
        }
        #endregion

        public async Task<bool> CheckAndDoUpdate(UpdateTarget target, bool usePreRelease = false)
        {
            try
            {
                this.Log($"[Updater] Checking for updates: {target.DisplayName}");

                var releases = await FetchAllReleasesAsync(target.Repo);
                var latest = usePreRelease ? releases?.FirstOrDefault() : releases?.FirstOrDefault(x => !x.Prerelease);
                if (latest == null) return false;

                // --- Parsing Remote Version ---
                // Example: "v10.5.3-260131" -> "10.5.3"
                var versionRaw = latest.TagName.TrimStart('v').Split('-')[0];
                var versionMatch = Regex.Match(versionRaw, @"[\d\.]+");
                var versionString = versionMatch.Success ? versionMatch.Value.Trim('.') : string.Empty;

                if (string.IsNullOrEmpty(versionString) || !Version.TryParse(versionString, out Version latestVersion))
                {
                    this.Log($"[Updater] Failed to parse version from tag: {latest.TagName}");
                    return false;
                }

                // --- Parsing Current Assembly Version ---
                var assembly = Assembly.GetAssembly(typeof(ACT.Hojoring.Common.Hojoring)) ?? Assembly.GetExecutingAssembly();
                var rawAssemblyVersion = target.CurrentVersion ?? assembly.GetName().Version;

                // Fix: 10.5.3 becoming 10.5.0.3 issue
                // Convert to string and re-parse to ensure Major.Minor.Build consistency with GitHub tags
                var assemblyVersionStr = rawAssemblyVersion.ToString();
                var assemblyMatch = Regex.Match(assemblyVersionStr, @"^\d+\.\d+\.\d+"); // Get only X.Y.Z
                var normalizedVersionStr = assemblyMatch.Success ? assemblyMatch.Value : assemblyVersionStr;

                if (!Version.TryParse(normalizedVersionStr, out Version currentVersion))
                {
                    currentVersion = rawAssemblyVersion; // Fallback to raw if parsing fails
                }

                // If no newer version and not forced re-install, exit
                if (latestVersion < currentVersion) return false;

                // --- アップデート実行確認の追加 ---
                var isNewer = latestVersion > currentVersion;
                var statusMsg = isNewer ? "新しいバージョンが見つかりました。" : "現在のバージョンは最新です。";
                var confirmMsg = $"{target.DisplayName}\n\n{statusMsg}\n最新バージョン: {latest.TagName}\n現在のバージョン: {currentVersion}\n\nアップデート(再インストール)を実行しますか？";

                var result = (DialogResult)ActGlobals.oFormActMain.Invoke((Func<DialogResult>)(() =>
                {
                    return MessageBox.Show(
                        ActGlobals.oFormActMain,
                        confirmMsg,
                        "Update Confirmation",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);
                }));

                if (result != DialogResult.Yes) return false;

                var asset = latest.Assets.FirstOrDefault(a => a.Name.Contains(target.AssetKeyword) && a.Name.EndsWith(".7z"))
                           ?? latest.Assets.FirstOrDefault(a => a.Name.Contains(target.AssetKeyword) && a.Name.EndsWith(".zip"));

                if (asset == null) return false;

                InitializePaths(target.PluginDirectory, target.DisplayName + ".tmp");

                return await (Task<bool>)ActGlobals.oFormActMain.Invoke((Func<Task<bool>>)(async () =>
                {
                    using (var dialog = new ProgressDialog(target.DisplayName))
                    {
                        dialog.Show();
                        dialog.SetReleaseNotes(latest.TagName, latest.Body);
                        dialog.Log($"Target version: {latest.TagName}");

                        var (success, failedFiles) = await PerformUpdateTask(asset.DownloadUrl, asset.Name, target, dialog);

                        if (success)
                        {
                            if (failedFiles.Count > 0)
                            {
                                dialog.Log("Preparation complete. Some files are pending.");
                                dialog.EnableCloseButton();
                                ACT.Hojoring.AtomicUpdater.RequestExternalUpdate();
                                MessageBox.Show(dialog, "一部のファイルが使用中のため .new として準備しました。ACT再起動時に適用されます。", "Update Pending", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            else
                            {
                                dialog.Log("Update prepared successfully.");
                                dialog.Log("Ready to restart ACT.");
                                dialog.EnableCloseButton();
                            }

                            TryRestartACT(target.DisplayName);
                        }
                        else
                        {
                            dialog.Log("FAILED to prepare update.");
                            dialog.EnableCloseButton();
                        }

                        await dialog.WaitForClose;

                        this.Log($"[Updater] Update check completed for {target.DisplayName}");
                        return success;
                    }
                }));
            }
            catch (Exception ex)
            {
                this.Log($"[Updater] Error during update check: {target.DisplayName}", ex);
                return false;
            }
        }
        private async Task<(bool success, List<string> failedFiles)> PerformUpdateTask(string url, string fileName, UpdateTarget target, ProgressDialog dialog)
        {
            List<string> failedFiles = new List<string>();
            try
            {
                if (Directory.Exists(TempDir)) try { Directory.Delete(TempDir, true); } catch { }
                Directory.CreateDirectory(TempDir);

                dialog.Log("Downloading update...");
                string archivePath = Path.Combine(TempDir, "update" + Path.GetExtension(fileName));

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
                    using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                    {
                        response.EnsureSuccessStatusCode();
                        var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                        var canReportProgress = totalBytes != -1;

                        using (var stream = await response.Content.ReadAsStreamAsync())
                        using (var fileStream = new FileStream(archivePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                        {
                            var buffer = new byte[8192];
                            var totalRead = 0L;
                            var readCount = 0;
                            var lastReportTime = DateTime.MinValue;

                            while ((readCount = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                await fileStream.WriteAsync(buffer, 0, readCount);
                                totalRead += readCount;

                                // ログが流れすぎるのを防ぐため200ms間隔で出力
                                if ((DateTime.Now - lastReportTime).TotalMilliseconds > 200)
                                {
                                    if (canReportProgress)
                                    {
                                        int percent = (int)((double)totalRead / totalBytes * 100);
                                        dialog.UpdateProgress(percent);
                                        dialog.Log($"Downloading... {percent}% ({totalRead / 1024 / 1024}MB / {totalBytes / 1024 / 1024}MB)");
                                    }
                                    else
                                    {
                                        dialog.Log($"Downloading... {totalRead / 1024 / 1024}MB");
                                    }
                                    lastReportTime = DateTime.Now;
                                }
                            }
                        }
                    }
                }

                dialog.Log("Extracting assets...");
                string extractDir = Path.Combine(TempDir, "contents");

                if (archivePath.EndsWith(".7z", StringComparison.OrdinalIgnoreCase))
                {
                    if (!ExtractWith7Zip(archivePath, extractDir, dialog))
                    {
                        dialog.Log("7-Zip extraction failed.");
                        return (false, failedFiles);
                    }
                }
                else
                {
                    ZipFile.ExtractToDirectory(archivePath, extractDir);
                }

                UnblockFiles(extractDir);
                string installSrc = GetStrippedPath(extractDir, target.StrippedDirs);

                this.OnBeforeUpdate?.Invoke();
                return await Task.Run(() => Install(installSrc, _destDir, dialog));
            }
            catch (Exception ex) { dialog.Log($"Error during task: {ex.Message}"); return (false, failedFiles); }
        }

        private bool ExtractWith7Zip(string archivePath, string outputDir, ProgressDialog dialog)
        {
            try
            {
                var assembly = Assembly.GetAssembly(typeof(ACT.Hojoring.Common.Hojoring)) ?? Assembly.GetExecutingAssembly();
                var pluginDir = Path.GetDirectoryName(assembly.Location);
                var exe7z = Path.Combine(pluginDir, "bin", "tools", "7z", "7za.exe");

                if (!File.Exists(exe7z))
                {
                    dialog.Log($"7za.exe not found at: {exe7z}");
                    return false;
                }

                if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);

                var startInfo = new ProcessStartInfo
                {
                    FileName = exe7z,
                    Arguments = $"x \"{archivePath}\" -o\"{outputDir}\" -y",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                };

                using (var p = Process.Start(startInfo))
                {
                    p.WaitForExit();
                    return p.ExitCode == 0;
                }
            }
            catch (Exception ex)
            {
                dialog.Log($"7-Zip Error: {ex.Message}");
                return false;
            }
        }

        private (bool success, List<string> failedFiles) Install(string src, string dest, ProgressDialog dialog)
        {
            List<string> failedFiles = new List<string>();
            try
            {
                var files = Directory.GetFiles(src, "*.*", SearchOption.AllDirectories);
                int total = files.Length;
                int count = 0;

                foreach (var file in files)
                {
                    string relativePath = file.Substring(src.Length).TrimStart('\\', '/');
                    string targetPath = Path.Combine(dest, relativePath);
                    string targetDir = Path.GetDirectoryName(targetPath);

                    if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

                    try
                    {
                        if (File.Exists(targetPath))
                        {
                            File.SetAttributes(targetPath, FileAttributes.Normal);
                            File.Delete(targetPath);
                        }
                        File.Copy(file, targetPath, true);
                    }
                    catch (Exception)
                    {
                        try
                        {
                            string pendingPath = targetPath + NewFileSuffix;
                            if (File.Exists(pendingPath)) File.SetAttributes(pendingPath, FileAttributes.Normal);
                            File.Copy(file, pendingPath, true);

                            MoveFileEx(targetPath, null, MOVEFILE_DELAY_UNTIL_REBOOT);
                            failedFiles.Add(relativePath);

                            // どのファイルが Pending になったかを明示的にログ出力
                            dialog.Log($"[Pending] {relativePath}");
                            this.Log($"[Updater] Pending: {relativePath}");
                        }
                        catch (Exception ex)
                        {
                            dialog.Log($"Copy error {relativePath}: {ex.Message}");
                            this.Log($"Copy error {relativePath}: {ex.Message}");
                        }
                    }

                    count++;
                    dialog.UpdateProgress((int)((float)count / total * 100));
                }
                return (true, failedFiles);
            }
            catch (Exception ex)
            {
                dialog.Log($"Install Exception: {ex.Message}");
                this.Log($"Install Exception: {ex.Message}");
                return (false, failedFiles);
            }
            finally { Cleanup(); }
        }

        public void Cleanup()
        {
            try { if (Directory.Exists(TempDir)) Directory.Delete(TempDir, true); } catch { }
            if (!string.IsNullOrEmpty(_destDir)) DeleteOldFiles(_destDir);
        }

        private async Task<List<GitHubRelease>> FetchAllReleasesAsync(string repo)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
                    var json = await client.GetStringAsync($"https://api.github.com/repos/{repo}/releases");
                    return JsonConvert.DeserializeObject<List<GitHubRelease>>(json);
                }
            }
            catch { return null; }
        }

        private string GetStrippedPath(string path, int level)
        {
            string current = path;
            for (int i = 0; i < level; i++)
            {
                var dirs = Directory.GetDirectories(current);
                if (dirs.Length == 1) current = dirs[0]; else break;
            }
            return current;
        }

        private void UnblockFiles(string dir) { foreach (var f in Directory.GetFiles(dir, "*", SearchOption.AllDirectories)) DeleteFile(f + ":Zone.Identifier"); }

        private void TryRestartACT(string name)
        {
            try
            {
                var act = ActGlobals.oFormActMain;
                act.GetType().GetMethod("RestartACT")?.Invoke(act, new object[] { true, $"{name} updated." });
            }
            catch { }
        }

        #region IDisposable
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    Cleanup();
                    OnBeforeUpdate = null;
                }
                disposed = true;
            }
        }

        ~FFXIVFrameworkUpdater()
        {
            Dispose(false);
        }
        #endregion
    }
}