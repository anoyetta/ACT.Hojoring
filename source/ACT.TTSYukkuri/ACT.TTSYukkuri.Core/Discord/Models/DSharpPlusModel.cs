#if false
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
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.VoiceNext;
using DSharpPlus.VoiceNext.Codec;
using FFXIV.Framework.Bridge;
using FFXIV.Framework.Common;
using NLog;
using Prism.Mvvm;

namespace ACT.TTSYukkuri.Discord.Models
{
    public class DSharpPlusModel :
        BindableBase,
        IDiscordClientModel
    {
#region Singleton

        private static DSharpPlusModel instance = new DSharpPlusModel();

        public static DSharpPlusModel Instance => instance;

#endregion Singleton

#region Logger

        private Logger Logger => AppLog.DefaultLogger;

        private readonly StringBuilder log = new StringBuilder();

        public string Log => this.log.ToString();

        private void AppendLogLine(
            string message,
            Exception ex = null)
        {
            // NLogに出力する
            if (ex == null)
            {
                this.Logger.Trace(message);
            }
            else
            {
                this.Logger.Error(ex, message);
            }

            // UIに出力する
            var text = $"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffff")}] {message}";
            if (ex != null)
            {
                text += Environment.NewLine + ex.ToString();
            }

            this.log.AppendLine(text);
            WPFHelper.BeginInvoke(() => this.RaisePropertyChanged(nameof(this.Log)));

            var log = $"[DISCORD] {message}";
            if (ex == null)
            {
                this.Logger.Info(log);
            }
            else
            {
                this.Logger.Error(ex, log);
            }
        }

