using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ACT.SpecialSpellTimer.resources;
using FFXIV.Framework.Dialog;
using FFXIV.Framework.FFXIVHelper;
using FFXIV.Framework.Globalization;
using Prism.Commands;

namespace ACT.SpecialSpellTimer.Config.Views
{
    /// <summary>
    /// OptionsVisualView.xaml の相互作用ロジック
    /// </summary>
    public partial class OptionsVisualView :
        UserControl,
        ILocalizable
    {
        public OptionsVisualView()
        {
            this.InitializeComponent();
            this.SetLocale(Settings.Default.UILocale);
            this.LoadConfigViewResources();

            this.nameStyleRadioButtons = new[]
            {
                this.NameStyle1RadioButton,
                this.NameStyle2RadioButton,
                this.NameStyle3RadioButton,
                this.NameStyle4RadioButton,
            };

            this.Loaded += (x, y) =>
            {
                var target = this.nameStyleRadioButtons.FirstOrDefault(z =>
                    Convert.ToInt32(z.Tag) == (int)this.Config.PCNameInitialOnDisplayStyle);
                if (target != null)
                {
                    target.IsChecked = true;
                }
            };

            void setNameStyle()
            {
                var source = this.nameStyleRadioButtons.FirstOrDefault(z => z.IsChecked ?? false);
                if (source != null)
                {
                    this.Config.PCNameInitialOnDisplayStyle = (NameStyles)Convert.ToInt32(source.Tag);
                }
            }

            foreach (var button in this.nameStyleRadioButtons)
            {
                button.Checked += (x, y) => setNameStyle();
            }
        }

        public void SetLocale(Locales locale) => this.ReloadLocaleDictionary(locale);

        public Settings Config => Settings.Default;

        private RadioButton[] nameStyleRadioButtons;

        private ICommand CreateChangeColorCommand(
            Func<Color> getCurrentColor,
            Action<Color> changeColorAction)
            => new DelegateCommand(() =>
            {
                var result = ColorDialogWrapper.ShowDialog(getCurrentColor(), true);
                if (result.Result)
                {
                    changeColorAction.Invoke(result.Color);
                }
            });

        private ICommand changeProgressBarBackgroundColorCommand;

        public ICommand ChangeProgressBarBackgroundColorCommand =>
            this.changeProgressBarBackgroundColorCommand ?? (this.changeProgressBarBackgroundColorCommand = this.CreateChangeColorCommand(
                () => this.Config.BarDefaultBackgroundColor,
                (color) => this.Config.BarDefaultBackgroundColor = color));
    }
}
