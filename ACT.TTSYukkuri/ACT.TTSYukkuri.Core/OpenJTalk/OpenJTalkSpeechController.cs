using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using ACT.TTSYukkuri.Config;
using NAudio.Wave;

namespace ACT.TTSYukkuri.OpenJTalk
{
    public class OpenJTalkSpeechController :
        ISpeechController
    {
        /// <summary>
        /// ユーザ辞書
        /// </summary>
        private List<KeyValuePair<string, string>> userDictionary = new List<KeyValuePair<string, string>>();

        /// <summary>
        /// 初期化する
        /// </summary>
        public void Initialize()
        {
        }

        public void Free()
        {
        }

        private OpenJTalkConfig Config => Settings.Default.OpenJTalkSettings;

        /// <summary>
        /// テキストを読み上げる
        /// </summary>
        /// <param name="text">読み上げるテキスト</param>
        public void Speak(
            string text,
            PlayDevices playDevice = PlayDevices.Both,
            bool isSync = false)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            // テキストをユーザ辞書で置き換える
            text = this.ReplaceByUserDictionary(text);

            // 現在の条件をハッシュ化してWAVEファイル名を作る
            var wave = this.GetCacheFileName(
                Settings.Default.TTS,
                text.Replace(Environment.NewLine, "+"),
                this.Config.ToString());

            lock (this)
            {
                if (!File.Exists(wave))
                {
                    this.CreateWave(
                        text,
                        wave);
                }
            }

            // 再生する
            SoundPlayerWrapper.Play(wave, playDevice, isSync);
        }

        /// <summary>
        /// WAVEファイルを生成する
        /// </summary>
        /// <param name="textToSpeak">
        /// Text to Speak</param>
        /// <param name="wave">
        /// WAVEファイルのパス</param>
        private void CreateWave(
            string textToSpeak,
            string wave)
        {
            // パス関係を生成する
            var openJTalkDir = Settings.Default.OpenJTalkSettings.OpenJTalkDirectory;
            if (string.IsNullOrWhiteSpace(openJTalkDir))
            {
                openJTalkDir = "OpenJTalk";
            }

            var openJTalk = Path.Combine(openJTalkDir, @"open_jtalk.exe");
            var dic = Path.Combine(openJTalkDir, @"dic");
            var voice = Path.Combine(openJTalkDir, @"voice\" + this.Config.Voice);
            var waveTemp = Path.GetTempFileName();
            if (File.Exists(waveTemp))
            {
                File.Delete(waveTemp);
            }

            var textFile = Path.GetTempFileName();
            File.WriteAllText(textFile, textToSpeak, Encoding.GetEncoding("Shift_JIS"));

            var args = new string[]
            {
                $"-x \"{dic}\"",
                $"-m \"{voice}\"",
                $"-ow \"{waveTemp}\"",
                $"-s 48000",
                $"-p 240",
                $"-g {this.Config.Volume.ToString("N2")}",
                $"-a {this.Config.AllPass.ToString("N2")}",
                $"-b {this.Config.PostFilter.ToString("N2")}",
                $"-r {this.Config.Rate.ToString("N2")}",
                $"-fm {this.Config.HalfTone.ToString("N2")}",
                $"-u {this.Config.UnVoice.ToString("N2")}",
                $"-jm {this.Config.Accent.ToString("N2")}",
                $"-jf {this.Config.Weight.ToString("N2")}",
                $"\"{textFile}\""
            };

            var pi = new ProcessStartInfo()
            {
                FileName = openJTalk,
                CreateNoWindow = true,
                UseShellExecute = false,
                Arguments = string.Join(" ", args),
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            };

            Debug.WriteLine(pi.FileName + " " + pi.Arguments);

            using (var p = Process.Start(pi))
            {
                var stderr = p.StandardError.ReadToEnd();
                var stdout = p.StandardOutput.ReadToEnd();

                if (!string.IsNullOrWhiteSpace(stderr))
                {
                    Debug.WriteLine(stderr);
                }

                if (!string.IsNullOrWhiteSpace(stdout))
                {
                    Debug.WriteLine(stdout);
                }

                p.WaitForExit();
            }

            if (File.Exists(textFile))
            {
                File.Delete(textFile);
            }

            if (this.Config.Gain != 1.0f)
            {
                using (var reader = new WaveFileReader(waveTemp))
                {
                    var prov = new VolumeWaveProvider16(reader);
                    prov.Volume = this.Config.Gain;

                    WaveFileWriter.CreateWaveFile(
                        wave,
                        prov);
                }
            }
            else
            {
                File.Move(waveTemp, wave);
            }

            if (File.Exists(waveTemp))
            {
                File.Delete(waveTemp);
            }
        }

        /// <summary>
        /// ユーザ辞書で置換する
        /// </summary>
        /// <param name="textToSpeak">
        /// Text to Speak</param>
        /// <returns>
        /// 置換後のText to Speak</returns>
        private string ReplaceByUserDictionary(
            string textToSpeak)
        {
            var t = textToSpeak;

            var openJTalkDir = Settings.Default.OpenJTalkSettings.OpenJTalkDirectory;
            if (string.IsNullOrWhiteSpace(openJTalkDir))
            {
                openJTalkDir = "OpenJTalk";
            }

            var userDic = Path.Combine(
                openJTalkDir,
                @"dic\user_dictionary.txt");

            if (!File.Exists(userDic))
            {
                return t;
            }

            if (this.userDictionary.Count < 1)
            {
                using (var sr = new StreamReader(userDic, new UTF8Encoding(false)))
                {
                    while (!sr.EndOfStream)
                    {
                        var line = sr.ReadLine().Trim();

                        if (string.IsNullOrWhiteSpace(line))
                        {
                            continue;
                        }

                        if (line.StartsWith("#"))
                        {
                            continue;
                        }

                        var words = line.Split('\t');
                        if (words.Length < 2)
                        {
                            continue;
                        }

                        this.userDictionary.Add(new KeyValuePair<string, string>(
                            words[0].Trim(),
                            words[1].Trim()));
                    }
                }
            }

            foreach (var item in this.userDictionary)
            {
                t = t.Replace(item.Key, item.Value);
            }

            return t;
        }
    }
}
