using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using CeVIO.Talk.RemoteService;
using FFXIV.Framework.Common;
using Microsoft.Win32;
using NLog;
using Prism.Mvvm;

namespace ACT.TTSYukkuri.Config
{
    /// <summary>
    /// TTSささら設定
    /// </summary>
    [Serializable]
    public class SasaraConfig :
        BindableBase
    {
        #region Logger

        private Logger Logger => AppLog.DefaultLogger;

        #endregion Logger

        private readonly Lazy<Talker> LazyTalker = new Lazy<Talker>(() => new Talker());

        internal Talker Talker => this.LazyTalker.Value;

        public SasaraConfig()
        {
        }

        /// <summary>
        /// CeVIOがアクティブか？
        /// </summary>
        [XmlIgnore]
        private bool IsActive => Settings.Default.TTS == TTSType.Sasara;

        [XmlIgnore]
        public bool IsInitialized { get; set; }

        [XmlIgnore]
        public ObservableCollection<string> AvailableCasts
        {
            get;
            private set;
        } = new ObservableCollection<string>();

        private string cast;

        public string Cast
        {
            get => this.cast;
            set
            {
                if (this.SetProperty(ref this.cast, value))
                {
                    this.RaisePropertyChanged(nameof(this.AvailableComponents));
                }
            }
        }

        public ObservableCollection<SasaraComponent> Components
        {
            get;
            private set;
        } = new ObservableCollection<SasaraComponent>();

        [XmlIgnore]
        public IEnumerable<SasaraComponent> AvailableComponents =>
            this.Components
            .Where(x => x.Cast == this.cast)
            .OrderBy(x => x.Id);

        private float gain = 2.1f;

        public float Gain
        {
            get => this.gain;
            set => this.SetProperty(ref this.gain, (float)Math.Round(value, 1));
        }

        private uint onryo = 50;

        public uint Onryo
        {
            get => this.onryo;
            set
            {
                if (this.SetProperty(ref this.onryo, value))
                {
                    this.SyncToCevio();
                }
            }
        }

        private uint hayasa = 50;

        public uint Hayasa
        {
            get => this.hayasa;
            set
            {
                if (this.SetProperty(ref this.hayasa, value))
                {
                    this.SyncToCevio();
                }
            }
        }

        private uint takasa = 50;

        public uint Takasa
        {
            get => this.takasa;
            set
            {
                if (this.SetProperty(ref this.takasa, value))
                {
                    this.SyncToCevio();
                }
            }
        }

        private uint seishitsu = 50;

        public uint Seishitsu
        {
            get => this.seishitsu;
            set
            {
                if (this.SetProperty(ref this.seishitsu, value))
                {
                    this.SyncToCevio();
                }
            }
        }

        private uint yokuyo = 50;

        public uint Yokuyo
        {
            get => this.yokuyo;
            set
            {
                if (this.SetProperty(ref this.yokuyo, value))
                {
                    this.SyncToCevio();
                }
            }
        }

        public override string ToString() =>
            $"{nameof(this.Cast)}:{this.Cast}," +
            $"{nameof(this.Gain)}:{this.Gain}," +
            $"{nameof(this.Onryo)}:{this.Onryo}," +
            $"{nameof(this.Hayasa)}:{this.Hayasa}," +
            $"{nameof(this.Takasa)}:{this.Takasa}," +
            $"{nameof(this.Seishitsu)}:{this.Seishitsu}," +
            $"{nameof(this.Yokuyo)}:{this.Yokuyo}," +
            this.Components
                .Select(x => $"{x.Name}.{x.Id}:{x.Value}")
                .Aggregate((x, y) => $"{x},{y}");

        private bool isCevioReady;

        /// <summary>
        /// CeVIOが実行されているか？
        /// </summary>
        [XmlIgnore]
        public bool IsCevioReady
        {
            get => this.isCevioReady;
            set => this.SetProperty(ref this.isCevioReady, value);
        }

        /// <summary>
        /// リモートに反映する
        /// </summary>
        private void SyncToCevio()
        {
            if (this.IsInitialized)
            {
                this.ApplyToCevio();
            }
        }

        /// <summary>
        /// リモートの設定を読み込む
        /// </summary>
        public void LoadRemoteConfig()
        {
            if (!this.IsActive)
            {
                return;
            }

            if (!this.TryStartCevio())
            {
                return;
            }

            var casts = Talker.AvailableCasts;

            // 有効なキャストを列挙する
            var addCasts = casts
                .Where(x => !this.AvailableCasts.Contains(x));
            var removeCasts = this.AvailableCasts
                .Where(x => !casts.Contains(x))
                .ToArray();

            this.AvailableCasts.AddRange(addCasts);
            foreach (var item in removeCasts)
            {
                this.AvailableCasts.Remove(item);
            }

            // キャストのコンポーネントを取得する
            var remoteComponents = new List<SasaraComponent>();

            foreach (var cast in casts)
            {
                this.Talker.Cast = cast;

                foreach (var x in this.Talker.Components)
                {
                    remoteComponents.Add(new SasaraComponent()
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Value = x.Value,
                        Cast = cast
                    });
                }
            }

            this.Components.AddRange(remoteComponents.Where(x => !this.Components.Any(y => x.Id == y.Id)));
            foreach (var item in this.Components
                .Where(x => !remoteComponents.Any(y => x.Id == y.Id))
                .ToArray())
            {
                this.Components.Remove(item);
            }

            // 重複を削除する
            foreach (var group in this.Components
                .GroupBy(x => x.Id)
                .Where(x => x.Count() > 1))
            {
                foreach (var item in group.Take(group.Count() - 1))
                {
                    this.Components.Remove(item);
                }
            }

            // ソートする
            var sorted = this.Components.OrderBy(x => x.Id).ToArray();
            this.Components.Clear();
            this.Components.AddRange(sorted);

            if (!this.AvailableCasts.Contains(this.Cast))
            {
                this.Cast = this.AvailableCasts.FirstOrDefault();
            }

            this.RaisePropertyChanged(nameof(this.AvailableComponents));
        }

