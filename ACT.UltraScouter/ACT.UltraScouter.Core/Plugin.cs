using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using Advanced_Combat_Tracker;

namespace ACT.TargetOverlay
{
    /// <summary>
    /// ACT Plugin
    /// </summary>
    public class Plugin :
        IActPluginV1
    {
        #region Singleton

        /// <summary>Instance</summary>
        private static Plugin instance;

        /// <summary>Instance</summary>
        public static Plugin Instance => instance;

        #endregion Singleton

        /// <summary>プラグイン用タブページ</summary>
        /// <summary>
        /// ACT標準のプラグインディレクトリ
        /// </summary>
        public readonly string ACTDefaultPluginDrectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            @"Advanced Combat Tracker\Plugins");

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public Plugin()
        {
            instance = this;

            this.SetAssemblyResolver();
        }

        public string PluginDirectory { get; private set; }

        /// <summary>このプラグインの場所</summary>
        public string PluginLocation { get; private set; }

        /// <summary>このプラグインのディレクトリ</summary>
        /// <summary>
        /// DeInitPlugin
        /// </summary>
        public void DeInitPlugin()
        {
        }

        /// <summary>
        /// Init Plugin
        /// </summary>
        /// <param name="pluginScreenSpace">
        /// プラグイン向けUI領域タブページ</param>
        /// <param name="pluginStatusText">
        /// プラグインステータス用ラベル</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void InitPlugin(
            TabPage pluginScreenSpace,
            Label pluginStatusText)
        {
        }

        /// <summary>
        /// プラグインの配置場所を取得する
        /// </summary>
        private void GetPluginLocation()
        {
            if (!string.IsNullOrEmpty(this.PluginLocation) ||
                !string.IsNullOrEmpty(this.PluginDirectory))
            {
                return;
            }

            var plugin = ActGlobals.oFormActMain.PluginGetSelfData(this);

            if (plugin != null)
            {
                this.PluginLocation = plugin.pluginFile.FullName;
                this.PluginDirectory = plugin.pluginFile.DirectoryName;
            }
        }

        /// <summary>
        /// AssemblyResolverを設定する
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void SetAssemblyResolver()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
            {
                this.GetPluginLocation();

                var asm = new AssemblyName(e.Name);

                var pathList = new string[]
                {
                    Path.Combine(this.PluginDirectory, asm.Name + ".dll"),
                    Path.Combine(this.ACTDefaultPluginDrectory, asm.Name + ".dll"),
                };

                foreach (var path in pathList)
                {
                    if (File.Exists(path))
                    {
                        return Assembly.LoadFrom(path);
                    }
                }

                return null;
            };
        }
    }
}