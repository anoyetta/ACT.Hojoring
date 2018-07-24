using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace FFXIV.Framework.Dialog.Views
{
    public static class ColorDialog
    {
        private static ColorDialogContent CreateContent() => new ColorDialogContent()
        {
            Color = Color,
            IgnoreAlpha = IgnoreAlpha,
        };

        private static Dialog CreateDialog(ColorDialogContent content) => new Dialog()
        {
            Title = "Color",
            Content = content,
            MaxWidth = 1390,
            MaxHeight = 700,
        };

        public static Color Color { get; set; }

        public static bool IgnoreAlpha { get; set; }

        public static bool? ShowDialog()
        {
            var content = CreateContent();
            var dialog = CreateDialog(content);

            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            dialog.OkButton.Click += (x, y) => content.Apply();

            var result = dialog.ShowDialog();
            if (result.Value)
            {
                Color = content.Color;
            }

            return result;
        }

        public static bool? ShowDialog(
            Window owner)
        {
            var content = CreateContent();
            var dialog = CreateDialog(content);

            dialog.WindowStartupLocation = owner != null ?
                WindowStartupLocation.CenterOwner :
                WindowStartupLocation.CenterScreen;

            dialog.OkButton.Click += (x, y) => content.Apply();

            var result = dialog.ShowDialog();
            if (result.Value)
            {
                Color = content.Color;
            }

            return result;
        }

        public static bool? ShowDialog(
            System.Windows.Forms.Form owner)
        {
            var content = CreateContent();
            var dialog = CreateDialog(content);

            dialog.WindowStartupLocation = owner != null ?
                WindowStartupLocation.CenterOwner :
                WindowStartupLocation.CenterScreen;

            if (owner != null)
            {
                var helper = new WindowInteropHelper(dialog);
                helper.Owner = owner.Handle;
            }

            dialog.OkButton.Click += (x, y) => content.Apply();

            var result = dialog.ShowDialog();
            if (result.Value)
            {
                Color = content.Color;
            }

            return result;
        }
    }
}
