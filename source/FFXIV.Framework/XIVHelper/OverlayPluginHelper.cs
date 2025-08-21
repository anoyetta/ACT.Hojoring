using Advanced_Combat_Tracker;
using FFXIV.Framework.Common;
using NLog;
using RainbowMage.OverlayPlugin.MemoryProcessors.Combatant;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FFXIV.Framework.XIVHelper
{
    public class OverlayPluginHelper
    {
        #region Singleton
        private static OverlayPluginHelper instance;
        public static OverlayPluginHelper Instance =>
            instance ?? (instance = new OverlayPluginHelper());
        public static void Free() => instance = null;
        private OverlayPluginHelper()
        {
        }
        #endregion

        private static NLog.Logger AppLogger => AppLog.DefaultLogger;

        private dynamic plugin;

        public object targetMemory;
        public MethodInfo method_GetFocusCombatant;
        public MethodInfo method_GetHoverCombatant;

        public interface IVersionedMemory
        {
            Version GetVersion();
            void ScanPointers();
            bool IsValid();
        }

        public interface ITargetMemory : IVersionedMemory
        {
            RainbowMage.OverlayPlugin.MemoryProcessors.Combatant.Combatant GetTargetCombatant();

            RainbowMage.OverlayPlugin.MemoryProcessors.Combatant.Combatant GetFocusCombatant();

            RainbowMage.OverlayPlugin.MemoryProcessors.Combatant.Combatant GetHoverCombatant();
        }

        
        private ThreadWorker attachOverlayPluginWorker;
        private volatile bool isStarted = false;
        public async void Start()
        {
            lock (this)
            {
                if (this.isStarted)
                {
                    return;
                }

                this.isStarted = true;
            }
            // FFXIV.Framework.config を読み込ませる
            lock (Config.ConfigBlocker)
            {
                _ = Config.Instance;
            }
            this.attachOverlayPluginWorker = new ThreadWorker(() =>
            {
                if (!ActGlobals.oFormActMain.InitActDone)
                {
                    return;
                }

                this.Attach();

                if (this.plugin == null)
                {
                    return;
                }
            },
            5000,
            nameof(this.attachOverlayPluginWorker),
            ThreadPriority.Lowest);

            var tasksG1 = new System.Action[]
            {
                () => this.attachOverlayPluginWorker.Run(),
            };
            await Task.WhenAll(CommonHelper.InvokeTasks(tasksG1));

        }
        public void End()
        {
            lock (this)
            {
                if (!this.isStarted)
                {
                    return;
                }

                this.isStarted = false;
            }

            // sharlayan を止める
            SharlayanHelper.Instance.End();

            try
            {
                this.attachOverlayPluginWorker?.Abort();
            }
            catch (ThreadAbortException)
            {
            }
        }
        public bool IsAttached => this.plugin != null;

        // グローバル変数としてキャッシュするメンバー
        private object pluginLoaderInstance = null;
        private object container = null;
        public bool wasAttached = false;

        private void Attach()
        {
            if (this.wasAttached)
            {
                return;
            }

            if (ActGlobals.oFormActMain == null ||
                !ActGlobals.oFormActMain.InitActDone)
            {
                return;
            }
            if (this.plugin == null)
            {
                var OverlayPlugin = (
                    from x in ActGlobals.oFormActMain.ActPlugins
                    where
                    x.pluginFile.Name.ToUpper().Contains("OverlayPlugin".ToUpper())
                    select
                    x.pluginObj).FirstOrDefault();

                this.plugin = OverlayPlugin;
            }
            if (this.plugin != null)
            {
                var attach_success = false;
                try
                {
                    var overlayPluginActData = ActGlobals.oFormActMain.ActPlugins
                        .FirstOrDefault(p => p.pluginFile.Name.Equals("OverlayPlugin.dll", StringComparison.OrdinalIgnoreCase));

                    if (overlayPluginActData == null) return;

                    pluginLoaderInstance = overlayPluginActData.pluginObj;
                    if (pluginLoaderInstance == null) return;

                    var overlayPluginAssembly = System.Reflection.Assembly.Load("OverlayPlugin");
                    var overlayPluginCoreAssembly = System.Reflection.Assembly.Load("OverlayPlugin.Core");

                    var pluginLoaderType = overlayPluginAssembly.GetType("RainbowMage.OverlayPlugin.PluginLoader");
                    var containerProperty = pluginLoaderType.GetProperties()
                        .FirstOrDefault(p => p.Name == "Container");

                    if (containerProperty == null) return;

                    container = containerProperty.GetValue(pluginLoaderInstance);
                    if (container == null) return;

                    // ITargetMemoryの実装オブジェクトを取得して、メソッドの存在を確認する
                    var targetMemoryInterfaceType = overlayPluginCoreAssembly.GetType("RainbowMage.OverlayPlugin.MemoryProcessors.Target.ITargetMemory");
                    var resolveMethod = container.GetType().GetMethod("Resolve", new Type[] { typeof(Type) });
                    var targetMemoryInstance = resolveMethod.Invoke(container, new object[] { targetMemoryInterfaceType });

                    if (targetMemoryInstance == null) return;

                    var actualTargetMemoryType = targetMemoryInstance.GetType();
                    var getFocusCombatantMethod = actualTargetMemoryType.GetMethod("GetFocusCombatant");
                    var getHoverCombatantMethod = actualTargetMemoryType.GetMethod("GetHoverCombatant");

                    // 必要なメソッドが両方とも存在する場合のみ、アタッチ成功と判断
                    if (getFocusCombatantMethod != null && getHoverCombatantMethod != null)
                    {
                        attach_success = true;
                        AppLogger.Trace("OverlayPluginへのアタッチに成功しました。");
                    }
                    else
                    {
                        attach_success = false;
                        AppLogger.Error("必要なメソッドが見つからないため、アタッチに失敗しました。");
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Error($"エラーが発生しました: {ex.Message}");
                    wasAttached = false;
                }
                if (!wasAttached)
                {
                    if (attach_success)
                    {
                        wasAttached = true;
                        AppLogger.Trace("attached OverlayPlugin.");
                    }
                }

            }
        }

        /// <summary>
        /// フォーカスターゲットのIDを取得します。
        /// </summary>
        /// <returns>CombatantのID。取得できない場合は0。</returns>
        public uint GetFocusCombatantID()
        {
            return GetCombatantID("GetFocusCombatant");
        }

        /// <summary>
        /// マウスターゲットのIDを取得します。
        /// </summary>
        /// <returns>CombatantのID。取得できない場合は0。</returns>
        public uint GetHoverCombatantID()
        {
            return GetCombatantID("GetHoverCombatant");
        }

        private uint GetCombatantID(string methodName)
        {
            if (!wasAttached || container == null) return 0;

            try
            {
                var overlayPluginCoreAssembly = System.Reflection.Assembly.Load("OverlayPlugin.Core");
                var targetMemoryInterfaceType = overlayPluginCoreAssembly.GetType("RainbowMage.OverlayPlugin.MemoryProcessors.Target.ITargetMemory");
                var resolveMethod = container.GetType().GetMethod("Resolve", new Type[] { typeof(Type) });
                var targetMemoryInstance = resolveMethod.Invoke(container, new object[] { targetMemoryInterfaceType });

                if (targetMemoryInstance == null) return 0;

                var targetMemoryField = targetMemoryInstance.GetType()
                    .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .FirstOrDefault(f => f.FieldType.IsAssignableFrom(targetMemoryInterfaceType));

                var targetMemory = targetMemoryField?.GetValue(targetMemoryInstance);
                if (targetMemory == null) return 0;

                var getCombatantMethod = targetMemory.GetType().GetMethod(methodName);
                if (getCombatantMethod == null) return 0;

                var combatantInstance = getCombatantMethod.Invoke(targetMemory, null);
                if (combatantInstance == null) return 0;

                var idMember = (MemberInfo)combatantInstance.GetType().GetProperty("ID", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?? combatantInstance.GetType().GetField("ID", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (idMember == null) return 0;

                var idValue = idMember is PropertyInfo ? ((PropertyInfo)idMember).GetValue(combatantInstance) : ((FieldInfo)idMember).GetValue(combatantInstance);

                return (uint)idValue;
            }
            catch
            {
                return 0;
            }
        }
    }
}
