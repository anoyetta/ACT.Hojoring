using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FFXIV.Framework.Common;

namespace FFXIV.Framework.Dialog.Views
{
    public partial class FontDialogContent : UserControl
    {
        private FontInfo fontInfo = new FontInfo();

        public FontDialogContent()
        {
            this.InitializeComponent();

            this.Loaded += (s, e) =>
            {
                this.ShowFontInfo();

                // リストボックスにフォーカスを設定する
                ListBox box;

                box = this.FontStyleListBox;
                if (box.SelectedItem != null)
                {
                    var item =
                        box.ItemContainerGenerator.ContainerFromItem(box.SelectedItem)
                        as ListBoxItem;

                    if (item != null)
                    {
                        item.Focus();
                    }
                }

                box = this.FontFamilyListBox;
                if (box.SelectedItem != null)
                {
                    var item =
                        box.ItemContainerGenerator.ContainerFromItem(box.SelectedItem)
                        as ListBoxItem;

                    if (item != null)
                    {
                        item.Focus();
                    }
                }
            };

            this.FontSizeTextBox.PreviewKeyDown += this.FontSizeTextBox_PreviewKeyDown;
            this.FontSizeTextBox.LostFocus += (s, e) =>
            {
                const double MinSize = 5.0;

                var t = (s as TextBox).Text;

                var ci = CultureInfo.InvariantCulture;
                if (double.TryParse(t, NumberStyles.Any, ci.NumberFormat, out double d))
                {
                    if (d < MinSize)
                    {
                        d = MinSize;
                    }

                    (s as TextBox).Text = d.ToString("N1", ci);
                }
                else
                {
                    (s as TextBox).Text = MinSize.ToString("N0", ci);
                }
            };

            this.FontFamilyListBox.SelectionChanged += this.FontFamilyListBox_SelectionChanged;
        }

        public FontInfo FontInfo
        {
            get
            {
                return this.fontInfo;
            }
            set
            {
                this.fontInfo = value;
                this.ShowFontInfo();
            }
        }

        internal void OKBUtton_Click(object sender, RoutedEventArgs e)
        {
            this.fontInfo = new FontInfo(
                this.PreviewTextBlock.FontFamily,
                this.PreviewTextBlock.FontSize,
                this.PreviewTextBlock.FontStyle,
                this.PreviewTextBlock.FontWeight,
                this.PreviewTextBlock.FontStretch);
        }

        private void FontFamilyListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.FontStyleListBox.SelectedIndex = 0;
        }

        private void FontSizeTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var t = sender as TextBox;

            double d;
            var ci = CultureInfo.InvariantCulture;

            if (e.Key == Key.Up)
            {
                if (double.TryParse(t.Text, NumberStyles.Any, ci.NumberFormat, out d))
                {
                    t.Text = (d + 0.1d).ToString("N1", ci);
                }
            }

            if (e.Key == Key.Down)
            {
                if (double.TryParse(t.Text, NumberStyles.Any, ci.NumberFormat, out d))
                {
                    if ((d - 0.1d) >= 1.0d)
                    {
                        t.Text = (d - 0.1d).ToString("N1", ci);
                    }
                }
            }
        }

        private void ShowFontInfo()
        {
            this.FontSizeTextBox.Text = this.fontInfo.Size.ToString("N1", CultureInfo.InvariantCulture);

            int i = 0;
            foreach (FontFamily item in this.FontFamilyListBox.Items)
            {
                if (this.fontInfo.FontFamily != null)
                {
                    if (item.Source == this.fontInfo.FontFamily.Source ||
                        item.FamilyNames.Any(x => x.Value == this.fontInfo.FontFamily.Source))
                    {
                        break;
                    }
                }

                i++;
            }

            if (i < this.FontFamilyListBox.Items.Count)
            {
                this.FontFamilyListBox.SelectedIndex = i;
                this.FontFamilyListBox.ScrollIntoView(this.FontFamilyListBox.Items[i]);
            }

            this.FontStyleListBox.SelectedItem = this.fontInfo.Typeface;
        }
    }
}
