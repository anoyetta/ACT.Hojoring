using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;

namespace FFXIV.Framework.TTS.Common
{
    public abstract class IPCServerBase
    {
        protected IpcServerChannel serverChannel;

        protected IpcServerChannel GetChannel(
            string channelName)
        {
            var chan = ChannelServices.GetChannel(Constants.TTSServerChannelName) as IpcServerChannel;

            if (chan == null)
            {
                chan = new IpcServerChannel(Constants.TTSServerChannelName);
                ChannelServices.RegisterChannel(chan, false);
            }

            return chan;
        }

        protected IpcServerChannel RegisterRemoteObject<T>(
            T remoteObject,
            string channelName = Constants.TTSServerChannelName,
            string remoteObjectName = Constants.RemoteTTSObjectName) where T : MarshalByRefObject
        {
            this.serverChannel = this.GetChannel(channelName);

            if (remoteObject != null)
            {
                RemotingServices.Marshal(remoteObject, remoteObjectName, typeof(T));
            }

            return this.serverChannel;
        }

        protected void UnregisterChannel()
        {
            if (this.serverChannel != null)
            {
                ChannelServices.UnregisterChannel(this.serverChannel);
                this.serverChannel = null;
            }
        }
    }
}
