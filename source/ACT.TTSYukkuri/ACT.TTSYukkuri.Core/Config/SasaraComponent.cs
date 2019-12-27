using System;
using Prism.Mvvm;

namespace ACT.TTSYukkuri.Config
{
    [Serializable]
    public class SasaraComponent :
        BindableBase
    {
        private string id;
        private string name;
        private uint value;
        private string cast;

        public string Id
        {
            get => this.id;
            set => this.SetProperty(ref this.id, value);
        }

        public string Name
        {
            get => this.name;
            set => this.SetProperty(ref this.name, value);
        }

        public uint Value
        {
            get => this.value;
            set => this.SetProperty(ref this.value, value);
        }

        public string Cast
        {
            get => this.cast;
            set => this.SetProperty(ref this.cast, value);
        }

        public override string ToString()
            => $"Id:{this.Id},Name:{this.Name},Value:{this.Value},Cast:{this.Cast}";
    }
}
