using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using FFXIV.Framework.TTS.Common.Models;

namespace FFXIV.Framework.TTS.Common
{
    public class RemoteTTSClient :
        IPCClientBase
    {
        #region Singleton

        private static RemoteTTSClient instance = new RemoteTTSClient();
        public static RemoteTTSClient Instance => instance;

        #endregion Singleton

        public string BaseDirectory { get; set; }
        public ITTSModel TTSModel { get; private set; }

        public void Close()
        {
            this.UnregisterChannel();

            if (this.TTSModel != null)
            {
                this.TTSModel = null;
            }
        }

        public void Open()
        {
            this.TTSModel = this.Connect<ITTSModel>(
                Constants.RemoteTTSServiceUri,
                true);
        }

        #region TTS Server Process Controllers

        public Process GetTTSServerProcess()
        {
            return Process.GetProcessesByName(Constants.TTSServerProcessName).FirstOrDefault();
        }

        public void ShutdownTTSServer()
        {
            var p = this.GetTTSServerProcess();
            if (p != null)
            {
                p.Kill();
            }
        }

        public void StartTTSServer()
        {
            var p = this.GetTTSServerProcess();
            if (p != null)
            {
                return;
            }

            var dir = string.IsNullOrEmpty(this.BaseDirectory) ?
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) :
                this.BaseDirectory;
            var ttsServer = Path.Combine(
                dir,
                $"{Constants.TTSServerProcessName}.exe");

            Process.Start(ttsServer);
        }

        #endregion TTS Server Process Controllers
    }
}
