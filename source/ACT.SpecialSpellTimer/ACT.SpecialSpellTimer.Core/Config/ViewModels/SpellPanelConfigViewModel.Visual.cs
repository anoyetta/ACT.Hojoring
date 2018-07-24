using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ACT.SpecialSpellTimer.Config.Views;
using ACT.SpecialSpellTimer.Models;
using FFXIV.Framework.Common;
using FFXIV.Framework.Dialog;
using FFXIV.Framework.Extensions;
using Prism.Commands;

namespace ACT.SpecialSpellTimer.Config.ViewModels
{
    public partial class SpellPanelConfigViewModel
    {
        public Spell FirstSpell { get; private set; } = null;

        public void ClearFirstSpellChanged()
        {
            foreach (var spell in this.Model.Spells)
            {
                spell.PropertyChanged -= this.FirstSpell_PropertyChanged;
            }
        }

        private void RefreshFirstSpell()
        {
            if (this.FirstSpell != null)
            {
                this.FirstSpell.PropertyChanged -= this.FirstSpell_PropertyChanged;
            }

            this.FirstSpell = this.Spells?.FirstOrDefault();
            if (this.FirstSpell == null)
            {
                this.FirstSpell = Spell.CreateNew();
            }

            this.FirstSpell.PropertyChanged += this.FirstSpell_PropertyChanged;

            this.RaisePropertyChanged(nameof(this.FirstSpell));
            this.RaisePropertyChanged(nameof(this.FontName));
        }

        private void FirstSpell_PropertyChanged(
            object sender,
            PropertyChangedEventArgs e)
        {
            var targets = new[]
            {
                nameof(Spell.WarningTime),
                nameof(Spell.ChangeFontColorsWhenWarning),
                nameof(Spell.IsReverse),
                nameof(Spell.BarWidth),
                nameof(Spell.BarHeight),
                nameof(Spell.SpellIconSize),
                nameof(Spell.HideSpellName),
                nameof(Spell.OverlapRecastTime),
                nameof(Spell.ReduceIconBrightness),
                nameof(Spell.ProgressBarVisible),
                nameof(Spell.HideCounter),
                nameof(Spell.DontHide),
            };

            if (!targets.Contains(e.PropertyName))
            {
                return;
            }

            var pi = typeof(Spell).GetProperty(e.PropertyName);
            var value = pi.GetValue(this.FirstSpell);

            foreach (var spell in this.Spells)
            {
                pi.SetValue(spell, value);
            }
        }

        public IEnumerable<Spell> Spells => this.Model?.Children?.Cast<Spell>();

        public string FontName => this.FirstSpell.Font.DisplayText;

        #region Change Font

        private ICommand CreateChangeFontCommand(
            Func<FontInfo> getCurrentoFont,
            Action<FontInfo> changeFont)
            => new DelegateCommand(() =>
            {
                var result = FontDialogWrapper.ShowDialog(getCurrentoFont());
                if (result.Result)
                {
                    changeFont.Invoke(result.Font);
                }
            });

        private ICommand changeFontCommand;

        public ICommand ChangeFontCommand =>
            this.changeFontCommand ?? (this.changeFontCommand = this.CreateChangeFontCommand(
                () => this.FirstSpell.Font,
                (font) =>
                {
                    foreach (var spell in this.Spells)
                    {
                        spell.Font.FontFamily = font.FontFamily;
                        spell.Font.Size = font.Size;
                        spell.Font.Style = font.Style;
                        spell.Font.Weight = font.Weight;
                        spell.Font.Stretch = font.Stretch;
                    }

                    this.RaisePropertyChanged(nameof(this.FontName));
                }));

        #endregion Change Font

        #region Change Colors

        private ICommand CreateChangeColorCommand(
            Func<string> getCurrentColor,
            Action<string> changeColorAction)
            => new DelegateCommand(() =>
            {
                var result = ColorDialogWrapper.ShowDialog(getCurrentColor().FromHTML(), true);
                if (result.Result)
                {
                    changeColorAction.Invoke(result.LegacyColor.ToHTML());
                }
            });

        private ICommand CreateChangeAlphaColorCommand(
            Func<string> getCurrentColor,
            Func<int> getCurrentAlpha,
            Action<string, int> changeColorAction)
            => new DelegateCommand(() =>
            {
                var baseColor = getCurrentColor().FromHTML();
                var color = Color.FromArgb(
                    (byte)getCurrentAlpha(),
                    baseColor.R,
                    baseColor.G,
                    baseColor.B);

                var result = ColorDialogWrapper.ShowDialog(color, false);
                if (result.Result)
                {
                    changeColorAction.Invoke(
                        result.LegacyColor.ToHTML(),
                        (int)result.Color.A);
                }
            });

