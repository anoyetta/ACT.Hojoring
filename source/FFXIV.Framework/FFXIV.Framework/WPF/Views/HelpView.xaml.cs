using System.Windows;
using System.Windows.Controls;
using FFXIV.Framework.Common;
using FFXIV.Framework.Globalization;
using FFXIV.Framework.WPF.ViewModels;

namespace FFXIV.Framework.WPF.Views
{
    /// <summary>
    /// HelpView.xaml の相互作用ロジック
    /// </summary>
    public partial class HelpView :
        UserControl
    {
        public HelpView()
        {
            this.InitializeComponent();
            this.DataContext = new HelpViewModel()
            {
                View = this
            };

            this.Loaded += (x, y) => this.LogTextBox.ScrollToEnd();
        }

        public HelpViewModel ViewModel => this.DataContext as HelpViewModel;

        public void SetLocale(
            Locales locale) =>
            this.Resources.MergedDictionaries.Add(new ResourceDictionary()
            {
                Source = locale.GetUri("Strings.Help.{0}.xaml")
            });

        private void LogTextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null)
            {
                WPFHelper.BeginInvoke(() =>
                {
                    textBox.CaretIndex = textBox.Text.Length;
                    textBox.ScrollToEnd();
                });
            }
        }
    }
}
