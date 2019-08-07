using System;
using System.Xml.Serialization;
using Prism.Mvvm;

namespace FFXIV.Framework.Common
{
    [Serializable]
    public class KeyValue<T1, T2> : BindableBase
    {
        private T1 key;

        [XmlAttribute]
        public T1 Key
        {
            get => this.key;
            set => this.SetProperty(ref this.key, value);
        }

        private T2 value;

        [XmlAttribute]
        public T2 Value
        {
            get => this.value;
            set => this.SetProperty(ref this.value, value);
        }
    }
}
