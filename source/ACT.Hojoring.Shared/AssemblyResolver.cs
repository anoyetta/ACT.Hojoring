using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace ACT.Hojoring.Shared
{
    public static class AssemblyResolver
    {
        public static void Initialize(
            Func<string> directoryResolver)
        {
            DirectoryResolvers.Add(directoryResolver);
            AppDomain.CurrentDomain.AssemblyResolve += CustomAssemblyResolve;
        }

        private static readonly List<Func<string>> DirectoryResolvers = new List<Func<string>>();

        private static Assembly CustomAssemblyResolve(object sender, ResolveEventArgs e)
        {
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
