using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace FFXIV.Framework.resources
{
    /// <summary>
    /// Window1.xaml の相互作用ロジック
    /// </summary>
    public partial class ResourcesDownloaderView :
        Window,
        INotifyPropertyChanged
    {
        public ResourcesDownloaderView()
        {
            this.InitializeComponent();
        }

        private string currentResources;

        public string CurrentResources
        {
            get => this.currentResources;
            set => this.SetProperty(ref this.currentResources, value);
        }

        #region INotifyPropertyChanged

        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(
            [CallerMemberName]string propertyName = null)
        {
            this.PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }

        protected virtual bool SetProperty<T>(
            ref T field,
            T value,
            [CallerMemberName]string propertyName = null)
        {
            if (Equals(field, value))
            {
                return false;
            }

            field = value;
            this.PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));

            return true;
        }

        #endregion INotifyPropertyChanged
    }
}
