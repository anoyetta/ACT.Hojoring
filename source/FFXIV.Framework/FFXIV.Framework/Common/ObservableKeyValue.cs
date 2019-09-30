using System;
using System.Xml.Serialization;
using Prism.Mvvm;

namespace FFXIV.Framework.Common
{
    [Serializable]
    public class ObservableKeyValue<T1, T2> :
        BindableBase
    {
        public ObservableKeyValue()
        {
        }

        public ObservableKeyValue(T1 key, T2 value)
        {
            this.key = key;
            this.value = value;
        }

        private T1 key;

        [XmlAttribute(AttributeName = "Key")]
        public T1 Key
        {
            get => this.key;
            set
            {
                if (this.SetProperty(ref this.key, value))
                {
                    this.RaisePropertyChanged(nameof(this.Text));
                }
            }
        }

        private T2 value;

        [XmlAttribute(AttributeName = "Value")]
        public T2 Value
        {
            get => this.value;
            set
            {
                if (this.SetProperty(ref this.value, value))
                {
                    this.RaisePropertyChanged(nameof(this.Text));
                }
            }
        }

        [XmlIgnore]
        public Func<T1, T2, string> FormatTextDelegate { get; set; }

        [XmlIgnore]
        public string Text => this.FormatTextDelegate == null ?
            this.Key.ToString() :
            this.FormatTextDelegate.Invoke(this.key, this.value);
    }
}
