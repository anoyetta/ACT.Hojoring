using Advanced_Combat_Tracker;
using FFXIV.Framework.Common;
using FFXIV_ACT_Plugin.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        private object container = null;
        public bool wasAttached = false;
        private ThreadWorker attachOverlayPluginWorker;
        private volatile bool isStarted = false;

        private object enmityMemoryInstance = null;
        private object combatantMemoryInstance = null;

        public List<object> EnmityEntryList { get; } = new List<object>();

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
                try
                {
                    var overlayPluginActData = ActGlobals.oFormActMain.ActPlugins
                        .FirstOrDefault(p => p.pluginFile.Name.Equals("OverlayPlugin.dll", StringComparison.OrdinalIgnoreCase));

                    if (overlayPluginActData == null) return;

                    var pluginLoaderInstance = overlayPluginActData.pluginObj;
                    if (pluginLoaderInstance == null) return;

                    var overlayPluginAssembly = System.Reflection.Assembly.Load("OverlayPlugin");
                    var overlayPluginCoreAssembly = System.Reflection.Assembly.Load("OverlayPlugin.Core");

                    var pluginLoaderType = overlayPluginAssembly.GetType("RainbowMage.OverlayPlugin.PluginLoader");
                    var containerProperty = pluginLoaderType.GetProperties()
                        .FirstOrDefault(p => p.Name == "Container");

                    // Nullチェックを追加
                    if (containerProperty == null)
                    {
                        AppLogger.Error("OverlayPluginの'Container'プロパティが見つかりません。");
                        return;
                    }

                    container = containerProperty.GetValue(pluginLoaderInstance);
                    // Nullチェックを追加
                    if (container == null)
                    {
                        AppLogger.Error("OverlayPluginのContainerインスタンスが見つかりません。");
                        return;
                    }

                    var enmityMemoryInterfaceType = overlayPluginCoreAssembly.GetType("RainbowMage.OverlayPlugin.MemoryProcessors.Enmity.IEnmityMemory");
                    var combatantMemoryInterfaceType = overlayPluginCoreAssembly.GetType("RainbowMage.OverlayPlugin.MemoryProcessors.Combatant.ICombatantMemory");
                    var targetMemoryInterfaceType = overlayPluginCoreAssembly.GetType("RainbowMage.OverlayPlugin.MemoryProcessors.Target.ITargetMemory");

                    var resolveMethod = container.GetType().GetMethod("Resolve", new Type[] { typeof(Type) });
                    this.enmityMemoryInstance = resolveMethod.Invoke(container, new object[] { enmityMemoryInterfaceType });
                    this.combatantMemoryInstance = resolveMethod.Invoke(container, new object[] { combatantMemoryInterfaceType });
                    var targetMemoryInstance = resolveMethod.Invoke(container, new object[] { targetMemoryInterfaceType });

                    if (this.enmityMemoryInstance == null || this.combatantMemoryInstance == null || targetMemoryInstance == null)
                    {
                        AppLogger.Error("必要なメモリインスタンスが見つからないため、アタッチに失敗しました。");
                        return;
                    }

                    var actualTargetMemoryType = targetMemoryInstance.GetType();
                    var getFocusCombatantMethod = actualTargetMemoryType.GetMethod("GetFocusCombatant");
                    var getHoverCombatantMethod = actualTargetMemoryType.GetMethod("GetHoverCombatant");

                    if (this.enmityMemoryInstance == null) return;
                    var actualEnmityMemoryType = enmityMemoryInstance.GetType();
                    var getEnmityEntryListMethod = actualEnmityMemoryType.GetMethod("GetEnmityEntryList");

                    var actualCombatantMemoryType = combatantMemoryInstance.GetType();
                    var getCombatantListMethod = actualCombatantMemoryType.GetMethod("GetCombatantList");

                    if (getFocusCombatantMethod != null && getHoverCombatantMethod != null &&
                        getCombatantListMethod != null && getEnmityEntryListMethod != null)
                    {
                        wasAttached = true;
                        AppLogger.Trace("attached OverlayPlugin.");
                    }
                    else
                    {
                        AppLogger.Error("必要なメソッドが見つからないため、アタッチに失敗しました。");
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Error($"エラーが発生しました: {ex.Message}");
                    wasAttached = false;
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

        /// <summary>
        /// 敵視リストを取得し、クラスメンバーに格納します。
        /// </summary>
        /// <returns>取得したエントリ数。</returns>
        public uint GetEnmityEntryList()
        {
            if (!this.wasAttached)
            {
                return 0;
            }

            this.EnmityEntryList.Clear();
            uint count = 0;

            try
            {
                var combatantList = GetCombatantListInternal();
                if (combatantList == null)
                {
                    AppLogger.Error("GetCombatantListInternalからのCombatantリストがnullです。");
                    return 0;
                }

                var overlayPluginCoreAssembly = System.Reflection.Assembly.Load("OverlayPlugin.Core");
                var combatantType = overlayPluginCoreAssembly.GetType("RainbowMage.OverlayPlugin.MemoryProcessors.Combatant.Combatant");
                var listType = typeof(List<>).MakeGenericType(combatantType);

                var getEnmityEntryListMethod = this.enmityMemoryInstance.GetType().GetMethod("GetEnmityEntryList", new[] { listType });
                if (getEnmityEntryListMethod == null)
                {
                    AppLogger.Error("OverlayPluginのGetEnmityEntryListメソッドが見つかりません。");
                    return 0;
                }

                dynamic enmityList = getEnmityEntryListMethod.Invoke(this.enmityMemoryInstance, new object[] { combatantList });
                if (enmityList == null)
                {
                    //AppLogger.Error("GetEnmityEntryListの呼び出し結果がnullです。");
                    return 0;
                }

                // OverlayPlugin側のEnmityEntryからHojoring側のEnmityEntryに変換
                foreach (dynamic enmityEntry in enmityList)
                {
                    var convertedEntry = new FFXIV.Framework.XIVHelper.EnmityEntry
                    {
                        ID = enmityEntry.ID,
                        OwnerID = enmityEntry.OwnerID,
                        Name = enmityEntry.Name,
                        Enmity = enmityEntry.Enmity,
                        IsMe = enmityEntry.isMe,
                        HateRate = enmityEntry.HateRate,
                        Job = enmityEntry.Job,
                    };
                    this.EnmityEntryList.Add(convertedEntry);
                }

                count = (uint)this.EnmityEntryList.Count;
            }
            catch (Exception ex)
            {
                AppLogger.Error($"GetEnmityEntryListエラー: {ex.Message}");
            }

            return count;
        }

        private IEnumerable<object> GetCombatantListInternal()
        {
            if (!this.wasAttached) return null;

            try
            {
                var getCombatantListMethod = this.combatantMemoryInstance.GetType().GetMethod("GetCombatantList");
                if (getCombatantListMethod == null) return null;

                return getCombatantListMethod.Invoke(this.combatantMemoryInstance, null) as IEnumerable<object>;
            }
            catch (Exception ex)
            {
                AppLogger.Error($"GetCombatantListInternalエラー: {ex.Message}");
                return null;
            }
        }

    }
}
