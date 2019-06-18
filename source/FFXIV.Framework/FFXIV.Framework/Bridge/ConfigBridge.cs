using System;
using FFXIV.Framework.Globalization;
using FFXIV.Framework.XIVHelper;
using Prism.Mvvm;

namespace FFXIV.Framework.Bridge
{
    public class ConfigBridge : BindableBase
    {
        #region Singleton

        private static ConfigBridge instance;

        public static ConfigBridge Instance =>
            instance ?? (instance = new ConfigBridge());

        private ConfigBridge()
        {
        }

        #endregion Singleton

        private NameStyles pcNameStyle = NameStyles.FullName;

        public NameStyles PCNameStyle
        {
            get => this.pcNameStyle;
            set => this.SetProperty(ref this.pcNameStyle, value);
        }

        public Func<Locales> GetUILocaleCallback { get; private set; }

        public void SetUILocaleCallback(
            Func<Locales> callback)
        {
            if (this.GetUILocaleCallback == null)
            {
                this.GetUILocaleCallback = callback;
            }
        }
    }
}
