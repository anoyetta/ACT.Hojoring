using System;
using System.Collections.Generic;
using System.Diagnostics;
using FFXIV.Framework.XIVHelper;
using Prism.Mvvm;

namespace ACT.SpecialSpellTimer.Config
{
    public partial class Settings :
        BindableBase
    {
        public static readonly Dictionary<string, object> DefaultValues = new Dictionary<string, object>()
        {
            { nameof(Settings.NotifyNormalSpellTimerPrefix), "spespe_" },
            { nameof(Settings.ReadyText), "Ready" },
            { nameof(Settings.OverText), "Over" },
            { nameof(Settings.TimeOfHideSpell), 1.0d },
            { nameof(Settings.LogPollSleepInterval), 30 },
            { nameof(Settings.RefreshInterval), 60 },
            { nameof(Settings.ReduceIconBrightness), 55 },
            { nameof(Settings.Opacity), 10 },
            { nameof(Settings.OverlayVisible), true },
            { nameof(Settings.AutoSortEnabled), true },
            { nameof(Settings.ClickThroughEnabled), false },
            { nameof(Settings.AutoSortReverse), false },
            { nameof(Settings.EnabledPartyMemberPlaceholder), true },
            { nameof(Settings.IsAutoIgnoreLogs), false },
            { nameof(Settings.AutoCombatLogAnalyze), false },
            { nameof(Settings.EnabledSpellTimerNoDecimal), true },
            { nameof(Settings.EnabledNotifyNormalSpellTimer), false },
            { nameof(Settings.SaveLogEnabled), false },
            { nameof(Settings.SaveLogDirectory), string.Empty },
            { nameof(Settings.HideWhenNotActive), false },
            { nameof(Settings.ResetOnWipeOut), true },
            { nameof(Settings.WipeoutNotifyToACT), true },
            { nameof(Settings.RemoveTooltipSymbols), true },
            { nameof(Settings.RemoveWorldName), true },
            { nameof(Settings.SimpleRegex), true },
            { nameof(Settings.DetectPacketDump), false },
            { nameof(Settings.TextBlurRate), 1.2d },
            { nameof(Settings.TextOutlineThicknessRate), 1.0d },
            { nameof(Settings.PCNameInitialOnDisplayStyle), NameStyles.FullName },
            { nameof(Settings.RenderCPUOnly), true },
            { nameof(Settings.SingleTaskLogMatching), false },
            { nameof(Settings.DisableStartCondition), false },
            { nameof(Settings.EnableMultiLineMaching), false },
            { nameof(Settings.MaxFPS), 30 },
            { nameof(Settings.IsEnabledPolon), false },

            { nameof(Settings.LPSViewVisible), false },
            { nameof(Settings.LPSViewX), 0 },
            { nameof(Settings.LPSViewY), 0 },
            { nameof(Settings.LPSViewScale), 1.0 },

            { nameof(Settings.BarBackgroundFixed), false },
            { nameof(Settings.BarBackgroundBrightness), 0.3 },
            { nameof(Settings.BarDefaultBackgroundColor), System.Windows.Media.Color.FromArgb(240, 0, 0, 0) },

            // 設定画面のない設定項目
            { nameof(Settings.LastUpdateDateTime), DateTime.MinValue },
            { nameof(Settings.BlinkBrightnessDark), 0.3d },
            { nameof(Settings.BlinkBrightnessLight), 2.5d },
            { nameof(Settings.BlinkPitch), 0.5d },
            { nameof(Settings.BlinkPeekHold), 0.08d },
        };

        public void Reset()
        {
            lock (this.locker)
            {
                var pis = this.GetType().GetProperties();
                foreach (var pi in pis)
                {
                    try
                    {
                        var defaultValue =
                            DefaultValues.ContainsKey(pi.Name) ?
                            DefaultValues[pi.Name] :
                            null;

                        if (defaultValue != null)
                        {
                            pi.SetValue(this, defaultValue);
                        }
                    }
                    catch
                    {
                        Debug.WriteLine($"Settings Reset Error: {pi.Name}");
                    }
                }
            }
        }
    }
}
