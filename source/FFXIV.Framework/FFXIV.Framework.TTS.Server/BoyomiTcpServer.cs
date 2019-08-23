using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using FFXIV.Framework.Common;
using FFXIV.Framework.Extensions;
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

        private int port;

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

                try
                {
                    this.port = port;
                    this.server = new TcpListener(IPAddress.Any, port);
                    this.server.Start();
                    this.BeginAccept();
                    this.Logger.Info($"Boyomi TCP server started. port={this.port}");

                    this.speakTask = new ThreadWorker(
                        this.DoSpeak,
                        SpeakDefaultInterval,
                        "Boyomi clone server speak task",
                        ThreadPriority.BelowNormal);

                    this.speakTask.Run();

                    this.IsRunning = true;
                }
                catch (Exception ex)
                {
                    this.Logger.Error(ex, $"Error, Boyomi TCP server on starting.");

                    this.server?.Stop();
                    this.server = null;
                }
            }
        }

        private volatile bool isStoping = false;

        public void Stop()
        {
            try
            {
                this.isStoping = true;

                lock (this)
                {
                    this.speakTask?.Abort();
                    this.speakTask = null;

                    this.server?.Stop();
                    Thread.Sleep(100);
                    this.server = null;

                    if (this.isRunning)
                    {
                        this.IsRunning = false;
                        this.Logger.Info($"Boyomi TCP server stoped.");
                    }
                }
            }
            finally
            {
                this.isStoping = false;
            }
        }

        private void BeginAccept()
        {
            lock (this)
            {
                if (this.server == null ||
                    this.isStoping)
                {
                    return;
                }

                this.server?.BeginAcceptTcpClient((result) =>
                {
                    try
                    {
                        lock (this)
                        {
                            if (this.server == null ||
                                this.isStoping)
                            {
                                return;
                            }

                            Thread.Sleep(10);

                            using (var client = this.server?.EndAcceptTcpClient(result))
                            using (var ns = client?.GetStream())
                            {
                                this.ProcessMessage(ns);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        this.Logger.Error(ex, $"Boyomi TCP server error.");
                    }
                    finally
                    {
                        this.BeginAccept();
                    }
                },
                this.server);
            }
        }

        private static readonly Regex WhitespacesRegex = new Regex(@"\s{2,}", RegexOptions.Compiled);

        private void ProcessMessage(
            NetworkStream stream)
        {
            if (stream == null)
            {
                return;
            }

            using (var reader = new BinaryReader(stream))
            {
                var speed = default(short);
                var tone = default(short);
                var volume = default(short);
                var type = default(short);
                var textEncoding = default(byte);
                var textSize = default(int);
                var textChars = default(byte[]);

                var command = reader.ReadInt16();
                switch (command)
                {
                    case 0:
                        speed = reader.ReadInt16();
                        tone = -1;
                        volume = reader.ReadInt16();
                        type = reader.ReadInt16();
                        textEncoding = reader.ReadByte();
                        textSize = reader.ReadInt32();
                        textChars = reader.ReadBytes(textSize);
                        break;

                    case 1:
                        speed = reader.ReadInt16();
                        tone = reader.ReadInt16();
                        volume = reader.ReadInt16();
                        type = reader.ReadInt16();
                        textEncoding = reader.ReadByte();
                        textSize = reader.ReadInt32();
                        textChars = reader.ReadBytes(textSize);
                        break;

                    case 48:
                        this.Logger.Info($"[{command}] skip talk queues, but no process on this server.");
                        return;

                    case 64:
                        while (this.SpeakQueue.TryDequeue(out SpeakTask t)) ;
                        this.Logger.Info($"[{command}] clear talk queues.");
                        return;

                    default:
                        this.Logger.Error($"Boyomi TCP server error. invalid command [{command}].");
                        return;
                }

                var text = textEncoding switch
                {
                    0 => Encoding.UTF8.GetString(textChars),
                    1 => Encoding.Unicode.GetString(textChars),
                    2 => Encoding.GetEncoding("Shift_JIS").GetString(textChars),
                    _ => string.Empty,
                };

                if (string.IsNullOrWhiteSpace(text))
                {
                    return;
                }

                if (speed == -1)
                {
                    speed = 50;
                }

                if (volume == -1)
                {
                    volume = 50;
                }

                // 99文字ずつで分割する
                var texts = text.Trim().Split(99);

                foreach (var t in texts)
                {
                    var tts = t.Trim();
                    tts = tts.Replace("　", " ");
                    tts = WhitespacesRegex.Replace(tts, " ");

                    this.SpeakQueue.Enqueue(new SpeakTask()
                    {
                        Speed = (uint)speed,
                        Volume = (uint)volume,
                        CastNo = type,
                        Text = tts,
                    });
                }
            }
        }

        private readonly ConcurrentQueue<SpeakTask> SpeakQueue = new ConcurrentQueue<SpeakTask>();

        private static readonly int SpeakDefaultInterval = 100;

        private ThreadWorker speakTask;

        private void DoSpeak()
        {
            if (this.server == null)
            {
                Thread.Sleep(1000);
                return;
            }

            while (this.SpeakQueue.TryDequeue(out SpeakTask task))
            {
                CevioModel.Instance.Speak(
                    task.Text,
                    speed: task.Speed,
                    volume: task.Volume);
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
