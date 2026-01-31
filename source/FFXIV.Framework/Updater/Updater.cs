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
        /// アップデート（ファイル置換）の直前に実行されるアクション
        /// </summary>
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

                progressBar = new ProgressBar { Dock = DockStyle.Top, Height = 25, Minimum = 0, Maximum = 100 };
                logBox = new RichTextBox { Dock = DockStyle.Bottom, Height = 180, ReadOnly = true, BackColor = Color.Black, ForeColor = Color.White };

                Panel bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 45, Padding = new Padding(5) };

                startButton = new Button { Text = "Update", Dock = DockStyle.Right, Width = 100 };
                cancelButton = new Button { Text = "キャンセル", Dock = DockStyle.Right, Width = 100 };
                closeButton = new Button { Text = "閉じる", Dock = DockStyle.Right, Width = 100, Enabled = false };

                startButton.Click += (s, e) => {
                    startButton.Enabled = false;
                    cancelButton.Enabled = false;
                    _startTask.TrySetResult(true);
                };
                cancelButton.Click += (s, e) => { this.Close(); };
                closeButton.Click += (s, e) => { this.Close(); };

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

                this.FormClosed += (s, e) => {
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
                    catch (Exception ex)
                    {
                        this.WriteLog($"ブラウザ起動失敗: {ex.Message}");
                    }
                }
            }

            public void UpdateProgress(int value) => this.SafeInvoke(() => progressBar.Value = Math.Min(100, Math.Max(0, value)));

            public void WriteLog(string message)
            {
                this.SafeInvoke(() => {
                    logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
                    logBox.ScrollToCaret();
                });
                this.ExternalLogger?.Invoke(message);
            }

            public void EnableCloseButton() => this.SafeInvoke(() => closeButton.Enabled = true);

            public void SetReleaseNotes(string tagName, string markdown)
            {
                var rawMarkdown = (markdown ?? "").Replace("\r\n", "\n").Replace("\r", "\n");

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
    a {{ color: #0066cc; text-decoration: none; font-weight: bold; }}
    a:hover {{ text-decoration: underline; }}
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

        /// <summary>
        /// アップデートの有無を確認し、ユーザーの指示に基づいてインストールを実行します。
        /// </summary>
        public async Task<bool> CheckAndDoUpdate(UpdateTarget target, bool usePreRelease = false)
        {
            try
            {
                this.Log($"[Updater] アップデート確認中: {target.DisplayName}");

                var releases = await FetchAllReleasesAsync(target.Repo);
                var latest = usePreRelease ? releases?.FirstOrDefault() : releases?.FirstOrDefault(x => !x.Prerelease);
                if (latest == null) return false;

                // 最新バージョンのパース
                var versionRaw = latest.TagName.TrimStart('v').Split('-')[0];
                var versionMatch = Regex.Match(versionRaw, @"[\d\.]+");
                var versionString = versionMatch.Success ? versionMatch.Value.Trim('.') : string.Empty;

                if (string.IsNullOrEmpty(versionString) || !Version.TryParse(versionString, out Version latestVersion))
                {
                    this.Log($"[Updater] タグからのバージョン解析に失敗しました: {latest.TagName}");
                    return false;
                }

                // 現在のバージョンのパース
                var assembly = Assembly.GetAssembly(typeof(ACT.Hojoring.Common.Hojoring)) ?? Assembly.GetExecutingAssembly();
                var rawAssemblyVersion = target.CurrentVersion ?? assembly.GetName().Version;
                var assemblyVersionStr = rawAssemblyVersion.ToString();
                var assemblyMatch = Regex.Match(assemblyVersionStr, @"^\d+\.\d+\.\d+");
                var normalizedVersionStr = assemblyMatch.Success ? assemblyMatch.Value : assemblyVersionStr;

                if (!Version.TryParse(normalizedVersionStr, out Version currentVersion))
                {
                    currentVersion = rawAssemblyVersion;
                }

                bool isNewVersionAvailable = latestVersion > currentVersion;

                var asset = latest.Assets.FirstOrDefault(a => a.Name.Contains(target.AssetKeyword) && a.Name.EndsWith(".7z"))
                           ?? latest.Assets.FirstOrDefault(a => a.Name.Contains(target.AssetKeyword) && a.Name.EndsWith(".zip"));

                if (asset == null) return false;

                InitializePaths(target.PluginDirectory, target.DisplayName + ".tmp");

                // ACTメインスレッドでUI処理を実行
                return await (Task<bool>)ActGlobals.oFormActMain.Invoke((Func<Task<bool>>)(async () =>
                {
                    using (var dialog = new ProgressDialog(target.DisplayName))
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
                            dialog.WriteLog("'Update' をクリックすると、最新バージョンで上書きインストールを行います。");
                        }

                        dialog.WriteLog("上記のリリースノートを確認し、続行する場合は 'Update' をクリックしてください。");

                        // ユーザーの承認待ち（最新であってもユーザーがUpdateを押せば続行する）
                        bool userApproved = await dialog.WaitForStart;

                        if (!userApproved)
                        {
                            this.Log($"[Updater] ユーザーによってキャンセルされました: {target.DisplayName}");
                            return false;
                        }

                        // ダウンロードとインストール開始
                        var (success, failedFiles) = await PerformUpdateTask(asset.DownloadUrl, asset.Name, target, dialog);

                        if (success)
                        {
                            if (failedFiles.Count > 0)
                            {
                                dialog.WriteLog("準備完了。一部のファイルは保留中です。");
                                dialog.EnableCloseButton();
                                ACT.Hojoring.AtomicUpdater.RequestExternalUpdate();
                                MessageBox.Show(dialog, "一部のファイルが使用中のため .new として準備しました。ACT再起動時に適用されます。", "Update Pending", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            else
                            {
                                dialog.WriteLog("アップデートの準備が正常に完了しました。");
                                dialog.WriteLog("ACTを再起動する準備ができました。");
                                dialog.EnableCloseButton();
                            }

                            TryRestartACT(target.DisplayName);
                        }
                        else
                        {
                            dialog.WriteLog("アップデートの準備に失敗しました。");
                            dialog.EnableCloseButton();
                        }

                        await dialog.WaitForClose;
                        return success;
                    }
                }));
            }
            catch (Exception ex)
            {
                this.Log($"[Updater] アップデート確認中にエラーが発生しました: {target.DisplayName}", ex);
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

                dialog.WriteLog("ダウンロード中...");
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

                                if ((DateTime.Now - lastReportTime).TotalMilliseconds > 200)
                                {
                                    if (canReportProgress)
                                    {
                                        int percent = (int)((double)totalRead / totalBytes * 100);
                                        dialog.UpdateProgress(percent);
                                        dialog.WriteLog($"ダウンロード中... {percent}% ({totalRead / 1024 / 1024}MB / {totalBytes / 1024 / 1024}MB)");
                                    }
                                    else
                                    {
                                        dialog.WriteLog($"ダウンロード中... {totalRead / 1024 / 1024}MB");
                                    }
                                    lastReportTime = DateTime.Now;
                                }
                            }
                        }
                    }
                }

                dialog.WriteLog("展開中...");
                string extractDir = Path.Combine(TempDir, "contents");

                if (archivePath.EndsWith(".7z", StringComparison.OrdinalIgnoreCase))
                {
                    if (!ExtractWith7Zip(archivePath, extractDir, dialog))
                    {
                        dialog.WriteLog("7-Zip での展開に失敗しました。");
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
            catch (Exception ex) { dialog.WriteLog($"タスク実行中にエラー: {ex.Message}"); return (false, failedFiles); }
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
                    dialog.WriteLog($"7za.exe が見つかりません: {exe7z}");
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
                dialog.WriteLog($"7-Zip エラー: {ex.Message}");
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

                            dialog.WriteLog($"[保留中] {relativePath}");
                        }
                        catch (Exception ex)
                        {
                            dialog.WriteLog($"コピーエラー {relativePath}: {ex.Message}");
                        }
                    }

                    count++;
                    dialog.UpdateProgress((int)((float)count / total * 100));
                }
                return (true, failedFiles);
            }
            catch (Exception ex)
            {
                dialog.WriteLog($"インストール例外: {ex.Message}");
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