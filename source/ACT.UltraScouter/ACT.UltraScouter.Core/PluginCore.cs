using System;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using ACT.UltraScouter.Common;
using ACT.UltraScouter.Config;
using ACT.UltraScouter.Config.UI.Views;
using ACT.UltraScouter.Models;
using ACT.UltraScouter.Models.FFLogs;
using ACT.UltraScouter.Workers;
using ACT.UltraScouter.Workers.TextCommands;
using Advanced_Combat_Tracker;
using FFXIV.Framework.Bridge;
using FFXIV.Framework.Common;
using FFXIV.Framework.Extensions;
using FFXIV.Framework.resources;
using FFXIV.Framework.WPF;
using FFXIV.Framework.WPF.Views;
using FFXIV.Framework.XIVHelper;
using NLog;

namespace ACT.UltraScouter
{
    public class PluginCore :
        IDisposable
    {
        #region Singleton

        private static PluginCore instance;

        public static PluginCore Instance => instance;

        public PluginCore()
        {
            instance = this;
        }

        public void Dispose()
        {
            instance = null;
        }

        #endregion Singleton

        #region Logger

        private Logger Logger => AppLog.DefaultLogger;

        #endregion Logger

        /// <summary>このプラグインの配置ディレクトリ</summary>
        public string PluginDirectory { get; set; }

        /// <summary>このプラグインの場所</summary>
        public string PluginLocation { get; set; }

        /// <summary>プラグインステータス表示用ラベル</summary>
        public Label PluginStatusLabel { get; private set; }

        /// <summary>プラグイン用タブページ表示用ラベル</summary>
        public TabPage PluginTabPage { get; private set; }

        public void EndPlugin()
        {
            if (!this.isLoaded)
            {
                return;
            }

            try
            {
                this.Logger.Trace("start DeInitPlugin");

                // 設定ファイルを保存する
                Settings.Instance.Save();
                FFXIV.Framework.Config.Save();
                FFXIV.Framework.Config.Free();

                EnvironmentHelper.GarbageLogs();

                // ターゲット情報ワーカを終了する
                MainWorker.Instance.End();

                // FFXIVプラグインへのアクセスを終了する
                XIVPluginHelper.Instance.End();
                XIVPluginHelper.Free();

                // 参照を開放する
                WavePlayer.Free();
                MainWorker.Free();
                Settings.Free();

                if (this.PluginStatusLabel != null)
                {
                    this.PluginStatusLabel.Text = "Plugin exited.";
                }

                this.Logger.Trace("end DeInitPlugin. succeeded.");
            }
            catch (Exception ex)
            {
                this.Logger.Fatal(ex, "DeInitPlugin error.");
                this.ShowMessage("DeInitPlugin error.", ex);
            }
            finally
            {
                AppLog.FlushAll();
            }
        }

        private bool isLoaded = false;

        public void StartPlugin(
            TabPage pluginScreenSpace,
            Label pluginStatusText)
        {
            this.isLoaded = false;

            // タイトルをセットする
            pluginScreenSpace.Text = "ULTRA SCOUTER";

            EnvironmentMigrater.Migrate();
            MasterFilePublisher.Publish();
            WPFHelper.Start();
            WPFHelper.BeginInvoke(async () =>
            {
                AppLog.LoadConfiguration(AppLog.HojoringConfig);
                this.Logger?.Trace(Assembly.GetExecutingAssembly().GetName().ToString() + " start.");

                try
                {
                    this.PluginTabPage = pluginScreenSpace;
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
                        this.EndPlugin();
                    });

                    this.Logger.Trace("[ULTRA SCOUTER] Start InitPlugin");

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

                    // 設定ファイルを読み込む
                    Settings.Instance.Load();
                    Settings.Instance.MPTicker.UpdateUnlockMPSync();

                    // 設定ファイルをバックアップする
                    await EnvironmentHelper.BackupFilesAsync(
                        Settings.Instance.FileName);

                    // 各種ファイルを読み込む
                    await Task.Run(() =>
                    {
                        TTSDictionary.Instance.Load();
                        Settings.Instance.MobList.LoadTargetMobList();
                    });

                    EnvironmentHelper.WaitInitActDone();

                    // FFXIVプラグインへのアクセスを開始する
                    await Task.Run(() =>
                    {
                        XIVPluginHelper.Instance.Start(
                            Settings.Instance.PollingRate,
                            Settings.Instance.FFXIVLocale);
                    });

                    // ターゲット情報ワーカを開始する
                    MainWorker.Instance.Start();

                    // タブページを登録する
                    this.SetupPluginTabPages(pluginScreenSpace);

                    // テキストコマンドの購読を追加する
                    this.SubscribeTextCommands();

                    if (this.PluginStatusLabel != null)
                    {
                        this.PluginStatusLabel.Text = "Plugin started.";
                    }

                    this.Logger.Trace("[ULTRA SCOUTER] End InitPlugin");

                    // 共通ビューを追加する
                    CommonViewHelper.Instance.AddCommonView(
                       pluginScreenSpace.Parent as TabControl);

                    this.isLoaded = true;

                    // FFLogsの統計データベースをロードする
                    StatisticsDatabase.Instance.Logger = Logger;
                    await StatisticsDatabase.Instance.LoadAsync();

                    // アップデートを確認する
                    await Task.Run(() => this.Update());
                }
                catch (Exception ex)
                {
                    this.Logger.Fatal(ex, "InitPlugin error.");
                    this.ShowMessage("InitPlugin error.", ex);
                }
            });
        }

        /// <summary>
        /// プラグインのタブページを設定する
        /// </summary>
        /// <param name="baseTabPage">
        /// 基本のタブページ</param>
        private void SetupPluginTabPages(
            TabPage baseTabPage)
        {
            baseTabPage.Controls.Clear();
            baseTabPage.Controls.Add(new ElementHost()
            {
                Child = new BaseView(),
                Dock = DockStyle.Fill,
            });
        }

        /// <summary>
        /// メッセージの表示
        /// </summary>
        /// <param name="message">メッセージ</param>
        /// <param name="ex">Exception</param>
        private void ShowMessage(
            string message,
            Exception ex = null)
        {
            var caption = "ULTRA SCOUTER";

            if (ex != null)
            {
                message += Environment.NewLine;
                message += Environment.NewLine;
                message += ex.ToString();
            }

            MessageBox.Show(
                ActGlobals.oFormActMain,
                message,
                caption,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        /// <summary>
        /// アップデートを行う
        /// </summary>
        private void Update()
        {
            if ((DateTime.Now - Settings.Instance.LastUpdateDateTime).TotalHours
                >= Settings.UpdateCheckInterval)
            {
                var message = UpdateChecker.Update(
                    "ACT.UltraScouter",
                    Assembly.GetExecutingAssembly());
                if (!string.IsNullOrWhiteSpace(message))
                {
                    this.Logger.Fatal(message);
                }

                Settings.Instance.LastUpdateDateTime = DateTime.Now;
                WPFHelper.Invoke(() => Settings.Instance.Save());
            }
        }

        #region TextCommands

        private const string ParseCommand = "/parse";

        private static readonly Regex ParseCommandRegex = new Regex(
            $@"{ParseCommand}(\s+""(?<characterName>.+)"")?(\s+(?<serverName>.+))?",
            RegexOptions.Compiled);

        private void SubscribeTextCommands()
        {
            // /parse コマンド
            TextCommandBridge.Instance.Subscribe(new TextCommand(
            (string logLine, out Match match) =>
            {
                match = null;

                if (!logLine.ContainsIgnoreCase(ParseCommand))
                {
                    return false;
                }

                match = ParseCommandRegex.Match(logLine);
                return match.Success;
            },
            (string logLine, Match match) =>
            {
                if (match == null ||
                    !match.Success)
                {
                    return;
                }

                var charName = match.Groups["characterName"].ToString();
                var serverName = match.Groups["serverName"].ToString();

                TargetInfoModel.GetFFLogsInfoFromTextCommand(
                    charName,
                    serverName);
            }));

            // MyUtilityの登録
            MyUtilityOnWipeoutCommand.Instance.Subscribe();
        }

        #endregion TextCommands
    }
}
