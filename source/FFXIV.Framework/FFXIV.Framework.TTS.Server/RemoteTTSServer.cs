using System;
using FFXIV.Framework.Common;
using FFXIV.Framework.TTS.Common;
using FFXIV.Framework.TTS.Server.Models;
using FFXIV.Framework.TTS.Server.Views;
using NLog;

namespace FFXIV.Framework.TTS.Server
{
    public class RemoteTTSServer :
        IPCServerBase
    {
        #region Singleton

        private static readonly Lazy<RemoteTTSServer> LazyInstance = new Lazy<RemoteTTSServer>(() => new RemoteTTSServer());

        public static RemoteTTSServer Instance => LazyInstance.Value;

        public RemoteTTSServer()
        {
        }

        #endregion Singleton

        #region Logger

        private Logger Logger => AppLog.DefaultLogger;

        #endregion Logger

        public TTSModel TTSModel { get; private set; } = new TTSModel();

        public void Close()
        {
            this.UnregisterChannel();

            this.Logger.Info($"IPC Channel Closed.");
        }

        public void Open()
        {
            var chan = this.RegisterRemoteObject(this.TTSModel);

            var uri = $"{chan.GetChannelUri()}/{Constants.RemoteTTSObjectName}";
            MainView.Instance.ViewModel.IPCChannelUri = uri;

            this.Logger.Info($"IPC Channel Listened. Uri={uri}");
        }
    }
}
