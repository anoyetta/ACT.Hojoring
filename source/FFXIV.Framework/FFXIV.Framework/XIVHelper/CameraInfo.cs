using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FFXIV.Framework.XIVHelper
{
    public class CameraInfo :
        ICloneable,
        INotifyPropertyChanged
    {
        #region Singleton

        private static CameraInfo instance = new CameraInfo();

        public static CameraInfo Instance => instance;

        #endregion Singleton

        private CameraInfo()
        {
        }

        public static readonly float HeadingRange = 6.3f;
        public static readonly float HeadingMax = 3.15f;
        public static readonly float HeadingMin = -3.15f;

        private byte mode;
        private float heading = HeadingMax;
        private float elevation;
        private bool isAvailable = false;

        public byte Mode
        {
            get => this.mode;
            set => this.SetProperty(ref this.mode, value);
        }

        public float Heading
        {
            get => this.heading;
            set
            {
                if (this.SetProperty(ref this.heading, value))
                {
                    this.RaisePropertyChanged(nameof(this.HeadingDegree));
                }
            }
        }

        public double HeadingDegree => (this.heading / HeadingRange) * 360.0 * -1.0;

        public float Elevation
        {
            get => this.elevation;
            set => this.SetProperty(ref this.elevation, value);
        }

        public bool IsAvailable
        {
            get => this.isAvailable;
            set => this.SetProperty(ref this.isAvailable, value);
        }

        public void Refresh()
        {
        }

        #region ICloneable

        public object Clone() => this.MemberwiseClone();

        public CameraInfo CloneCameraInfo() => this.MemberwiseClone() as CameraInfo;

        #endregion ICloneable

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
