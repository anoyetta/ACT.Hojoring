using System;
using FFXIV.Framework.XIVHelper;
using Prism.Mvvm;

namespace ACT.SpecialSpellTimer.Config.Models
{
    public class PartyComposiotionSelector :
        BindableBase
    {
        private bool isSelected;

        public PartyComposiotionSelector(
            PartyCompositions composition,
            string text,
            bool isSelected = false,
            Action selectedChangedDelegate = null)
        {
            this.Composition = composition;
            this.Text = text;
            this.isSelected = isSelected;
            this.SelectedChangedDelegate = selectedChangedDelegate;
        }

        public PartyCompositions Composition { get; set; }

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

        public string Text { get; private set; }

        public override string ToString() => this.Text;
    }
}
