using ACT.Hojoring.Shared;
using Advanced_Combat_Tracker;
using FFXIV.Framework.Common;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace ACT.SpecialSpellTimer
{
    /// <summary>
    /// SpecialSpellTimer Plugin
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

        /// <summary>
        /// 後片付けをする
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        void IActPluginV1.DeInitPlugin()
        {
            PluginCore.Instance?.DeInitPluginCore();
            PluginCore.Free();
        }

        /// <summary>
        /// 初期化する
        /// </summary>
        /// <param name="pluginScreenSpace">Pluginタブ</param>
        /// <param name="pluginStatusText">Pluginステータスラベル</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        void IActPluginV1.InitPlugin(
            TabPage pluginScreenSpace,
            Label pluginStatusText)
        {
            Assembly.Load("FFXIV.Framework");

            DirectoryHelper.GetPluginRootDirectoryDelegate = () => ActGlobals.oFormActMain?.PluginGetSelfData(this)?.pluginFile.DirectoryName;

            PluginCore.Initialize(this);
            PluginCore.Instance?.InitPluginCore(
                pluginScreenSpace,
                pluginStatusText);
        }
    }
}
