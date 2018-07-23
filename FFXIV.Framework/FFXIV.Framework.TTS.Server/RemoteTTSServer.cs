using FFXIV.Framework.Common;
using FFXIV.Framework.TTS.Common;
using FFXIV.Framework.TTS.Common.Models;
using FFXIV.Framework.TTS.Server.Models;
using FFXIV.Framework.TTS.Server.Views;
using NLog;

namespace FFXIV.Framework.TTS.Server
{
    public class RemoteTTSServer :
        IPCServerBase
    {
        #region Singleton

        private static RemoteTTSServer instance = new RemoteTTSServer();
        public static RemoteTTSServer Instance => instance;

        #endregion Singleton

        #region Logger

        private Logger Logger => AppLog.DefaultLogger;

        #endregion Logger

        private TTSModel ttsModel = new TTSModel();
        public TTSModel TTSModel => this.ttsModel;

        public void Close()
        {
            this.UnregisterChannel();

            this.Logger.Info($"IPC Channel Closed.");
        }

        public void Open()
        {
            var chan = this.RegisterRemoteObject(
                Constants.TTSServerChannelName,
                this.ttsModel,
                Constants.RemoteTTSObjectName,
                typeof(ITTSModel));

            var uri = $"{chan.GetChannelUri()}/{Constants.RemoteTTSObjectName}";
            MainView.Instance.ViewModel.IPCChannelUri = uri;

            this.Logger.Info($"IPC Channel Listened. Uri={uri}");
        }
    }
}
