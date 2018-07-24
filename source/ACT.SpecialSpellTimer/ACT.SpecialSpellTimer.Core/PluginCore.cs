using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Threading;
using ACT.SpecialSpellTimer.Config;
using ACT.SpecialSpellTimer.Models;
using ACT.SpecialSpellTimer.RaidTimeline;
using ACT.SpecialSpellTimer.Sound;
using ACT.SpecialSpellTimer.Utility;
using ACT.SpecialSpellTimer.Views;
using Advanced_Combat_Tracker;
using FFXIV.Framework.Common;
using FFXIV.Framework.FFXIVHelper;
using FFXIV.Framework.WPF.Views;

namespace ACT.SpecialSpellTimer
{
    /// <summary>
    /// PluginCore
    /// </summary>
    public class PluginCore
    {
        #region Singleton

        private static PluginCore instance;

        public static PluginCore Instance => instance;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Initialize(
            IActPluginV1 plugin)
        {
            instance = new PluginCore();
            instance.PluginRoot = plugin;
        }

        public static void Free()
        {
            if (instance != null)
            {
                instance.PluginRoot = null;
                instance = null;
            }
        }

        #endregion Singleton

        #region Logger

        private NLog.Logger AppLogger => AppLog.DefaultLogger;

        #endregion Logger

        /// <summary>
        /// 自身の場所
        /// </summary>
        public string Location { get; private set; }

        public IActPluginV1 PluginRoot { get; private set; }

        /// <summary>
        /// プラグインステータス表示ラベル
        /// </summary>
        private Label PluginStatusLabel { get; set; }

        /// <summary>
        /// 表示切り替えボタン
        /// </summary>
        public CheckBox SwitchVisibleButton { get; set; }

        /// <summary>
        /// 後片付けをする
        /// </summary>
        public void DeInitPluginCore()
        {
            try
            {
                // 付加情報オーバーレイを閉じる
                LPSView.CloseLPS();
                POSView.ClosePOS();

                PluginMainWorker.Instance.End();
                PluginMainWorker.Free();
                TimelineController.Free();

                this.RemoveSwitchVisibleButton();
                this.PluginStatusLabel.Text = "Plugin Exited";

                // 設定ファイルを保存する
                TimelineSettings.Save();
                Settings.Default.Save();

                Logger.Write("Plugin Exited.");
            }
            catch (Exception ex)
            {
                ActGlobals.oFormActMain.WriteExceptionLog(
                    ex,
                    "Plugin deinit error.");

                Logger.Write("Plugin deinit error.", ex);

                if (this.PluginStatusLabel != null)
                {
                    this.PluginStatusLabel.Text = "Plugin Exit Error";
                }
            }

            Logger.DeInit();
        }

        /// <summary>
        /// 初期化する
        /// </summary>
        /// <param name="pluginScreenSpace">Pluginタブ</param>
        /// <param name="pluginStatusText">Pluginステータスラベル</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void InitPluginCore(
            TabPage pluginScreenSpace,
            Label pluginStatusText)
        {
            // タイトルをセットする
            pluginScreenSpace.Text = "SPESPE";

            WPFHelper.Start();
            WPFHelper.BeginInvoke(async () =>
            {
                // FFXIV_MemoryReaderを先にロードさせる
                await FFXIVReader.Instance.WaitForReaderToStartedAsync();

                this.PluginStatusLabel = pluginStatusText;

                AppLog.LoadConfiguration(AppLog.HojoringConfig);
                this.AppLogger.Trace(Assembly.GetExecutingAssembly().GetName().ToString() + " start.");

                try
                {
                    Logger.Init();
                    Logger.Write("[SPESPE] Start InitPlugin");

                    // .NET FrameworkとOSのバージョンを確認する
                    if (!UpdateChecker.IsAvailableDotNet() ||
                        !UpdateChecker.IsAvailableWindows())
                    {
                        return;
                    }

                    // メイン設定ファイルを読み込む
                    Settings.Default.Load();
                    Settings.Default.ApplyRenderMode();

                    // 最小化する？
                    if (Settings.Default.IsMinimizeOnStart)
                    {
                        ActGlobals.oFormActMain.WindowState = FormWindowState.Minimized;
                        Application.DoEvents();
                    }

                    // HojoringのSplashを表示する
                    UpdateChecker.ShowSplash();

                    // 自身の場所を格納しておく
                    var plugin = ActGlobals.oFormActMain.PluginGetSelfData(this.PluginRoot);
                    if (plugin != null)
                    {
                        this.Location = plugin.pluginFile.DirectoryName;
                    }

                    // 設定ファイルを読み込む
                    SpellPanelTable.Instance.Load();
                    SpellTable.Instance.Load();
                    TickerTable.Instance.Load();
                    TagTable.Instance.Load();

                    await Task.Run(() =>
                    {
                        SpellPanelTable.Instance.Backup();
                        SpellTable.Instance.Backup();
                        TickerTable.Instance.Backup();
                        TagTable.Instance.Backup();
                    });

                    TTSDictionary.Instance.Load();

                    // 設定Panelを追加する
                    var baseView = new BaseView(pluginScreenSpace.Font);
                    pluginScreenSpace.Controls.Add(new ElementHost()
                    {
                        Child = baseView,
                        Dock = DockStyle.Fill,
                        Font = pluginScreenSpace.Font,
                    });

                    // 本体を開始する
                    PluginMainWorker.Instance.Begin();
                    TimelineController.Init();

                    // 付加情報オーバーレイを表示する
                    LPSView.ShowLPS();
                    POSView.ShowPOS();

                    this.SetSwitchVisibleButton();
                    this.PluginStatusLabel.Text = "Plugin Started";

                    Logger.Write("[SPESPE] End InitPlugin");

                    // アップデートを確認する
                    await Task.Run(() => this.Update());
                }
                catch (Exception ex)
                {
                    Logger.Write("InitPlugin error.", ex);

                    if (this.PluginStatusLabel != null)
                    {
                        this.PluginStatusLabel.Text = "Plugin Initialize Error";
                    }

                    ModernMessageBox.ShowDialog(
                        "Plugin init error !",
                        "ACT.SpecialSpellTimer",
                        System.Windows.MessageBoxButton.OK,
                        ex);
                }
            });
        }

