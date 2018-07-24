using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using FFXIV.Framework.Globalization;

namespace XIVDBDownloader.Models
{
    [DataContract]
    public class InstanceData
    {
        [DataMember(Order = 1, Name = "id")]
        public int ID { get; set; }

        [DataMember(Order = 2, Name = "name_de")]
        public string NameDe { get; set; }

        [DataMember(Order = 3, Name = "name_en")]
        public string NameEn { get; set; }

        [DataMember(Order = 4, Name = "name_fr")]
        public string NameFr { get; set; }

        [DataMember(Order = 5, Name = "name_ja")]
        public string NameJa { get; set; }
    }

    public class InstanceModel :
        XIVDBApiBase<IList<InstanceData>>
    {
        public override string Uri =>
            @"https://api.xivdb.com/instance?pretty=1&columns=id,name_ja,name_en,name_fr,name_de";

        public void SaveToCSV(
            string file,
            Locales language)
        {
            if (this.ResultList == null ||
                this.ResultList.Count < 1)
            {
                return;
            }

            if (!Directory.Exists(Path.GetDirectoryName(file)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(file));
            }

            var buffer = new StringBuilder(5120);

            using (var sw = new StreamWriter(file, false, new UTF8Encoding(false)))
            {
                sw.WriteLine(
                    $"ID,NameEn,Name");

                var orderd =
                    from x in this.ResultList
                    orderby
                    x.ID
                    select
                    x;

                foreach (var data in orderd)
                {
                    var name = data.NameEn;

                    switch (language)
                    {
                        case Locales.JA:
                            name = data.NameJa;
                            break;

                        case Locales.FR:
                            name = data.NameFr;
                            break;

                        case Locales.DE:
                            name = data.NameDe;
                            break;
                    }

                    buffer.AppendLine(
                        $"{data.ID},{data.NameEn},{name}");

                    if (buffer.Length >= 5120)
                    {
                        sw.Write(buffer.ToString());
                        buffer.Clear();
                    }
                }

                if (buffer.Length > 0)
                {
                    sw.Write(buffer.ToString());
                }
            }
        }
    }
}
