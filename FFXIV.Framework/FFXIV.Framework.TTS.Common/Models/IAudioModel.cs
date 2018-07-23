namespace FFXIV.Framework.TTS.Common.Models
{
    public interface IAudioModel :
        IReady
    {
        void Play(string wavefileName);

        void Speak(string textToSpeak);
    }
}
