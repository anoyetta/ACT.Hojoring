using System.IO;

namespace FFXIV.Framework.Common
{
    public static class EnvironmentMigrater
    {
        private static readonly object locker = new object();

        public static void Migrate()
        {
            lock (locker)
            {
                var references = DirectoryHelper.FindSubDirectory("references");
                if (Directory.Exists(references))
                {
                    var sqlite = Path.Combine(references, "SQLite.Interop.dll");
                    if (File.Exists(sqlite))
                    {
                        File.Delete(sqlite);
                    }
                }
            }
        }
    }
}
