using System;
using Prism.Mvvm;

namespace ACT.TTSYukkuri.Config
{
    [Serializable]
    public class DiscordSettings :
        BindableBase
    {
        private string token = string.Empty;
        private string defaultServer = string.Empty;
        private string defaultTextChannelID = "0";
        private string defaultVoiceChannelID = "0";
        private bool autoJoin = false;

        public string Token
        {
            get => this.token;
            set => this.SetProperty(ref this.token, value?.Trim() ?? string.Empty);
        }

        public string DefaultServer
        {
            get => this.defaultServer;
            set => this.SetProperty(ref this.defaultServer, value);
        }

        public string DefaultTextChannelID
        {
            get => this.defaultTextChannelID;
            set => this.SetProperty(ref this.defaultTextChannelID, value);
        }

        public string DefaultVoiceChannelID
        {
            get => this.defaultVoiceChannelID;
            set => this.SetProperty(ref this.defaultVoiceChannelID, value);
        }

        public bool AutoJoin
        {
            get => this.autoJoin;
            set => this.SetProperty(ref this.autoJoin, value);
        }
    }
}
