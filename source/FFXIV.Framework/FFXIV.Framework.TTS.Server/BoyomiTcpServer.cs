using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using FFXIV.Framework.Common;
using FFXIV.Framework.TTS.Server.Models;
using NLog;
using Prism.Mvvm;

namespace FFXIV.Framework.TTS.Server
{
    public class BoyomiTcpServer : BindableBase
    {
        #region Lazy Instance

        private static readonly Lazy<BoyomiTcpServer> LazyInstance = new Lazy<BoyomiTcpServer>(() => new BoyomiTcpServer());

        public static BoyomiTcpServer Instance => LazyInstance.Value;

        private BoyomiTcpServer()
        {
        }

        #endregion Lazy Instance

        #region Logger

        private Logger Logger => AppLog.DefaultLogger;

        #endregion Logger

        private TcpListener server;

        private bool isRunning;

        public bool IsRunning
        {
            get => this.isRunning;
            set => this.SetProperty(ref this.isRunning, value);
        }

        public void Start(
            int port)
        {
            if (port < 1 || port > 65535)
            {
                this.Logger.Error($"Boyomi TCP server error. invalid port no [{port}].");
                return;
            }

            lock (this)
            {
                if (this.server != null)
                {
                    this.Logger.Error($"Boyomi TCP server has already started.");
                    return;
                }

                this.server = new TcpListener(IPAddress.Any, port);
                this.server.Start();
                this.BeginAccept();

                this.speakTask = new ThreadWorker(
                    this.DoSpeak,
                    SpeakDefaultInterval,
                    "Boyomi clone server speak task",
                    ThreadPriority.Lowest);

                this.speakTask.Run();

                this.IsRunning = true;
                this.Logger.Info($"Boyomi TCP server started. port={port}");
            }
        }

        public void Stop()
        {
            lock (this)
            {
                if (this.server != null)
                {
                    this.server.Stop();
                    this.server = null;
                }

                if (this.speakTask != null)
                {
                    this.speakTask.Abort();
                    this.speakTask = null;
                }

                this.IsRunning = false;
                this.Logger.Info($"Boyomi TCP server stoped.");
            }
        }

        private void BeginAccept()
        {
            this.server.AcceptTcpClientAsync().ContinueWith(t =>
            {
                lock (this)
                {
                    if (this.server == null)
                    {
                        return;
                    }

                    this.BeginAccept();
                    this.ProcessMessage(t.Result?.GetStream());
                }
            });
        }

        private void ProcessMessage(
            NetworkStream stream)
        {
            if (stream == null)
            {
                return;
            }

            using (var reader = new BinaryReader(stream))
            {
                var command = reader.ReadInt16();
                if (command != 0)
                {
                    this.Logger.Error($"Boyomi TCP server errer. invalid command [{command}].");
                    return;
                }

                var speed = reader.ReadInt16();
                var volume = reader.ReadInt16();
                var type = reader.ReadInt16();

                var textEncoding = reader.ReadByte();
                var textSize = reader.ReadInt32();
                var textChars = reader.ReadBytes(textSize);

                var text = textEncoding switch
                {
                    0 => Encoding.UTF8.GetString(textChars),
                    1 => Encoding.Unicode.GetString(textChars),
                    2 => Encoding.GetEncoding("Shift_JIS").GetString(textChars),
                    _ => string.Empty,
                };

                if (string.IsNullOrEmpty(text))
                {
                    return;
                }

                this.SpeakQueue.Enqueue(new SpeakTask()
                {
                    Speed = (uint)speed,
                    Volume = (uint)volume,
                    CastNo = type,
                    Text = text,
                });
            }
        }

        private readonly ConcurrentQueue<SpeakTask> SpeakQueue = new ConcurrentQueue<SpeakTask>();

        private static readonly int SpeakDefaultInterval = 100;

        private ThreadWorker speakTask;

        private void DoSpeak()
        {
            while (this.SpeakQueue.TryDequeue(out SpeakTask task))
            {
                CevioModel.Instance.Speak(task.Text);
                Thread.Sleep(1);
            }
        }
    }

    public class SpeakTask
    {
        public uint Speed { get; set; }

        public uint Volume { get; set; }

        public int CastNo { get; set; }

        public string Text { get; set; }
    }
}
