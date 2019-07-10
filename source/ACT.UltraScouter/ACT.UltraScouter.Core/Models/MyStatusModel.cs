using System;
using FFXIV.Framework.XIVHelper;
using Prism.Mvvm;

namespace ACT.UltraScouter.Models
{
    public class MyStatusModel : BindableBase
    {
        #region Lazy Singleton

        private static readonly Lazy<MyStatusModel> LazyInstance = new Lazy<MyStatusModel>(() => new MyStatusModel());

        public static MyStatusModel Instance => LazyInstance.Value;

        private MyStatusModel()
        {
        }

        #endregion Lazy Singleton

        public uint CurrentHP { get; private set; }

        public double CurrentHPRate => this.MaxHP != 0 ? ((double)this.CurrentHP / (double)this.MaxHP) : 0;

        public uint CurrentMP { get; private set; }

        public double CurrentMPRate => this.MaxMP != 0 ? ((double)this.CurrentMP / (double)this.MaxMP) : 0;

        public uint MaxHP { get; private set; }

        public uint MaxMP => 10000;

        public void Update(
            CombatantEx me)
        {
            var isHPChanged = false;
            var isMPChanged = false;

            if (this.MaxHP != me.MaxHP ||
                this.CurrentHP != me.CurrentHP)
            {
                this.MaxHP = me.MaxHP;
                this.CurrentHP = me.CurrentHP;
                isHPChanged = true;
            }

            if (this.CurrentMP != me.CurrentMP)
            {
                this.CurrentMP = me.CurrentMP;
                isMPChanged = true;
            }

            if (isHPChanged)
            {
                this.RaisePropertyChanged(nameof(this.CurrentHP));
            }

            if (isMPChanged)
            {
                this.RaisePropertyChanged(nameof(this.CurrentMP));
            }
        }
    }
}
