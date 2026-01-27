using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using ACT.Hojoring;
using ACT.Hojoring.Shared;
using Advanced_Combat_Tracker;
using FFXIV.Framework.Common;

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
        /// <remarks>
        /// このメソッド内に FFXIV.Framework.dll の型を直接記述してはいけません。
        /// </remarks>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ApplyUpdate()
        {
            // AtomicUpdater 自体が FFXIV.Framework に含まれている場合は、
            // リフレクション経由で呼ぶか、AtomicUpdater だけを別DLLにする必要があります。
            // ここでは AtomicUpdater が ACT.Hojoring.Shared 等の
            // 既にロード済みの別DLLにあることを前提としています。
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
            // ここで初めて FFXIV.Framework.dll に触れる
            Assembly.Load("FFXIV.Framework");

            DirectoryHelper.GetPluginRootDirectoryDelegate = () => ActGlobals.oFormActMain?.PluginGetSelfData(this)?.pluginFile.DirectoryName;

            PluginCore.Initialize(this);
            PluginCore.Instance?.InitPluginCore(
                pluginScreenSpace,
                pluginStatusText);
        }
    }
}