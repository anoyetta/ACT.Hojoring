using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using Advanced_Combat_Tracker;

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
            AssemblyResolver.Instance.Initialize(this);
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
