using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace FFLogsRankingDonwloader
{
    public class AssemblyResolver
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

        public List<string> Directories { get; private set; } = new List<string>();

        public void Initialize()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= this.CustomAssemblyResolve;
            AppDomain.CurrentDomain.AssemblyResolve += this.CustomAssemblyResolve;
        }

        public void Dispose()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= this.CustomAssemblyResolve;
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

            var location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            this.Directories.Add(location);
            this.Directories.Add(Path.Combine(location, "bin"));

            var architect = Environment.Is64BitProcess ? "x64" : "x86";
            this.Directories.Add(Path.Combine(location, $@"{architect}"));
            this.Directories.Add(Path.Combine(location, $@"bin\{architect}"));

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
