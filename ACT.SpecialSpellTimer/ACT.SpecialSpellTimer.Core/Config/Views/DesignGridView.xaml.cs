using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using FFXIV.Framework.WPF.Views;

namespace ACT.SpecialSpellTimer.Config.Views
{
    /// <summary>
    /// DesignGirdView.xaml の相互作用ロジック
    /// </summary>
    public partial class DesignGridView :
        Window,
        IOverlay
    {
        private const double DefaultOpacity = 0.01;

        public DesignGridView()
        {
            this.InitializeComponent();
            this.ToNonActive();
            this.Opacity = 0;

            for (int r = 0; r < this.BaseGrid.RowDefinitions.Count; r++)
            {
                for (int c = 0; c < this.BaseGrid.ColumnDefinitions.Count; c++)
                {
                    var rect = new Rectangle()
                    {
                        Stroke = Brushes.WhiteSmoke,
                        StrokeThickness = 0.2,
                    };

                    Grid.SetRow(rect, r);
                    Grid.SetColumn(rect, c);
                    this.BaseGrid.Children.Insert(0, rect);
                }
            }
        }

        /// <summary>オーバーレイとして表示状態</summary>
        private bool overlayVisible;

        /// <summary>
        /// オーバーレイとして表示状態
        /// </summary>
        public bool OverlayVisible
        {
            get => this.overlayVisible;
            set => this.SetOverlayVisible(ref this.overlayVisible, value, DefaultOpacity);
        }
    }
}
