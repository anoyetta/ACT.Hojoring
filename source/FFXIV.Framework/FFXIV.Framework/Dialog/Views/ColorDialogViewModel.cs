using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Media;
using FFXIV.Framework.Common;

namespace FFXIV.Framework.Dialog.Views
{
    public class ColorDialogViewModel : INotifyPropertyChanged
    {
        private static PredefinedColor[] predefinedColors;

        public PredefinedColor[] PredefinedColors
        {
            get { return predefinedColors ?? (predefinedColors = this.EnumlatePredefinedColors()); }
            set
            {
                predefinedColors = value;
                this.RaisePropertyChanged();
            }
        }

        private PredefinedColor[] EnumlatePredefinedColors()
        {
            var list = new List<PredefinedColor>();

            var t1 = Task.Run(() =>
            {
                var solidColors = new List<PredefinedColor>();
                foreach (var color in typeof(Colors).GetProperties())
                {
                    try
                    {
                        solidColors.Add(new PredefinedColor()
                        {
                            Name = color.Name,
                            Color = (Color)ColorConverter.ConvertFromString(color.Name)
                        });
                    }
                    catch
                    {
                    }
                }

                return solidColors.OrderBy(x => x.Color.ToString());
            });

            var t2 = Task.Run(() =>
            {
                var waColors = new List<PredefinedColor>();
                foreach (var color in typeof(WaColors).GetProperties())
                {
                    try
                    {
                        waColors.Add(new PredefinedColor()
                        {
                            Name = color.Name,
                            Color = (Color)color.GetValue(null, null),
                        });
                    }
                    catch
                    {
                    }
                }

                return waColors.OrderBy(x => x.Color.ToString());
            });

            list.AddRange(t1.Result);
            list.AddRange(t2.Result);

            return list.ToArray();
        }

        #region Implementation of INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged([CallerMemberName] string caller = "")
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(caller));
            }
        }

        #endregion Implementation of INotifyPropertyChanged
    }

    public class PredefinedColor
    {
        public SolidColorBrush Brush => new SolidColorBrush(this.Color);
        public Color Color { get; set; }
        public string Name { get; set; }
    }
}
