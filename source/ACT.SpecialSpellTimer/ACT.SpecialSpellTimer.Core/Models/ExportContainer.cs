using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace ACT.SpecialSpellTimer.Models
{
    [Serializable]
    [XmlType(TypeName = "Exports")]
    public class ExportContainer
    {
        public Tag Tag
        {
            get;
            set;
        } = null;

        public List<SpellPanel> Panels
        {
            get;
            set;
        } = new List<SpellPanel>();

        public List<Spell> Spells
        {
            get;
            set;
        } = new List<Spell>();

        public List<Ticker> Tickers
        {
            get;
            set;
        } = new List<Ticker>();

        #region Load / Save

        public void Save(
            string file)
        {
            var dir = Path.GetDirectoryName(file);

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var ns = new XmlSerializerNamespaces();
            ns.Add(string.Empty, string.Empty);

            var sb = new StringBuilder();
            using (var sw = new StringWriter(sb))
            {
                var xs = new XmlSerializer(this.GetType());
                xs.Serialize(sw, this, ns);
            }

            sb.Replace("utf-16", "utf-8");

            File.WriteAllText(
                file,
                sb.ToString(),
                new UTF8Encoding(false));
        }

        public static ExportContainer LoadFromFile(
            string file)
        {
            var data = default(ExportContainer);

            if (!File.Exists(file))
            {
                return data;
            }

            using (var sr = new StreamReader(file, new UTF8Encoding(false)))
            {
                if (sr.BaseStream.Length > 0)
                {
                    var xs = new XmlSerializer(typeof(ExportContainer));
                    data = xs.Deserialize(sr) as ExportContainer;
                }
            }

            if (data == null)
            {
                return data;
            }

            if (data.Tag != null)
            {
                data.Tag.ID = Guid.NewGuid();
            }

            var panelIDConverter = new Dictionary<Guid, Guid>();
            foreach (var panel in data.Panels)
            {
                var newID = Guid.NewGuid();
                panelIDConverter[panel.ID] = newID;
                panel.ID = newID;
            }

            var seq = default(long);

            seq =
                SpellTable.Instance.Table.Any() ?
                SpellTable.Instance.Table.Max(x => x.ID) + 1 :
                1;
            foreach (var spell in data.Spells)
            {
                spell.ID = seq++;
                spell.Guid = Guid.NewGuid();

                if (panelIDConverter.ContainsKey(spell.PanelID))
                {
                    spell.PanelID = panelIDConverter[spell.PanelID];
                }
            }

            seq =
                TickerTable.Instance.Table.Any() ?
                TickerTable.Instance.Table.Max(x => x.ID) + 1 :
                1;
            foreach (var spell in data.Tickers)
            {
                spell.ID = seq++;
                spell.Guid = Guid.NewGuid();
            }

            return data;
        }

        #endregion Load / Save
    }
}
