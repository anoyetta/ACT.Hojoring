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
    }
}