#endregion Logger

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

        public const string DicordCommandPrefix = "//";

        private DiscordSettings Config => Settings.Default.DiscordSettings;

        public void Initialize()
        {
            // Bridgeにデリゲートを登録する
            DiscordBridge.Instance.SendMessageDelegate = this.SendMessage;
            DiscordBridge.Instance.SendSpeakingDelegate = this.Play;
        }

        public void Dispose()
        {
            // Bridgeのデリゲートを解除する
            DiscordBridge.Instance.SendMessageDelegate = null;
            DiscordBridge.Instance.SendSpeakingDelegate = null;

            this.Disconnect();
        }

        private DiscordChannelContainer selectedTextChannel;

        public DiscordChannelContainer SelectedTextChannel
        {
            get => this.selectedTextChannel;
            set
            {
                this.selectedTextChannel = value;
                this.Config.DefaultTextChannelID = (value?.ID ?? 0).ToString();
            }
        }

        private DiscordChannelContainer selectedVoiceChannel;

        public DiscordChannelContainer SelectedVoiceChannel
        {
            get => this.selectedVoiceChannel;
            set
            {
                this.selectedVoiceChannel = value;
                this.Config.DefaultVoiceChannelID = (value?.ID ?? 0).ToString();
            }
        }

        public string[] AvailableGuilds =>
            this.guilds.OrderBy(x => x.Id).Select(x => x.Name).ToArray();

        public string AvailableGuildsText => string.Join(
            Environment.NewLine,
            this.AvailableGuilds);

        private readonly ObservableCollection<DiscordChannelContainer> channels = new ObservableCollection<DiscordChannelContainer>();

        public ObservableCollection<DiscordChannelContainer> Channels => this.channels;

        private void RefreshChannels()
        {
            var list = new List<DiscordChannel>();
            foreach (var guild in this.guilds.OrderBy(x => x.Id))
            {
                list.AddRange(guild.Channels);
            }

            this.Channels.Clear();
            this.Channels.AddRange(
                from x in list
                orderby
                x.Type,
                x.Id
                select new DiscordChannelContainer()
                {
                    ID = x.Id,
                    Name = x.Name,
                    ServerName = x.Guild.Name,
                    Type = x.Type,
                    ChannelObject = x
                });

            this.RaisePropertyChanged(nameof(this.TextChannels));
            this.RaisePropertyChanged(nameof(this.VoiceChannels));
        }

        private void ClearChannels()
        {
            this.Channels.Clear();
            this.RaisePropertyChanged(nameof(this.TextChannels));
            this.RaisePropertyChanged(nameof(this.VoiceChannels));
        }

        public ObservableCollection<DiscordChannelContainer> TextChannels =>
            new ObservableCollection<DiscordChannelContainer>(
                this.Channels?.Where(x => (ChannelType)x.Type == ChannelType.Text));

        public ObservableCollection<DiscordChannelContainer> VoiceChannels =>
            new ObservableCollection<DiscordChannelContainer>(
                this.Channels?.Where(x => (ChannelType)x.Type == ChannelType.Voice));

        private DiscordClient discord;
        private readonly List<DiscordGuild> guilds = new List<DiscordGuild>();
        private VoiceNextClient voice;
        private VoiceNextConnection vnc;

        private bool isInit = false;

        public async void Connect(
            bool isInit = false)
        {
            this.isInit = isInit;

            this.Disconnect();

            if (string.IsNullOrEmpty(this.Config.Token))
            {
                return;
            }

            this.discord = new DiscordClient(new DiscordConfiguration()
            {
                Token = this.Config.Token,
                TokenType = TokenType.Bot,

                AutoReconnect = true,
                LogLevel = DSharpPlus.LogLevel.Error,
                UseInternalLogHandler = true
            });

            this.discord.MessageCreated += async e =>
            {
                if (e.Message.Content.ToLower().Contains($"{DicordCommandPrefix}ping"))
                {
                    await e.Message.RespondAsync("pong");
                }
            };

            this.discord.Ready += this.Ready;
            this.discord.GuildAvailable += this.GuildAvailable;
            this.discord.ClientErrored += this.ClientError;

            var vcfg = new VoiceNextConfiguration
            {
                VoiceApplication = VoiceApplication.Voice
            };

            this.voice = this.discord.UseVoiceNext(vcfg);

            try
            {
                await this.discord.ConnectAsync();
            }
            catch (Exception ex)
            {
                this.AppendLogLine("Connection failed.", ex);
            }
        }

        public async void Disconnect()
        {
            if (this.discord != null)
            {
                await this.discord?.DisconnectAsync()
                    .ContinueWith((task) =>
                {
                    if (this.vnc != null)
                    {
                        this.vnc.Dispose();
                        this.vnc = null;
                        this.voice = null;
                    }

                    this.guilds.Clear();
                    this.discord = null;

                    this.IsConnected = false;
                    this.IsJoinedVoiceChannel = false;

                    this.AppendLogLine("Disconnected from Guild.");

                    this.RaisePropertyChanged(nameof(this.AvailableGuilds));
                    this.RaisePropertyChanged(nameof(this.AvailableGuildsText));

                    this.ClearChannels();
                });
            }
        }

        public async void JoinVoiceChannel()
        {
            var chn = this.SelectedVoiceChannel?.ChannelObject as DiscordChannel;
            if (chn == null)
            {
                return;
            }

            // libopus.dll
            // libsodium.dll
            // の存在を確認する
            var entryDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var opus = Path.Combine(entryDirectory, "libopus.dll");
            var sodium = Path.Combine(entryDirectory, "libsodium.dll");
            if (!File.Exists(opus))
            {
                this.AppendLogLine($"Join Error", new FileNotFoundException("Opus not found.", opus));
                return;
            }

            if (!File.Exists(sodium))
            {
                this.AppendLogLine($"Join Error", new FileNotFoundException("Sodium not found.", sodium));
                return;
            }

            this.vnc = await this.voice.ConnectAsync(chn)
                .ContinueWith<VoiceNextConnection>((task) =>
                {
                    this.AppendLogLine($"Joined channel: {chn.Name}");

                    this.isInit = false;
                    this.IsJoinedVoiceChannel = true;

                    return task.Result;
                });

            if (this.playWorker == null)
            {
                this.playWorker = new Thread(this.PlayThread)
                {
                    IsBackground = true,
                    Priority = ThreadPriority.BelowNormal
                };
            }

            this.playWorker?.Start();
        }

        public async void LeaveVoiceChannel()
        {
            if (this.discord != null)
            {
                await this.discord.DisconnectAsync()
                    .ContinueWith(async (task) =>
                    {
                        if (this.vnc != null)
                        {
                            this.vnc.Dispose();
                            this.vnc = null;
                            this.voice = null;
                        }

                        this.guilds.Clear();
                        this.discord = null;

                        this.IsConnected = false;
                        this.IsJoinedVoiceChannel = false;

                        this.AppendLogLine($"Left channel.");

                        this.RaisePropertyChanged(nameof(this.AvailableGuilds));
                        this.RaisePropertyChanged(nameof(this.AvailableGuildsText));

                        this.ClearChannels();

                        await Task.Delay(TimeSpan.FromMilliseconds(200));
                        this.Connect();
                    });
            }

            if (this.playWorker != null)
            {
                this.playWorker.Abort();
                this.playWorker = null;

                while (this.playQueue.TryDequeue(out string wave)) ;
            }
        }

        private Thread playWorker;
        private readonly ConcurrentQueue<string> playQueue = new ConcurrentQueue<string>();

        public void Play(
            string wave)
        {
            this.playQueue.Enqueue(wave);
        }

        private void PlayThread()
        {
            const int Max = 4;

            while (true)
            {
                var i = 0;
                while (this.playQueue.TryDequeue(out string wave))
                {
                    this.PlayCore(wave);
                    i++;

                    if (i < Max)
                    {
                        Thread.Sleep(TimeSpan.FromSeconds(0.1));
                    }
                    else
                    {
                        i = 0;
                        Thread.Sleep(TimeSpan.FromSeconds(1));
                    }
                }

                Thread.Sleep(0);
            }
        }

        private bool IsPlaying => this.vnc?.IsPlaying ?? false;

        private async void PlayCore(
            string wave)
        {
            try
            {
                if (this.vnc != null)
                {
                    try
                    {
                        await this.vnc.SendSpeakingAsync(true);

                        await WaveModel.Instance.SendEncodeAsync(
                            wave,
                            (bytes, blocksize) => this.vnc.SendAsync(bytes, blocksize));
                    }
                    finally
                    {
                        await this.vnc.SendSpeakingAsync(false);
                    }

                    this.AppendLogLine($"Play sound: {wave}");
                }
            }
            catch (Exception ex)
            {
                this.AppendLogLine($"Play sound error!", ex);
                this.LeaveVoiceChannel();
            }
        }

        public async void SendMessage(
            string message,
            bool tts = false)
        {
            try
            {
                var chn = this.SelectedTextChannel?.ChannelObject as DiscordChannel;
                if (chn != null)
                {
                    await chn.SendMessageAsync(message, tts);
                }
            }
            catch (Exception ex)
            {
                this.AppendLogLine($"Send Message error !", ex);
            }
        }

        private Task ClientError(
            ClientErrorEventArgs e)
        {
            this.AppendLogLine(
                $"Client error. event: {e.EventName}",
                e.Exception);

            this.IsConnected = false;
            this.IsJoinedVoiceChannel = false;

            return Task.CompletedTask;
        }

        private Task Ready(
            ReadyEventArgs e)
        {
            this.AppendLogLine("Client is Ready.");
            return Task.CompletedTask;
        }

        private Task GuildAvailable(
            GuildCreateEventArgs e)
        {
            this.guilds.Add(e.Guild);

            this.RaisePropertyChanged(nameof(this.AvailableGuilds));
            this.RaisePropertyChanged(nameof(this.AvailableGuildsText));

            this.RefreshChannels();

            if (e.Guild.Channels.Any())
            {
                var ch = default(DiscordChannelContainer);

                ch = this.TextChannels.FirstOrDefault(x => x.ID.ToString() == this.Config.DefaultTextChannelID);
                if (ch != null)
                {
                    this.SelectedTextChannel = ch;
                }

                ch = this.VoiceChannels.FirstOrDefault(x => x.ID.ToString() == this.Config.DefaultVoiceChannelID);
                if (ch != null)
                {
                    this.SelectedVoiceChannel = ch;
                }
            }

            this.RaisePropertyChanged(nameof(this.SelectedTextChannel));
            this.RaisePropertyChanged(nameof(this.SelectedVoiceChannel));

            this.AppendLogLine($"Guild available: {e.Guild.Name}");

            this.IsConnected = true;

            if (this.isInit)
            {
                if (this.Config.AutoJoin &&
                    Convert.ToUInt64(this.Config.DefaultVoiceChannelID) != 0 &&
                    this.SelectedVoiceChannel != null)
                {
                    this.isInit = false;
                    this.JoinVoiceChannel();
                }
            }

            return Task.CompletedTask;
        }
    }
}
#endif
