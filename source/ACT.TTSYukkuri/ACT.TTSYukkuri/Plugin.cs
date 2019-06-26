using System;
using System.IO;
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
        #region Singleton

        private static Plugin instance;

        public static Plugin Instance => instance;

        #endregion Singleton

        public Plugin()
        {
            instance = this;

            // このDLLの配置場所とACT標準のPluginディレクトリも解決の対象にする
            AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
            {
                try
                {
                    var pluginDirectory = ActGlobals.oFormActMain.PluginGetSelfData(this)?.pluginFile.DirectoryName;

                    var architect = Environment.Is64BitProcess ? "x64" : "x86";
                    var directories = new string[]
                    {
                        pluginDirectory,
                        Path.Combine(pluginDirectory, "bin"),
                        Path.Combine(pluginDirectory, $@"{architect}"),
                        Path.Combine(pluginDirectory, $@"bin\{architect}"),
                        Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                            @"Advanced Combat Tracker\Plugins"),
                    };

                    var asm = new AssemblyName(e.Name);

                    foreach (var directory in directories)
                    {
                        if (!string.IsNullOrWhiteSpace(directory))
                        {
                            var dll = Path.Combine(directory, asm.Name + ".dll");
                            if (File.Exists(dll))
                            {
                                return Assembly.LoadFrom(dll);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ActGlobals.oFormActMain.WriteExceptionLog(
                        ex,
                        "ACT.TTSYukkuri Assemblyの解決で例外が発生しました");
                }

                return null;
            };
        }

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

        public void Speak(string textToSpeak) =>
            PluginCore.Instance.Speak(textToSpeak);

        #endregion ISpeak
    }
}
