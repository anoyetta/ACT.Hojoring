using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ACT.SpecialSpellTimer.Config.ViewModels;
using ACT.SpecialSpellTimer.Models;
using ACT.SpecialSpellTimer.resources;
using FFXIV.Framework.Common;
using FFXIV.Framework.Globalization;
using FFXIV.Framework.WPF.Views;
using ICSharpCode.AvalonEdit;

namespace ACT.SpecialSpellTimer.Config.Views
{
    /// <summary>
    /// TickerConfigView.xaml の相互作用ロジック
    /// </summary>
    public partial class TickerConfigView : UserControl, ILocalizable
    {
        public TickerConfigView()
        {
            this.InitializeComponent();
            this.SetLocale(Settings.Default.UILocale);
            this.LoadConfigViewResources();
            this.InitXML();
        }

        public TickerConfigViewModel ViewModel => this.DataContext as TickerConfigViewModel;

        public void SetLocale(Locales locale) => this.ReloadLocaleDictionary(locale);

        private void TextBoxSelect(
            object sender,
            MouseButtonEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                if (!textBox.IsKeyboardFocusWithin)
                {
                    textBox.Focus();
                    e.Handled = true;
                }
            }
        }

        private void TextBoxOnGotFocus(
            object sender,
            RoutedEventArgs e)
        {
            (sender as TextBox)?.SelectAll();
        }

        private void TabControl_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (this.VisualTab != null)
            {
                this.ViewModel.IsActiveVisualTab = this.VisualTab.IsSelected;
            }
        }

        private void FilterExpander_Expanded(object sender, RoutedEventArgs e)
        {
            this.BaseScrollViewer.ScrollToEnd();
        }

        #region for XML Panel

        private static async void RefreshXML(
            TextEditor editor,
            ITrigger trigger)
        {
            if (editor == null ||
                trigger == null)
            {
                return;
            }

            editor.Text = await trigger.ToXMLAsync();
        }

        private void InitXML()
        {
            this.DataContextChanged += (x, y) =>
            {
                if (this.ViewModel != null)
                {
                    this.ViewModel.PropertyChanged += (s, e) =>
                    {
                        switch (e.PropertyName)
                        {
                            case "Model":
                                RefreshXML(this.XMLEditor, this.ViewModel.Model);
                                break;
                        }
                    };
                }
            };
        }

        private void XMLEditorOnLoaded(
            object sender,
            RoutedEventArgs e)
            => RefreshXML(sender as TextEditor, this.ViewModel?.Model);

        private async void XMLCopyOnClick(object sender, RoutedEventArgs e) => await WPFHelper.InvokeAsync(() =>
        {
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    Clipboard.SetDataObject(this.XMLEditor.Text);
                    CommonSounds.Instance.PlayAsterisk();
                    break;
                }
                catch (Exception ex) when (ex is InvalidOperationException || ex is COMException)
                {
                    Thread.Sleep(5);
                }
            }
        });

        private async void XMLPasteOnClick(object sender, RoutedEventArgs e)
        {
            var text = string.Empty;
            await WPFHelper.InvokeAsync(() =>
            {
                for (int i = 0; i < 10; i++)
                {
                    try
                    {
                        var obj = Clipboard.GetDataObject();
                        if (obj != null &&
                            obj.GetDataPresent(DataFormats.Text))
                        {
                            text = (string)obj.GetData(DataFormats.Text);
                        }

                        break;
                    }
                    catch (Exception ex) when (ex is InvalidOperationException || ex is COMException)
                    {
                        Thread.Sleep(5);
                    }
                }
            });

            if (!string.IsNullOrEmpty(text))
            {
                this.XMLEditor.Text = text;
                CommonSounds.Instance.PlayAsterisk();
            }
        }

        private async void XMLApplyOnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (this.ViewModel == null ||
                    this.ViewModel.Model == null)
                {
                    return;
                }

                var xml = this.XMLEditor.Text;
                if (string.IsNullOrEmpty(xml))
                {
                    return;
                }

                var obj = await this.ViewModel.Model.FromXMLAsync(xml);
                if (obj == null)
                {
                    return;
                }

                this.ViewModel.Model.ImportProperties(obj);
            }
            catch (Exception ex)
            {
                ModernMessageBox.ShowDialog(
                    "Error on applying XML to this settings.",
                    "XML Error",
                    MessageBoxButton.OK,
                    ex);
            }
        }

        #endregion for XML Panel
    }
}
