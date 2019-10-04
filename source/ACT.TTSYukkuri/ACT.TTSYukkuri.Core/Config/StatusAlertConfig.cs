using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Serialization;
using Prism.Mvvm;
using FFXIV.Framework.Bridge;

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
        private string hpTextToSpeack = "<job>,HP <hpp>%.";

        private bool enabledMPAlert;
        private int mpThreshold;
        private string mpTextToSpeack = "<job>,MP <mpp>%.";

        private bool enabledTPAlert;
        private int tpThreshold;
        private string tpTextToSpeack = "<job>,TP <tpp>%.";

        private bool enabledGPAlert;
        private int gpThreshold;
        private string gpTextToSpeack = "<job>,GP <gpp>%.";

        private ObservableCollection<AlertTarget> alertTargetsHP = new ObservableCollection<AlertTarget>();
        private ObservableCollection<AlertTarget> alertTargetsMP = new ObservableCollection<AlertTarget>();
        private ObservableCollection<AlertTarget> alertTargetsTP = new ObservableCollection<AlertTarget>();
        private ObservableCollection<AlertTarget> alertTargetsGP = new ObservableCollection<AlertTarget>();

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
        /// GPの監視を有効にする
        /// </summary>
        public bool EnabledGPAlert
        {
            get => this.enabledGPAlert;
            set
            {
                if (this.SetProperty(ref this.enabledGPAlert, value))
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
        /// GP読上げのしきい値
        /// </summary>
        public int GPThreshold
        {
            get => this.gpThreshold;
            set => this.SetProperty(ref this.gpThreshold, value);
        }

        /// <summary>
        /// GP低下時の読上げテキスト
        /// </summary>
        public string GPTextToSpeack
        {
            get => this.gpTextToSpeack;
            set => this.SetProperty(ref this.gpTextToSpeack, value);
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
        /// GPの監視対象
        /// </summary>
        public ObservableCollection<AlertTarget> AlertTargetsGP
        {
            get => this.alertTargetsGP;
            set => this.SetProperty(ref this.alertTargetsGP, value);
        }

        private PlayDevices noticeDeviceForHP = PlayDevices.Both;

        /// <summary>
        /// HPの通知先デバイス
        /// </summary>
        public PlayDevices NoticeDeviceForHP
        {
            get => this.noticeDeviceForHP;
            set => this.SetProperty(ref this.noticeDeviceForHP, value);
        }

        private PlayDevices noticeDeviceForMP = PlayDevices.Both;

        /// <summary>
        /// MPの通知先デバイス
        /// </summary>
        public PlayDevices NoticeDeviceForMP
        {
            get => this.noticeDeviceForMP;
            set => this.SetProperty(ref this.noticeDeviceForMP, value);
        }

        private PlayDevices noticeDeviceForTP = PlayDevices.Both;

        /// <summary>
        /// TPの通知先デバイス
        /// </summary>
        public PlayDevices NoticeDeviceForTP
        {
            get => this.noticeDeviceForTP;
            set => this.SetProperty(ref this.noticeDeviceForTP, value);
        }

        private PlayDevices noticeDeviceForGP = PlayDevices.Both;

        /// <summary>
        /// GPの通知先デバイス
        /// </summary>
        public PlayDevices NoticeDeviceForGP
        {
            get => this.noticeDeviceForGP;
            set => this.SetProperty(ref this.noticeDeviceForGP, value);
        }

        private VoicePalettes noticeVoicePaletteForHP = FFXIV.Framework.Bridge.VoicePalettes.Default;

        /// <summary>
        /// HPの通知用設定
        /// </summary>
        public VoicePalettes NoticeVoicePaletteForHP
        {
            get => this.noticeVoicePaletteForHP;
            set => this.SetProperty(ref this.noticeVoicePaletteForHP, value);
        }

        private VoicePalettes noticeVoicePaletteForMP = FFXIV.Framework.Bridge.VoicePalettes.Default;

        /// <summary>
        /// MPの通知用設定
        /// </summary>
        public VoicePalettes NoticeVoicePaletteForMP
        {
            get => this.noticeVoicePaletteForMP;
            set => this.SetProperty(ref this.noticeVoicePaletteForMP, value);
        }

        private VoicePalettes noticeVoicePaletteForTP = FFXIV.Framework.Bridge.VoicePalettes.Default;

        /// <summary>
        /// TPの通知用設定
        /// </summary>
        public VoicePalettes NoticeVoicePaletteForTP
        {
            get => this.noticeVoicePaletteForTP;
            set => this.SetProperty(ref this.noticeVoicePaletteForTP, value);
        }

        private VoicePalettes noticeVoicePaletteForGP = FFXIV.Framework.Bridge.VoicePalettes.Default;

        /// <summary>
        /// GPの通知用設定
        /// </summary>
        public VoicePalettes NoticeVoicePaletteForGP
        {
            get => this.noticeVoicePaletteForGP;
            set => this.SetProperty(ref this.noticeVoicePaletteForGP, value);
        }

        [XmlIgnore]
        public IEnumerable<PlayDevices> Devices => (IEnumerable<PlayDevices>)Enum.GetValues(typeof(PlayDevices));

        [XmlIgnore]
        public IEnumerable<VoicePalettes> Palettes => (IEnumerable<VoicePalettes>)Enum.GetValues(typeof(VoicePalettes));

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

            var missingTargetsGP = defaultTargets.Where(x =>
                !this.AlertTargetsGP.Any(y => y.Category == x.Category));
            this.AlertTargetsGP.AddRange(missingTargetsGP);
        }
    }
}
