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
using Advanced_Combat_Tracker;
using Markdig;
using Newtonsoft.Json;

namespace FFXIV.Framework.Updater
{
    public class FFXIVFrameworkUpdater
    {
        private const string GitHubRepo = "anoyetta/ACT.Hojoring";
        private const string UserAgent = "FFXIV-Framework-Updater";

        private const uint FILE_OVERWRITE_RETRIES = 10;
        private const int FILE_OVERWRITE_WAIT_BASE = 500;

        // アップデート用サフィックス
        public const string NewFileSuffix = ".new";
        public const string OldFileSuffix = ".old";

        private bool bUpdated = false;

        /// <summary>
        /// アップデート（ファイル置換）の直前に実行されるアクション。
        /// </summary>
        public Action OnBeforeUpdate { get; set; }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteFile(string lpFileName);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool MoveFileEx(string lpExistingFileName, string lpNewFileName, uint dwFlags);

        private const uint MOVEFILE_REPLACE_EXISTING = 0x00000001;
        private const uint MOVEFILE_COPY_ALLOWED = 0x00000002;
        private const uint MOVEFILE_DELAY_UNTIL_REBOOT = 0x00000004;

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

        /// <summary>
        /// 起動時に呼び出され、保留されているアップデートファイルを適用します。
        /// DLLがメインドメインにロードされる前に実行する必要があります。
        /// </summary>
        public void ApplyPendingUpdates()
        {
            if (this.bUpdated)
            {
                return;
            }

            try
            {
                // アセンブリの場所から自動的にプラグインディレクトリを特定する
                var assembly = Assembly.GetAssembly(typeof(ACT.Hojoring.Common.Hojoring));
                var pluginDir = Path.GetDirectoryName(assembly.Location);

                if (!Directory.Exists(pluginDir))
                {
                    return;
                }

                // 1. まず過去の .old ファイルを掃除する
                DeleteOldFiles(pluginDir);

                // 2. .new ファイルを探して置換する
                var newFiles = Directory.GetFiles(pluginDir, "*" + NewFileSuffix, SearchOption.AllDirectories);
                foreach (var newFile in newFiles)
                {
                    string targetPath = newFile.Substring(0, newFile.Length - NewFileSuffix.Length);
                    try
                    {
                        if (File.Exists(targetPath))
                        {
                            File.SetAttributes(targetPath, FileAttributes.Normal);
                            File.Delete(targetPath);
                        }
                        File.Move(newFile, targetPath);
                    }
                    catch
                    {
                        // 起動直後のロック等で失敗した場合は、次回のチャンスに回す
                    }
                }

                this.bUpdated = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to apply pending updates: {ex.Message}");
            }
        }

        /// <summary>
        /// 指定ディレクトリ以下の .old ファイルをすべて削除します。
        /// </summary>
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
                    catch { /* ロード中の場合は消せないので無視 */ }
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
                this.MaximizeBox = false;

                progressBar = new ProgressBar { Dock = DockStyle.Top, Height = 25, Minimum = 0, Maximum = 100 };

                logBox = new RichTextBox
                {
                    Dock = DockStyle.Bottom,
                    Height = 180,
                    ReadOnly = true,
                    BackColor = Color.FromArgb(30, 30, 30),
                    ForeColor = Color.White,
                    Font = new Font("Consolas", 9),
                    HideSelection = false
                };

                Panel bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 45, Padding = new Padding(8) };
                closeButton = new Button { Text = "Close", Dock = DockStyle.Right, Width = 120, Enabled = false };
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
            public void Log(string message) => this.SafeInvoke(() => {
                logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
                logBox.SelectionStart = logBox.Text.Length;
                logBox.ScrollToCaret();
            });

            public void EnableCloseButton() => this.SafeInvoke(() => closeButton.Enabled = true);

            public void SetReleaseNotes(string markdown)
            {
                var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
                string html = Markdown.ToHtml(markdown ?? "", pipeline);
                string styledHtml = $"<html><head><style>body{{font-family:'Segoe UI',sans-serif;font-size:10pt;background:#fefefe;padding:20px;line-height:1.6;}} code{{background:#eee;padding:2px 4px;}}</style></head><body>{html}</body></html>";
                this.SafeInvoke(() => releaseNoteBox.DocumentText = styledHtml);
            }

