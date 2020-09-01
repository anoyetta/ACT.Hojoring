using System;
using FFXIV.Framework.Common;
using Prism.Mvvm;

namespace ACT.UltraScouter.Config
{
    [Serializable]
    public class MyUtility : BindableBase
    {
        private int delayFromWipeout;

        public int DelayFromWipeout
        {
            get => this.delayFromWipeout;
            set => this.SetProperty(ref this.delayFromWipeout, value);
        }

        private ExtendMealEffectSendKeyConfig extendMealEffect;

        public ExtendMealEffectSendKeyConfig ExtendMealEffect
        {
            get => this.extendMealEffect;
            set => this.SetProperty(ref this.extendMealEffect, value);
        }

        private SendKeyConfig restoreTankStance;

        public SendKeyConfig RestoreTankStance
        {
            get => this.restoreTankStance;
            set => this.SetProperty(ref this.restoreTankStance, value);
        }

        private SendKeyConfig summonFairy;

        public SendKeyConfig SummonFairy
        {
            get => this.summonFairy;
            set => this.SetProperty(ref this.summonFairy, value);
        }

        private SendKeyConfig drawCard;

        public SendKeyConfig DrawCard
        {
            get => this.drawCard;
            set => this.SetProperty(ref this.drawCard, value);
        }

        private SendKeyConfig summonEgi;

        public SendKeyConfig SummonEgi
        {
            get => this.summonEgi;
            set => this.SetProperty(ref this.summonEgi, value);
        }
    }

    public class ExtendMealEffectSendKeyConfig : SendKeyConfig
    {
        private int remainingTimeThreshold;

        public int RemainingTimeThreshold
        {
            get => this.remainingTimeThreshold;
            set => this.SetProperty(ref this.remainingTimeThreshold, value);
        }
    }
}
