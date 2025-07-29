using System;

namespace FFXIV.Framework.Common
{
    public class AppendedLogEventArgs :
        EventArgs
    {
        public AppLogEntry AppendedLogEntry { get; private set; } = new AppLogEntry();

        public AppendedLogEventArgs(AppLogEntry e)
        {
            this.AppendedLogEntry = e;
        }
    }
}