            private void SafeInvoke(Action action)
            {
                if (!this.IsDisposed && this.IsHandleCreated)
                {
                    if (this.InvokeRequired) this.Invoke(action);
                    else action();
                }
            }
        }
        #endregion

        public async Task<bool> CheckAndDoUpdate(UpdateTarget target, bool usePreRelease = false)
        {
            try
            {
                var releases = await FetchAllReleasesAsync(target.Repo);
                var latest = usePreRelease ? releases?.FirstOrDefault() : releases?.FirstOrDefault(x => !x.Prerelease);
                if (latest == null) return false;

                var assembly = Assembly.GetAssembly(typeof(ACT.Hojoring.Common.Hojoring));
                var currentVersion = target.CurrentVersion ?? assembly.GetName().Version;

                var versionRaw = latest.TagName.TrimStart('v').Split('-')[0];
                var versionMatch = Regex.Match(versionRaw, @"[\d\.]+");
                var versionString = versionMatch.Success ? versionMatch.Value.Trim('.') : string.Empty;

                if (string.IsNullOrEmpty(versionString) || !Version.TryParse(versionString, out Version latestVersion)) return false;
                if (latestVersion <= currentVersion) return false;

                var asset = latest.Assets.FirstOrDefault(a => a.Name.Contains(target.AssetKeyword) && (a.Name.EndsWith(".7z") || a.Name.EndsWith(".zip")));
                if (asset == null) return false;

                InitializePaths(target.PluginDirectory, target.DisplayName + ".tmp");

                return await (Task<bool>)ActGlobals.oFormActMain.Invoke((Func<Task<bool>>)(async () =>
                {
                    using (var dialog = new ProgressDialog(target.DisplayName))
                    {
                        dialog.Show();
                        dialog.SetReleaseNotes(latest.Body);
                        dialog.Log($"New version found: {latest.TagName}");

                        var (success, failedFiles) = await PerformUpdateTask(asset.DownloadUrl, asset.Name, target, dialog);

                        if (success)
                        {
                            if (failedFiles.Count > 0)
                            {
                                dialog.Log("Update completed with some pending files.");
                                dialog.EnableCloseButton();
                                MessageBox.Show(dialog,
                                    "一部のファイルは使用中のため、次回のACT起動時に適用されます。このままACTを再起動してください。",
                                    "Pending Updates", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                TryRestartACT(target.DisplayName);
                            }
                            else
                            {
                                dialog.Log("Update applied successfully.");
                                await Task.Delay(1000);
                                TryRestartACT(target.DisplayName);
                                dialog.Close();
                            }
                        }
                        else
                        {
                            dialog.Log("!!! UPDATE FAILED !!!");
                            dialog.EnableCloseButton();
                        }
                        return success;
                    }
                }));
            }
            catch (Exception ex) { Debug.WriteLine(ex); return false; }
        }

        private async Task<(bool success, List<string> failedFiles)> PerformUpdateTask(string url, string fileName, UpdateTarget target, ProgressDialog dialog)
        {
            List<string> failedFiles = new List<string>();
            try
            {
                if (Directory.Exists(TempDir)) try { Directory.Delete(TempDir, true); } catch { }
                Directory.CreateDirectory(TempDir);

                string zipPath = Path.Combine(TempDir, "update.file");

                dialog.Log($"Downloading: {fileName}");
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
                    using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                    {
                        response.EnsureSuccessStatusCode();
                        var totalBytes = response.Content.Headers.ContentLength;
                        using (var contentStream = await response.Content.ReadAsStreamAsync())
                        using (var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                        {
                            var buffer = new byte[8192];
                            long totalReadBytes = 0;
                            int readBytes;
                            while ((readBytes = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                await fileStream.WriteAsync(buffer, 0, readBytes);
                                totalReadBytes += readBytes;
                                if (totalBytes.HasValue) dialog.UpdateProgress((int)((double)totalReadBytes / totalBytes.Value * 100));
                            }
                        }
                    }
                }

                string extractDir = Path.Combine(TempDir, "contents");
                dialog.Log("Extracting files...");
                ZipFile.ExtractToDirectory(zipPath, extractDir);

                UnblockFiles(extractDir);
                string installSrc = GetStrippedPath(extractDir, target.StrippedDirs);

                // 解放処理の実行
                this.OnBeforeUpdate?.Invoke();

                return await Task.Run(() => Install(installSrc, _destDir, dialog));
            }
            catch (Exception ex)
            {
                dialog.Log($"Error: {ex.Message}");
                return (false, failedFiles);
            }
        }

        private (bool success, List<string> failedFiles) Install(string src, string dest, ProgressDialog dialog)
        {
            List<string> failedFiles = new List<string>();
            try
            {
                var files = Directory.GetFiles(src, "*.*", SearchOption.AllDirectories);
                dialog.Log($"Installing {files.Length} files...");

                foreach (var file in files)
                {
                    string relativePath = file.Substring(src.Length).TrimStart('\\', '/');
                    string targetPath = Path.Combine(dest, relativePath);
                    string targetDir = Path.GetDirectoryName(targetPath);

                    if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

                    bool fileSuccess = false;
                    for (int i = 0; i < FILE_OVERWRITE_RETRIES; i++)
                    {
                        try
                        {
                            // 1. 通常の上書きを試みる
                            if (File.Exists(targetPath))
                            {
                                File.SetAttributes(targetPath, FileAttributes.Normal);
                            }
                            File.Copy(file, targetPath, true);
                            fileSuccess = true;
                            break;
                        }
                        catch (IOException)
                        {
                            // 2. ロックされている場合、アトミックリネームを試みる (.oldに逃がす)
                            try
                            {
                                string oldPath = targetPath + "." + DateTime.Now.Ticks.ToString("x") + OldFileSuffix;
                                if (MoveFileEx(targetPath, oldPath, MOVEFILE_REPLACE_EXISTING | MOVEFILE_COPY_ALLOWED))
                                {
                                    // 削除予約（OS再起動時に確実に消去するため）
                                    MoveFileEx(oldPath, null, MOVEFILE_DELAY_UNTIL_REBOOT);

                                    File.Copy(file, targetPath, true);
                                    fileSuccess = true;
                                    break;
                                }
                                throw new IOException("MoveFileEx failed");
                            }
                            catch
                            {
                                // 3. リネームすらできない場合 (ロード中かつ排他ロック)
                                // 次回起動時に置換するため、.new として配置する
                                try
                                {
                                    string pendingPath = targetPath + NewFileSuffix;
                                    File.Copy(file, pendingPath, true);
                                    fileSuccess = true;
                                    dialog.Log($"Pending: {relativePath} (Will be replaced on next restart)");
                                    failedFiles.Add(relativePath);
                                    break;
                                }
                                catch (Exception ex)
                                {
                                    if (i == FILE_OVERWRITE_RETRIES - 1)
                                    {
                                        dialog.Log($"Failed to replace {relativePath}: {ex.Message}");
                                    }
                                }
                            }
                        }
                        Thread.Sleep(FILE_OVERWRITE_WAIT_BASE + (i * 100));
                    }
                }

                return (true, failedFiles);
            }
            catch (Exception ex)
            {
                dialog.Log($"Installation Error: {ex.Message}");
                return (false, failedFiles);
            }
            finally
            {
                // 全ファイル成功時のみ一時ファイルを掃除（保留がある場合は残す）
                // 同時に、消去可能な .old ファイルも掃除する
                Cleanup();
            }
        }

        public void Cleanup()
        {
            // 一時フォルダの削除
            if (Directory.Exists(TempDir))
            {
                try { Directory.Delete(TempDir, true); } catch { }
            }

            // インストール先にある消去可能な .old ファイルの掃除
            if (!string.IsNullOrEmpty(_destDir))
            {
                DeleteOldFiles(_destDir);
            }
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
                if (dirs.Length == 1) current = dirs[0];
                else break;
            }
            return current;
        }

        private void UnblockFiles(string dir)
        {
            foreach (var f in Directory.GetFiles(dir, "*", SearchOption.AllDirectories)) DeleteFile(f + ":Zone.Identifier");
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
    }
}