        #region SpeSpeButton

        /// <summary>
        /// 表示切り替えボタン（スペスペボタン）の状態を切り替える
        /// </summary>
        /// <param name="visible">
        /// 切り替える状態</param>
        public async void ChangeSwitchVisibleButton(
            bool visible)
        {
            await Task.Run(() =>
            {
                this.SwitchOverlay(visible);
            });

            await WPFHelper.InvokeAsync(() =>
            {
                this.ChangeButtonColor();
            },
            DispatcherPriority.Normal);
        }

        public void ChangeButtonColor()
        {
            var button = this.SwitchVisibleButton;

            if (Settings.Default.OverlayVisible)
            {
                button.BackColor = Color.SandyBrown;
                button.ForeColor = Color.WhiteSmoke;
            }
            else
            {
                button.BackColor = SystemColors.Control;
                button.ForeColor = Color.Black;
            }
        }

        /// <summary>
        /// 表示切り替えボタンを除去する
        /// </summary>
        private void RemoveSwitchVisibleButton()
        {
            if (this.SwitchVisibleButton != null)
            {
                ActGlobals.oFormActMain.Controls.Remove(this.SwitchVisibleButton);

                this.SwitchVisibleButton.Dispose();
                this.SwitchVisibleButton = null;
            }
        }

        private async void ReplaceButton()
        {
            if (this.SwitchVisibleButton != null &&
                !this.SwitchVisibleButton.IsDisposed &&
                this.SwitchVisibleButton.IsHandleCreated)
            {
                var leftButton = (
                    from Control x in ActGlobals.oFormActMain.Controls
                    where
                    !x.Equals(this.SwitchVisibleButton) &&
                    (
                        x is Button ||
                        x is CheckBox
                    )
                    orderby
                    x.Left
                    select
                    x).FirstOrDefault();

                var location = leftButton != null ?
                    new Point(leftButton.Left - this.SwitchVisibleButton.Width - 1, 0) :
                    new Point(ActGlobals.oFormActMain.Width - 533, 0);

                await WPFHelper.InvokeAsync(() =>
                {
                    if (this.SwitchVisibleButton != null)
                    {
                        this.SwitchVisibleButton.Location = location;
                    }
                });
            }
        }

        /// <summary>
        /// 表示切り替えボタンを配置する
        /// </summary>
        private void SetSwitchVisibleButton()
        {
            this.SwitchVisibleButton = new CheckBox()
            {
                Name = "SpecialSpellTimerSwitchVisibleButton",
                Text = "SPESPE",
                TextAlign = ContentAlignment.MiddleCenter,
                Appearance = Appearance.Button,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(ActGlobals.oFormActMain.Width - 533, 0),
                AutoSize = true,
            };

            this.SwitchVisibleButton.CheckedChanged += async (s, e) =>
            {
                await Task.Run(() =>
                {
                    this.SwitchOverlay(!Settings.Default.OverlayVisible);
                });

                this.ChangeButtonColor();
                Application.DoEvents();
            };

            this.ChangeButtonColor();

            ActGlobals.oFormActMain.Resize += (s, e) => this.ReplaceButton();
            ActGlobals.oFormActMain.Controls.Add(this.SwitchVisibleButton);
            ActGlobals.oFormActMain.Controls.SetChildIndex(this.SwitchVisibleButton, 1);

            Task.Run(async () =>
            {
                this.ReplaceButton();

                for (int i = 0; i < 10; i++)
                {
                    await Task.Delay(TimeSpan.FromSeconds(3));
                    this.ReplaceButton();
                }
            });
        }

        private void SwitchOverlay(
            bool visibility)
        {
            Settings.Default.OverlayVisible = visibility;
            Settings.Default.Save();

            SpellsController.Instance.ClosePanels();
            TickersController.Instance.CloseTelops();

            TableCompiler.Instance.RefreshPlayerPlacceholder();
            TableCompiler.Instance.RefreshPartyPlaceholders();
            TableCompiler.Instance.RefreshPetPlaceholder();
            TableCompiler.Instance.RecompileSpells();
            TableCompiler.Instance.RecompileTickers();
        }

        #endregion SpeSpeButton

        /// <summary>
        /// アップデートを行う
        /// </summary>
        private void Update()
        {
            if ((DateTime.Now - Settings.Default.LastUpdateDateTime).TotalHours
                >= Settings.UpdateCheckInterval)
            {
                var message = UpdateChecker.Update(
                    "ACT.SpecialSpellTimer",
                    Assembly.GetExecutingAssembly());
                if (!string.IsNullOrWhiteSpace(message))
                {
                    Logger.Write(message);
                }

                Settings.Default.LastUpdateDateTime = DateTime.Now;
                Settings.Default.Save();
            }
        }
    }
}
