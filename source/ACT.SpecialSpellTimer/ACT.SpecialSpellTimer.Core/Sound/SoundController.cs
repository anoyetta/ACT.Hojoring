using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using ACT.SpecialSpellTimer.Utility;
using Advanced_Combat_Tracker;
using FFXIV.Framework.Bridge;

namespace ACT.SpecialSpellTimer.Sound
{
    /// <summary>
    /// Soundコントローラ
    /// </summary>
    public class SoundController
    {
        #region Singleton

        private static SoundController instance = new SoundController();

        public static SoundController Instance => instance;

        #endregion Singleton

        #region Begin / End

        public void Begin()
        {
        }

        public void End()
        {
        }

        #endregion Begin / End

        private string waveDirectory;

        public string WaveDirectory
        {
            get
            {
                if (string.IsNullOrEmpty(this.waveDirectory))
                {
                    do
                    {
                        // ACTのパスを取得する
                        var asm = Assembly.GetEntryAssembly();
                        if (asm != null)
                        {
                            var actDirectory = Path.GetDirectoryName(asm.Location);
                            var resourcesUnderAct = Path.Combine(actDirectory, @"resources\wav");

                            if (Directory.Exists(resourcesUnderAct))
                            {
                                this.waveDirectory = resourcesUnderAct;
                                break;
                            }
                        }

                        // 自身の場所を取得する
                        var selfDirectory = PluginCore.Instance?.Location ?? string.Empty;
                        var resourcesUnderThis = Path.Combine(selfDirectory, @"resources\wav");

                        if (Directory.Exists(resourcesUnderThis))
                        {
                            this.waveDirectory = resourcesUnderThis;
                            break;
                        }
                    } while (false);
                }

                return this.waveDirectory;
            }
        }

        /// <summary>
        /// Waveファイルを列挙する
        /// </summary>
        /// <returns>
        /// Waveファイルのコレクション</returns>
        public WaveFile[] EnumlateWave()
        {
            var list = new List<WaveFile>();

            // 未選択用のダミーをセットしておく
            list.Add(new WaveFile()
            {
                FullPath = string.Empty
            });

            if (Directory.Exists(this.WaveDirectory))
            {
                var files = new List<string>();
                files.AddRange(Directory.GetFiles(this.WaveDirectory, "*.wav"));
                files.AddRange(Directory.GetFiles(this.WaveDirectory, "*.mp3"));

                foreach (var wave in files
                    .OrderBy(x => x)
                    .ToArray())
                {
                    list.Add(new WaveFile()
                    {
                        FullPath = wave
                    });
                }
            }

            return list.ToArray();
        }

        /// <summary>
        /// 再生する
        /// </summary>
        /// <param name="source">
        /// 再生する対象</param>
        public void Play(
            string source,
            bool isSync = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(source))
                {
                    return;
                }

                // wav？
                if (source.EndsWith(".wav") ||
                    source.EndsWith(".wave") ||
                    source.EndsWith(".mp3"))
                {
                    // ファイルが存在する？
                    if (File.Exists(source))
                    {
                        if (PlayBridge.Instance.IsAvailable)
                        {
                            PlayBridge.Instance.Play(source, isSync);
                        }
                        else
                        {
                            ActGlobals.oFormActMain.PlaySound(source);
                        }
                    }
                }
                else
                {
                    source = TTSDictionary.Instance.ReplaceWordsTTS(source);

                    if (PlayBridge.Instance.IsAvailable)
                    {
                        PlayBridge.Instance.Play(source, isSync);
                    }
                    else
                    {
                        ActGlobals.oFormActMain.TTS(source);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Write("Play sound error.", ex);
            }
        }

        /// <summary>
        /// Waveファイル
        /// </summary>
        public class WaveFile
        {
            /// <summary>
            /// フルパス
            /// </summary>
            public string FullPath { get; set; }

            /// <summary>
            /// ファイル名
            /// </summary>
            public string Name =>
                !string.IsNullOrWhiteSpace(this.FullPath) ?
                Path.GetFileName(this.FullPath) :
                string.Empty;

            /// <summary>
            /// ToString()
            /// </summary>
            /// <returns>一般化された文字列</returns>
            public override string ToString() => this.Name;
        }
    }
}
