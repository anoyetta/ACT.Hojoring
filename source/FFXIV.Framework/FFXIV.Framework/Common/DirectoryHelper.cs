using System.IO;
using System.Runtime.CompilerServices;

namespace FFXIV.Framework.Common
{
    public class DirectoryHelper
    {
        public delegate string GetPluginRootDirectory();

        public static GetPluginRootDirectory GetPluginRootDirectoryDelegate;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string FindSubDirectory(
            string path)
            => GetPluginRootDirectoryDelegate != null ?
            Path.Combine(GetPluginRootDirectoryDelegate.Invoke(), path) :
            path;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string FindSubDirectory(
            string path1, string path2)
            => GetPluginRootDirectoryDelegate != null ?
            Path.Combine(GetPluginRootDirectoryDelegate.Invoke(), path1, path2) :
            Path.Combine(path1, path2);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string FindSubDirectory(
            string path1, string path2, string path3)
            => GetPluginRootDirectoryDelegate != null ?
            Path.Combine(GetPluginRootDirectoryDelegate.Invoke(), path1, path2, path3) :
            Path.Combine(path1, path2, path3);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string FindSubDirectory(
            string path1, string path2, string path3, string path4)
            => GetPluginRootDirectoryDelegate != null ?
            Path.Combine(GetPluginRootDirectoryDelegate.Invoke(), path1, path2, path3, path4) :
            Path.Combine(path1, path2, path3, path4);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string FindFile(
            string fileName,
            string subDirectoryName = null)
        {
            var dirs = new[]
            {
                GetPluginRootDirectoryDelegate?.Invoke(),
                FindSubDirectory(subDirectoryName ?? string.Empty),
            };

            foreach (var dir in dirs)
            {
                var f = Path.Combine(dir, fileName);
                if (File.Exists(f))
                {
                    return f;
                }
            }

            return fileName;
        }

        public static void DirectoryCopy(string sourcePath, string destinationPath)
        {
            var sourceDirectory = new DirectoryInfo(sourcePath);
            var destinationDirectory = new DirectoryInfo(destinationPath);

            if (destinationDirectory.Exists == false)
            {
                destinationDirectory.Create();
                destinationDirectory.Attributes = sourceDirectory.Attributes;
            }

            foreach (FileInfo fileInfo in sourceDirectory.GetFiles())
            {
                fileInfo.CopyTo(destinationDirectory.FullName + @"\" + fileInfo.Name, true);
            }

            foreach (DirectoryInfo directoryInfo in sourceDirectory.GetDirectories())
            {
                DirectoryCopy(directoryInfo.FullName, destinationDirectory.FullName + @"\" + directoryInfo.Name);
            }
        }
    }
}
