using System.IO;
using System.Linq;
using CeVIO.Talk.RemoteService;
using FFXIV.Framework.Common;
using FFXIV.Framework.TTS.Common.Models;
using NAudio.Wave;
using NLog;

namespace FFXIV.Framework.TTS.Server.Models
{
    public class CevioModel
    {
        #region Singleton

        private static CevioModel instance = new CevioModel();
        public static CevioModel Instance => instance;

        #endregion Singleton

        #region Logger

        private Logger Logger => AppLog.DefaultLogger;

        #endregion Logger

        private Talker cevioTalker;

        #region Start / Kill

        public void KillCevio()
        {
            if (ServiceControl.IsHostStarted)
            {
                ServiceControl.CloseHost(HostCloseMode.Interrupt);
                this.Logger.Info($"CeVIO Remote Service, CloseHost.");
            }

            if (this.cevioTalker != null)
            {
                this.cevioTalker = null;
            }
        }

        public void StartCevio()
        {
            if (!ServiceControl.IsHostStarted)
            {
                ServiceControl.StartHost(false);
                this.Logger.Info($"CeVIO Remote Service, StartHost.");
            }

            if (this.cevioTalker == null)
            {
                this.cevioTalker = new Talker();

                // 最初に何か有効なキャストを設定する必要がある
                this.cevioTalker.Cast = Talker.AvailableCasts.FirstOrDefault();
            }
        }

        #endregion Start / Kill

        public CevioTalkerModel GetCevioTalker()
        {
            var talkerModel = new CevioTalkerModel();

            this.StartCevio();

            if (this.cevioTalker == null)
            {
                return talkerModel;
            }

            // キャストを最初に取得する
            talkerModel.Cast = this.cevioTalker.Cast;

            talkerModel.Volume = this.cevioTalker.Volume;
            talkerModel.Speed = this.cevioTalker.Speed;
            talkerModel.Tone = this.cevioTalker.Tone;
            talkerModel.Alpha = this.cevioTalker.Alpha;
            talkerModel.ToneScale = this.cevioTalker.ToneScale;
            talkerModel.AvailableCasts = Talker.AvailableCasts;

            if (this.cevioTalker.Components != null)
            {
                // Components にはインデックスでしかアクセスできない
                for (int i = 0; i < this.cevioTalker.Components.Length; i++)
                {
                    var component = new CevioTalkerModel.CevioTalkerComponent()
                    {
                        Id = this.cevioTalker.Components[i].Id,
                        Name = this.cevioTalker.Components[i].Name,
                        Value = this.cevioTalker.Components[i].Value,
                    };

                    talkerModel.Components.Add(component);
                }
            }

            return talkerModel;
        }

        public void SetCevioTalker(
            CevioTalkerModel talkerModel)
        {
            this.StartCevio();

            if (this.cevioTalker == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(talkerModel.Cast) ||
                !Talker.AvailableCasts.Contains(talkerModel.Cast))
            {
                return;
            }

            lock (this)
            {
                // キャストを最初に指定する
                this.cevioTalker.Cast = talkerModel.Cast;

                this.cevioTalker.Volume = talkerModel.Volume;
                this.cevioTalker.Speed = talkerModel.Speed;
                this.cevioTalker.Tone = talkerModel.Tone;
                this.cevioTalker.Alpha = talkerModel.Alpha;
                this.cevioTalker.ToneScale = talkerModel.ToneScale;

                if (this.cevioTalker.Components != null)
                {
                    // Components にはインデックスでしかアクセスできない
                    for (int i = 0; i < this.cevioTalker.Components.Length; i++)
                    {
                        var component = this.cevioTalker.Components[i];
                        var src = talkerModel.Components.FirstOrDefault(x => x.Id == component.Id);
                        if (src != null)
                        {
                            component.Value = src.Value;
                        }
                    }
                }
            }
        }

        public void TextToWave(
            string textToSpeak,
            string waveFileName,
            float gain = 1.0f)
        {
            if (string.IsNullOrEmpty(textToSpeak))
            {
                return;
            }

            this.StartCevio();

            lock (this)
            {
                var tempWave = Path.GetTempFileName();

                try
                {
                    var result = this.cevioTalker.OutputWaveToFile(
                        textToSpeak,
                        tempWave);

                    if (result)
                    {
                        FileHelper.CreateDirectory(waveFileName);

                        if (gain != 1.0)
                        {
                            // ささらは音量が小さめなので増幅する
                            using (var reader = new WaveFileReader(tempWave))
                            {
                                var prov = new VolumeWaveProvider16(reader)
                                {
                                    Volume = gain
                                };

                                WaveFileWriter.CreateWaveFile(
                                    waveFileName,
                                    prov);
                            }
                        }
                        else
                        {
                            File.Move(tempWave, waveFileName);
                        }
                    }
                }
                finally
                {
                    if (File.Exists(tempWave))
                    {
                        File.Delete(tempWave);
                    }
                }
            }
        }

        /// <summary>
        /// 話す
        /// </summary>
        /// <param name="textToSpeak">TTS</param>
        /// <param name="speed">スピード(0-100)</param>
        /// <param name="volume">ボリューム(0-100)</param>
        /// <param name="tone">音の高さ(0-100)</param>
        /// <param name="alpha">声質(0-100)</param>
        /// <param name="toneScale">抑揚(0-100)</param>
        public void Speak(
            string textToSpeak,
            uint? speed = null,
            uint? volume = null,
            uint? tone = null,
            uint? alpha = null,
            uint? toneScale = null,
            int? castNo = null)
        {
            if (string.IsNullOrEmpty(textToSpeak))
            {
                return;
            }

            lock (this)
            {
                this.StartCevio();

                if (speed.HasValue)
                {
                    this.cevioTalker.Speed = speed.Value;
                }

                if (volume.HasValue)
                {
                    this.cevioTalker.Volume = volume.Value;
                }

                if (tone.HasValue)
                {
                    this.cevioTalker.Tone = tone.Value;
                }

                if (alpha.HasValue)
                {
                    this.cevioTalker.Alpha = alpha.Value;
                }

                if (toneScale.HasValue)
                {
                    this.cevioTalker.ToneScale = toneScale.Value;
                }

                if (castNo.HasValue)
                {
                    var casts = Talker.AvailableCasts;
                    if (castNo.Value < casts.Length)
                    {
                        this.cevioTalker.Cast = casts[castNo.Value];
                    }
                }

                this.cevioTalker.Speak(textToSpeak);
            }
        }
    }
}
