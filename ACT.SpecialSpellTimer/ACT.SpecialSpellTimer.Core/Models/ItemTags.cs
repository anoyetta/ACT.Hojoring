using System;
using System.Linq;
using System.Windows.Input;
using System.Xml.Serialization;
using ACT.SpecialSpellTimer.Config.Models;
using Prism.Commands;

namespace ACT.SpecialSpellTimer.Models
{
    [Serializable]
    public class ItemTags
    {
        public ItemTags()
        {
        }

        public ItemTags(
            Guid itemID,
            Guid tagID)
        {
            this.ItemID = itemID;
            this.TagID = tagID;
        }

        private Guid itemID;
        private Guid tagID;
        private ITreeItem item;
        private Tag tag;

        public Guid ItemID
        {
            get => this.itemID;
            set
            {
                this.itemID = value;
                this.item = null;
            }
        }

        public Guid TagID
        {
            get => this.tagID;
            set
            {
                this.tagID = value;
                this.tag = null;
            }
        }

        [XmlIgnore]
        public ItemTypes ItemType => this.Item?.ItemType ?? ItemTypes.Unknown;

        [XmlIgnore]
        public ITreeItem Item
        {
            get
            {
                if (this.item != null)
                {
                    return this.item;
                }

                this.item = SpellPanelTable.Instance.Table.FirstOrDefault(x => x.ID == this.ItemID);
                if (this.item != null)
                {
                    return this.item;
                }

                this.item = SpellTable.Instance.Table.FirstOrDefault(x => x.Guid == this.ItemID);
                if (this.item != null)
                {
                    return this.item;
                }

                this.item = TickerTable.Instance.Table.FirstOrDefault(x => x.Guid == this.ItemID);
                if (this.item != null)
                {
                    return this.item;
                }

                this.item = TagTable.Instance.Tags.FirstOrDefault(x => x.ID == this.ItemID);

                return this.item;
            }
        }

        [XmlIgnore]
        public Tag Tag
        {
            get
            {
                if (this.tag != null)
                {
                    return this.tag;
                }

                this.tag = TagTable.Instance.Tags.FirstOrDefault(x => x.ID == this.TagID);
                return this.tag;
            }
        }

        private ICommand removeTagCommand;

        [XmlIgnore]
        public ICommand RemoveTagCommand =>
            this.removeTagCommand ?? (this.removeTagCommand = new DelegateCommand<ItemTags>(itemTags =>
            {
                if (itemTags == null)
                {
                    return;
                }

                TagTable.Instance.ItemTags.Remove(itemTags);
            }));
    }
}