        private ICommand CreateChangeColorWPFCommand(
            Func<Color> getCurrentColor,
            Action<Color> changeColorAction,
            bool ignoreAlpha = true)
            => new DelegateCommand(() =>
            {
                var result = ColorDialogWrapper.ShowDialog(getCurrentColor(), ignoreAlpha);
                if (result.Result)
                {
                    changeColorAction.Invoke(result.Color);
                }
            });

        private ICommand changeFontColorCommand;

        public ICommand ChangeFontColorCommand =>
            this.changeFontColorCommand ?? (this.changeFontColorCommand = this.CreateChangeColorCommand(
                () => this.FirstSpell.FontColor,
                (color) =>
                {
                    foreach (var spell in this.Spells)
                    {
                        spell.FontColor = color;
                    }
                }));

        private ICommand changeFontOutlineColorCommand;

        public ICommand ChangeFontOutlineColorCommand =>
            this.changeFontOutlineColorCommand ?? (this.changeFontOutlineColorCommand = this.CreateChangeColorCommand(
                () => this.FirstSpell.FontOutlineColor,
                (color) =>
                {
                    foreach (var spell in this.Spells)
                    {
                        spell.FontOutlineColor = color;
                    }
                }));

        private ICommand changeWarningFontColorCommand;

        public ICommand ChangeWarningFontColorCommand =>
            this.changeWarningFontColorCommand ?? (this.changeWarningFontColorCommand = this.CreateChangeColorCommand(
                () => this.FirstSpell.WarningFontColor,
                (color) =>
                {
                    foreach (var spell in this.Spells)
                    {
                        spell.WarningFontColor = color;
                    }
                }));

        private ICommand changeWarningFontOutlineColorCommand;

        public ICommand ChangeWarningFontOutlineColorCommand =>
            this.changeWarningFontOutlineColorCommand ?? (this.changeWarningFontOutlineColorCommand = this.CreateChangeColorCommand(
                () => this.FirstSpell.WarningFontOutlineColor,
                (color) =>
                {
                    foreach (var spell in this.Spells)
                    {
                        spell.WarningFontOutlineColor = color;
                    }
                }));

        private ICommand changeBarColorCommand;

        public ICommand ChangeBarColorCommand =>
            this.changeBarColorCommand ?? (this.changeBarColorCommand = this.CreateChangeColorCommand(
                () => this.FirstSpell.BarColor,
                (color) =>
                {
                    foreach (var spell in this.Spells)
                    {
                        spell.BarColor = color;
                    }
                }));

        private ICommand changeBarOutlineColorCommand;

        public ICommand ChangeBarOutlineColorCommand =>
            this.changeBarOutlineColorCommand ?? (this.changeBarOutlineColorCommand = this.CreateChangeColorCommand(
                () => this.FirstSpell.BarOutlineColor,
                (color) =>
                {
                    foreach (var spell in this.Spells)
                    {
                        spell.BarOutlineColor = color;
                    }
                }));

        private ICommand changeBackgroundColorCommand;

        public ICommand ChangeBackgroundColorCommand =>
            this.changeBackgroundColorCommand ?? (this.changeBackgroundColorCommand = this.CreateChangeAlphaColorCommand(
                () => this.FirstSpell.BackgroundColor,
                () => this.FirstSpell.BackgroundAlpha,
                (color, alpha) =>
                {
                    foreach (var spell in this.Spells)
                    {
                        spell.BackgroundColor = color;
                        spell.BackgroundAlpha = alpha;
                    }
                }));

        private ICommand changeAdvancedBackgroundColorCommand;

        public ICommand ChangeAdvancedBackgroundColorCommand =>
            this.changeAdvancedBackgroundColorCommand ?? (this.changeAdvancedBackgroundColorCommand = this.CreateChangeColorWPFCommand(
                () => this.Model.BackgroundColor,
                (color) =>
                {
                    this.Model.BackgroundColor = color;
                },
                false));

        #endregion Change Colors

        #region Select Icon

        private ICommand selectIconCommand;

        public ICommand SelectIconCommand =>
            this.selectIconCommand ?? (this.selectIconCommand = new DelegateCommand(() =>
            {
                var view = new IconBrowserView();

                view.SelectedIconName = this.FirstSpell?.SpellIcon;

                if (view.ShowDialog() ?? false)
                {
                    foreach (var spell in this.Spells)
                    {
                        spell.SpellIcon = view.SelectedIconName;
                    }
                }
            }));

        #endregion Select Icon

        private void OnValueChanged(
            object sender,
            RoutedPropertyChangedEventArgs<object> e)
        {
        }
    }
}
