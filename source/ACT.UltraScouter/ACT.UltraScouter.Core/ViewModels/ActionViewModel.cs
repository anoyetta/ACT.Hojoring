using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Threading;
using ACT.UltraScouter.Common;
using ACT.UltraScouter.Config;
using ACT.UltraScouter.Models;
using ACT.UltraScouter.ViewModels.Bases;
using ACT.UltraScouter.Views;
using Advanced_Combat_Tracker;
using FFXIV.Framework.Common;
using FFXIV.Framework.Extensions;
using NLog;

namespace ACT.UltraScouter.ViewModels
{
    public class ActionViewModel :
        OverlayViewModelBase,
        IOverlayViewModel
    {
        #region Logger

        private Logger logger = AppLog.DefaultLogger;

        #endregion Logger

        public ActionViewModel() : this(null, null)
        {
        }

        public ActionViewModel(
            TargetAction config,
            TargetInfoModel model)
        {
            this.config = config ?? Settings.Instance.TargetAction;
            this.model = model ?? TargetInfoModel.Instance;

            this.RaisePropertyChanged(nameof(Config));
            this.RaisePropertyChanged(nameof(Model));

            this.Initialize();
        }

        public override void Initialize()
        {
            this.countdownTimer.Interval = TimeSpan.FromMilliseconds(100);

            this.Model.Casting -= this.Model_Casting;
            this.countdownTimer.Tick -= this.CountdownTimer_Tick;
            this.Config.PropertyChanged -= this.Config_PropertyChanged;

            this.Model.Casting += this.Model_Casting;
            this.countdownTimer.Tick += this.CountdownTimer_Tick;
            this.Config.PropertyChanged += this.Config_PropertyChanged;

            this.RaisePropertyChanged(nameof(this.CounterFontSize));
        }

        public override void Dispose()
        {
            this.countdownTimer.Stop();

            this.Model.Casting -= this.Model_Casting;
            this.countdownTimer.Tick -= this.CountdownTimer_Tick;
            this.Config.PropertyChanged -= this.Config_PropertyChanged;

            base.Dispose();
        }

        private void Config_PropertyChanged(
            object sender,
            PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(this.Config.CastingRateVisible):
                case nameof(this.Config.CastingRemainVisible):
                    this.RaisePropertyChanged(nameof(this.CounterFontSize));
                    break;
            }
        }

        private TargetAction config;
        private TargetInfoModel model;

        public virtual Settings RootConfig => Settings.Instance;
        public virtual TargetAction Config => this.config;
        public virtual TargetInfoModel Model => this.model;

        public bool OverlayVisible => this.Config.Visible;

        public double CounterFontSize
        {
            get
            {
                var size = this.Config.DisplayText.Font.Size;

                if (this.Config.CastingRateVisible && this.Config.CastingRemainVisible)
                {
                    size *= Settings.Instance.ActionCounterFontSizeRatio;
                }
                else
                {
                    size *= Settings.Instance.ActionCounterSingleFontSizeRatio;
                }

                return Math.Round(size, 1);
            }
        }

        public Color ProgressBarBackColor =>
            !this.Config.UseCircle ?
            this.ProgressBarForeColor.ChangeBrightness(Settings.Instance.ProgressBarDarkRatio) :
            this.ProgressBarForeColor.ChangeBrightness(Settings.Instance.CircleBackBrightnessRate);

        public Color ProgressBarEffectColor =>
            this.ProgressBarForeColor.ChangeBrightness(Settings.Instance.ProgressBarEffectRatio);

        public Color ProgressBarForeColor =>
            this.Config.ProgressBar.AvailableColor(
                this.castingProgressRate * 100d);

        public Color ProgressBarStrokeColor =>
            this.Config.ProgressBar.LinkOutlineColor ?
            this.ProgressBarForeColor :
            this.Config.ProgressBar.OutlineColor;

        private Stopwatch castingStopwatch = new Stopwatch();

        private readonly DispatcherTimer countdownTimer =
            new DispatcherTimer(DispatcherPriority.Background);

        private float castDurationMax;
        private double castingProgressRate;
        private Color foreColorBefore;

        public double CastingRemain { get; protected set; } = 0;
        public string CastingRemainText => this.CastingRemain.ToString(this.CastingRemainFormat);
        public double CastingProgressRateToDisplay { get; protected set; } = 0;

