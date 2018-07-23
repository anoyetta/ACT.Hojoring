using System;
using System.Collections.ObjectModel;
using System.Linq;
using Prism.Mvvm;

namespace ACT.TTSYukkuri.Config
{
    /// <summary>
    /// オプション設定
    /// </summary>
    [Serializable]
    public class StatusAlertConfig :
        BindableBase
    {
        private bool enabledHPAlert;
        private int hpThreshold;
        private string hpTextToSpeack = "<pcname>,HP<hpp>%.";

        private bool enabledMPAlert;
        private int mpThreshold;
        private string mpTextToSpeack = "<pcname>,MP<mpp>%.";

        private bool enabledTPAlert;
        private int tpThreshold;
        private string tpTextToSpeack = "<pcname>,TP<tpp>%.";

        private ObservableCollection<AlertTarget> alertTargetsHP = new ObservableCollection<AlertTarget>();
        private ObservableCollection<AlertTarget> alertTargetsMP = new ObservableCollection<AlertTarget>();
        private ObservableCollection<AlertTarget> alertTargetsTP = new ObservableCollection<AlertTarget>();

        /// <summary>
        /// HPの監視を有効にする
        /// </summary>
        public bool EnabledHPAlert
        {
            get => this.enabledHPAlert;
            set
            {
                if (this.SetProperty(ref this.enabledHPAlert, value))
                {
                    if (value)
                    {
                        FFXIVWatcher.Default?.Start();
                    }
                    else
                    {
                        FFXIVWatcher.Default?.Stop();
                    }
                }
            }
        }

        /// <summary>
        /// HP読上げのしきい値
        /// </summary>
        public int HPThreshold
        {
            get => this.hpThreshold;
            set => this.SetProperty(ref this.hpThreshold, value);
        }

        /// <summary>
        /// HP低下時の読上げテキスト
        /// </summary>
        public string HPTextToSpeack
        {
            get => this.hpTextToSpeack;
            set => this.SetProperty(ref this.hpTextToSpeack, value);
        }

        /// <summary>
        /// MPの監視を有効にする
        /// </summary>
        public bool EnabledMPAlert
        {
            get => this.enabledMPAlert;
            set
            {
                if (this.SetProperty(ref this.enabledMPAlert, value))
                {
                    if (value)
                    {
                        FFXIVWatcher.Default?.Start();
                    }
                    else
                    {
                        FFXIVWatcher.Default?.Stop();
                    }
                }
            }
        }

        /// <summary>
        /// MP読上げのしきい値
        /// </summary>
        public int MPThreshold
        {
            get => this.mpThreshold;
            set => this.SetProperty(ref this.mpThreshold, value);
        }

        /// <summary>
        /// MP低下時の読上げテキスト
        /// </summary>
        public string MPTextToSpeack
        {
            get => this.mpTextToSpeack;
            set => this.SetProperty(ref this.mpTextToSpeack, value);
        }

        /// <summary>
        /// TPの監視を有効にする
        /// </summary>
        public bool EnabledTPAlert
        {
            get => this.enabledTPAlert;
            set
            {
                if (this.SetProperty(ref this.enabledTPAlert, value))
                {
                    if (value)
                    {
                        FFXIVWatcher.Default?.Start();
                    }
                    else
                    {
                        FFXIVWatcher.Default?.Stop();
                    }
                }
            }
        }

        /// <summary>
        /// TP読上げのしきい値
        /// </summary>
        public int TPThreshold
        {
            get => this.tpThreshold;
            set => this.SetProperty(ref this.tpThreshold, value);
        }

        /// <summary>
        /// TP低下時の読上げテキスト
        /// </summary>
        public string TPTextToSpeack
        {
            get => this.tpTextToSpeack;
            set => this.SetProperty(ref this.tpTextToSpeack, value);
        }

        /// <summary>
        /// HPの監視対象
        /// </summary>
        public ObservableCollection<AlertTarget> AlertTargetsHP
        {
            get => this.alertTargetsHP;
            set => this.SetProperty(ref this.alertTargetsHP, value);
        }

        /// <summary>
        /// MPの監視対象
        /// </summary>
        public ObservableCollection<AlertTarget> AlertTargetsMP
        {
            get => this.alertTargetsMP;
            set => this.SetProperty(ref this.alertTargetsMP, value);
        }

        /// <summary>
        /// TPの監視対象
        /// </summary>
        public ObservableCollection<AlertTarget> AlertTargetsTP
        {
            get => this.alertTargetsTP;
            set => this.SetProperty(ref this.alertTargetsTP, value);
        }

        /// <summary>
        /// アラート対象に初期値をセットする
        /// </summary>
        public void SetDefaultAlertTargets()
        {
            var defaultTargets = AlertTarget.EnumlateAlertTargets;

            var missingTargetsHP = defaultTargets.Where(x =>
                !this.AlertTargetsHP.Any(y => y.Category == x.Category));
            this.AlertTargetsHP.AddRange(missingTargetsHP);

            var missingTargetsMP = defaultTargets.Where(x =>
                !this.AlertTargetsMP.Any(y => y.Category == x.Category));
            this.AlertTargetsMP.AddRange(missingTargetsMP);

            var missingTargetsTP = defaultTargets.Where(x =>
                !this.AlertTargetsTP.Any(y => y.Category == x.Category));
            this.AlertTargetsTP.AddRange(missingTargetsTP);
        }
    }
}
