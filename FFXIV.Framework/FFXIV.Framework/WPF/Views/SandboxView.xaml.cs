using System.Windows;

namespace FFXIV.Framework.WPF.Views
{
    /// <summary>
    /// SandboxView.xaml の相互作用ロジック
    /// </summary>
    public partial class SandboxView : Window
    {
        public SandboxView()
        {
            this.InitializeComponent();

            this.CloseButton.Click += (sender, args) =>
                this.Close();

            this.MouseLeftButtonDown += (sender, args) =>
                this.DragMove();
        }
    }
}
