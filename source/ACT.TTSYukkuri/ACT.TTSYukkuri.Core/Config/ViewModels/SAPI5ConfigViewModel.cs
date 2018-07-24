using System.Collections.Generic;
using System.Speech.Synthesis;
using ACT.TTSYukkuri.SAPI5;
using Prism.Mvvm;

namespace ACT.TTSYukkuri.Config.ViewModels
{
    public class SAPI5ConfigViewModel : BindableBase
    {
        public SAPI5Configs Config => Settings.Default.SAPI5Settings;

        public IReadOnlyList<InstalledVoice> Voices => SAPI5SpeechController.Synthesizers;

        public IReadOnlyList<KeyValuePair<Pitches, string>> PitchList => new List<KeyValuePair<Pitches, string>>()
        {
            new KeyValuePair<SAPI5.Pitches, string>(Pitches.Default, Pitches.Default.ToXML()),
            new KeyValuePair<SAPI5.Pitches, string>(Pitches.XLow, Pitches.XLow.ToXML()),
            new KeyValuePair<SAPI5.Pitches, string>(Pitches.Low, Pitches.Low.ToXML()),
            new KeyValuePair<SAPI5.Pitches, string>(Pitches.Medium, Pitches.Medium.ToXML()),
            new KeyValuePair<SAPI5.Pitches, string>(Pitches.High, Pitches.High.ToXML()),
            new KeyValuePair<SAPI5.Pitches, string>(Pitches.XHigh, Pitches.XHigh.ToXML()),
        };
    }
}
