using System;
using System.Linq;
using ACT.UltraScouter.Config;
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

        public bool IsAvailableMPView { get; private set; }

        public void Update(
            CombatantEx me)
        {
            if (Settings.Instance.MyHP.IsDesignMode ||
                Settings.Instance.MyMP.IsDesignMode)
            {
                this.UpdateDesignData();
                return;
            }

            var isHPChanged = false;
            var isMPChanged = false;

            if (this.MaxHP != me.MaxHP ||
                this.CurrentHP != me.CurrentHP)
            {
                this.MaxHP = me.MaxHP;
                this.CurrentHP = me.CurrentHP;
                isHPChanged = true;
            }

            if (this.IsAvailableMPView)
            {
                if (this.CurrentMP != me.CurrentMP)
                {
                    this.CurrentMP = me.CurrentMP;
                    isMPChanged = true;
                }
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

        public void UpdateAvailablityMPView(
            CombatantEx me)
        {
            var result = false;

            if (!Settings.Instance.MyMP.TargetJobs.Any(x => x.Available))
            {
                result = true;
            }
            else
            {
                result = Settings.Instance.MyMP.TargetJobs
                    .FirstOrDefault(x => x.Job == me.JobID)?
                    .Available ?? false;
            }

            this.IsAvailableMPView = result;
        }

        private static readonly uint DummyMaxHP = 112233;

        private void UpdateDesignData()
        {
            this.MaxHP = DummyMaxHP;

            var rate = (double)(((DateTime.Now.Second % 10) + 1)) / 10d;

            var currentHP = (uint)(DummyMaxHP * rate);
            var currentMP = (uint)(this.MaxMP * rate);

            if (this.CurrentHP != currentHP)
            {
                this.CurrentHP = currentHP;
                this.RaisePropertyChanged(nameof(this.CurrentHP));
            }

            if (this.CurrentMP != currentMP)
            {
                this.CurrentMP = currentMP;
                this.RaisePropertyChanged(nameof(this.CurrentMP));
            }
        }
    }
}
