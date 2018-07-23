using FFXIV.Framework.Common;
using FFXIV.Framework.Dialog.Views;

namespace FFXIV.Framework.Dialog
{
    public class FontDialogWrapper
    {
        public static FontDialogResult ShowDialog(
            FontInfo font = null)
        {
            var result = new FontDialogResult()
            {
                Font = font,
            };

            FontDialog.Font = result.Font;
            if (FontDialog.ShowDialog() ?? false)
            {
                result.Font = FontDialog.Font;
                result.Result = true;
            }

            return result;
        }
    }
}
