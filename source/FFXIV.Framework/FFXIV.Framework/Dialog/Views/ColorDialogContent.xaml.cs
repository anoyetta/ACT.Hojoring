using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FFXIV.Framework.Dialog.Views
{
    public partial class ColorDialogContent : UserControl
    {
        public ColorDialogContent()
        {
            this.InitializeComponent();

            this.Color = Colors.White;

            this.Loaded += this.ColorDialogContent_Loaded;
            this.PredefinedColorsListBox.SelectionChanged += this.PredefinedColorsListBox_SelectionChanged;
            this.RTextBox.TextChanged += (s, e) => this.ToHex();
            this.GTextBox.TextChanged += (s, e) => this.ToHex();
            this.BTextBox.TextChanged += (s, e) => this.ToHex();
            this.ATextBox.TextChanged += (s, e) => this.ToHex();
            this.HexTextBox.LostFocus += (s, e) =>
            {
                var color = Colors.White;

                try
                {
                    color = (Color)ColorConverter.ConvertFromString(this.HexTextBox.Text);
                }
                catch
                {
                }

                this.RTextBox.Text = color.R.ToString();
                this.GTextBox.Text = color.G.ToString();
                this.BTextBox.Text = color.B.ToString();
                this.ATextBox.Text = !this.IgnoreAlpha ?
                    color.A.ToString() :
                    "255";

                this.ToPreview();
            };
        }

        public Color Color { get; set; }

        private bool ignoreAlpha;

        public bool IgnoreAlpha
        {
            get => this.ignoreAlpha;
            set
            {
                if (this.ignoreAlpha != value)
                {
                    this.ignoreAlpha = value;

                    if (this.ignoreAlpha)
                    {
                        this.ATextBox.Text = "255";
                        this.APanel.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        this.APanel.Visibility = Visibility.Visible;
                    }
                }
            }
        }

        public void Apply()
        {
            var color = Colors.White;

            try
            {
                color = (Color)ColorConverter.ConvertFromString(this.HexTextBox.Text);
            }
            catch
            {
            }

            this.Color = color;
        }

        private async void ColorDialogContent_Loaded(object sender, RoutedEventArgs e)
        {
            var item = await Task.Run(() => this.PredefinedColorsListBox.Items.Cast<PredefinedColor>().AsParallel()
                .FirstOrDefault(x => x.Color == this.Color));

            if (item != null)
            {
                this.PredefinedColorsListBox.SelectedItem = item;
                (this.PredefinedColorsListBox.ItemContainerGenerator.ContainerFromItem(item) as ListBoxItem)?.Focus();
            }

            if (this.PredefinedColorsListBox.SelectedItem == null &&
                this.Color != null)
            {
                this.RTextBox.Text = this.Color.R.ToString();
                this.GTextBox.Text = this.Color.G.ToString();
                this.BTextBox.Text = this.Color.B.ToString();
                this.ATextBox.Text = !this.IgnoreAlpha ?
                    this.Color.A.ToString() :
                    "255";
            }
        }

        private void PredefinedColorsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.PredefinedColorsListBox.SelectedItem != null)
            {
                var color = ((PredefinedColor)this.PredefinedColorsListBox.SelectedItem).Color;

                this.RTextBox.Text = color.R.ToString();
                this.GTextBox.Text = color.G.ToString();
                this.BTextBox.Text = color.B.ToString();
                this.ATextBox.Text = !this.IgnoreAlpha ?
                    color.A.ToString() :
                    "255";
            }
        }

        private void ToHex()
        {
            byte a, r, g, b;
            byte.TryParse(this.ATextBox.Text, out a);
            byte.TryParse(this.RTextBox.Text, out r);
            byte.TryParse(this.GTextBox.Text, out g);
            byte.TryParse(this.BTextBox.Text, out b);

            var color = Color.FromArgb(a, r, g, b);

            this.HexTextBox.Text = color.ToString();

            this.ToPreview();
        }

        private void ToPreview()
        {
            var color = Colors.White;

            try
            {
                color = (Color)ColorConverter.ConvertFromString(this.HexTextBox.Text);
            }
            catch
            {
            }

            this.PreviewRectangle.Fill = new SolidColorBrush(color);
        }
    }
}
