using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;
using FFXIV.Framework.TTS.Common;
using FFXIV.Framework.TTS.Common.Models;
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
        private CevioTalkerModel talker;

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
            set => this.SetProperty(ref this.gain, value);
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
        public ObservableCollection<SasaraComponent> Components
        {
            get => this.components;
        }

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
            this.SyncRemoteModel();
        }

        private void ItemOnPropertyChanged(object sender, PropertyChangedEventArgs e)
            => this.SyncRemoteModel();

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
                    this.SyncRemoteModel();
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
                    this.SyncRemoteModel();
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
                    this.SyncRemoteModel();
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
                    this.SyncRemoteModel();
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
                    this.SyncRemoteModel();
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
        public bool AutoSync { get; private set; } = true;

        /// <summary>
        /// CeVIOがアクティブか？
        /// </summary>
        [XmlIgnore]
        private bool IsActive => Settings.Default.TTS == TTSType.Sasara;

        /// <summary>
        /// リモートに反映する
        /// </summary>
        private void SyncRemoteModel()
        {
            if (this.AutoSync)
            {
                this.SetToRemote();
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

            this.talker = RemoteTTSClient.Instance.TTSModel?.GetCevioTalker();
            if (this.talker == null)
            {
                return;
            }

            // 有効なキャストを列挙する
            var addCasts = this.talker.AvailableCasts
                .Where(x => !this.AvailableCasts.Contains(x));
            var removeCasts = this.AvailableCasts
                .Where(x => !this.talker.AvailableCasts.Contains(x))
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
                // 現在の設定をリモートに送る
                this.SetToRemote();
            }
            else
            {
                var cast = this.talker.AvailableCasts.FirstOrDefault();
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

            if (string.IsNullOrWhiteSpace(cast) ||
               this.talker == null)
            {
                return;
            }

            try
            {
                this.AutoSync = false;

                this.talker.Cast = cast;
                RemoteTTSClient.Instance.TTSModel.SetCevioTalker(this.talker);
                this.talker = RemoteTTSClient.Instance.TTSModel.GetCevioTalker();

                this.Components.Clear();
                this.Components.AddRange(talker.Components.Select(x => new SasaraComponent()
                {
                    Id = x.Id,
                    Name = x.Name.Trim(),
                    Value = x.Value,
                    Cast = talker.Cast,
                }));

                this.cast = cast;
                this.Onryo = talker.Volume;
                this.Hayasa = talker.Speed;
                this.Takasa = talker.Tone;
                this.Seishitsu = talker.Alpha;
                this.Yokuyo = talker.ToneScale;
            }
            finally
            {
                this.AutoSync = true;
            }
        }

        /// <summary>
        /// ささらの設定を取得する
        /// </summary>
        /// <returns>
        /// ささらの設定モデル</returns>
        public CevioTalkerModel ToRemoteModel()
        {
            if (this.talker == null)
            {
                this.talker = new CevioTalkerModel();
            }

            this.talker.Cast = this.Cast;
            this.talker.Volume = this.Onryo;
            this.talker.Speed = this.Hayasa;
            this.talker.Tone = this.Takasa;
            this.talker.Alpha = this.Seishitsu;
            this.talker.ToneScale = this.Yokuyo;

            this.talker.Components = this.Components.Select(
                x => new CevioTalkerModel.CevioTalkerComponent()
                {
                    Id = x.Id,
                    Name = x.Name,
                    Value = x.Value,
                }).ToList();

            return this.talker;
        }

        /// <summary>
        /// ささらを設定する
        /// </summary>
        public void SetToRemote(CevioTalkerModel remoteModel)
        {
            if (this.IsActive)
            {
                RemoteTTSClient.Instance.TTSModel?.SetCevioTalker(remoteModel);
            }
        }

        /// <summary>
        /// ささらを設定する
        /// </summary>
        public void SetToRemote() =>
            this.SetToRemote(this.ToRemoteModel());
    }
}
