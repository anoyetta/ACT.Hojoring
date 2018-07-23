using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows.Media;
using FFXIV.Framework.Extensions;

namespace FFXIV.Framework.Dialog
{
    [DataContract]
    public class ColorDialogResult
    {
        public const string SymbolKeyword = nameof(ColorDialogResult);

        [DataMember(Order = 1)]
        public string Symbol { get; set; } = SymbolKeyword;

        [DataMember(Order = 2)]
        public bool Result { get; set; }

        [DataMember(Order = 3)]
        public Color Color { get; set; }

        [DataMember(Order = 4)]
        public bool IgnoreAlpha { get; set; }

        public System.Drawing.Color LegacyColor => this.Color.ToLegacy();

        public static ColorDialogResult FromString(
            string json)
        {
            var obj = default(ColorDialogResult);

            var serializer = new DataContractJsonSerializer(typeof(ColorDialogResult));
            var data = Encoding.UTF8.GetBytes(json);
            using (var ms = new MemoryStream(data))
            {
                obj = (ColorDialogResult)serializer.ReadObject(ms);
            }

            return obj;
        }

        public override string ToString()
        {
            var json = string.Empty;

            var serializer = new DataContractJsonSerializer(typeof(ColorDialogResult));
            using (var ms = new MemoryStream())
            {
                serializer.WriteObject(ms, this);
                json = Encoding.UTF8.GetString(ms.ToArray());
            }

            return json;
        }
    }
}
