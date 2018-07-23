using System.Windows.Controls;
using ACT.UltraScouter.resources;
using ACT.UltraScouter.Workers;
using FFXIV.Framework.Globalization;

namespace ACT.UltraScouter.Config.UI.Views
{
    /// <summary>
    /// GeneralView.xaml の相互作用ロジック
    /// </summary>
    public partial class GeneralConfigView : Page, ILocalizable
    {
        public GeneralConfigView()
        {
            this.InitializeComponent();

            this.SetLocale(Settings.Instance.UILocale);

            this.OpacitySlider.ValueChanged += (s, e) =>
                MainWorker.Instance.RefreshAllViewModels();
            this.OutlineSlider.ValueChanged += (s, e) =>
                MainWorker.Instance.RefreshAllViewModels();
            this.BlurSlider.ValueChanged += (s, e) =>
                MainWorker.Instance.RefreshAllViewModels();
        }

        public void SetLocale(Locales locale) => this.ReloadLocaleDictionary(locale);
    }
}
