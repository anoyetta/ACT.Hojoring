using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FFXIV.Framework.Common
{
    public class FileHelper
    {
        public static void CreateDirectory(
            string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                var dir = Path.GetDirectoryName(path);

                if (!string.IsNullOrEmpty(dir))
                {
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                }
            }
        }

        public static string[] FindFiles(
            string directory,
            string fileName)
        {
            var files = new List<string>();

            files.AddRange(Directory.GetFiles(
                directory,
                fileName));

            foreach (var dir in Directory.GetDirectories(directory))
            {
                files.AddRange(FindFiles(dir, fileName));
            }

            return files.OrderBy(x => x).ToArray();
        }

        public static void SetReadOnly(
            string file)
        {
            if (!File.Exists(file))
            {
                return;
            }

            var att = File.GetAttributes(file);
            att |= FileAttributes.ReadOnly;
            File.SetAttributes(file, att);
        }

        public static void DeleteForce(
            string file)
        {
            if (!File.Exists(file))
            {
                return;
            }

            var fi = new FileInfo(file);

            if ((fi.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
            {
                fi.Attributes = FileAttributes.Normal;
            }

            fi.Delete();
        }
    }
}
