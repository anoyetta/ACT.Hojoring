using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace ACT.Hojoring.Shared
{
    public static class AssemblyResolver
    {
        private static bool isConflictChecked = false;
        private static readonly object ConflictLock = new object();

        // Costuraに埋め込まれているアセンブリ名のリスト
        private static readonly HashSet<string> EmbeddedAssemblies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public static void Initialize(
            Func<string> directoryResolver)
        {
            // 埋め込みアセンブリ名をスキャンして収集
            ScanEmbeddedAssemblies();

            DirectoryResolvers.Add(directoryResolver);
            AppDomain.CurrentDomain.AssemblyResolve += CustomAssemblyResolve;

            // 起動時に1回だけアセンブリ競合をチェックする
            lock (ConflictLock)
            {
                if (!isConflictChecked)
                {
                    isConflictChecked = true;
                    // 起動をブロックしないよう非同期でチェックを実行
                    // 競合検出が強すぎて不要な警告が出るため一時的に呼び出しを無効化
                    // System.Threading.Tasks.Task.Run(() => CheckConflicts(directoryResolver?.Invoke()));
                }
            }
        }

        private static void ScanEmbeddedAssemblies()
        {
            try
            {
                // 現在のAppDomainにロードされている全アセンブリを走査
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        var name = asm.GetName().Name;
                        if (string.IsNullOrEmpty(name)) continue;

                        // Hojoring関連アセンブリのみを走査対象にして無関係なスキャンを避ける
                        if (name.StartsWith("ACT.Hojoring", StringComparison.OrdinalIgnoreCase) ||
                            name.StartsWith("FFXIV.Framework", StringComparison.OrdinalIgnoreCase) ||
                            name.StartsWith("ACT.SpecialSpellTimer", StringComparison.OrdinalIgnoreCase) ||
                            name.StartsWith("ACT.TTSYukkuri", StringComparison.OrdinalIgnoreCase) ||
                            name.StartsWith("ACT.UltraScouter", StringComparison.OrdinalIgnoreCase) ||
                            name.StartsWith("ACT.XIVLog", StringComparison.OrdinalIgnoreCase))
                        {
                            var resourceNames = asm.GetManifestResourceNames();
                            if (resourceNames == null) continue;

                            foreach (var resName in resourceNames)
                            {
                                if (resName.StartsWith("costura.", StringComparison.OrdinalIgnoreCase))
                                {
                                    string cleanName = resName.Substring("costura.".Length);
                                    if (cleanName.EndsWith(".dll.compressed", StringComparison.OrdinalIgnoreCase))
                                    {
                                        cleanName = cleanName.Substring(0, cleanName.Length - ".dll.compressed".Length);
                                    }
                                    else if (cleanName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                                    {
                                        cleanName = cleanName.Substring(0, cleanName.Length - ".dll".Length);
                                    }

                                    if (!string.IsNullOrEmpty(cleanName))
                                    {
                                        EmbeddedAssemblies.Add(cleanName);
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        // 個別アセンブリのスキャン失敗は無視
                    }
                }
            }
            catch
            {
                // 全体的な例外は無視して安全性を確保
            }
        }

        private static void CheckConflicts(string baseDir)
        {
            try
            {
                if (string.IsNullOrEmpty(baseDir)) return;

                // 探索するパス（Hojoringのルートフォルダとbinフォルダ）
                var searchPaths = new[] { baseDir, Path.Combine(baseDir, "bin") };
                var dllFiles = new List<string>();
                foreach (var path in searchPaths)
                {
                    if (Directory.Exists(path))
                    {
                        dllFiles.AddRange(Directory.GetFiles(path, "*.dll"));
                    }
                }

                // 現在プロセスにロードされているアセンブリの一覧を収集
                var loadedAssemblies = new Dictionary<string, Version>(StringComparer.OrdinalIgnoreCase);
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        var name = asm.GetName();
                        if (name != null && !string.IsNullOrEmpty(name.Name))
                        {
                            if (!loadedAssemblies.ContainsKey(name.Name))
                            {
                                loadedAssemblies[name.Name] = name.Version;
                            }
                        }
                    }
                    catch
                    {
                        // Ignore
                    }
                }

                var conflicts = new List<string>();

                foreach (var dllPath in dllFiles)
                {
                    try
                    {
                        // ファイル名からアセンブリ名を取得（ロードはしない）
                        var bundledAsmName = AssemblyName.GetAssemblyName(dllPath);
                        if (bundledAsmName != null && !string.IsNullOrEmpty(bundledAsmName.Name))
                        {
                            if (loadedAssemblies.TryGetValue(bundledAsmName.Name, out var loadedVersion))
                            {
                                var bundledVersion = bundledAsmName.Version;
                                if (loadedVersion != null && bundledVersion != null)
                                {
                                    // ロードされているバージョンが、同梱のバージョンより古い場合（競合）
                                    if (loadedVersion < bundledVersion)
                                    {
                                        conflicts.Add($"・{bundledAsmName.Name}\n  [ロード済] {loadedVersion}  ➔  [必要] {bundledVersion}");
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        // ネイティブDLLなどは例外になるのでスルー
                    }
                }

                if (conflicts.Count > 0)
                {
                    ShowConflictWarning(conflicts);
                }
            }
            catch
            {
                // 診断処理が原因でプラグインのロード自体が落ちないよう安全にスルー
            }
        }

        private static void ShowConflictWarning(List<string> conflicts)
        {
            var details = string.Join("\n", conflicts);
            var message = 
                $"【重要】他プラグインとのアセンブリ（DLL）競合を検出しました。\n" +
                $"Hojoringが必要とするものより古いバージョンのDLLが先にロードされています。この状態では、すぺすぺタイムラインのロードエラーや発話機能（TTS）が正常に動かない可能性があります。\n\n" +
                $"■ 競合アセンブリの一覧:\n{details}\n\n" +
                $"【推奨する解決方法】\n" +
                $"1. ACTの「Plugins」➔「Plugin Listing」タブを開きます。\n" +
                $"2. 「ACT.SpecialSpellTimer.dll」や「ACT.TTSYukkuri.dll」などのHojoringプラグインを, 右側の『UP (⬆️)』ボタンでリストの最上部（FFXIV_ACT_Plugin.dllのすぐ下）に引き上げてください。\n" +
                $"3. OverlayPluginやCactbotなどの併用プラグインをすべて最新版にアップデートしてください。\n" +
                $"4. ACTを再起動してください。";

            try
            {
                // リフレクションを使って System.Windows.Forms.MessageBox.Show を動的に呼び出す。
                // これにより、コンパイル時の参照設定に依存しないポータブルな警告ダイアログを実現。
                var winFormsAsm = Assembly.Load("System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
                var msgBoxType = winFormsAsm.GetType("System.Windows.Forms.MessageBox");
                var msgBoxButtonsType = winFormsAsm.GetType("System.Windows.Forms.MessageBoxButtons");
                var msgBoxIconType = winFormsAsm.GetType("System.Windows.Forms.MessageBoxIcon");

                var okButton = Enum.Parse(msgBoxButtonsType, "OK");
                var warningIcon = Enum.Parse(msgBoxIconType, "Warning");

                var showMethod = msgBoxType.GetMethod("Show", new[] { typeof(string), typeof(string), msgBoxButtonsType, msgBoxIconType });
                if (showMethod != null)
                {
                    showMethod.Invoke(null, new object[] { message, "ACT.Hojoring - アセンブリ競合検出", okButton, warningIcon });
                }
            }
            catch
            {
                Console.Error.WriteLine("[ACT.Hojoring] Assembly Conflict Detected:\n" + message);
            }
        }

        private static readonly List<Func<string>> DirectoryResolvers = new List<Func<string>>();

        private static Assembly CustomAssemblyResolve(object sender, ResolveEventArgs e)
        {
            try
            {
                var asmName = new AssemblyName(e.Name);
                if (asmName != null && !string.IsNullOrEmpty(asmName.Name))
                {
                    // Costura埋め込み対象のアセンブリである場合、物理ファイルからのロードを回避する。
                    // nullを返すことで.NET FrameworkはCostura自身のハンドラでアセンブリを解決する。
                    if (EmbeddedAssemblies.Contains(asmName.Name))
                    {
                        return null;
                    }
                }
            }
            catch
            {
                // Ignore
            }

            var dirs = new List<string>();

            foreach (var directoryResolver in DirectoryResolvers)
            {
                if (directoryResolver == null)
                {
                    continue;
                }

                var baseDir = directoryResolver?.Invoke();
                if (string.IsNullOrEmpty(baseDir))
                {
                    continue;
                }

                dirs.Add(baseDir);
                dirs.Add(Path.Combine(baseDir, "bin"));

                var architect = Environment.Is64BitProcess ? "x64" : "x86";
                dirs.Add(Path.Combine(baseDir, $@"{architect}"));
                dirs.Add(Path.Combine(baseDir, $@"bin\{architect}"));
            }

            // Directories プロパティで指定されたディレクトリを基準にアセンブリを検索する
            foreach (var directory in dirs)
            {
                var asm = TryLoadAssembly(e.Name, directory, ".dll");
                if (asm != null)
                {
                    return asm;
                }
            }

            return null;
        }

        private static Assembly TryLoadAssembly(
            string assemblyName,
            string directory,
            string extension)
        {
            var asm = new AssemblyName(assemblyName);

            var asmPath = Path.Combine(directory, asm.Name + extension);
            if (File.Exists(asmPath))
            {
                return Assembly.LoadFrom(asmPath);
            }

            return null;
        }
    }
}