        internal void ApplyToCevio()
        {
            if (!this.TryStartCevio())
            {
                return;
            }

            if (string.IsNullOrEmpty(this.Cast))
            {
                return;
            }

            this.Talker.Cast = this.Cast;
            this.Talker.Volume = this.Onryo;
            this.Talker.Speed = this.Hayasa;
            this.Talker.Tone = this.Takasa;
            this.Talker.Alpha = this.Seishitsu;
            this.Talker.ToneScale = this.Yokuyo;

            foreach (var src in this.AvailableComponents)
            {
                var dst = this.Talker.Components.FirstOrDefault(x => x.Id == src.Id);
                if (dst != null)
                {
                    dst.Value = src.Value;
                }
            }
        }

        private static readonly string CeVIOPath = @"C:\Program Files\CeVIO\CeVIO Creative Studio (64bit)\CeVIO Creative Studio.exe";

        private volatile bool isStarting;

        private async void StartCevio()
        {
            if (this.isStarting)
            {
                return;
            }

            try
            {
                this.isStarting = true;

                if (!ServiceControl.IsHostStarted)
                {
                    var result = await Task.Run(() => ServiceControl.StartHost(false));

                    switch (result)
                    {
                        case HostStartResult.Succeeded:
                            this.Logger.Info($"CeVIO RPC start.");
                            break;

                        case HostStartResult.AlreadyStarted:
                            this.Logger.Info($"CeVIO RPC already started, connect to CeVIO.");
                            break;

                        case HostStartResult.NotRegistered:
                            var path = CeVIOPath;

                            if (!File.Exists(path))
                            {
                                using (var regKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\CeVIO\Subject\Editor\x64"))
                                {
                                    var folder = regKey.GetValue("InstallFolder") as string;
                                    if (string.IsNullOrEmpty(folder))
                                    {
                                        this.IsCevioReady = false;
                                        return;
                                    }

                                    path = Path.Combine(folder, "CeVIO Creative Studio.exe");
                                    if (!File.Exists(path))
                                    {
                                        this.IsCevioReady = false;
                                        return;
                                    }
                                }
                            }

                            await Task.Run(() =>
                            {
                                var ps = Process.GetProcessesByName("CeVIO Creative Studio");
                                if (ps != null &&
                                    ps.Length > 0)
                                {
                                    return;
                                }

                                var p = Process.Start(new ProcessStartInfo()
                                {
                                    FileName = path,
                                    UseShellExecute = false,
                                    WorkingDirectory = Path.GetDirectoryName(path)
                                });

                                p.WaitForInputIdle();
                            });
                            break;

                        default:
                            this.IsCevioReady = false;
                            return;
                    }

                    for (int i = 0; i < 60; i++)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1));

                        if (ServiceControl.IsHostStarted)
                        {
                            break;
                        }
                    }

                    this.Talker.Cast = Talker.AvailableCasts.FirstOrDefault();
                }

                this.IsCevioReady = true;
            }
            finally
            {
                this.isStarting = false;
            }
        }

        public bool TryStartCevio()
        {
            if (!this.IsCevioReady)
            {
                this.StartCevio();
            }

            return this.IsCevioReady;
        }

        private async void KillCevio()
        {
            if (ServiceControl.IsHostStarted)
            {
                await Task.Run(() => ServiceControl.CloseHost(HostCloseMode.Interrupt));
                await Task.Delay(100);

                if (!ServiceControl.IsHostStarted)
                {
                    this.Logger.Info($"CeVIO RPC close.");
                }
            }

            this.IsCevioReady = false;
        }
    }
}
