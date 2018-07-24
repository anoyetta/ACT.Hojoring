using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using FFXIV.Framework.Common;

namespace FFXIV.Framework.Dialog
{
    [DataContract]
    public class FontDialogResult
    {
        public const string SymbolKeyword = nameof(FontDialogResult);

        [DataMember(Order = 1)]
        public string Symbol { get; set; } = SymbolKeyword;

        [DataMember(Order = 2)]
        public bool Result { get; set; }

        [DataMember(Order = 3)]
        public FontInfo Font { get; set; }

        public static FontDialogResult FromString(
            string json)
        {
            var obj = default(FontDialogResult);

            var serializer = new DataContractJsonSerializer(typeof(FontDialogResult));
            var data = Encoding.UTF8.GetBytes(json);
            using (var ms = new MemoryStream(data))
            {
                obj = (FontDialogResult)serializer.ReadObject(ms);
            }

            return obj;
        }

        public override string ToString()
        {
            var json = string.Empty;

            var serializer = new DataContractJsonSerializer(typeof(FontDialogResult));
            using (var ms = new MemoryStream())
            {
                serializer.WriteObject(ms, this);
                json = Encoding.UTF8.GetString(ms.ToArray());
            }

            return json;
        }
    }
}
