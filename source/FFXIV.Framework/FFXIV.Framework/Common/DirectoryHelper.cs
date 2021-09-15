using System;
using System.Collections.Generic;
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
            => FindSubDirectories(subDirectoryName).FirstOrDefault();

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static IEnumerable<string> FindSubDirectories(
            string subDirectoryName)
        {
            var basePathes = new string[]
            {
                Assembly.GetEntryAssembly()?.Location,
                Assembly.GetExecutingAssembly()?.Location,
            };

            var dirs = basePathes
                .Where(x => !string.IsNullOrEmpty(x))
                .Select(x => Path.GetDirectoryName(x));

            foreach (var parentDir in dirs)
            {
                var dir = !string.IsNullOrEmpty(subDirectoryName) ?
                    Path.Combine(parentDir, subDirectoryName) :
                    parentDir;

                if (Directory.Exists(dir))
                {
                    yield return dir;
                }
                else
                {
                    if (parentDir.EndsWith("bin", StringComparison.OrdinalIgnoreCase))
                    {
                        dir = !string.IsNullOrEmpty(subDirectoryName) ?
                            Path.Combine(parentDir, "..", subDirectoryName) :
                            Path.Combine(parentDir, "..");

                        if (Directory.Exists(dir))
                        {
                            yield return dir;
                        }
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string FindFile(
            string fileName,
            string subDirectoryName = null)
        {
            var dirs = FindSubDirectories(subDirectoryName);

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
