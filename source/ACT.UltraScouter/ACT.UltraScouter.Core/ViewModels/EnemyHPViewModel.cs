using System.ComponentModel;
using System.Windows;
using ACT.UltraScouter.Config;
using ACT.UltraScouter.Models;
using ACT.UltraScouter.ViewModels.Bases;
using FFXIV.Framework.XIVHelper;

namespace ACT.UltraScouter.ViewModels
{
    public class EnemyHPViewModel :
        OverlayViewModelBase,
        IOverlayViewModel
    {
        public EnemyHPViewModel() : this(null, null)
        {
        }

        public EnemyHPViewModel(
            EnemyHP config,
            EnemyHPListModel model)
        {
            this.Config = config ?? this.GetConfig;
            this.Model = model ?? EnemyHPListModel.Instance;

            this.RaisePropertyChanged(nameof(Config));
            this.RaisePropertyChanged(nameof(Model));

            this.Initialize();
        }

        protected virtual EnemyHP GetConfig => Settings.Instance.EnemyHP;

        public override void Initialize()
        {
            this.Model.PropertyChanged += this.Model_PropertyChanged;
            this.Config.PropertyChanged += this.Config_PropertyChanged;
        }

        public override void Dispose()
        {
            this.Model.PropertyChanged -= this.Model_PropertyChanged;
            this.Config.PropertyChanged -= this.Config_PropertyChanged;
            base.Dispose();
        }

        private void Model_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
        }

        private void Config_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(this.Config.IsLock):
                    this.RaisePropertyChanged(nameof(this.ResizeMode));
                    break;
            }
        }

        public virtual EnemyHP Config { get; protected set; }

        public virtual EnemyHPListModel Model { get; protected set; }

        public virtual bool OverlayVisible
        {
            get
            {
                if (!this.Config.Visible)
                {
                    return false;
                }

                if (!this.Config.IsDesignMode)
                {
                    if (this.Config.HideInNotCombat &&
                        !XIVPluginHelper.Instance.InCombat)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public ResizeMode ResizeMode => this.Config.IsLock ?
            ResizeMode.NoResize :
            ResizeMode.CanResizeWithGrip;
    }
}
