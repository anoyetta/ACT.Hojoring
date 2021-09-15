using ACT.Hojoring.Shared;
using Advanced_Combat_Tracker;
using FFXIV.Framework.Common;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace ACT.TTSYukkuri
{
    public class Plugin :
        IActPluginV1,
        ISpeak
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Plugin()
        {
            CosturaUtility.Initialize();
            AssemblyResolver.Initialize(() => ActGlobals.oFormActMain?.PluginGetSelfData(this)?.pluginFile.DirectoryName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void DeInitPlugin()
        {
            PluginCore.Instance?.DeInitPlugin();
            PluginCore.Free();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void InitPlugin(
            TabPage pluginScreenSpace,
            Label pluginStatusText)
        {
            Assembly.Load("FFXIV.Framework");

            DirectoryHelper.GetPluginRootDirectoryDelegate = () => ActGlobals.oFormActMain?.PluginGetSelfData(this)?.pluginFile.DirectoryName;

            PluginCore.Instance.InitPlugin(
                this,
                pluginScreenSpace,
                pluginStatusText);
        }

        #region ISpeak

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Speak(string textToSpeak) =>
            PluginCore.Instance.Speak(textToSpeak);

        #endregion ISpeak
    }
}
