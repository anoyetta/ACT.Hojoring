using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using ACT.Hojoring;
using ACT.Hojoring.Shared;
using Advanced_Combat_Tracker;
using FFXIV.Framework.Common;

namespace ACT.UltraScouter
{
    /// <summary>
    /// ACT Plugin
    /// </summary>
    public class Plugin :
        IActPluginV1
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Plugin()
        {
            CosturaUtility.Initialize();
            AssemblyResolver.Initialize(() => ActGlobals.oFormActMain?.PluginGetSelfData(this)?.pluginFile.DirectoryName);
        }

        private PluginCore core;
        private string pluginDirectory;
        private string pluginLocation;

        /// <summary>
        /// DeInitPlugin
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        void IActPluginV1.DeInitPlugin()
        {
            this.core?.EndPlugin();
            this.core?.Dispose();
            this.core = null;
        }

        /// <summary>
        /// Init Plugin
        /// </summary>
        /// <param name="pluginScreenSpace">
        /// プラグイン向けUI領域タブページ</param>
        /// <param name="pluginStatusText">
        /// プラグインステータス用ラベル</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        void IActPluginV1.InitPlugin(
            TabPage pluginScreenSpace,
            Label pluginStatusText)
        {
            // 1. まず何よりも先にファイルを置き換える
            // このメソッド自体に FFXIV.Framework の型を書いてはいけない
            this.ApplyUpdate();

            // 2. ファイルの置換が終わってから、Framework を使う処理（別メソッド）を呼ぶ
            this.InitializePlugin(pluginScreenSpace, pluginStatusText);
        }

        /// <summary>
        /// アップデートを適用する
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ApplyUpdate()
        {
            AtomicUpdater.Apply();
        }

        /// <summary>
        /// Frameworkロード後の初期化
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void InitializePlugin(
            TabPage pluginScreenSpace,
            Label pluginStatusText)
        {
            // ここで初めて FFXIV.Framework.dll をロードする
            Assembly.Load("FFXIV.Framework");

            // FFXIV.Framework 内の静的プロパティにデリゲートをセット
            DirectoryHelper.GetPluginRootDirectoryDelegate = () => ActGlobals.oFormActMain?.PluginGetSelfData(this)?.pluginFile.DirectoryName;

            this.GetPluginLocation();

            this.core = new PluginCore();
            this.core.PluginDirectory = this.pluginDirectory;
            this.core.PluginLocation = this.pluginLocation;
            this.core.StartPlugin(
                pluginScreenSpace,
                pluginStatusText);
        }

        /// <summary>
        /// プラグインの配置場所を取得する
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void GetPluginLocation()
        {
            if (!string.IsNullOrEmpty(this.pluginLocation) ||
                !string.IsNullOrEmpty(this.pluginDirectory))
            {
                return;
            }

            var plugin = ActGlobals.oFormActMain.PluginGetSelfData(this);

            if (plugin != null)
            {
                this.pluginLocation = plugin.pluginFile.FullName;
                this.pluginDirectory = plugin.pluginFile.DirectoryName;
            }
        }
    }
}