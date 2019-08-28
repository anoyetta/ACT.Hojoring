using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using FFXIV.Framework.Bridge;
using FFXIV.Framework.Common;
using NLog;
using Prism.Commands;
using Prism.Mvvm;

namespace ACT.TTSYukkuri.Config.ViewModels
{
    public class GeneralViewModel : BindableBase
    {
        #region Logger

        private Logger Logger => AppLog.DefaultLogger;

        #endregion Logger

        public Settings Config => Settings.Default;

        public FFXIV.Framework.Config FrameworkConfig => FFXIV.Framework.Config.Instance;

        public ComboBoxItem[] TTSTypes => TTSType.ToComboBox;

        public IEnumerable<VoicePalettes> Palettes => (IEnumerable<VoicePalettes>)Enum.GetValues(typeof(VoicePalettes));

        public VoicePalettes TestPlayPalette { get; set; } = VoicePalettes.Default;

        public dynamic[] Players =>
            WavePlayerTypes.WASAPI.GetAvailablePlayers()
            .Select(x => new
            {
                Value = x,
                Text = x.ToDisplay()
            }).ToArray();

        private ICommand playTTSCommand;

        public ICommand PlayTTSCommand =>
            this.playTTSCommand ?? (this.playTTSCommand = new DelegateCommand<string>((tts) =>
            {
                if (string.IsNullOrWhiteSpace(tts))
                {
                    return;
                }

                PlayBridge.Instance.Play(tts, TestPlayPalette, false, Settings.Default.WaveVolume / 100f);
            }));

        private ICommand openCacheFolderCommand;

        public ICommand OpenCacheFolderCommand =>
            this.openCacheFolderCommand ?? (this.openCacheFolderCommand = new DelegateCommand(() =>
            {
                if (Directory.Exists(SpeechControllerExtentions.CacheDirectory))
                {
                    Process.Start(SpeechControllerExtentions.CacheDirectory);
                }
            }));

        private ICommand changePlayMethodCommand;

        public ICommand clearCacheCommand;

        public ICommand ClearCacheCommand =>
            this.clearCacheCommand ?? (this.clearCacheCommand = new DelegateCommand(async () =>
            {
                if (Directory.Exists(SpeechControllerExtentions.CacheDirectory))
                {
                    var result = MessageBox.Show(
                        "Are you sure you want to delete cached wave files?",
                        "ACT.TTSYukkuri",
                        MessageBoxButton.OKCancel,
                        MessageBoxImage.Question);

                    if (result != MessageBoxResult.OK)
                    {
                        return;
                    }

                    await Task.Run(() =>
                    {
                        foreach (var file in Directory.GetFiles(
                            SpeechControllerExtentions.CacheDirectory,
                            "*.wav",
                            SearchOption.TopDirectoryOnly))
                        {
                            File.Delete(file);
                        }
                    });

                    MessageBox.Show(
                        "Cached wave files deleted.",
                        "ACT.TTSYukkuri",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }));

        public ICommand ChangePlayMethodCommand =>
            this.changePlayMethodCommand ?? (this.changePlayMethodCommand = new DelegateCommand(() =>
            {
                var devices = this.Config.PlayDevices;
                if (!devices.Any(x => x.ID == this.Config.MainDeviceID))
                {
                    this.Config.MainDeviceID = devices.FirstOrDefault()?.ID;
                }

                if (!devices.Any(x => x.ID == this.Config.SubDeviceID))
                {
                    this.Config.SubDeviceID = devices.FirstOrDefault()?.ID;
                }
            }));

        private DelegateCommand clearBufferCommand;

        public DelegateCommand ClearBufferCommand =>
            this.clearBufferCommand ?? (this.clearBufferCommand = new DelegateCommand(this.ExecuteClearBufferCommand));

        private void ExecuteClearBufferCommand()
        {
            BufferedWavePlayer.Instance?.ClearBuffers();
            this.Logger.Info("Playback buffers cleared.");
        }

        private DelegateCommand resetWasapiDeviceCommand;

        public DelegateCommand ResetWasapiDeviceCommand =>
            this.resetWasapiDeviceCommand ?? (this.resetWasapiDeviceCommand = new DelegateCommand(this.ExecuteResetWasapiDeviceCommand));

        private async void ExecuteResetWasapiDeviceCommand()
        {
            SoundPlayerWrapper.Init();
            await Task.Delay(10);
            SoundPlayerWrapper.LoadTTSCache();

            this.Logger.Info("Reset WASAPI Player, and Reload TTS chache.");
        }
    }
}
