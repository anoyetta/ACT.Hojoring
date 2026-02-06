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
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FFXIV.Framework.Updater
{
    public interface IUpdaterUI : IDisposable
    {
        void Show();
        void SetReleaseNotes(string tagName, string markdown);
        void WriteLog(string message);
        void UpdateProgress(int value);
        void EnableCloseButton();
        Task<bool> WaitForStart { get; }
        Task WaitForClose { get; }
        Action<string> ExternalLogger { get; set; }
    }

    /// <summary>
    /// FFXIV.Framework および ACT.Hojoring のアップデート（ダウンロード・展開・配置準備）を管理するクラスです。
    /// 旧PowerShell版のロジックを継承し、除外リストを考慮したクリーンアップ・バックアップ・マイグレーションを行います。
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

        public Action OnBeforeUpdate { get; set; }
        public Action<string, Exception> Logger { get; set; }

        private void Log(string message, Exception ex = null)
        {
            this.Logger?.Invoke(message, ex);
        }
        #endregion

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
            if (string.IsNullOrEmpty(parent))
            {
                parent = AppDomain.CurrentDomain.BaseDirectory;
            }
            TempDir = Path.Combine(parent, tmpName);
        }

        #region Progress Dialog (Default UI)
        public class ProgressDialog : Form, IUpdaterUI
        {
            private ProgressBar progressBar;
            private RichTextBox logBox;
            private WebBrowser releaseNoteBox;
            private Button startButton;
            private Button cancelButton;
            private Button closeButton;

            private TaskCompletionSource<bool> _startTask = new TaskCompletionSource<bool>();
            private TaskCompletionSource<bool> _dialogCloseTask = new TaskCompletionSource<bool>();

            public Action<string> ExternalLogger { get; set; }

            public Task<bool> WaitForStart => _startTask.Task;
            public Task WaitForClose => _dialogCloseTask.Task;

            public ProgressDialog(string title)
            {
                this.Text = $"{title} - アップデートの進行状況";
                this.Size = new Size(750, 600);
                this.StartPosition = FormStartPosition.CenterScreen;
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.TopMost = true;

                progressBar = new ProgressBar
                {
                    Dock = DockStyle.Top,
                    Height = 25,
                    Minimum = 0,
                    Maximum = 100
                };

                logBox = new RichTextBox
                {
                    Dock = DockStyle.Bottom,
                    Height = 180,
                    ReadOnly = true,
                    BackColor = Color.Black,
                    ForeColor = Color.White
                };

                Panel bottomPanel = new Panel
                {
                    Dock = DockStyle.Bottom,
                    Height = 45,
                    Padding = new Padding(5)
                };

                startButton = new Button { Text = "Update", Dock = DockStyle.Right, Width = 100 };
                cancelButton = new Button { Text = "キャンセル", Dock = DockStyle.Right, Width = 100 };
                closeButton = new Button { Text = "閉じる", Dock = DockStyle.Right, Width = 100, Enabled = false };

                startButton.Click += (s, e) =>
                {
                    startButton.Enabled = false;
                    cancelButton.Enabled = false;
                    _startTask.TrySetResult(true);
                };

                cancelButton.Click += (s, e) =>
                {
                    this.Close();
                };

                closeButton.Click += (s, e) =>
                {
                    this.Close();
                };

                bottomPanel.Controls.Add(closeButton);
                bottomPanel.Controls.Add(new Label { Dock = DockStyle.Right, Width = 10 });
                bottomPanel.Controls.Add(startButton);
                bottomPanel.Controls.Add(new Label { Dock = DockStyle.Right, Width = 10 });
                bottomPanel.Controls.Add(cancelButton);

                releaseNoteBox = new WebBrowser
                {
                    Dock = DockStyle.Fill,
                    AllowWebBrowserDrop = false,
                    IsWebBrowserContextMenuEnabled = false
                };

                releaseNoteBox.Navigating += releaseNoteBox_Navigating;

                this.Controls.Add(releaseNoteBox);
                this.Controls.Add(progressBar);
                this.Controls.Add(logBox);
                this.Controls.Add(bottomPanel);

                this.FormClosed += (s, e) =>
                {
                    _startTask.TrySetResult(false);
                    _dialogCloseTask.TrySetResult(true);
                };
            }

            private void releaseNoteBox_Navigating(object sender, WebBrowserNavigatingEventArgs e)
            {
                if (e.Url != null && !string.Equals(e.Url.ToString(), "about:blank", StringComparison.OrdinalIgnoreCase))
                {
                    e.Cancel = true;
                    try
                    {
                        Process.Start(e.Url.ToString());
                    }
                    catch { }
                }
            }

            public void UpdateProgress(int value)
            {
                this.SafeInvoke(() =>
                {
                    progressBar.Value = Math.Min(100, Math.Max(0, value));
                });
            }

            public void WriteLog(string message)
            {
                this.SafeInvoke(() =>
                {
                    logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
                    logBox.ScrollToCaret();
                });
                this.ExternalLogger?.Invoke(message);
            }

            public void EnableCloseButton()
            {
                this.SafeInvoke(() =>
                {
                    closeButton.Enabled = true;
                });
            }

            public void SetReleaseNotes(string tagName, string markdown)
            {
                var rawMarkdown = (markdown ?? "").Replace("\r\n", "\n").Replace("\r", "\n");
                var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().UseSoftlineBreakAsHardlineBreak().Build();
                var htmlContent = Markdown.ToHtml(rawMarkdown, pipeline);
                var fullHtml = $@"<html><head><meta http-equiv='X-UA-Compatible' content='IE=edge' /><style>body {{ font-family: 'Segoe UI', 'Meiryo', sans-serif; font-size: 10pt; line-height: 1.6; padding: 15px; color: #333; }} h1.release-tag {{ color: #0056b3; border-bottom: 2px solid #0056b3; padding-bottom: 10px; margin-top: 0; font-size: 18pt; }} h2, h3 {{ border-bottom: 1px solid #ddd; padding-bottom: 5px; color: #444; margin-top: 20px; }} code {{ background-color: #f0f0f0; padding: 2px 4px; border-radius: 3px; font-family: 'Consolas', monospace; }} pre {{ background-color: #f8f8f8; padding: 10px; border-radius: 5px; overflow-x: auto; border: 1px solid #eee; }} ul, ol {{ padding-left: 25px; }} li {{ margin-bottom: 4px; }} a {{ color: #0066cc; text-decoration: none; font-weight: bold; }} a:hover {{ text-decoration: underline; }}</style></head><body><h1 class='release-tag'>{tagName}</h1>{htmlContent}</body></html>";
                this.SafeInvoke(() =>
                {
                    releaseNoteBox.DocumentText = fullHtml;
                });
            }

            private void SafeInvoke(Action action)
            {
                if (!this.IsDisposed && this.IsHandleCreated)
                {
                    if (this.InvokeRequired)
                    {
                        this.Invoke(action);
                    }
                    else
                    {
                        action();
                    }
                }
            }
        }
        #endregion

        public async Task<bool> CheckAndDoUpdate(UpdateTarget target, bool usePreRelease = false)
        {
            try
            {
                this.Log($"[Updater] アップデート確認中: {target.DisplayName}");

                GitHubRelease latest = null;

                if (usePreRelease)
                {
                    var releases = await FetchAllReleasesAsync(target.Repo);
                    latest = releases?.FirstOrDefault();
                }
                else
                {
                    // 最適化: 最新リリースAPIを使用
                    latest = await FetchLatestReleaseAsync(target.Repo);
                }

                if (latest == null)
                {
                    return false;
                }

                var versionRaw = latest.TagName.TrimStart('v').Split('-')[0];
                var versionMatch = Regex.Match(versionRaw, @"[\d\.]+");
                var versionString = versionMatch.Success ? versionMatch.Value.Trim('.') : string.Empty;

                if (string.IsNullOrEmpty(versionString) || !Version.TryParse(versionString, out Version latestVersion))
                {
                    this.Log($"[Updater] バージョン解析失敗: {latest.TagName}");
                    return false;
                }

                var assembly = Assembly.GetAssembly(typeof(ACT.Hojoring.Common.Hojoring)) ?? Assembly.GetExecutingAssembly();
                var rawAssemblyVersion = target.CurrentVersion ?? assembly.GetName().Version;
                var assemblyVersionStr = rawAssemblyVersion.ToString();
                var assemblyMatch = Regex.Match(assemblyVersionStr, @"^\d+\.\d+\.\d+");
                var normalizedVersionStr = assemblyMatch.Success ? assemblyMatch.Value : assemblyVersionStr;
                Version.TryParse(normalizedVersionStr, out Version currentVersion);

                bool isNewVersionAvailable = latestVersion > currentVersion;

                var asset = latest.Assets.FirstOrDefault(a => a.Name.Contains(target.AssetKeyword) && a.Name.EndsWith(".7z"))
                           ?? latest.Assets.FirstOrDefault(a => a.Name.Contains(target.AssetKeyword) && a.Name.EndsWith(".zip"));

                if (asset == null)
                {
                    return false;
                }

                InitializePaths(target.PluginDirectory, target.DisplayName + ".tmp");

                // UIスレッドで実行
                return await (Task<bool>)ActGlobals.oFormActMain.Invoke((Func<Task<bool>>)(async () =>
                {
                    using (IUpdaterUI dialog = new ProgressDialog(target.DisplayName))
                    {
                        dialog.ExternalLogger = (msg) => this.Log($"[Dialog] {msg}");
                        dialog.Show();
                        dialog.SetReleaseNotes(latest.TagName, latest.Body);

                        if (isNewVersionAvailable)
                        {
                            dialog.WriteLog($"新しいバージョンが見つかりました: {latest.TagName}");
                        }
                        else
                        {
                            dialog.WriteLog($"現在のバージョン {currentVersion} は最新です。");
                            dialog.WriteLog("'Update' をクリックすると、最新バージョンで再インストールを行います。");
                        }

                        bool userApproved = await dialog.WaitForStart;
                        if (!userApproved)
                        {
                            return false;
                        }

                        var result = await PerformUpdateTask(asset.DownloadUrl, asset.Name, target, dialog);

                        if (result.success)
                        {
                            if (result.failedFiles.Count > 0)
                            {
                                dialog.WriteLog("一部のファイルは保留中です。ACT再起動時に適用されます。");
                                ACT.Hojoring.AtomicUpdater.RequestExternalUpdate();
                            }
                            else
                            {
                                dialog.WriteLog("アップデートが正常に完了しました。");
                            }

                            dialog.EnableCloseButton();
                            TryRestartACT(target.DisplayName);
                        }
                        else
                        {
                            dialog.WriteLog("アップデートに失敗しました。");
                            dialog.EnableCloseButton();
                        }

                        await dialog.WaitForClose;
                        return result.success;
                    }
                }));
            }
            catch (Exception ex)
            {
                this.Log($"[Updater] エラー: {target.DisplayName}", ex);
                return false;
            }
        }

        private async Task<(bool success, List<string> failedFiles)> PerformUpdateTask(string url, string fileName, UpdateTarget target, IUpdaterUI dialog)
        {
            List<string> failedFiles = new List<string>();
            try
            {
                if (Directory.Exists(TempDir))
                {
                    try
                    {
                        Directory.Delete(TempDir, true);
                    }
                    catch { }
                }
                Directory.CreateDirectory(TempDir);

                dialog.WriteLog("ダウンロード中...");
                string archivePath = Path.Combine(TempDir, "update" + Path.GetExtension(fileName));

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
                    using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                    {
                        response.EnsureSuccessStatusCode();
                        var totalBytes = response.Content.Headers.ContentLength;

                        using (var stream = await response.Content.ReadAsStreamAsync())
                        using (var fileStream = new FileStream(archivePath, FileMode.Create))
                        {
                            var buffer = new byte[81920]; // 80KB
                            long totalRead = 0;
                            int read;
                            while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                await fileStream.WriteAsync(buffer, 0, read);
                                totalRead += read;
                                if (totalBytes.HasValue)
                                {
                                    int progress = (int)((double)totalRead / totalBytes.Value * 100);
                                    dialog.UpdateProgress(progress);
                                }
                            }
                        }
                    }
                }

                dialog.WriteLog("展開中...");
                dialog.UpdateProgress(0);
                string extractDir = Path.Combine(TempDir, "contents");
                if (archivePath.EndsWith(".7z", StringComparison.OrdinalIgnoreCase))
                {
                    if (!ExtractWith7Zip(archivePath, extractDir, dialog))
                    {
                        return (false, failedFiles);
                    }
                }
                else
                {
                    ZipFile.ExtractToDirectory(archivePath, extractDir);
                }

                UnblockFiles(extractDir);
                string installSrc = GetStrippedPath(extractDir, target.StrippedDirs);

                var ignoreList = GetIgnoreList(_destDir);

                dialog.WriteLog("バックアップを作成中...");
                CreateBackup(_destDir, dialog);

                dialog.WriteLog("クリーンアップ中...");
                CleanupOldAssets(_destDir, ignoreList, dialog);

                dialog.WriteLog("マイグレーション中...");
                MigrateDirectories(_destDir, dialog);

                dialog.WriteLog("ファイルを配置中...");
                this.OnBeforeUpdate?.Invoke();
                return await Task.Run(() => Install(installSrc, _destDir, ignoreList, dialog));
            }
            catch (Exception ex)
            {
                dialog.WriteLog($"例外発生: {ex.Message}");
                return (false, failedFiles);
            }
        }

        private List<string> GetIgnoreList(string dest)
        {
            try
            {
                string ignoreFile = Path.Combine(dest, "config", "update_hojoring_ignores.txt");
                if (File.Exists(ignoreFile))
                {
                    return File.ReadAllLines(ignoreFile)
                        .Where(l => !string.IsNullOrWhiteSpace(l) && !l.TrimStart().StartsWith("#"))
                        .Select(l => l.Trim())
                        .ToList();
                }
            }
            catch { }
            return new List<string>();
        }

        private bool IsIgnored(string relativePath, List<string> ignoreList)
        {
            var normalizedPath = relativePath.Replace('/', '\\');
            return ignoreList.Any(ig =>
                normalizedPath.IndexOf(ig.Replace('/', '\\'), StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private void CreateBackup(string dest, IUpdaterUI dialog)
        {
            try
            {
                string backupDir = Path.Combine(dest, "backup");
                if (Directory.Exists(backupDir))
                {
                    Directory.Delete(backupDir, true);
                }

                Directory.CreateDirectory(backupDir);
                foreach (var file in Directory.GetFiles(dest))
                {
                    File.Copy(file, Path.Combine(backupDir, Path.GetFileName(file)), true);
                }
            }
            catch (Exception ex)
            {
                dialog.WriteLog($"バックアップ失敗 (継続): {ex.Message}");
            }
        }

        private void CleanupOldAssets(string dest, List<string> ignoreList, IUpdaterUI dialog)
        {
            string[] dirsToClean = { "references", "openJTalk", "yukkuri", "tools" };
            foreach (var d in dirsToClean)
            {
                if (IsIgnored(d, ignoreList))
                {
                    continue;
                }
                string path = Path.Combine(dest, d);
                if (Directory.Exists(path))
                {
                    try
                    {
                        Directory.Delete(path, true);
                        dialog.WriteLog($"構成削除: {d}");
                    }
                    catch { }
                }
            }

            foreach (var dll in Directory.GetFiles(dest, "*.dll"))
            {
                string fileName = Path.GetFileName(dll);
                if (IsIgnored(fileName, ignoreList))
                {
                    continue;
                }
                try
                {
                    File.Delete(dll);
                }
                catch { }
            }

            string binPath = Path.Combine(dest, "bin");
            if (Directory.Exists(binPath) && !IsIgnored("bin", ignoreList))
            {
                var targetFiles = Directory.GetFiles(binPath, "*.*", SearchOption.AllDirectories)
                    .Where(s => s.EndsWith(".dll") || s.EndsWith(".exe"));

                foreach (var f in targetFiles)
                {
                    string relativePath = f.Substring(dest.Length).TrimStart('\\', '/');
                    if (IsIgnored(relativePath, ignoreList))
                    {
                        continue;
                    }
                    try
                    {
                        File.Delete(f);
                    }
                    catch { }
                }
            }
        }

        private void MigrateDirectories(string dest, IUpdaterUI dialog)
        {
            string[][] maps = {
                new[] { "resources\\icon\\Timeline EN", "resources\\icon\\Timeline_EN" },
                new[] { "resources\\icon\\Timeline JP", "resources\\icon\\Timeline_JP" }
            };

            foreach (var map in maps)
            {
                string oldPath = Path.Combine(dest, map[0]);
                string newPath = Path.Combine(dest, map[1]);
                if (Directory.Exists(oldPath))
                {
                    try
                    {
                        if (!Directory.Exists(newPath))
                        {
                            Directory.Move(oldPath, newPath);
                        }
                        else
                        {
                            foreach (var f in Directory.GetFiles(oldPath, "*", SearchOption.AllDirectories))
                            {
                                string r = f.Substring(oldPath.Length).TrimStart('\\');
                                string d = Path.Combine(newPath, r);
                                Directory.CreateDirectory(Path.GetDirectoryName(d));
                                File.Copy(f, d, true);
                            }
                            Directory.Delete(oldPath, true);
                        }
                    }
                    catch { }
                }
            }
        }

        private (bool success, List<string> failedFiles) Install(string src, string dest, List<string> ignoreList, IUpdaterUI dialog)
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
                    if (IsIgnored(relativePath, ignoreList))
                    {
                        count++;
                        continue;
                    }

                    string targetPath = Path.Combine(dest, relativePath);
                    string targetDir = Path.GetDirectoryName(targetPath);
                    if (!Directory.Exists(targetDir))
                    {
                        Directory.CreateDirectory(targetDir);
                    }

                    try
                    {
                        if (File.Exists(targetPath))
                        {
                            File.SetAttributes(targetPath, FileAttributes.Normal);
                            File.Delete(targetPath);
                        }
                        File.Copy(file, targetPath, true);
                    }
                    catch
                    {
                        try
                        {
                            string pendingPath = targetPath + NewFileSuffix;
                            if (File.Exists(pendingPath))
                            {
                                File.SetAttributes(pendingPath, FileAttributes.Normal);
                            }
                            File.Copy(file, pendingPath, true);
                            MoveFileEx(targetPath, null, MOVEFILE_DELAY_UNTIL_REBOOT);
                            failedFiles.Add(relativePath);
                            dialog.WriteLog($"[Pending] {relativePath}");
                        }
                        catch (Exception ex)
                        {
                            dialog.WriteLog($"Copy Failed {relativePath}: {ex.Message}");
                        }
                    }
                    count++;
                    dialog.UpdateProgress((int)((float)count / total * 100));
                }
                return (true, failedFiles);
            }
            catch (Exception ex)
            {
                dialog.WriteLog($"Install Error: {ex.Message}");
                return (false, failedFiles);
            }
            finally
            {
                Cleanup();
            }
        }

        private bool ExtractWith7Zip(string archivePath, string outputDir, IUpdaterUI dialog)
        {
            try
            {
                var assembly = Assembly.GetAssembly(typeof(ACT.Hojoring.Common.Hojoring)) ?? Assembly.GetExecutingAssembly();
                var pluginDir = Path.GetDirectoryName(assembly.Location);
                string[] possiblePaths = {
                    Path.Combine(pluginDir, "bin", "tools", "7z", "7za.exe"),
                    Path.Combine(pluginDir, "tools", "7z", "7za.exe")
                };
                string exe7z = possiblePaths.FirstOrDefault(File.Exists);

                if (exe7z == null)
                {
                    dialog.WriteLog("7za.exe not found.");
                    return false;
                }

                if (!Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

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
                dialog.WriteLog($"7-Zip Error: {ex.Message}");
                return false;
            }
        }

        public void Cleanup()
        {
            try
            {
                if (Directory.Exists(TempDir))
                {
                    Directory.Delete(TempDir, true);
                }
            }
            catch { }

            if (!string.IsNullOrEmpty(_destDir))
            {
                DeleteOldFiles(_destDir);
            }
        }

        public static void DeleteOldFiles(string dir)
        {
            if (!Directory.Exists(dir))
            {
                return;
            }

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
            catch
            {
                return null;
            }
        }

        private async Task<GitHubRelease> FetchLatestReleaseAsync(string repo)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
                    var json = await client.GetStringAsync($"https://api.github.com/repos/{repo}/releases/latest");
                    return JsonConvert.DeserializeObject<GitHubRelease>(json);
                }
            }
            catch
            {
                return null;
            }
        }

        private string GetStrippedPath(string path, int level)
        {
            string current = path;
            for (int i = 0; i < level; i++)
            {
                var dirs = Directory.GetDirectories(current);
                if (dirs.Length == 1)
                {
                    current = dirs[0];
                }
                else
                {
                    break;
                }
            }
            return current;
        }

        private void UnblockFiles(string dir)
        {
            foreach (var f in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
            {
                DeleteFile(f + ":Zone.Identifier");
            }
        }

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