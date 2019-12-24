using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using CeVIO.Talk.RemoteService;
using FFXIV.Framework.Common;
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

        private string cast;
        private float gain = 2.1f;
        private uint onryo = 50;
        private uint hayasa = 50;
        private uint takasa = 50;
        private uint seishitsu = 50;
        private uint yokuyo = 50;
        private ObservableCollection<SasaraComponent> components = new ObservableCollection<SasaraComponent>();

        public SasaraConfig()
        {
            this.components.CollectionChanged += this.ComponentsCollectionChanged;
        }

        /// <summary>
        /// 有効なキャストのリスト
        /// </summary>
        public ObservableCollection<string> AvailableCasts
        {
            get;
            private set;
        } = new ObservableCollection<string>();

        /// <summary>
        /// ゲイン
        /// </summary>
        public float Gain
        {
            get => this.gain;
            set => this.SetProperty(ref this.gain, (float)Math.Round(value, 1));
        }

        /// <summary>
        /// キャスト
        /// </summary>
        public string Cast
        {
            get => this.cast;
            set
            {
                if (this.SetProperty(ref this.cast, value))
                {
                    this.SetCast(this.cast);
                }
            }
        }

        /// <summary>
        /// 感情コンポーネント
        /// </summary>
        public ObservableCollection<SasaraComponent> Components => this.components;

        private void ComponentsCollectionChanged(
            object sender,
            NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (INotifyPropertyChanged item in e.OldItems)
                {
                    item.PropertyChanged -= this.ItemOnPropertyChanged;
                }
            }

            if (e.NewItems != null)
            {
                foreach (INotifyPropertyChanged item in e.NewItems)
                {
                    item.PropertyChanged += this.ItemOnPropertyChanged;
                }
            }

            // 変更を同期させる
            this.SyncToCevio();
        }

        private void ItemOnPropertyChanged(object sender, PropertyChangedEventArgs e)
            => this.SyncToCevio();

        /// <summary>
        /// 音量
        /// </summary>
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

        /// <summary>
        /// 早さ
        /// </summary>
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

        /// <summary>
        /// 高さ
        /// </summary>
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

        /// <summary>
        /// 声質
        /// </summary>
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

        /// <summary>
        /// 抑揚
        /// </summary>
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

        /// <summary>
        /// リモートに自動的に反映するか？
        /// </summary>
        [XmlIgnore]
        public bool AutoSync { get; set; } = false;

        /// <summary>
        /// CeVIOがアクティブか？
        /// </summary>
        [XmlIgnore]
        private bool IsActive => Settings.Default.TTS == TTSType.Sasara;

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
            if (this.AutoSync)
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

            this.StartCevio();
            if (!this.IsCevioReady)
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

            if (!string.IsNullOrWhiteSpace(this.Cast) &&
                this.AvailableCasts.Contains(this.Cast) &&
                this.Components.Any())
            {
                this.ApplyToCevio();
            }
            else
            {
                var cast = casts.FirstOrDefault();
                if (this.AvailableCasts.Contains(this.Cast))
                {
                    cast = this.Cast;
                }

                this.SetCast(cast);
            }
        }

        /// <summary>
        /// キャストを変更する
        /// </summary>
        /// <param name="cast">
        /// キャスト</param>
        public void SetCast(
            string cast)
        {
            if (!this.IsActive)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(cast))
            {
                return;
            }

            this.StartCevio();
            if (!this.IsCevioReady)
            {
                return;
            }

            try
            {
                this.AutoSync = false;

                this.Talker.Cast = cast;

                var list = new List<SasaraComponent>();
                for (int i = 0; i < this.Talker.Components.Length; i++)
                {
                    var x = this.Talker.Components[i];

                    list.Add(new SasaraComponent()
                    {
                        Id = x.Id,
                        Name = x.Name.Trim(),
                        Value = x.Value,
                        Cast = cast,
                    });
                }

                this.Components.Clear();
                this.Components.AddRange(list);

                this.cast = cast;
                this.Onryo = this.Talker.Volume;
                this.Hayasa = this.Talker.Speed;
                this.Takasa = this.Talker.Tone;
                this.Seishitsu = this.Talker.Alpha;
                this.Yokuyo = this.Talker.ToneScale;
            }
            finally
            {
                this.AutoSync = true;
            }
        }

        internal async void ApplyToCevio()
        {
            this.StartCevio();
            if (!this.IsCevioReady)
            {
                return;
            }

            await Task.Run(() =>
            {
                this.Talker.Cast = this.Cast;
                this.Talker.Volume = this.Onryo;
                this.Talker.Speed = this.Hayasa;
                this.Talker.Tone = this.Takasa;
                this.Talker.Alpha = this.Seishitsu;
                this.Talker.ToneScale = this.Yokuyo;

                foreach (var src in this.components)
                {
                    var dst = this.Talker.Components[src.Name];
                    dst.Value = src.Value;
                }
            });
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
                            if (!File.Exists(CeVIOPath))
                            {
                                this.IsCevioReady = false;
                                return;
                            }

                            await Task.Run(() =>
                            {
                                var ps = Process.GetProcessesByName("CeVIO Creative Studio");
                                if (ps != null &&
                                    ps.Length > 0)
                                {
                                    return;
                                }

                                var p = Process.Start(CeVIOPath);
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
