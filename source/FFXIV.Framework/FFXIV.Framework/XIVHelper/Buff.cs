namespace FFXIV.Framework.XIVHelper
{
    public class Buff
    {
        public uint ID { get; set; }
        public string Name { get; set; }

        public override string ToString() => $"ID={this.ID} Name={this.Name}";
    }
}