        public string CastingRemainFormat =>
            this.Config.CastingRemainInInteger ? "N0" : "N1";

        private void Model_Casting(
            object sender,
            CastingEventArgs args)
        {
            var view = this.View as ActionView;
            if (view == null)
            {
                return;
            }

            // キャストの最大値を保存する
            this.castDurationMax =
                args.CastDurationMax - args.CastDurationCurrent;

            this.CastingRemain = this.castDurationMax;
            this.CastingProgressRateToDisplay = 0;

            // サウンド
            Task.Run(() => this.PlaySound(args.CastSkillName));

            // カウントダウン
            this.RefreshCountdown();

            // カウントダウンの開始
            this.castingStopwatch.Restart();
            if (this.countdownTimer.IsEnabled)
            {
                this.countdownTimer.Stop();
            }

            this.countdownTimer.Start();

            // アニメーション開始
            view.BeginAnimation(this.castDurationMax);

            var message =
                $"{args.Actor} starts using {args.CastSkillName}. duration={args.CastDurationMax}, id={args.CastSkillID}";
            this.logger.Info(message);
        }

        private void CountdownTimer_Tick(
            object sender,
            EventArgs e)
            => this.RefreshCountdown();

        private void RefreshCountdown()
        {
            var current = this.castingStopwatch.Elapsed.TotalSeconds;

            if (current >= this.castDurationMax)
            {
                this.countdownTimer.Stop();
                this.castingStopwatch.Stop();
            }

            var remain = this.castDurationMax - current;
            if (remain < 0)
            {
                remain = 0;
            }

            var rate =
                this.castDurationMax != 0 ?
                current / this.castDurationMax :
                1;
            if (rate > 1)
            {
                rate = 1;
            }

            var remainToDisplay = remain;
            if (this.Config.CastingRemainInInteger)
            {
                remainToDisplay = Math.Ceiling(remain);
            }
            else
            {
                remain = Math.Ceiling(remain * 10);
                remainToDisplay = remain / 10;
            }

            var rateToDisplay = Math.Floor(rate * 100);

            if (this.Config.CastingRemainVisible &&
                this.CastingRemain != remainToDisplay)
            {
                this.CastingRemain = remainToDisplay;
                this.RaisePropertyChanged(nameof(this.CastingRemain));
                this.RaisePropertyChanged(nameof(this.CastingRemainText));
            }

            if (this.Config.CastingRateVisible &&
                this.CastingProgressRateToDisplay != rateToDisplay)
            {
                this.CastingProgressRateToDisplay = rateToDisplay;
                this.RaisePropertyChanged(nameof(this.CastingProgressRateToDisplay));
            }

            this.castingProgressRate = rate;

            if (this.foreColorBefore !=
                this.ProgressBarForeColor)
            {
                this.RaisePropertyChanged(nameof(this.ProgressBarBackColor));
                this.RaisePropertyChanged(nameof(this.ProgressBarEffectColor));
                this.RaisePropertyChanged(nameof(this.ProgressBarForeColor));
                this.RaisePropertyChanged(nameof(this.ProgressBarStrokeColor));
            }

            this.foreColorBefore = this.ProgressBarForeColor;
        }

        private void PlaySound(
            string skillName)
        {
            if (this.Config.WaveSoundEnabled)
            {
                if (Settings.Instance.UseNAudio)
                {
                    BufferedWavePlayer.Instance.Play(
                        this.Config.WaveFile,
                        Settings.Instance.WaveVolume / 100f);
                }
                else
                {
                    ActGlobals.oFormActMain?.PlaySoundMethod(
                        this.Config.WaveFile,
                        (int)Settings.Instance.WaveVolume);
                }
            }

            if (this.Config.TTSEnabled)
            {
                // スキル名を辞書で置換する
                skillName = TTSDictionary.Instance.ReplaceTTS(skillName);

                if (!string.IsNullOrEmpty(skillName))
                {
                    if (skillName.Contains("UNKNOWN", StringComparison.InvariantCultureIgnoreCase))
                    {
                        skillName = "アンノウン";
                    }

                    TTSWrapper.Speak(skillName);
                }
            }
        }
    }
}
