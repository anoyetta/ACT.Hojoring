using System;
using System.Windows;
using FFXIV.Framework.WPF.Views;
using Prism.Mvvm;

namespace ACT.UltraScouter.ViewModels.Bases
{
    public abstract class OverlayViewModelBase :
        BindableBase,
        IViewModel,
        IDisposable
    {
        private bool isTransparentWindow;

        public bool IsTransparentWindow
        {
            get => this.isTransparentWindow;
            set => this.SetProperty(ref this.isTransparentWindow, value);
        }

        public Window View { get; set; }

        public abstract void Initialize();

        /// <summary>
        /// 透明Window（クリック透過）を設定する
        /// </summary>
        /// <param name="clickThrough">
        /// クリック透過？</param>
        public void SetTransparentWindow(
            bool clickThrough)
        {
            if (this.IsTransparentWindow != clickThrough)
            {
                if (clickThrough)
                {
                    this.View.ToTransparent();
                }
                else
                {
                    this.View.ToNotTransparent();
                }

                this.IsTransparentWindow = clickThrough;
            }
        }

        public virtual void Dispose()
        {
        }

        /// <summary>
        /// すべてのPropertiesの変更通知を発生させる
        /// </summary>
        public void RaiseAllPropertiesChanged()
        {
            foreach (var pi in this.GetType().GetProperties())
            {
                this.RaisePropertyChanged(pi.Name);
            }
        }
    }
}
