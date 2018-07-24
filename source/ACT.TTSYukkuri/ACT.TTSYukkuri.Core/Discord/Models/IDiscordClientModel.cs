using System;
using System.Collections.ObjectModel;

namespace ACT.TTSYukkuri.Discord.Models
{
    public interface IDiscordClientModel :
        IDisposable
    {
        bool IsConnected { get; }

        bool IsJoinedVoiceChannel { get; }

        DiscordChannelContainer SelectedTextChannel { get; set; }

        DiscordChannelContainer SelectedVoiceChannel { get; set; }

        string[] AvailableGuilds { get; }

        string AvailableGuildsText { get; }

        ObservableCollection<DiscordChannelContainer> Channels { get; }

        ObservableCollection<DiscordChannelContainer> TextChannels { get; }

        ObservableCollection<DiscordChannelContainer> VoiceChannels { get; }

        string Log { get; }

        void Initialize();

        void Connect(bool isInitialize = false);

        void Disconnect();

        void JoinVoiceChannel();

        void LeaveVoiceChannel();

        void Play(string audioFile);

        void SendMessage(string message, bool tts = false);
    }

    public class DiscordChannelContainer
    {
        public object ID { get; set; }

        public string ServerName { get; set; }

        public string Name { get; set; }

        public object Type { get; set; }

        public dynamic ChannelObject { get; set; }
    }
}
