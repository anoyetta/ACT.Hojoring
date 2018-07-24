using System.Windows;
using ACT.UltraScouter.ViewModels.Bases;

namespace ACT.UltraScouter.Workers
{
    public class ViewAndViewModel
    {
        public ViewAndViewModel(
            Window view,
            OverlayViewModelBase viewModel)
        {
            this.View = view;
            this.ViewModel = viewModel;
        }

        public Window View { get; set; }
        public OverlayViewModelBase ViewModel { get; set; }
    }
}
