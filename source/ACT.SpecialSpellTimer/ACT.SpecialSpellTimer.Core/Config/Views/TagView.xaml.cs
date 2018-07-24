using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ACT.SpecialSpellTimer.Models;
using ACT.SpecialSpellTimer.resources;
using FFXIV.Framework.Globalization;

namespace ACT.SpecialSpellTimer.Config.Views
{
    /// <summary>
    /// TagView.xaml の相互作用ロジック
    /// </summary>
    public partial class TagView : Window, ILocalizable
    {
        public TagView()
        {
            this.InitializeComponent();
            this.SetLocale(Settings.Default.UILocale);
            this.LoadConfigViewResources();

            // ウィンドウのスタート位置を決める
            this.WindowStartupLocation = WindowStartupLocation.Manual;

            // マウスの座標を取得する
            var swp = new Point(
                System.Windows.Forms.Cursor.Position.X,
                System.Windows.Forms.Cursor.Position.Y);

            this.Left = swp.X;
            this.Top = swp.Y;

            this.MouseLeftButtonDown += (x, y) => this.DragMove();

            this.Loaded += this.TagViewOnLoaded;
            this.CloseButton.Click += (x, y) => this.Close();
            this.ApplyButton.Click += ApplyButtonOnClick;

            this.PreviewKeyUp += (x, y) =>
            {
                if (y.Key == Key.Escape)
                {
                    this.Close();
                }
            };
        }

        public void SetLocale(Locales locale) => this.ReloadLocaleDictionary(locale);

        public Guid TargetItemID { get; set; } = Guid.Empty;

        public ObservableCollection<TagManageContainer> Tags { get; set; } = new ObservableCollection<TagManageContainer>();

        private void TagViewOnLoaded(
            object sender,
            RoutedEventArgs e)
            => this.LoadTags();

        private void LoadTags()
        {
            var q =
                from x in TagTable.Instance.Tags
                orderby
                x.SortPriority ascending,
                x.Name ascending
                select
                x;

            this.Tags.Clear();
            foreach (var tag in q)
            {
                var container = new TagManageContainer()
                {
                    Tag = tag,
                    IsSelected = TagTable.Instance.ItemTags.Any(x =>
                        x.ItemID == this.TargetItemID &&
                        x.TagID == tag.ID),
                };

                this.Tags.Add(container);
            }
        }

        private void TagsOnChanged(
            object sender,
            RoutedEventArgs e)
        {
            this.ApplyButton.Visibility = Visibility.Visible;
        }

        private void ApplyButtonOnClick(
            object sender,
            RoutedEventArgs e)
        {
            // 新しい関連を追加する
            foreach (var entity in this.Tags.Where(x => x.IsSelected))
            {
                if (!TagTable.Instance.ItemTags.Any(x =>
                    x.ItemID == this.TargetItemID &&
                    x.TagID == entity.Tag.ID))
                {
                    TagTable.Instance.ItemTags.Add(new ItemTags()
                    {
                        ItemID = this.TargetItemID,
                        TagID = entity.Tag.ID
                    });
                }
            }

            // 解除された関係を削除する
            var entitis = TagTable.Instance.ItemTags.Where(x => x.ItemID == this.TargetItemID).ToArray();
            foreach (var entity in entitis)
            {
                if (!this.Tags
                    .Where(x => x.IsSelected)
                    .Any(x => x.Tag.ID == entity.TagID))
                {
                    TagTable.Instance.ItemTags.Remove(entity);
                }
            }

            TagTable.Instance.Save();

            this.Close();
        }

        public class TagManageContainer
        {
            public Tag Tag { get; set; }

            public bool IsSelected { get; set; }

            public string Text => this.Tag?.Name;

            public override string ToString() => this.Text;
        }
    }
}
