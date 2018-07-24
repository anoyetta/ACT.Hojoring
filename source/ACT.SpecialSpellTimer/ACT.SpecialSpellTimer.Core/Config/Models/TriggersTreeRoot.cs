using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;

namespace ACT.SpecialSpellTimer.Config.Models
{
    public class TriggersTreeRoot :
        TreeItemBase
    {
        private ItemTypes itemType;
        private string displayText = string.Empty;
        private ICollectionView children;

        public TriggersTreeRoot(
            ItemTypes itemType,
            string displayText,
            ICollectionView children)
        {
            this.itemType = itemType;
            this.displayText = displayText;
            this.children = children;
        }

        [XmlIgnore]
        public override ItemTypes ItemType => this.itemType;

        public override int SortPriority { get; set; }

        public override string DisplayText => this.displayText;

        public override ICollectionView Children => this.children;

        public override bool IsExpanded
        {
            get
            {
                var entry = Settings.Default.ExpandedList.FirstOrDefault(x => x.Key == this.DisplayText);
                if (entry == null)
                {
                    entry = new ExpandedContainer()
                    {
                        Key = this.DisplayText,
                        IsExpanded = true,
                    };

                    Settings.Default.ExpandedList.Add(entry);
                }

                return entry.IsExpanded;
            }
            set
            {
                var entry = Settings.Default.ExpandedList.FirstOrDefault(x => x.Key == this.DisplayText);
                if (entry == null)
                {
                    entry = new ExpandedContainer()
                    {
                        Key = this.DisplayText,
                        IsExpanded = value,
                    };

                    Settings.Default.ExpandedList.Add(entry);
                }

                if (entry.IsExpanded != value)
                {
                    entry.IsExpanded = value;
                    this.RaisePropertyChanged();
                }
            }
        }

        public override bool Enabled
        {
            get => false;
            set { }
        }
    }
}
