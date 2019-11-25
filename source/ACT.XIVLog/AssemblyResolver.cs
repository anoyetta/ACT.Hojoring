using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Advanced_Combat_Tracker;

namespace ACT.XIVLog
{
    public class AssemblyResolver :
        IDisposable
    {
        #region Singleton

        private static AssemblyResolver instance;

        public static AssemblyResolver Instance =>
            instance ?? (instance = new AssemblyResolver());

        public static void Free()
        {
            instance?.Dispose();
            instance = null;
        }

        #endregion Singleton

        private IActPluginV1 plugin;
        public List<string> Directories { get; private set; } = new List<string>();

        public void Initialize(
            IActPluginV1 plugin)
        {
            this.plugin = plugin;

            this.Directories.Add(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                @"Advanced Combat Tracker\Plugins"));

            AppDomain.CurrentDomain.AssemblyResolve -= this.CustomAssemblyResolve;
            AppDomain.CurrentDomain.AssemblyResolve += this.CustomAssemblyResolve;
        }

        public void Dispose()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= this.CustomAssemblyResolve;
            this.plugin = null;
        }

        private Assembly CustomAssemblyResolve(object sender, ResolveEventArgs e)
        {
            Assembly tryLoadAssembly(
                string directory,
                string extension)
            {
                var asm = new AssemblyName(e.Name);

                var asmPath = Path.Combine(directory, asm.Name + extension);
                if (File.Exists(asmPath))
                {
                    return Assembly.LoadFrom(asmPath);
                }

                return null;
            }

            var pluginDirectory = ActGlobals.oFormActMain?.PluginGetSelfData(this.plugin)?.pluginFile.DirectoryName;
            if (!string.IsNullOrEmpty(pluginDirectory))
            {
                if (!this.Directories.Any(x => x == pluginDirectory))
                {
                    this.Directories.Add(pluginDirectory);
                    this.Directories.Add(Path.Combine(pluginDirectory, "bin"));

                    var architect = Environment.Is64BitProcess ? "x64" : "x86";
                    this.Directories.Add(Path.Combine(pluginDirectory, $@"{architect}"));
                    this.Directories.Add(Path.Combine(pluginDirectory, $@"bin\{architect}"));
                }
            }

            // Directories プロパティで指定されたディレクトリを基準にアセンブリを検索する
            foreach (var directory in this.Directories)
            {
                var asm = tryLoadAssembly(directory, ".dll");
                if (asm != null)
                {
                    return asm;
                }
            }

            return null;
        }
    }
}
