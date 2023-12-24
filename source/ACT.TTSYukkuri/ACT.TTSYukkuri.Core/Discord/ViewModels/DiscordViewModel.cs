using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using ACT.TTSYukkuri.Config;
using ACT.TTSYukkuri.Discord.Models;
using ACT.TTSYukkuri.Discord.Views;
using Prism.Commands;

namespace ACT.TTSYukkuri.Discord.ViewModels
{
    public class DiscordViewModel
    {
        public DiscordView View { get; set; }

        public DiscordSettings Config => Settings.Default.DiscordSettings;

        public IDiscordClientModel Model => DiscordClientModel.Model;

        private ICommand connectCommand;
        private ICommand disconnectCommand;
        private ICommand joinCommand;
        private ICommand leaveCommand;
        private ICommand openHelperCommand;

        public ICommand ConnectCommand =>
            this.connectCommand ?? (this.connectCommand = new DelegateCommand(async () =>
            {
                Action action = () =>
                {
                    this.Model.Connect();
                };
                try
                {
                    action();
                } catch (TypeLoadException e)
                {
                    MessageBox.Show(e.Message);
                }
 //               this.Model.Connect();
                await Task.Delay(TimeSpan.FromMilliseconds(100));
            }));

        public ICommand DisconnectCommand =>
            this.disconnectCommand ?? (this.disconnectCommand = new DelegateCommand(async () =>
            {
                this.Model.Disconnect();
                await Task.Delay(TimeSpan.FromMilliseconds(100));
            }));

        public ICommand JoinCommand =>
            this.joinCommand ?? (this.joinCommand = new DelegateCommand(async () =>
            {
                try
                {
                    this.View.JoinVoiceChannelLink.IsEnabled = false;
                    this.Model.JoinVoiceChannel();
                    await Task.Delay(TimeSpan.FromMilliseconds(100));
                }
                finally
                {
                    this.View.JoinVoiceChannelLink.IsEnabled = true;
                }
            }));

        public ICommand LeaveCommand =>
            this.leaveCommand ?? (this.leaveCommand = new DelegateCommand(async () =>
            {
                try
                {
                    this.View.LeaveTextVoiceLink.IsEnabled = false;
                    this.Model.LeaveVoiceChannel();
                    await Task.Delay(TimeSpan.FromMilliseconds(100));
                }
                finally
                {
                    this.View.LeaveTextVoiceLink.IsEnabled = true;
                }
            }));

        public ICommand OpenHelperCommand =>
            this.openHelperCommand ?? (this.openHelperCommand = new DelegateCommand(() =>
            {
                var window = new PermissionHelperView();
                ElementHost.EnableModelessKeyboardInterop(window);
                window.Show();
            }));

        private ICommand pingCommand;

        public ICommand PingCommand =>
            this.pingCommand ?? (this.pingCommand = new DelegateCommand(() =>
            {
                this.Model.SendMessage("ping!");
            }));
    }
}
