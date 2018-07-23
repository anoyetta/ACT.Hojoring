using System.Windows.Media;
using FFXIV.Framework.Dialog.Views;
using FFXIV.Framework.Extensions;

namespace FFXIV.Framework.Dialog
{
    public class ColorDialogWrapper
    {
        public static ColorDialogResult ShowDialog(
            System.Drawing.Color? color = null,
            bool ignoreAlpha = false)
        {
            var wpfColor = color?.ToWPF();
            return ColorDialogWrapper.ShowDialog(wpfColor, ignoreAlpha);
        }

        public static ColorDialogResult ShowDialog(
            Color? color = null,
            bool ignoreAlpha = false)
        {
            var result = new ColorDialogResult()
            {
                Color = color.HasValue ? color.Value : Colors.Transparent,
                IgnoreAlpha = ignoreAlpha,
            };

            ColorDialog.Color = result.Color;
            ColorDialog.IgnoreAlpha = result.IgnoreAlpha;
            if (ColorDialog.ShowDialog() ?? false)
            {
                result.Color = ColorDialog.Color;
                result.Result = true;
            }

            return result;
        }
    }
}
