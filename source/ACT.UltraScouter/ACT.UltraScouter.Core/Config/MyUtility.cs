using System;
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
    }
}
