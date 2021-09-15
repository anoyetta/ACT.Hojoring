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
            string subDirectoryName)
            => GetPluginRootDirectoryDelegate != null ?
            Path.Combine(GetPluginRootDirectoryDelegate.Invoke(), subDirectoryName ?? string.Empty) :
            subDirectoryName;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string FindFile(
            string fileName,
            string subDirectoryName = null)
        {
            var dirs = new[]
            {
                GetPluginRootDirectoryDelegate?.Invoke(),
                FindSubDirectory(subDirectoryName),
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
