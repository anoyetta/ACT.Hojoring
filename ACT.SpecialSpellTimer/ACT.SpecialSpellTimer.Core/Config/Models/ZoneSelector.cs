using System;
using Prism.Mvvm;

namespace ACT.SpecialSpellTimer.Config.Models
{
    public class ZoneSelector :
        BindableBase
    {
        private bool isSelected;

        public ZoneSelector(
            string id,
            string name,
            bool isSelected = false,
            Action selectedChangedDelegate = null)
        {
            this.ID = id;
            this.Name = name;
            this.isSelected = isSelected;
            this.SelectedChangedDelegate = selectedChangedDelegate;
        }

        public string ID { get; set; }
        public string Name { get; set; }

        public bool IsSelected
        {
            get => this.isSelected;
            set
            {
                if (this.SetProperty(ref this.isSelected, value))
                {
                    this.SelectedChangedDelegate?.Invoke();
                }
            }
        }

        public Action SelectedChangedDelegate { get; set; }

        public override string ToString() => this.Name;
    }
}
