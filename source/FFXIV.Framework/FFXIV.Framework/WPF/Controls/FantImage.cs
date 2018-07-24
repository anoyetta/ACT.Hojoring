using System.Windows.Controls;
using System.Windows.Media;

namespace FFXIV.Framework.WPF.Controls
{
    public class FantImage : Image
    {
        protected override void OnRender(
            DrawingContext context)
        {
            this.VisualBitmapScalingMode = BitmapScalingMode.Fant;
            base.OnRender(context);
        }
    }
}
