using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Commands;
using System.Windows.Input;

namespace ACT.TTSYukkuri.Config.ViewModels
{
    public class GoogleCloudTextToSpeechConfigViewModel
    {
        public GoogleCloudTextToSpeechConfig Config => Settings.Default.GoogleCloudTextToSpeechSettings;

        public GoogleCloudTextToSpeechLanguageCode[] LanguageCodeList => Settings.Default.GoogleCloudTextToSpeechSettings.EnumerateLanguageCode();

        public GoogleCloudTextToSpeechSampleRateHertz[] SampleRateHertzList => Settings.Default.GoogleCloudTextToSpeechSettings.EnumerateSampleRateHertz();

        private ICommand setRecommendCommand;

        public ICommand SetRecommendCommand =>
            this.setRecommendCommand ?? (this.setRecommendCommand = new DelegateCommand(() =>
            {
                this.Config.SetRecommend();
            }));
    }
}
