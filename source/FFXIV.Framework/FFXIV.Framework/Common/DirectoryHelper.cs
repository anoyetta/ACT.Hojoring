using System.IO;
using System.Linq;
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
            var basePathes = new string[]
            {
                Assembly.GetEntryAssembly()?.Location,
                Assembly.GetExecutingAssembly()?.Location
            };

            var dirs = basePathes
                .Where(x => !string.IsNullOrEmpty(x))
                .Select(x => Path.GetDirectoryName(x));

            foreach (var parentDir in dirs)
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
