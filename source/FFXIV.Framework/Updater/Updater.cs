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
    /// <summary>
    /// FFXIV.Framework and ACT.Hojoring update management class.
    /// Supports re-installation even if the current version is up-to-date.
    /// Handles release notes display with Markdown support.
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
        private const string UserAgent = "ACT.Hojoring.Updater";
        private bool disposed = false;

        public Action<string, Exception> Logger { get; set; }
        public Action OnBeforeUpdate { get; set; }
        #endregion

        #region Public Methods

        public async Task<bool> CheckAndDoUpdate(UpdateTarget target, bool usePreRelease = false)
        {
            try
            {
                this.Log($"[Updater] Update check started: {target.DisplayName}");

                var releases = await FetchAllReleasesAsync(target.Repo);
                var latest = usePreRelease ? releases?.FirstOrDefault() : releases?.FirstOrDefault(x => !x.Prerelease);
                if (latest == null) return false;

                var versionRaw = latest.TagName.TrimStart('v').Split('-')[0];
                var versionMatch = Regex.Match(versionRaw, @"[\d\.]+");
                var versionString = versionMatch.Success ? versionMatch.Value.Trim('.') : string.Empty;

                if (string.IsNullOrEmpty(versionString) || !Version.TryParse(versionString, out Version latestVersion))
                {
                    this.Log($"[Updater] Failed to parse version: {latest.TagName}");
                    return false;
                }

                var assembly = Assembly.GetAssembly(typeof(ACT.Hojoring.Common.Hojoring)) ?? Assembly.GetExecutingAssembly();
                var rawAssemblyVersion = target.CurrentVersion ?? assembly.GetName().Version;

                var assemblyVersionStr = rawAssemblyVersion.ToString();
                var assemblyMatch = Regex.Match(assemblyVersionStr, @"^(\d+)\.(\d+)\.(\d+)");
                Version currentVersion = assemblyMatch.Success ? new Version(assemblyMatch.Value) : rawAssemblyVersion;

                var asset = latest.Assets.FirstOrDefault(a => a.Name.Contains(target.AssetKeyword) && (a.Name.EndsWith(".7z") || a.Name.EndsWith(".zip")));
                if (asset == null)
                {
                    this.Log($"[Updater] No matching asset found: {target.DisplayName}");
                    return false;
                }

                return await (Task<bool>)ActGlobals.oFormActMain.Invoke((Func<Task<bool>>)(async () =>
                {
                    using (var dialog = new ProgressDialog(target.DisplayName))
                    {
                        dialog.Show();
                        dialog.SetReleaseNotes(latest.TagName, latest.Body);

                        if (latestVersion <= currentVersion)
                        {
                            dialog.Log($"Current version {currentVersion} is up to date.");
                            dialog.Log("You can still perform an overwrite installation.");
                        }
                        else
                        {
                            dialog.Log($"New version found: {latest.TagName} (Current: {currentVersion})");
                        }

                        dialog.Log("Click 'Start Update' to proceed.");

                        bool userConfirmed = await dialog.WaitForStartConfirmation();
                        if (!userConfirmed)
                        {
                            dialog.Log("Update cancelled by user.");
                            return false;
                        }

                        var (success, failedFiles) = await PerformUpdateTask(asset.DownloadUrl, asset.Name, target, dialog);

                        if (success)
                        {
                            if (failedFiles.Count > 0)
                            {
                                dialog.Log("Some files are in use. Prepared as .new files.");
                                try
                                {
                                    var atomicType = assembly.GetType("ACT.Hojoring.AtomicUpdater");
                                    atomicType?.GetMethod("RequestExternalUpdate")?.Invoke(null, null);
                                }
                                catch { }
                                MessageBox.Show(dialog, "Remaining files will be applied on next ACT restart.", "Update Ready", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            else
                            {
                                dialog.Log("All files deployed successfully.");
                            }

                            dialog.EnableCloseButton();
                            TryRestartACT(target.DisplayName);
                        }
                        else
                        {
                            dialog.Log("An error occurred during update.");
                            dialog.EnableCloseButton();
                        }

                        await dialog.WaitForClose;
                        return success;
                    }
                }));
            }
            catch (Exception ex)
            {
                this.Log($"[Updater] Update failed: {target.DisplayName}", ex);
                return false;
            }
        }

        #endregion

        #region Internal Logic

        private async Task<(bool success, List<string> failedFiles)> PerformUpdateTask(string url, string fileName, UpdateTarget target, ProgressDialog dialog)
        {
            var failedFiles = new List<string>();
            try
            {
                var tempZip = Path.Combine(Path.GetTempPath(), fileName);
                var extractPath = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(fileName));

                dialog.Log("Starting download...");
                await DownloadFileAsync(url, tempZip, dialog);

                dialog.Log("Extracting files...");
                if (Directory.Exists(extractPath)) Directory.Delete(extractPath, true);

                bool extracted = fileName.EndsWith(".7z") ? Extract7z(tempZip, extractPath) : ExtractZip(tempZip, extractPath);
                if (!extracted) return (false, failedFiles);

                this.OnBeforeUpdate?.Invoke();

                dialog.Log("Deploying files...");
                var sourceDir = GetStrippedPath(extractPath, target.IsFullPackage ? 0 : 1);
                failedFiles = DeployFiles(sourceDir, target.PluginDirectory);

                if (File.Exists(tempZip)) File.Delete(tempZip);
                if (Directory.Exists(extractPath)) Directory.Delete(extractPath, true);

                return (true, failedFiles);
            }
            catch (Exception ex)
            {
                dialog.Log($"Error: {ex.Message}");
                return (false, failedFiles);
            }
        }

        private List<string> DeployFiles(string sourceDir, string targetDir)
        {
            var failed = new List<string>();
            if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

            foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
            {
                var relativePath = file.Substring(sourceDir.Length).TrimStart(Path.DirectorySeparatorChar);
                var destPath = Path.Combine(targetDir, relativePath);
                var destDir = Path.GetDirectoryName(destPath);

                if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);

                try
                {
                    if (File.Exists(destPath))
                    {
                        File.SetAttributes(destPath, FileAttributes.Normal);
                        var oldPath = destPath + ".old";
                        if (File.Exists(oldPath)) File.Delete(oldPath);
                        File.Move(destPath, oldPath);
                    }
                    File.Copy(file, destPath, true);
                }
                catch
                {
                    try
                    {
                        File.Copy(file, destPath + ".new", true);
                        failed.Add(destPath);
                    }
                    catch { }
                }
            }
            return failed;
        }

        private async Task DownloadFileAsync(string url, string dest, ProgressDialog dialog)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
                using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    var total = response.Content.Headers.ContentLength ?? -1L;
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(dest, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                    {
                        var buffer = new byte[8192];
                        var read = 0;
                        var processed = 0L;
                        while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, read);
                            processed += read;
                            if (total > 0) dialog.UpdateProgress((int)((processed * 100) / total));
                        }
                    }
                }
            }
        }

        private bool ExtractZip(string zipPath, string destPath)
        {
            try { ZipFile.ExtractToDirectory(zipPath, destPath); return true; }
            catch { return false; }
        }

        private bool Extract7z(string archivePath, string destPath)
        {
            try
            {
                var exePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "tools", "7za.exe");
                if (!File.Exists(exePath)) return false;

                var startInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = $"x \"{archivePath}\" -o\"{destPath}\" -y",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                using (var p = Process.Start(startInfo))
                {
                    p.WaitForExit();
                    return p.ExitCode == 0;
                }
            }
            catch { return false; }
        }

        private async Task<List<GitHubRelease>> FetchAllReleasesAsync(string repo = GitHubRepo)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
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

        private void Log(string msg, Exception ex = null) => Logger?.Invoke(msg, ex);

        private void TryRestartACT(string name)
        {
            try
            {
                var act = ActGlobals.oFormActMain;
                act.GetType().GetMethod("RestartACT")?.Invoke(act, new object[] { true, $"{name} has been updated." });
            }
            catch { }
        }

        #endregion

        #region Data Models
        public class UpdateTarget
        {
            public string DisplayName { get; set; }
            public string Repo { get; set; } = GitHubRepo;
            public string AssetKeyword { get; set; }
            public string PluginDirectory { get; set; }
            public Version CurrentVersion { get; set; }
            public bool IsFullPackage { get; set; }
        }

        private class GitHubRelease
        {
            [JsonProperty("tag_name")] public string TagName { get; set; }
            [JsonProperty("prerelease")] public bool Prerelease { get; set; }
            [JsonProperty("body")] public string Body { get; set; }
            [JsonProperty("assets")] public List<GitHubAsset> Assets { get; set; }
        }

        private class GitHubAsset
        {
            [JsonProperty("name")] public string Name { get; set; }
            [JsonProperty("browser_download_url")] public string DownloadUrl { get; set; }
        }
        #endregion

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
                if (disposing) OnBeforeUpdate = null;
                disposed = true;
            }
        }

        ~FFXIVFrameworkUpdater() { Dispose(false); }
        #endregion
    }

    public class ProgressDialog : Form
    {
        private ProgressBar progressBar;
        private Label statusLabel;
        private TextBox logTextBox;
        private WebBrowser releaseNotes;
        private Button startButton;
        private Button closeButton;

        public Task<bool> WaitForStartConfirmation() => startCompletionSource.Task;
        public Task WaitForClose => closeCompletionSource.Task;

        private TaskCompletionSource<bool> startCompletionSource = new TaskCompletionSource<bool>();
        private TaskCompletionSource<bool> closeCompletionSource = new TaskCompletionSource<bool>();

        public ProgressDialog(string title)
        {
            this.Text = $"{title} Update";
            this.Size = new Size(620, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            statusLabel = new Label { Text = "Initializing...", Top = 10, Left = 15, Width = 570 };
            progressBar = new ProgressBar { Top = 35, Left = 15, Width = 570, Height = 20 };

            var lblNotes = new Label { Text = "Release Notes:", Top = 65, Left = 15, Width = 570 };

            releaseNotes = new WebBrowser
            {
                Top = 85,
                Left = 15,
                Width = 570,
                Height = 300,
                IsWebBrowserContextMenuEnabled = false,
                AllowWebBrowserDrop = false
            };

            // [NEW] 外部ブラウザでリンクを開くためのハンドラ
            releaseNotes.Navigating += ReleaseNotes_Navigating;

            logTextBox = new TextBox { Top = 395, Left = 15, Width = 570, Height = 110, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Font = new Font("Consolas", 9) };

            startButton = new Button { Text = "Start Update", Top = 515, Left = 375, Width = 110, Height = 35, BackColor = Color.LightBlue };
            startButton.Click += (s, e) => {
                startButton.Enabled = false;
                startCompletionSource.TrySetResult(true);
            };

            closeButton = new Button { Text = "Cancel", Top = 515, Left = 495, Width = 90, Height = 35 };
            closeButton.Click += (s, e) => this.Close();

            this.Controls.AddRange(new Control[] { statusLabel, progressBar, lblNotes, releaseNotes, logTextBox, startButton, closeButton });

            this.FormClosing += (s, e) => {
                startCompletionSource.TrySetResult(false);
                closeCompletionSource.TrySetResult(true);
            };
        }

        private void ReleaseNotes_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            // about:blank（初期ロード時など）以外はすべて外部ブラウザで開く
            if (e.Url.ToString() != "about:blank")
            {
                e.Cancel = true; // コントロール内での遷移をキャンセル
                try
                {
                    Process.Start(e.Url.ToString());
                }
                catch (Exception ex)
                {
                    Log($"Failed to open URL: {ex.Message}");
                }
            }
        }

        public void UpdateProgress(int percent) => this.SafeInvoke(() => { progressBar.Value = percent; statusLabel.Text = $"Progress: {percent}%"; });
        public void Log(string msg) => this.SafeInvoke(() => { logTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}{Environment.NewLine}"); statusLabel.Text = msg; });

        public void EnableCloseButton() => this.SafeInvoke(() => {
            closeButton.Text = "Close";
            closeButton.Enabled = true;
            startButton.Visible = false;
        });

        public void SetReleaseNotes(string tag, string markdown)
        {
            this.SafeInvoke(() => {
                var pipeline = new MarkdownPipelineBuilder()
                    .UseSoftlineBreakAsHardlineBreak()
                    .UseAdvancedExtensions()
                    .Build();

                var html = Markdown.ToHtml(markdown ?? "No release notes provided.", pipeline);

                var styledHtml = $@"
                    <html>
                    <head>
                        <meta charset='utf-8'>
                        <style>
                            body {{ 
                                font-family: 'Segoe UI', 'Meiryo', 'MS PGothic', sans-serif; 
                                font-size: 13px; 
                                line-height: 1.6; 
                                padding: 15px; 
                                color: #24292e; 
                                background-color: #ffffff;
                                margin: 0;
                            }}
                            h2 {{ 
                                border-bottom: 1px solid #eaecef; 
                                padding-bottom: 0.3em; 
                                font-size: 1.5em; 
                                margin-top: 0; 
                                margin-bottom: 16px; 
                                font-weight: 600;
                            }}
                            p, ul, ol {{ margin-top: 0; margin-bottom: 10px; }}
                            code {{ 
                                background-color: rgba(27,31,35,0.05); 
                                border-radius: 3px; 
                                font-family: Consolas, monospace;
                                font-size: 85%;
                                padding: 0.2em 0.4em;
                            }}
                            pre {{ 
                                background-color: #f6f8fa; 
                                border-radius: 3px; 
                                padding: 16px; 
                                overflow: auto; 
                            }}
                            ul, ol {{ padding-left: 20px; }}
                            li {{ margin-bottom: 4px; }}
                            a {{ color: #0366d6; text-decoration: none; }}
                            a:hover {{ text-decoration: underline; }}
                        </style>
                    </head>
                    <body>
                        <h2>Version {tag}</h2>
                        <div class='content'>{html}</div>
                    </body>
                    </html>";
                releaseNotes.DocumentText = styledHtml;
            });
        }

        private void SafeInvoke(Action action)
        {
            if (this.IsDisposed) return;
            if (this.InvokeRequired) this.Invoke(action);
            else action();
        }
    }
}