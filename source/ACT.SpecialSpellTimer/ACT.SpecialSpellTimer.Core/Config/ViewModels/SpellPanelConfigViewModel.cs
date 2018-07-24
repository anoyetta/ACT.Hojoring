using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using ACT.SpecialSpellTimer.Config.Views;
using ACT.SpecialSpellTimer.Models;
using Prism.Commands;
using Prism.Mvvm;

namespace ACT.SpecialSpellTimer.Config.ViewModels
{
    public partial class SpellPanelConfigViewModel :
        BindableBase
    {
        public SpellPanelConfigViewModel() : this(new SpellPanel())
        {
        }

        public SpellPanelConfigViewModel(
            SpellPanel model)
        {
            this.Model = model;
        }

        private SpellPanel model;

        public SpellPanel Model
        {
            get => this.model;
            set
            {
                if (this.SetProperty(ref this.model, value))
                {
                    this.SetupTagsSource();
                    this.RaisePropertyChanged(nameof(this.IsPreset));
                }

                this.RefreshFirstSpell();
            }
        }

        public bool IsPreset => this.Model.ID == SpellPanel.GeneralPanel.ID;

        #region Tags

        private ICommand addTagsCommand;

        public ICommand AddTagsCommand =>
            this.addTagsCommand ?? (this.addTagsCommand = new DelegateCommand<Guid?>(targetItemID =>
            {
                if (!targetItemID.HasValue)
                {
                    return;
                }

                new TagView()
                {
                    TargetItemID = targetItemID.Value,
                }.Show();
            }));

        public ICollectionView Tags => this.TagsSource.View;

        private CollectionViewSource TagsSource;

        private void SetupTagsSource()
        {
            this.TagsSource = new CollectionViewSource()
            {
                Source = TagTable.Instance.ItemTags,
                IsLiveFilteringRequested = true,
                IsLiveSortingRequested = true,
            };

            this.TagsSource.Filter += (x, y) =>
                y.Accepted =
                    (y.Item as ItemTags).ItemID == this.Model.ID;

            this.TagsSource.SortDescriptions.AddRange(new[]
            {
                new SortDescription()
                {
                    PropertyName = "Tag.SortPriority",
                    Direction = ListSortDirection.Descending
                },
                new SortDescription()
                {
                    PropertyName = "Tag.Name",
                    Direction = ListSortDirection.Ascending
                },
            });

            this.RaisePropertyChanged(nameof(this.Tags));
        }

        #endregion Tags
    }
}
