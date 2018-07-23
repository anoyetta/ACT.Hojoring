namespace FFXIV.Framework.TTS.Common.Models
{
    public interface ITTSModel :
        IReady
    {
        CevioTalkerModel GetCevioTalker();

        void SetCevioTalker(CevioTalkerModel talkerModel);

        void TextToWave(
            TTSTypes ttsType,
            string textToSpeak,
            string waveFileName,
            int speed,
            float gain);
    }
}
