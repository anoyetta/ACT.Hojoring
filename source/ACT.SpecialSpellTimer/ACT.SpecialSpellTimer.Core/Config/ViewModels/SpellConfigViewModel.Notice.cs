using System;
using System.Threading.Tasks;
using System.Windows.Input;
using ACT.SpecialSpellTimer.Config.Models;
using ACT.SpecialSpellTimer.Sound;
using Prism.Commands;

namespace ACT.SpecialSpellTimer.Config.ViewModels
{
    public partial class SpellConfigViewModel
    {
        public static SoundController.WaveFile[] WaveList => SoundController.Instance.EnumlateWave();

        private void ClearSoundTestCommands()
        {
            this.testTTS1Command = null;
            this.testTTS2Command = null;
            this.testTTS3Command = null;
            this.testTTS4Command = null;

            this.testWave1Command = null;
            this.testWave2Command = null;
            this.testWave3Command = null;
            this.testWave4Command = null;

            foreach (var item in new[]
            {
                nameof(this.TestWave1Command),
                nameof(this.TestWave2Command),
                nameof(this.TestWave3Command),
                nameof(this.TestWave4Command),
                nameof(this.TestTTS1Command),
                nameof(this.TestTTS2Command),
                nameof(this.TestTTS3Command),
                nameof(this.TestTTS4Command),
            })
            {
                this.RaisePropertyChanged(item);
            }
        }

        private ICommand CreateTestWaveCommand(
            Func<string> getWave,
            AdvancedNoticeConfig noticeConfig)
            => new DelegateCommand(()
                => this.Model.Play(getWave(), noticeConfig));

        private ICommand CreateTestTTSCommand(
            Func<string> getTTS,
            AdvancedNoticeConfig noticeConfig)
            => new DelegateCommand(()
                => this.Model.Play(getTTS(), noticeConfig));

        private ICommand testWave1Command;
        private ICommand testWave2Command;
        private ICommand testWave3Command;
        private ICommand testWave4Command;

        public ICommand TestWave1Command =>
            this.testWave1Command ?? (this.testWave1Command = this.CreateTestWaveCommand(
                () => this.Model.MatchSound,
                this.Model.MatchAdvancedConfig));

        public ICommand TestWave2Command =>
            this.testWave2Command ?? (this.testWave2Command = this.CreateTestWaveCommand(
                () => this.Model.OverSound,
                this.Model.OverAdvancedConfig));

        public ICommand TestWave3Command =>
            this.testWave3Command ?? (this.testWave3Command = this.CreateTestWaveCommand(
                () => this.Model.BeforeSound,
                this.Model.BeforeAdvancedConfig));

        public ICommand TestWave4Command =>
            this.testWave4Command ?? (this.testWave4Command = this.CreateTestWaveCommand(
                () => this.Model.TimeupSound,
                this.Model.TimeupAdvancedConfig));

        private ICommand testTTS1Command;
        private ICommand testTTS2Command;
        private ICommand testTTS3Command;
        private ICommand testTTS4Command;

        public ICommand TestTTS1Command =>
            this.testTTS1Command ?? (this.testTTS1Command = this.CreateTestTTSCommand(
                () => this.Model.MatchTextToSpeak,
                this.Model.MatchAdvancedConfig));

        public ICommand TestTTS2Command =>
            this.testTTS2Command ?? (this.testTTS2Command = this.CreateTestTTSCommand(
                () => this.Model.OverTextToSpeak,
                this.Model.OverAdvancedConfig));

        public ICommand TestTTS3Command =>
            this.testTTS3Command ?? (this.testTTS3Command = this.CreateTestTTSCommand(
                () => this.Model.BeforeTextToSpeak,
                this.Model.BeforeAdvancedConfig));

        public ICommand TestTTS4Command =>
            this.testTTS4Command ?? (this.testTTS4Command = this.CreateTestTTSCommand(
                () => this.Model.TimeupTextToSpeak,
                this.Model.TimeupAdvancedConfig));

        private ICommand testSequencialTTSCommand;

        public ICommand TestSequencialTTSCommand =>
            this.testSequencialTTSCommand ?? (this.testSequencialTTSCommand = new DelegateCommand(async () =>
            {
                var config = this.Model.MatchAdvancedConfig;

                this.Model.Play("シンクロ再生のテストを開始します。", config);
                await Task.Delay(2 * 1000);

                this.Model.Play("おしらせ1番", config);
                this.Model.Play("おしらせ2番", config);
                this.Model.Play("おしらせ3番", config);
                this.Model.Play("おしらせ4番", config);

                await Task.Delay(3 * 1000);

                this.Model.Play("1番目に登録した通知です", config, true);
                this.Model.Play("2番目に登録した通知です", config, true);
                this.Model.Play("3番目に登録した通知です", config, true);
                this.Model.Play("4番目に登録した通知です", config, true);
            }));
    }
}
