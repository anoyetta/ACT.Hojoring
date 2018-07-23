#if DEBUG
// マルチスタートアップでデバッグするときの定義
#define MULTI_START_DEBUG
#endif

namespace ACT.TTSYukkuri.TTSServer
{
    using System;
    using System.Diagnostics;
    using System.Runtime.Remoting.Channels;
    using System.Runtime.Remoting.Channels.Ipc;
    using System.Threading;

    using ACT.TTSYukkuri.TTSServer.Core;

    public static class IPCTTSServerController
    {
        private static IpcClientChannel channel;

        public static TTSMessage Message
        {
            get;
            private set;
        }

        private static Process ServerProcess { get; set; }

        private static string ServerProcessPath
        {
            get
            {
                var p = string.Empty;

                var dir = TTSYukkuriPlugin.PluginDirectory;
                p = System.IO.Path.Combine(
                    dir,
                    @"ACT.TTSYukkuri.TTSServer.exe");

                return p;
            }
        }

        public static void Start()
        {
#if !MULTI_START_DEBUG
            // ゾンビプロセスがいたら殺す
            var ps = Process.GetProcessesByName("ACT.TTSYukkuri.TTSServer");
            if (ps != null)
            {
                foreach (var p in ps)
                {
                    p.Kill();
                    p.Dispose();
                }
            }

            var pi = new ProcessStartInfo(ServerProcessPath)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
            };

            ServerProcess = Process.Start(pi);
#endif
            channel = new IpcClientChannel();
            ChannelServices.RegisterChannel(channel, true);

            Message = (TTSMessage)Activator.GetObject(typeof(TTSMessage), "ipc://TTSYukkuriChannel/message");

            // 通信の確立を待つ
            // 200ms x 150 = 30s
            var ready = false;
            var retryCount = 0;
            while (!ready)
            {
                try
                {
                    Thread.Sleep(200);
                    ready = Message.IsReady();
                }
                catch (Exception ex)
                {
                    retryCount++;

                    if (retryCount >= 150)
                    {
                        Message = null;
                        throw new Exception(
                            "TT制御プロセスへの接続がタイムアウトしました。",
                            ex);
                    }
                }
            }
        }

        public static void End()
        {
            if (Message != null)
            {
                Message.End();
                Message = null;

                if (channel != null)
                {
                    ChannelServices.UnregisterChannel(channel);
                    channel = null;
                }
            }

            if (ServerProcess != null)
            {
                if (!ServerProcess.HasExited)
                {
                    ServerProcess.Kill();
                }

                ServerProcess.Dispose();
                ServerProcess = null;
            }
        }
    }
}
