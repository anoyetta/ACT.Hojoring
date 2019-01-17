using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace FFXIV.Framework.Common
{
    public class DirectoryHelper
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string FindSubDirectory(
            string subDirectoryName)
        {
            var parentDirs = new string[]
            {
                Path.GetDirectoryName(Assembly.GetEntryAssembly().Location),
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
            };

            foreach (var parentDir in parentDirs)
            {
                var dir = Path.Combine(parentDir, subDirectoryName);
                if (Directory.Exists(dir))
                {
                    return dir;
                }
            }

            return string.Empty;
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
