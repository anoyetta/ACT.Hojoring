using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using RazorEngine.Templating;

namespace ACT.SpecialSpellTimer.RaidTimeline
{
    /// <summary>
    /// RazorEngine を分離された AppDomain で実行するためのプロバイダーです。
    /// </summary>
    public class RazorEngineProvider : IDisposable
    {
        private AppDomain ad;
        private IsolatedRazorEngineService service;

        public RazorEngineProvider()
        {
            this.Initialize();
        }

        private void Initialize()
        {
            lock (this)
            {
                if (this.ad == null)
                {
                    var commonAssembly = Assembly.GetAssembly(typeof(ACT.Hojoring.Common.Hojoring));
                    var pluginDir = Path.GetDirectoryName(commonAssembly.Location);

                    var setup = new AppDomainSetup()
                    {
                        ApplicationBase = pluginDir,
                        ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile,
                        LoaderOptimization = LoaderOptimization.MultiDomainHost,
                        PrivateBinPath = "bin"
                    };

                    this.ad = AppDomain.CreateDomain(
                        "RazorEngineIsolationDomain_" + Guid.NewGuid().ToString("N"),
                        null,
                        setup);

                    this.service = (IsolatedRazorEngineService)this.ad.CreateInstanceAndUnwrap(
                        typeof(IsolatedRazorEngineService).Assembly.FullName,
                        typeof(IsolatedRazorEngineService).FullName);
                }
            }
        }

        public string RunCompile(string key, string file, object model)
        {
            if (this.service == null)
            {
                this.Initialize();
            }

            try
            {
                // AppDomainの境界を越える際、modelおよびその子要素すべてが[Serializable]である必要があります。
                return this.service.RunCompile(key, file, model);
            }
            catch (SerializationException ex)
            {
                // シリアル化エラーが発生した場合、XMLパースエラーにならないようコメント形式でエラーを返します。
                // メインドメインへのフォールバックは行いません。
                return $"<!-- RazorEngine Serialization Error: {ex.Message} -->" +
                       Environment.NewLine +
                       "<!-- Please ensure all models (TimelineRazorModel, TimelineTables, etc.) are marked as [Serializable]. -->";
            }
            catch (Exception ex)
            {
                return $"<!-- RazorEngine Unexpected Error: {ex.Message} -->";
            }
        }

        public void Dispose()
        {
            lock (this)
            {
                if (this.ad != null)
                {
                    this.service = null;
                    AppDomain.Unload(this.ad);
                    this.ad = null;
                }
            }
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// 別ドメインで実行される実体
    /// </summary>
    public class IsolatedRazorEngineService : MarshalByRefObject
    {
        public string RunCompile(string key, string file, object model)
        {
            if (!File.Exists(file))
            {
                return string.Empty;
            }

            var template = File.ReadAllText(file);

            // RazorEngineの実行
            return RazorEngine.Engine.Razor.RunCompile(
                template,
                key,
                null,
                model);
        }

        public override object InitializeLifetimeService() => null;
    }
}