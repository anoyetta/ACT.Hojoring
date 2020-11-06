using System;
using System.Drawing;
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
using FFXIV.Framework.resources;
using FFXIV.Framework.WPF;
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
        /// すべての設定を保存する
        /// </summary>
        public async void SaveSettingsAsync() => await WPFHelper.InvokeAsync(async () =>
        {
            SpellPanelTable.Instance.Save();
            SpellTable.Instance.Save();
            TickerTable.Instance.Save();
            TagTable.Instance.Save();
            TimelineSettings.Save();
            Settings.Default.Save();
            await Task.Delay(50);
        },
        DispatcherPriority.Background);

        /// <summary>
        /// 後片付けをする
        /// </summary>
        public void DeInitPluginCore()
        {
            if (!this.isLoaded)
            {
                return;
            }

            try
            {
                // 設定ファイルを保存する
                Settings.Default.Save();
                Settings.Default.DeInit();
                FFXIV.Framework.Config.Save();
                FFXIV.Framework.Config.Free();

                // 付加情報オーバーレイを閉じる
                LPSView.CloseLPS();
                POSView.ClosePOS();

                PluginMainWorker.Instance.End();
                PluginMainWorker.Free();
                TimelineController.Free();

                this.RemoveSwitchVisibleButton();

                if (this.PluginStatusLabel != null)
                {
                    this.PluginStatusLabel.Text = "Plugin Exited";
                }

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

        private bool isLoaded = false;

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

            EnvironmentMigrater.Migrate();
            MasterFilePublisher.Publish();
            WPFHelper.Start();
            WPFHelper.BeginInvoke(async () =>
            {
                AppLog.LoadConfiguration(AppLog.HojoringConfig);
                this.AppLogger?.Trace(Assembly.GetExecutingAssembly().GetName().ToString() + " start.");

                try
                {
                    this.PluginStatusLabel = pluginStatusText;

                    if (!EnvironmentHelper.IsValidPluginLoadOrder())
                    {
                        if (pluginStatusText != null)
                        {
                            pluginStatusText.Text = "Plugin Initialize Error";
                        }

                        return;
                    }

                    EnvironmentHelper.GarbageLogs();
                    EnvironmentHelper.StartActivator(() =>
                    {
                        BaseView.Instance.SetActivationStatus(false);
                        this.DeInitPluginCore();
                    });

                    Logger.Init();
                    Logger.Write("[SPESPE] Start InitPlugin");

                    // .NET FrameworkとOSのバージョンを確認する
                    if (!UpdateChecker.IsAvailableDotNet() ||
                        !UpdateChecker.IsAvailableWindows())
                    {
                        NotSupportedView.AddAndShow(pluginScreenSpace);
                        return;
                    }

                    // HojoringのSplashを表示する
                    UpdateChecker.ShowSplash();

                    // 外部リソースをダウンロードする
                    await ResourcesDownloader.Instance.DownloadAsync();

                    // メイン設定ファイルを読み込む
                    Settings.Default.Load();
                    Settings.Default.ApplyRenderMode();
                    Settings.Default.StartAutoSave();

                    // 最小化する？
                    if (Settings.Default.IsMinimizeOnStart)
                    {
                        ActGlobals.oFormActMain.WindowState = FormWindowState.Minimized;
                    }

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
                    _ = TimelineSettings.Instance;

                    // 設定ファイルをバックアップする
                    await EnvironmentHelper.BackupFilesAsync(
                        Settings.Default.FileName,
                        SpellPanelTable.Instance.DefaultFile,
                        SpellTable.Instance.DefaultFile,
                        TickerTable.Instance.DefaultFile,
                        TagTable.Instance.DefaultFile,
                        TimelineSettings.FileName,
                        FFXIV.Framework.Config.FileName);

                    TTSDictionary.Instance.Load();

                    // 設定Panelを追加する
                    var baseView = new BaseView(pluginScreenSpace.Font);
                    pluginScreenSpace.Controls.Add(new ElementHost()
                    {
                        Child = baseView,
                        Dock = DockStyle.Fill,
                        Font = pluginScreenSpace.Font,
                    });

                    EnvironmentHelper.WaitInitActDone();

                    // 本体を開始する
                    PluginMainWorker.Instance.Begin();
                    TimelineController.Init();

                    // 付加情報オーバーレイを表示する
                    LPSView.ShowLPS();
                    POSView.ShowPOS();

                    this.SetSwitchVisibleButton();

                    if (this.PluginStatusLabel != null)
                    {
                        this.PluginStatusLabel.Text = "Plugin Started";
                    }

                    Logger.Write("[SPESPE] End InitPlugin");

                    // 共通ビューを追加する
                    CommonViewHelper.Instance.AddCommonView(
                       pluginScreenSpace.Parent as TabControl);

                    this.isLoaded = true;

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
                ActGlobals.oFormActMain.CornerControlRemove(this.SwitchVisibleButton);

                this.SwitchVisibleButton.Dispose();
                this.SwitchVisibleButton = null;
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

            ActGlobals.oFormActMain.CornerControlAdd(this.SwitchVisibleButton);
            this.ChangeButtonColor();
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
