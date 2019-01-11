using System.IO;

namespace FFXIV.Framework.Common
{
    public static class MasterFilePublisher
    {
        private static readonly object locker = new object();

        public static void Publish()
        {
            var dir = DirectoryHelper.FindSubDirectory("resources");
            var masters = Directory.GetFiles(dir, "*.master*");
            if (masters == null)
            {
                return;
            }

            lock (locker)
            {
                foreach (var master in masters)
                {
                    var publish = master.Replace(".master", string.Empty);
                    if (!File.Exists(publish))
                    {
                        File.Copy(master, publish);
                    }
                }
            }
        }
    }
}
