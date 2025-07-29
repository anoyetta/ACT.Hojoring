using System.Collections.ObjectModel;
using System.Windows;
using Prism.Mvvm;

namespace FFXIV.Framework.Common
{
    public class TreeSource :
        BindableBase
    {
        private string text = string.Empty;
        private bool isSelected = false;
        private bool isExpanded = false;
        private TreeSource parent = null;
        private ObservableCollection<TreeSource> child = new ObservableCollection<TreeSource>();
        private FrameworkElement content = null;
        private object tag = null;

        public TreeSource()
        {
        }

        public TreeSource(
            string text,
            TreeSource parent = null)
        {
            this.text = text;
            this.parent = parent;
        }

        public string Text
        {
            get => this.text;
            set => this.SetProperty(ref this.text, value);
        }

        public bool IsSelected
        {
            get => this.isSelected;
            set => this.SetProperty(ref this.isSelected, value);
        }

        public bool IsExpanded
        {
            get => this.isExpanded;
            set => this.SetProperty(ref this.isExpanded, value);
        }

        public TreeSource Parent
        {
            get => this.parent;
            set => this.SetProperty(ref this.parent, value);
        }

        public ObservableCollection<TreeSource> Child
        {
            get => this.child;
            set => this.SetProperty(ref this.child, value);
        }

        public FrameworkElement Content
        {
            get => this.content;
            set => this.SetProperty(ref this.content, value);
        }

        public object Tag
        {
            get => this.tag;
            set => this.SetProperty(ref this.tag, value);
        }

        public override string ToString() => this.Text;

        public TreeSource Clone()
        {
            var clone = (TreeSource)this.MemberwiseClone();

            clone.parent = null;
            clone.child = new ObservableCollection<TreeSource>();
            clone.content = null;
            clone.tag = null;

            return clone;
        }
    }
}
