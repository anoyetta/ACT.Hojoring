using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ACT.TTSYukkuri.Config;
using Discord;
using Discord.Audio;
using Discord.WebSocket;
using FFXIV.Framework.Bridge;
using FFXIV.Framework.Common;
using NLog;
using Prism.Mvvm;

namespace ACT.TTSYukkuri.Discord.Models
{
    public class DiscordNetModel :
        BindableBase,
        IDiscordClientModel
    {
        #region Singleton

        private static DiscordNetModel instance;

        public static DiscordNetModel Instance =>
            instance ?? (instance = new DiscordNetModel());

        private DiscordNetModel()
        {
        }

        #endregion Singleton

        #region Logger

        private Logger Logger => AppLog.DefaultLogger;

        private readonly StringBuilder log = new StringBuilder();

        public string Log => this.log.ToString();

        private void AppendLogLine(
            string message,
            Exception ex = null,
            bool err = false)
        {
            // UIに出力する
            var text = $"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffff")}] {message}";
            if (ex != null)
            {
                text += Environment.NewLine + ex.ToString();
            }

            this.log.AppendLine(text);
            WPFHelper.BeginInvoke(() => this.RaisePropertyChanged(nameof(this.Log)));

            // NLogに出力する
            var log = $"[DISCORD] {message}";
            if (ex == null && !err)
            {
                this.Logger.Trace(log);
            }
            else
            {
                this.Logger.Error(ex, log);
            }
        }

        #endregion Logger

        #region IDiscordClientModel

        private bool connected;

        public bool IsConnected
        {
            get => this.connected;
            set => this.SetProperty(ref this.connected, value);
        }

        private bool joinedVoiceChannel;

        public bool IsJoinedVoiceChannel
        {
            get => this.joinedVoiceChannel;
            set => this.SetProperty(ref this.joinedVoiceChannel, value);
        }

        private string previousAvailableTextChannelID;
        private string previousAvailableVoiceChannelID;

        private DiscordChannelContainer selectedTextChannel;

        public DiscordChannelContainer SelectedTextChannel
        {
            get => this.selectedTextChannel;
            set
            {
                this.selectedTextChannel = value;

                var id = (value?.ID ?? 0).ToString();
                if (id == "0")
                {
                    this.previousAvailableTextChannelID = this.Config.DefaultTextChannelID;
                }

                this.Config.DefaultTextChannelID = id;

                this.RaisePropertyChanged();
            }
        }

        private DiscordChannelContainer selectedVoiceChannel;

        public DiscordChannelContainer SelectedVoiceChannel
        {
            get => this.selectedVoiceChannel;
            set
            {
                this.selectedVoiceChannel = value;

                var id = (value?.ID ?? 0).ToString();
                if (id == "0")
                {
                    this.previousAvailableVoiceChannelID = this.Config.DefaultVoiceChannelID;
                }

                this.Config.DefaultVoiceChannelID = id;

                this.RaisePropertyChanged();
            }
        }

        public string[] AvailableGuilds => this.guilds
            .OrderBy(x => x.Id)
            .Select(x => x.Name).ToArray();

        public string AvailableGuildsText => string.Join(
            Environment.NewLine,
            this.guilds);

        private readonly ObservableCollection<DiscordChannelContainer> channels = new ObservableCollection<DiscordChannelContainer>();

        private readonly ObservableCollection<DiscordChannelContainer> textChannels = new ObservableCollection<DiscordChannelContainer>();

        private readonly ObservableCollection<DiscordChannelContainer> voiceChannels = new ObservableCollection<DiscordChannelContainer>();

        public ObservableCollection<DiscordChannelContainer> Channels => this.channels;

        public ObservableCollection<DiscordChannelContainer> TextChannels => this.textChannels;

        public ObservableCollection<DiscordChannelContainer> VoiceChannels => this.voiceChannels;

        public void Initialize()
        {
            // Bridgeにデリゲートを登録する
            DiscordBridge.Instance.SendMessageDelegate = this.SendMessage;
            DiscordBridge.Instance.SendSpeakingDelegate = this.Play;

            this.SetupLibrary();
        }

        public void Dispose()
        {
            // Bridgeのデリゲートを解除する
            DiscordBridge.Instance.SendMessageDelegate = null;
            DiscordBridge.Instance.SendSpeakingDelegate = null;

            this.Disconnect();
        }

        public async void Connect(
            bool isInitialize = false)
        {
            if (!UpdateChecker.IsWindowsNewer)
            {
                this.AppendLogLine("Unsupported Operating System. Windows 8.1 or Later is Required.");
                return;
            }

            if (this.discordClient == null)
            {
                try
                {
                    this.discordClient = new DiscordSocketClient();

                    this.discordClient.Ready += this.DiscordClientOnReady;
                    this.discordClient.LoggedOut += this.DiscordClientOnLoggedOut;
                }
                catch (NotSupportedException)
                {
                    this.AppendLogLine("Unsupported Operating System.");
                    return;
                }
            }

            if (this.discordClient != null &&
                !string.IsNullOrEmpty(this.Config.Token))
            {
                try
                {
                    await this.discordClient.LoginAsync(TokenType.Bot, this.Config.Token);
                    await Task.Delay(TimeSpan.FromSeconds(0.25));
                    await this.discordClient.StartAsync();
                }
                catch (Exception ex)
                {
                    this.AppendLogLine("Connection Error.", ex);
                }
            }
        }

        public void ClearQueue()
        {
            while (this.playQueue.TryDequeue(out string s)) ;
        }

        public async void Disconnect()
        {
            this.ClearQueue();

            this.LeaveVoiceChannel();

            if (this.audioOutStream != null)
            {
                this.audioOutStream.Dispose();
                this.audioOutStream = null;
            }

            if (this.audioClient?.ConnectionState == ConnectionState.Connected)
            {
                await audioClient?.StopAsync();
                this.audioClient?.Dispose();
                this.audioClient = null;
            }

            if (this.discordClient != null)
            {
                await this.discordClient?.StopAsync();
                await this.discordClient?.LogoutAsync();
                this.discordClient?.Dispose();
                this.discordClient = null;
            }
        }

        public async void JoinVoiceChannel()
        {
            var ch = this.SelectedVoiceChannel?.ChannelObject as SocketVoiceChannel;
            if (ch == null)
            {
                return;
            }

            // opus.dll の存在を確認する
            var entryDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var opus = Path.Combine(entryDirectory, "opus.dll");
            if (!File.Exists(opus))
            {
                this.AppendLogLine(
                    $"Join Error. Opus not found. you must installed opus.dll or libopus.dll in working directory.",
                    null,
                    true);

                return;
            }

            try
            {
                this.audioClient = await ch.ConnectAsync();
                this.AppendLogLine($"Joined Voice Channel: [{ch.Guild.Name}] {ch.Name}");
                this.IsJoinedVoiceChannel = true;

                lock (this)
                {
                    this.ClearQueue();

                    if (this.playWorker == null)
                    {
                        this.playWorker = new Thread(this.PlayThread)
                        {
                            IsBackground = true,
                            Priority = ThreadPriority.BelowNormal
                        };
                    }

                    this.playWorkerRunning = true;
                    this.playWorker?.Start();
                }
            }
            catch (Exception ex)
            {
                this.AppendLogLine($"Join Voice Channel Error.", ex);
            }
        }

        public async void LeaveVoiceChannel()
        {
            this.playWorkerRunning = false;
            await Task.Delay(TimeSpan.FromSeconds(0.1));

            lock (this)
            {
                if (this.playWorker != null)
                {
                    this.playWorker.Join(TimeSpan.FromSeconds(0.5));

                    if (this.playWorker.IsAlive)
                    {
                        this.playWorker.Abort();
                    }

                    this.playWorker = null;

                    while (this.playQueue.TryDequeue(out string wave)) ;
                }
            }

            if (this.audioOutStream != null)
            {
                this.audioOutStream.Dispose();
                this.audioOutStream = null;
            }

            if (this.audioClient?.ConnectionState == ConnectionState.Connected)
            {
                await audioClient?.StopAsync();
                this.audioClient?.Dispose();
                this.audioClient = null;
            }

            this.AppendLogLine($"Left Voice Channel");
            this.IsJoinedVoiceChannel = false;
        }

        private static readonly object SendBlocker = new object();
        private Thread playWorker;
        private readonly ConcurrentQueue<string> playQueue = new ConcurrentQueue<string>();
        private volatile bool playWorkerRunning = false;

        public void Play(
            string audioFile)
        {
            if (!File.Exists(audioFile))
            {
                this.AppendLogLine($"Play Sound Error. File not found. {audioFile}");
                return;
            }

            this.playQueue.Enqueue(audioFile);
        }

        private void PlayCore(
            string audioFile)
        {
            lock (SendBlocker)
            {
                this.AppendLogLine($"Play Sound: {Path.GetFileName(audioFile)}");

                if (this.audioOutStream == null)
                {
                    this.audioOutStream = this.audioClient.CreatePCMStream(
                        AudioApplication.Voice,
                        128 * 1024,
                        200);
                }

                try
                {
                    WaveModel.Instance.WriteAudioStream(
                        this.audioOutStream,
                        audioFile);

                    this.audioOutStream.Flush();
                }
                catch (Exception ex)
                {
                    this.AppendLogLine($"Play Sound Error.", ex);

                    if (this.audioOutStream != null)
                    {
                        this.audioOutStream.Dispose();
                        this.audioOutStream = null;
                    }
                }
            }
        }

        private void PlayThread()
        {
            while (this.playWorkerRunning)
            {
                if (this.playQueue.IsEmpty)
                {
                    Thread.Sleep(50);
                    continue;
                }

                while (this.playQueue.TryDequeue(out string wave))
                {
                    if (!this.playWorkerRunning)
                    {
                        return;
                    }

                    this.PlayCore(wave);
                    Thread.Sleep(TimeSpan.FromSeconds(0.05));
                }

                Thread.Yield();
            }
        }

        public void SendMessage(
            string message,
            bool tts = false)
        {
            lock (SendBlocker)
            {
                var ch = this.SelectedTextChannel?.ChannelObject as SocketTextChannel;
                if (ch != null)
                {
                    ch.SendMessageAsync(message, tts);
                }
            }
        }

        #endregion IDiscordClientModel

        #region Discord Client Events

        private async Task DiscordClientOnReady()
        {
            await this.discordClient.SetGameAsync("ACT.Hojoring");
            this.EnumerateGuilds();
            this.EnumerateChannels();

            await WPFHelper.InvokeAsync(() =>
            {
                this.IsConnected = true;
            });

            this.AppendLogLine("Conected to DISCORD. Client is Ready!");

            if (this.Config.AutoJoin)
            {
                this.JoinVoiceChannel();
            }
        }

        private async Task DiscordClientOnLoggedOut()
        {
            this.ClearGuilds();
            this.ClearChannels();

            await WPFHelper.InvokeAsync(() =>
            {
                this.IsConnected = false;
            });

            this.AppendLogLine("Disconnected from DISCORD. Bye!");
        }

        #endregion Discord Client Events

        private DiscordSettings Config => Settings.Default.DiscordSettings;

        private DiscordSocketClient discordClient;
        private IAudioClient audioClient;
        private AudioOutStream audioOutStream;

        private readonly List<SocketGuild> guilds = new List<SocketGuild>();

        private async void ClearGuilds()
        {
            await WPFHelper.InvokeAsync(() =>
            {
                this.guilds.Clear();

                this.RaisePropertyChanged(nameof(this.AvailableGuilds));
                this.RaisePropertyChanged(nameof(this.AvailableGuildsText));
            });
        }

        private async void EnumerateGuilds()
        {
            await WPFHelper.InvokeAsync(() =>
            {
                this.guilds.Clear();
                this.guilds.AddRange(this.discordClient.Guilds);

                this.RaisePropertyChanged(nameof(this.AvailableGuilds));
                this.RaisePropertyChanged(nameof(this.AvailableGuildsText));
            });
        }

        private async void ClearChannels()
        {
            await WPFHelper.InvokeAsync(() =>
            {
                this.channels.Clear();
                this.textChannels.Clear();
                this.voiceChannels.Clear();
            });
        }

        private async void EnumerateChannels()
        {
            var textChID = this.Config.DefaultTextChannelID != "0" ?
                this.Config.DefaultTextChannelID :
                this.previousAvailableTextChannelID;

            var voiceChID = this.Config.DefaultVoiceChannelID != "0" ?
                this.Config.DefaultVoiceChannelID :
                this.previousAvailableVoiceChannelID;

            await WPFHelper.InvokeAsync(() =>
            {
                this.channels.Clear();
                this.textChannels.Clear();
                this.voiceChannels.Clear();

                this.textChannels.Add(new DiscordChannelContainer()
                {
                    ID = "-1",
                    ServerName = "DISABLED",
                    Type = ChannelType.Text
                });

                foreach (var g in this.discordClient.Guilds)
                {
                    this.AppendLogLine($"[{g.Name}]");

                    foreach (var ch in g.TextChannels.OrderBy(x => x.Position))
                    {
                        this.AppendLogLine($"-> #{ch.Name}");

                        this.textChannels.Add(new DiscordChannelContainer()
                        {
                            ChannelObject = ch,
                            ID = ch.Id,
                            Name = ch.Name,
                            ServerName = ch.Guild.Name,
                            Type = ChannelType.Text
                        });
                    }

                    foreach (var ch in g.VoiceChannels.OrderBy(x => x.Position))
                    {
                        this.AppendLogLine($"-> #{ch.Name}");

                        this.voiceChannels.Add(new DiscordChannelContainer()
                        {
                            ChannelObject = ch,
                            ID = ch.Id,
                            Name = ch.Name,
                            ServerName = ch.Guild.Name,
                            Type = ChannelType.Voice
                        });
                    }
                }

                this.channels.AddRange(this.textChannels);
                this.channels.AddRange(this.voiceChannels);

                this.SelectedTextChannel = this.textChannels.FirstOrDefault(x =>
                    x.ID.ToString() == textChID);

                this.SelectedVoiceChannel = this.voiceChannels.FirstOrDefault(x =>
                    x.ID.ToString() == voiceChID);
            });
        }

        private void SetupLibrary()
        {
            if (string.IsNullOrEmpty(PluginCore.Instance?.PluginDirectory))
            {
                return;
            }

            var entryDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var libDirectory = new[]
            {
                Path.Combine(PluginCore.Instance.PluginDirectory, "bin", "lib"),
                Path.Combine(PluginCore.Instance.PluginDirectory, "lib"),
            }.FirstOrDefault(x => Directory.Exists(x));

            var opus = Path.Combine(entryDirectory, "opus.dll");
            var sodium = Path.Combine(entryDirectory, "libsodium.dll");

            if (!File.Exists(opus))
            {
                var src = Path.Combine(libDirectory, "libopus.dll");

                if (File.Exists(src))
                {
                    this.AppendLogLine("Install Opus.");
                    File.Copy(src, opus, true);
                }
            }

            if (!File.Exists(sodium))
            {
                var src = Path.Combine(libDirectory, "libsodium.dll");

                if (File.Exists(src))
                {
                    this.AppendLogLine("Install Sodium.");
                    File.Copy(src, sodium, true);
                }
            }
        }
    }

    public enum ChannelType
    {
        Text = 0,
        Voice,
    }
}
