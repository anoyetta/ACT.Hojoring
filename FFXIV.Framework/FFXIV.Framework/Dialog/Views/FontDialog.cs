using System.Windows;
using System.Windows.Interop;
using FFXIV.Framework.Common;

namespace FFXIV.Framework.Dialog.Views
{
    public static class FontDialog
    {
        private static FontDialogContent CreateContent() => new FontDialogContent()
        {
            FontInfo = FontDialog.Font,
        };

        private static Dialog CreateDialog(FontDialogContent content) => new Dialog()
        {
            Title = "Font",
            Content = content,
            MaxWidth = 1100,
            MaxHeight = 620,
        };

        public static FontInfo Font { get; set; }

        public static bool? ShowDialog()
        {
            var content = CreateContent();
            var dialog = CreateDialog(content);

            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            dialog.OkButton.Click += content.OKBUtton_Click;

            var result = dialog.ShowDialog();
            if (result.Value)
            {
                Font = content.FontInfo;
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

            dialog.OkButton.Click += content.OKBUtton_Click;

            var result = dialog.ShowDialog();
            if (result.Value)
            {
                Font = content.FontInfo;
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

            dialog.OkButton.Click += content.OKBUtton_Click;

            var result = dialog.ShowDialog();
            if (result.Value)
            {
                Font = content.FontInfo;
            }

            return result;
        }
    }
}
