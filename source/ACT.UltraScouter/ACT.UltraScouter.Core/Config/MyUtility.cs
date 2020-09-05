using System;
using System.Windows.Input;
using FFXIV.Framework.Common;
using FFXIV.Framework.XIVHelper;
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

        private MyUtilitySendKeyConfig restoreTankStance;

        public MyUtilitySendKeyConfig RestoreTankStance
        {
            get => this.restoreTankStance;
            set => this.SetProperty(ref this.restoreTankStance, value);
        }

        private MyUtilitySendKeyConfig summonFairy;

        public MyUtilitySendKeyConfig SummonFairy
        {
            get => this.summonFairy;
            set => this.SetProperty(ref this.summonFairy, value);
        }

        private MyUtilitySendKeyConfig drawCard;

        public MyUtilitySendKeyConfig DrawCard
        {
            get => this.drawCard;
            set => this.SetProperty(ref this.drawCard, value);
        }

        private MyUtilitySendKeyConfig summonEgi;

        public MyUtilitySendKeyConfig SummonEgi
        {
            get => this.summonEgi;
            set => this.SetProperty(ref this.summonEgi, value);
        }
    }

    public class MyUtilitySendKeyConfig : SendKeyConfig
    {
        private bool isOnlyRAIDParty;

        public bool IsOnlyRAIDParty
        {
            get => this.isOnlyRAIDParty;
            set => this.SetProperty(ref this.isOnlyRAIDParty, value);
        }

        public bool IsAvailable()
        {
            if (!this.IsEnabled ||
                this.KeySet.Key == Key.None)
            {
                return false;
            }

            if (this.isOnlyRAIDParty &&
                CombatantsManager.Instance.PartyCount != 8)
            {
                return false;
            }

            return true;
        }
    }

    public class ExtendMealEffectSendKeyConfig : MyUtilitySendKeyConfig
    {
        private int remainingTimeThreshold;

        public int RemainingTimeThreshold
        {
            get => this.remainingTimeThreshold;
            set => this.SetProperty(ref this.remainingTimeThreshold, value);
        }
    }
}
