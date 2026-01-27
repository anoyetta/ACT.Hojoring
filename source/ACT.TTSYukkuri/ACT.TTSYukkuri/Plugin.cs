using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using ACT.Hojoring;
using ACT.Hojoring.Shared;
using Advanced_Combat_Tracker;
using FFXIV.Framework.Common;

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