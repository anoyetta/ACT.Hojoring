using System;
using System.IO;
using System.Text;

namespace ACT.Hojoring.Activator
{
    internal class Logger
    {
        #region Lazy Singleton

        private static readonly Lazy<Logger> LazyInstance = new Lazy<Logger>(() => new Logger());

        internal static Logger Instance => LazyInstance.Value;

        private Logger()
        {
        }

        #endregion Lazy Singleton

        private Lazy<string> LazyLogFileName = new Lazy<string>(() =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "anoyetta",
                "ACT",
                "logs",
                "activator.log"));

        internal void Write(string message, Exception ex = null)
        {
            var log = string.Empty;

            log += $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";

            if (ex != null)
            {
                log += Environment.NewLine + ex.ToString();
            }

            lock (this)
            {
                var dir = Path.GetDirectoryName(this.LazyLogFileName.Value);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                File.AppendAllText(
                    this.LazyLogFileName.Value,
                    log + Environment.NewLine,
                    new UTF8Encoding(false));
            }
        }
    }
}
