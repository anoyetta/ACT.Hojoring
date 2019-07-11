using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Prism.Mvvm;

namespace ACT.UltraScouter.Config.UI.ViewModels
{
    public class MyHPConfigViewModel :
        BindableBase
    {
        public MyHPConfigViewModel()
        {
            this.Config.PropertyChanged += (_, e) =>
            {
                switch (e.PropertyName)
                {
                    case nameof(this.Config.BarStyle):
                        this.RaisePropertyChanged(nameof(this.BarSize1));
                        this.RaisePropertyChanged(nameof(this.BarSize2));
                        break;
                }
            };
        }

        public virtual MyStatus Config => Settings.Instance.MyHP;

        public IEnumerable<StatusStyles> Styles => Enum.GetValues(typeof(StatusStyles))
            .Cast<StatusStyles>();

        public string BarSize1 => this.Config.BarStyle switch
        {
            StatusStyles.Horizontal => "W",
            StatusStyles.Vertical => "H",
            StatusStyles.Circle => "D",
            _ => string.Empty,
        };

        public string BarSize2 => this.Config.BarStyle switch
        {
            StatusStyles.Horizontal => "H",
            StatusStyles.Vertical => "W",
            StatusStyles.Circle => "T",
            _ => string.Empty,
        };

        public IEnumerable<HorizontalAlignment> HorizontalAlignments => Enum.GetValues(typeof(HorizontalAlignment))
            .Cast<HorizontalAlignment>();
    }
}
