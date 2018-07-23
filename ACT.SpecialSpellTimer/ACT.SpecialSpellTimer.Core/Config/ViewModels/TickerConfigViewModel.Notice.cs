using System;
using System.Threading;
using System.Windows.Input;
using ACT.SpecialSpellTimer.Config.Models;
using ACT.SpecialSpellTimer.Sound;
using Prism.Commands;

namespace ACT.SpecialSpellTimer.Config.ViewModels
{
    public partial class TickerConfigViewModel
    {
        public static SoundController.WaveFile[] WaveList => SoundController.Instance.EnumlateWave();

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

        public ICommand TestWave1Command =>
            this.testWave1Command ?? (this.testWave1Command = this.CreateTestWaveCommand(
                () => this.Model.MatchSound,
                this.Model.MatchAdvancedConfig));

        public ICommand TestWave2Command =>
            this.testWave2Command ?? (this.testWave2Command = this.CreateTestWaveCommand(
                () => this.Model.DelaySound,
                this.Model.DelayAdvancedConfig));

        private ICommand testTTS1Command;
        private ICommand testTTS2Command;

        public ICommand TestTTS1Command =>
            this.testTTS1Command ?? (this.testTTS1Command = this.CreateTestTTSCommand(
                () => this.Model.MatchTextToSpeak,
                this.Model.MatchAdvancedConfig));

        public ICommand TestTTS2Command =>
            this.testTTS2Command ?? (this.testTTS2Command = this.CreateTestTTSCommand(
                () => this.Model.DelayTextToSpeak,
                this.Model.DelayAdvancedConfig));

        private ICommand testSequencialTTSCommand;

        public ICommand TestSequencialTTSCommand =>
            this.testSequencialTTSCommand ?? (this.testSequencialTTSCommand = new DelegateCommand(() =>
            {
                var config = this.Model.MatchAdvancedConfig;

                this.Model.Play("シンクロ再生のテストを開始します。", config);
                Thread.Sleep(2 * 1000);

                this.Model.Play("おしらせ1番", config);
                this.Model.Play("おしらせ2番", config);
                this.Model.Play("おしらせ3番", config);
                this.Model.Play("おしらせ4番", config);

                Thread.Sleep(3 * 1000);

                this.Model.Play("/sync 4 1番目に登録したシンク4通知です", config);
                this.Model.Play("/sync 3 2番目に登録したシンク3通知です", config);
                this.Model.Play("/sync 2 3番目に登録したシンク2通知です", config);
                this.Model.Play("/sync 1 4番目に登録したシンク1通知です", config);
            }));
    }
}
