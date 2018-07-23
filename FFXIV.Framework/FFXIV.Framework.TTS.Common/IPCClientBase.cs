using System;
using System.Diagnostics;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Threading;
using FFXIV.Framework.TTS.Common.Models;

namespace FFXIV.Framework.TTS.Common
{
    public abstract class IPCClientBase
    {
        private const double ConnectionTimeout = 30;

        private IpcClientChannel clientChannel;

        public T Connect<T>(
            string remoteObjectUri,
            bool isWait = false)
        {
            if (this.clientChannel == null)
            {
                this.clientChannel = new IpcClientChannel();
                ChannelServices.RegisterChannel(this.clientChannel, false);
            }

            var remoteObject = (T)Activator.GetObject(
                typeof(T),
                remoteObjectUri);

            if (!isWait)
            {
                return remoteObject;
            }

            var readyObject = remoteObject as IReady;
            if (readyObject == null)
            {
                return remoteObject;
            }

            // 通信の確立を待つ
            Exception exception = null;
            var sw = Stopwatch.StartNew();
            do
            {
                try
                {
                    Thread.Sleep(100);
                    if (readyObject.IsReady())
                    {
                        return remoteObject;
                    }
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            } while (sw.Elapsed.TotalSeconds <= ConnectionTimeout);
            sw.Stop();

            throw new TimeoutException(
                $"Timeout Connect to {remoteObjectUri}",
                exception);
        }

        public void UnregisterChannel()
        {
            if (this.clientChannel != null)
            {
                ChannelServices.UnregisterChannel(this.clientChannel);
                this.clientChannel = null;
            }
        }
    }
}
