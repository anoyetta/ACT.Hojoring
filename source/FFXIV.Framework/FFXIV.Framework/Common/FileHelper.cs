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
    }
}
