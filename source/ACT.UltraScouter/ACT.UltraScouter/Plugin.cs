using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

using Advanced_Combat_Tracker;

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
            AssemblyResolver.Instance.Initialize(this);
        }

        private PluginCore core;
        private string pluginDirectory;
        private string pluginLocation;

        /// <summary>このプラグインのディレクトリ</summary>
        /// <summary>
        /// DeInitPlugin
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        void IActPluginV1.DeInitPlugin()
        {
            this.core?.EndPlugin();

            AssemblyResolver.Free();
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
            Assembly.Load("FFXIV.Framework");

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
