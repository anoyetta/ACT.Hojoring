using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ACT.UltraScouter.Config;
using FFXIV.Framework.Bridge;
using FFXIV.Framework.Common;
using FFXIV.Framework.XIVHelper;
using WindowsInput;

namespace ACT.UltraScouter.Workers.TextCommands
{
    public class MyUtilityOnWipeoutCommand
    {
        #region Lazy Singleton

        private static readonly Lazy<MyUtilityOnWipeoutCommand> LazyInstance
            = new Lazy<MyUtilityOnWipeoutCommand>(() => new MyUtilityOnWipeoutCommand());

        public static MyUtilityOnWipeoutCommand Instance => LazyInstance.Value;

        #endregion Lazy Singleton

        private readonly Lazy<InputSimulator> LazyInput = new Lazy<InputSimulator>(() => new InputSimulator());

        public MyUtility Config => Settings.Instance.MyUtility;

        public void Subscribe() =>
            TextCommandBridge.Instance.Subscribe(new TextCommand(this.CanExecute, this.Execute)
            {
                IsSilent = true
            });

        private bool CanExecute(
            string logLine,
            out Match match)
        {
            match = null;

            if (!this.Config.ExtendMealEffect.IsEnabled &&
                !this.Config.RestoreTankStance.IsEnabled &&
                !this.Config.SummonFairy.IsEnabled &&
                !this.Config.DrawCard.IsEnabled &&
                !this.Config.SummonEgi.IsEnabled)
            {
                return false;
            }

            this.StoreTankStance(logLine);

            return logLine.Contains("wipeout");
        }

        private IntPtr xivHandle;

        private void Execute(
            string logLine,
            Match match) => Task.Run(async () =>
        {
            // wipeoutの検出からのディレイ
            await Task.Delay(TimeSpan.FromSeconds(this.Config.DelayFromWipeout));

            var sendKeySetList = new List<KeyShortcut>();

            var player = CombatantsManager.Instance.Player;
            var playerEffects = XIVPluginHelper.Instance.GetEffects(player.ID);
            var partyCount = CombatantsManager.Instance.PartyCount;

            // タンクスタンスを復元する
            if (this.Config.RestoreTankStance.IsAvailable())
            {
                if (player.Role == Roles.Tank &&
                    this.inTankStance.HasValue)
                {
                    var inTankStanceNow = playerEffects.Any(x =>
                        x != null &&
                        TankStanceEffectIDs.Contains(x.BuffID));

                    if (this.inTankStance.Value != inTankStanceNow)
                    {
                        sendKeySetList.Add(this.Config.RestoreTankStance.KeySet);
                    }
                }
            }

            // フェアリーを召喚する
            if (this.Config.SummonFairy.IsAvailable())
            {
                if (player.JobID == JobIDs.SCH)
                {
                    sendKeySetList.Add(this.Config.SummonFairy.KeySet);
                }
            }

            // カードをドローする
            if (this.Config.DrawCard.IsAvailable())
            {
                if (player.JobID == JobIDs.AST)
                {
                    sendKeySetList.Add(this.Config.DrawCard.KeySet);
                }
            }

            // エギを召喚する
            if (this.Config.SummonEgi.IsAvailable())
            {
                if (player.JobID == JobIDs.SMN)
                {
                    sendKeySetList.Add(this.Config.SummonEgi.KeySet);
                }
            }

            // 食事効果を延長する
            if (this.Config.ExtendMealEffect.IsAvailable())
            {
                var remainOfWellFed = playerEffects.FirstOrDefault(x =>
                    x != null &&
                    x.BuffID == WellFedEffectID)?.Timer ?? 0;

                if (0 < remainOfWellFed && remainOfWellFed < (this.Config.ExtendMealEffect.RemainingTimeThreshold * 60))
                {
                    sendKeySetList.Add(this.Config.ExtendMealEffect.KeySet);
                }
            }

            // キーを送る
            if (sendKeySetList.Count > 0)
            {
                if (this.xivHandle == IntPtr.Zero)
                {
                    this.xivHandle = FindWindow(null, "FINAL FANTASY XIV");
                }

                if (this.xivHandle != IntPtr.Zero)
                {
                    SetForegroundWindow(xivHandle);
                }

                var isFirst = true;
                foreach (var keySet in sendKeySetList)
                {
                    if (!isFirst)
                    {
                        Thread.Sleep(TimeSpan.FromMilliseconds(3000));
                    }

                    var modifiers = keySet.GetModifiers();
                    var keys = keySet.GetKeys();
                    var sim = this.LazyInput.Value;

                    foreach (var key in modifiers)
                    {
                        sim.Keyboard.KeyDown(key);
                        Thread.Sleep(TimeSpan.FromMilliseconds(50));
                    }

                    for (int i = 0; i < 3; i++)
                    {
                        if (i > 0)
                        {
                            Thread.Sleep(TimeSpan.FromMilliseconds(100));
                        }

                        sim.Keyboard.KeyPress(keys);
                    }

                    foreach (var key in modifiers.Reverse())
                    {
                        Thread.Sleep(TimeSpan.FromMilliseconds(50));
                        sim.Keyboard.KeyUp(key);
                    }

                    isFirst = false;
                }
            }
        });

        private static readonly string[] EngageLogs = new string[]
        {
            "00:0039:戦闘開始",
            "00:0039:Engage!",
            "00:0039:전투 시작!",
            "00:0039:战斗开始！",
        };

        private static readonly uint[] TankStanceEffectIDs = new uint[]
        {
            91,     // ディフェンダー
            1833,   // ロイヤルガード
            79,     // アイアンウィル
            743,    // グリットスタンス
        };

        /// <summary>食事効果のエフェクトID</summary>
        private static readonly uint WellFedEffectID = 48;

        private bool? inTankStance;

        private void StoreTankStance(
            string logLine)
        {
            if (!this.Config.RestoreTankStance.IsEnabled)
            {
                return;
            }

            var player = CombatantsManager.Instance.Player;

            if (player.Role != Roles.Tank)
            {
                return;
            }

            if (!EngageLogs.Any(x => logLine.Contains(x)))
            {
                return;
            }

            var playerEffects = XIVPluginHelper.Instance.GetEffects(player.ID);

            this.inTankStance = playerEffects.Any(x =>
                x != null &&
                TankStanceEffectIDs.Contains(x.BuffID));
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
    }
}
